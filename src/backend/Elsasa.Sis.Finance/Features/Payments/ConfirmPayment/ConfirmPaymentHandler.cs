namespace Elsasa.Sis.Finance.Features.Payments.ConfirmPayment;

using Dapper;
using Elsasa.Sis.Contracts.Finance.Events;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler for confirming a submitted payment in the cashier verification queue (SCR-FIN-02, UC-FIN-04, TD-04, OQ-004).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class ConfirmPaymentHandler(
    ISqlConnectionFactory connectionFactory,
    IPublisher publisher,
    ICurrentUser? currentUser = null,
    ILogger<ConfirmPaymentHandler>? logger = null)
{
    static ConfirmPaymentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectPaymentForLockSql = """
        SELECT
            id AS Id,
            payment_number AS PaymentNumber,
            student_id AS StudentId,
            applicant_id AS ApplicantId,
            payment_amount AS PaymentAmount,
            confirmation_status AS ConfirmationStatus,
            receipt_number AS ReceiptNumber
        FROM finance.payments WITH (UPDLOCK, HOLDLOCK)
        WHERE id = @Id;
        """;

    private const string _updatePaymentConfirmedSql = """
        UPDATE finance.payments
        SET confirmation_status = 'confirmed',
            receipt_number = @ReceiptNumber,
            receipt_issued_at = @Now,
            confirmation_date = @Now,
            confirmed_by = @ConfirmedBy,
            updated_at = @Now,
            updated_by = @ConfirmedBy
        WHERE id = @Id;
        """;

    private sealed record PaymentLockRecord(
        Guid Id,
        string PaymentNumber,
        Guid? StudentId,
        Guid? ApplicantId,
        long PaymentAmount,
        string ConfirmationStatus,
        string? ReceiptNumber);

    public async Task<Result<ConfirmPaymentResponse>> HandleAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        if (paymentId == Guid.Empty)
        {
            return Result.Failure<ConfirmPaymentResponse>(
                DomainError.Validation("Finance.Payment.InvalidId", "Payment ID cannot be empty."));
        }

        var auditUser = currentUser?.Username ?? "CASHIER_VERIFIER";
        var now = DateTimeOffset.UtcNow;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 1. Acquire pessimistic lock on target payment
        var payment = await connection.QueryFirstOrDefaultAsync<PaymentLockRecord>(new CommandDefinition(
            _selectPaymentForLockSql,
            new { Id = paymentId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (payment is null)
        {
            return Result.Failure<ConfirmPaymentResponse>(
                DomainError.NotFound("Finance.Payment.NotFound", $"Payment with ID '{paymentId}' was not found."));
        }

        if (payment.ConfirmationStatus.Equals("confirmed", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ConfirmPaymentResponse>(
                DomainError.Conflict(
                    "Finance.Payment.AlreadyConfirmed",
                    $"Payment '{payment.PaymentNumber}' is already confirmed with receipt number '{payment.ReceiptNumber}'."));
        }

        if (!payment.ConfirmationStatus.Equals("submitted", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ConfirmPaymentResponse>(
                DomainError.Conflict(
                    "Finance.Payment.InvalidStatusForConfirmation",
                    $"Cannot confirm payment '{payment.PaymentNumber}' with status '{payment.ConfirmationStatus}'. Only 'submitted' payments can be confirmed."));
        }

        // 2. Generate sequential receipt voucher number inside transaction
        var receiptNumber = await ReceiptNumberGenerator.GenerateAsync(connection, transaction, cancellationToken);

        // 3. Update payment to confirmed
        await connection.ExecuteAsync(new CommandDefinition(
            _updatePaymentConfirmedSql,
            new
            {
                Id = paymentId,
                ReceiptNumber = receiptNumber,
                Now = now,
                ConfirmedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        logger?.LogInformation(
            "Confirmed payment {PaymentNumber} ({PaymentId}) by {ConfirmedBy} with receipt {ReceiptNumber}.",
            payment.PaymentNumber,
            paymentId,
            auditUser,
            receiptNumber);

        // 4. Post-commit: publish domain event (ADR-003)
        await publisher.Publish(
            new PaymentConfirmedEvent(paymentId, payment.StudentId, payment.ApplicantId, payment.PaymentAmount, receiptNumber, now),
            cancellationToken);

        var response = new ConfirmPaymentResponse(
            paymentId,
            payment.PaymentNumber,
            receiptNumber,
            "confirmed",
            payment.PaymentAmount,
            now,
            auditUser,
            payment.StudentId,
            payment.ApplicantId);

        return Result.Success(response);
    }
}
