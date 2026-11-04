namespace Elsasa.Sis.Finance.Features.Payments.RejectPayment;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler for cashier payment submission rejection (SCR-FIN-02, TD-04).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class RejectPaymentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser? currentUser = null,
    ILogger<RejectPaymentHandler>? logger = null)
{
    static RejectPaymentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectPaymentForLockSql = """
        SELECT
            id AS Id,
            payment_number AS PaymentNumber,
            confirmation_status AS ConfirmationStatus
        FROM finance.payments WITH (UPDLOCK, HOLDLOCK)
        WHERE id = @Id;
        """;

    private const string _updatePaymentRejectedSql = """
        UPDATE finance.payments
        SET confirmation_status = 'rejected',
            rejection_reason = @RejectionReason,
            notes = CASE WHEN @Notes IS NOT NULL THEN @Notes ELSE notes END,
            updated_at = @Now,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record PaymentLockRecord(Guid Id, string PaymentNumber, string ConfirmationStatus);

    public async Task<Result<RejectPaymentResponse>> HandleAsync(
        Guid paymentId,
        RejectPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (paymentId == Guid.Empty)
        {
            return Result.Failure<RejectPaymentResponse>(
                DomainError.Validation("Finance.Payment.InvalidId", "Payment ID cannot be empty."));
        }

        var auditUser = currentUser?.Username ?? "CASHIER_VERIFIER";
        var now = DateTimeOffset.UtcNow;
        var cleanReason = request.RejectionReason.Trim();
        var cleanNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

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
            return Result.Failure<RejectPaymentResponse>(
                DomainError.NotFound("Finance.Payment.NotFound", $"Payment with ID '{paymentId}' was not found."));
        }

        if (payment.ConfirmationStatus.Equals("rejected", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RejectPaymentResponse>(
                DomainError.Conflict(
                    "Finance.Payment.AlreadyRejected",
                    $"Payment '{payment.PaymentNumber}' is already rejected."));
        }

        if (payment.ConfirmationStatus.Equals("confirmed", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RejectPaymentResponse>(
                DomainError.Conflict(
                    "Finance.Payment.CannotRejectConfirmed",
                    $"Cannot reject confirmed payment '{payment.PaymentNumber}'. Confirmed payments must be reversed or reallocated."));
        }

        if (!payment.ConfirmationStatus.Equals("submitted", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RejectPaymentResponse>(
                DomainError.Conflict(
                    "Finance.Payment.InvalidStatusForRejection",
                    $"Cannot reject payment '{payment.PaymentNumber}' with status '{payment.ConfirmationStatus}'. Only 'submitted' payments can be rejected."));
        }

        // 2. Update status to rejected
        await connection.ExecuteAsync(new CommandDefinition(
            _updatePaymentRejectedSql,
            new
            {
                Id = paymentId,
                RejectionReason = cleanReason,
                Notes = cleanNotes,
                Now = now,
                UpdatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        logger?.LogInformation(
            "Rejected payment {PaymentNumber} ({PaymentId}) by {UpdatedBy}. Reason: {RejectionReason}.",
            payment.PaymentNumber,
            paymentId,
            auditUser,
            cleanReason);

        var response = new RejectPaymentResponse(
            paymentId,
            payment.PaymentNumber,
            "rejected",
            cleanReason,
            now,
            auditUser);

        return Result.Success(response);
    }
}
