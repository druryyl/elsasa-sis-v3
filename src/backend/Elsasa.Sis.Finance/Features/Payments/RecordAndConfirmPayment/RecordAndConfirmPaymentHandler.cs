namespace Elsasa.Sis.Finance.Features.Payments.RecordAndConfirmPayment;

using Dapper;
using Elsasa.Sis.Contracts.Finance.Events;
using Elsasa.Sis.Finance.Features.Accounts.Services;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler for cashier counter payment recording and atomic sequential receipt issuance (SCR-FIN-02, UC-FIN-04, TD-04, OQ-004).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class RecordAndConfirmPaymentHandler(
    ISqlConnectionFactory connectionFactory,
    IPayerResolver payerResolver,
    IStudentAccountService studentAccountService,
    IPublisher publisher,
    ICurrentUser? currentUser = null,
    ILogger<RecordAndConfirmPaymentHandler>? logger = null)
{
    static RecordAndConfirmPaymentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _insertConfirmedPaymentSql = """
        INSERT INTO finance.payments (
            id,
            payment_number,
            student_account_id,
            student_id,
            applicant_id,
            payer_id,
            payment_amount,
            currency_code,
            payment_method,
            external_reference,
            payment_date,
            submission_date,
            confirmation_status,
            confirmation_date,
            confirmed_by,
            receipt_number,
            receipt_issued_at,
            rejection_reason,
            notes,
            created_at,
            created_by
        ) VALUES (
            @Id,
            @PaymentNumber,
            @StudentAccountId,
            @StudentId,
            @ApplicantId,
            @PayerId,
            @PaymentAmount,
            'IDR',
            @PaymentMethod,
            @ExternalReference,
            @PaymentDate,
            @SubmissionDate,
            'confirmed',
            @ConfirmationDate,
            @ConfirmedBy,
            @ReceiptNumber,
            @ReceiptIssuedAt,
            NULL,
            @Notes,
            @SubmissionDate,
            @CreatedBy
        );
        """;

    private const string _selectAccountByIdSql = """
        SELECT id, account_number AS AccountNumber, student_id AS StudentId, applicant_id AS ApplicantId
        FROM finance.student_financial_accounts
        WHERE id = @Id;
        """;

    private const string _checkPayerExistsSql = """
        SELECT COUNT(1)
        FROM finance.payers
        WHERE id = @PayerId;
        """;

    public async Task<Result<RecordAndConfirmPaymentResponse>> HandleAsync(
        RecordAndConfirmPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditUser = currentUser?.Username ?? "CASHIER_COUNTER";
        var paymentDate = request.PaymentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 1. Resolve or verify payer
        Guid payerId;
        if (request.PayerId.HasValue && request.PayerId.Value != Guid.Empty)
        {
            var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _checkPayerExistsSql,
                new { PayerId = request.PayerId.Value },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (exists == 0)
            {
                return Result.Failure<RecordAndConfirmPaymentResponse>(
                    DomainError.NotFound("Finance.Payment.PayerNotFound", $"Payer with ID '{request.PayerId.Value}' was not found."));
            }

            payerId = request.PayerId.Value;
        }
        else
        {
            payerId = await payerResolver.ResolveOrCreatePayerAsync(
                request.PayerName,
                request.PayerIdentificationNumber,
                request.PayerEmail,
                request.PayerPhone,
                request.PayerType ?? "parent_guardian",
                cancellationToken);
        }

        // 2. Resolve or provision candidate account
        Guid accountId;
        Guid? studentId = request.StudentId == Guid.Empty ? null : request.StudentId;
        Guid? applicantId = request.ApplicantId == Guid.Empty ? null : request.ApplicantId;

        if (request.StudentAccountId.HasValue && request.StudentAccountId.Value != Guid.Empty)
        {
            var existingAccount = await connection.QueryFirstOrDefaultAsync<dynamic>(new CommandDefinition(
                _selectAccountByIdSql,
                new { Id = request.StudentAccountId.Value },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (existingAccount is null)
            {
                return Result.Failure<RecordAndConfirmPaymentResponse>(
                    DomainError.NotFound("Finance.Payment.AccountNotFound", $"Student financial account '{request.StudentAccountId.Value}' was not found."));
            }

            accountId = existingAccount.id;
            studentId ??= (Guid?)existingAccount.StudentId;
            applicantId ??= (Guid?)existingAccount.ApplicantId;
        }
        else
        {
            var accountSummary = await studentAccountService.GetOrCreateCandidateAccountAsync(
                studentId,
                applicantId,
                connection,
                transaction,
                createdBy: auditUser,
                cancellationToken: cancellationToken);

            accountId = accountSummary.Id;
            studentId ??= accountSummary.StudentId;
            applicantId ??= accountSummary.ApplicantId;
        }

        // 3. Atomically generate payment number and sequential receipt voucher number
        var paymentId = Guid.NewGuid();
        var paymentNumber = await PaymentNumberGenerator.GenerateAsync(connection, transaction, cancellationToken);
        var receiptNumber = await ReceiptNumberGenerator.GenerateAsync(connection, transaction, cancellationToken);

        // 4. Insert confirmed payment
        await connection.ExecuteAsync(new CommandDefinition(
            _insertConfirmedPaymentSql,
            new
            {
                Id = paymentId,
                PaymentNumber = paymentNumber,
                StudentAccountId = accountId,
                StudentId = studentId,
                ApplicantId = applicantId,
                PayerId = payerId,
                PaymentAmount = request.Amount,
                PaymentMethod = request.PaymentMethod.Trim().ToLowerInvariant(),
                ExternalReference = string.IsNullOrWhiteSpace(request.ExternalReference) ? null : request.ExternalReference.Trim(),
                PaymentDate = paymentDate,
                SubmissionDate = now,
                ConfirmationDate = now,
                ConfirmedBy = auditUser,
                ReceiptNumber = receiptNumber,
                ReceiptIssuedAt = now,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        logger?.LogInformation(
            "Cashier {Cashier} recorded and confirmed payment {PaymentNumber} ({PaymentId}) with receipt {ReceiptNumber} for amount {Amount} IDR.",
            auditUser,
            paymentNumber,
            paymentId,
            receiptNumber,
            request.Amount);

        // 5. Post-commit: publish PaymentConfirmedEvent (ADR-003)
        await publisher.Publish(
            new PaymentConfirmedEvent(paymentId, studentId, applicantId, request.Amount, receiptNumber, now),
            cancellationToken);

        var response = new RecordAndConfirmPaymentResponse(
            paymentId,
            paymentNumber,
            receiptNumber,
            "confirmed",
            request.Amount,
            request.PaymentMethod.Trim().ToLowerInvariant(),
            paymentDate,
            now,
            auditUser,
            payerId,
            accountId,
            studentId,
            applicantId);

        return Result.Success(response);
    }
}
