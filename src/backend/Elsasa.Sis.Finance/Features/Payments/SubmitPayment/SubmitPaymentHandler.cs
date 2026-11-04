namespace Elsasa.Sis.Finance.Features.Payments.SubmitPayment;

using Dapper;
using Elsasa.Sis.Finance.Features.Accounts.Services;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler for self-service payment intake (SCR-SELF-02, UC-FIN-01, TD-04, OQ-004).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class SubmitPaymentHandler(
    ISqlConnectionFactory connectionFactory,
    IPayerResolver payerResolver,
    IStudentAccountService studentAccountService,
    ICurrentUser? currentUser = null,
    ILogger<SubmitPaymentHandler>? logger = null)
{
    static SubmitPaymentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _insertPaymentSql = """
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
            'submitted',
            NULL,
            NULL,
            NULL,
            NULL,
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

    public async Task<Result<SubmitPaymentResponse>> HandleAsync(
        SubmitPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditUser = currentUser?.Username ?? "PORTAL_PAYER";
        var paymentDate = request.PaymentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 1. Resolve or verify payer identity
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
                return Result.Failure<SubmitPaymentResponse>(
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
                return Result.Failure<SubmitPaymentResponse>(
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

        // 3. Generate sequential payment number
        var paymentId = Guid.NewGuid();
        var paymentNumber = await PaymentNumberGenerator.GenerateAsync(connection, transaction, cancellationToken);

        // 4. Insert submitted payment record
        await connection.ExecuteAsync(new CommandDefinition(
            _insertPaymentSql,
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
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedBy = auditUser
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        logger?.LogInformation(
            "Submitted payment {PaymentNumber} ({PaymentId}) for candidate StudentId={StudentId}, ApplicantId={ApplicantId}, Amount={Amount}.",
            paymentNumber,
            paymentId,
            studentId,
            applicantId,
            request.Amount);

        var response = new SubmitPaymentResponse(
            paymentId,
            paymentNumber,
            "submitted",
            request.Amount,
            request.PaymentMethod.Trim().ToLowerInvariant(),
            paymentDate,
            now,
            payerId,
            accountId,
            studentId,
            applicantId);

        return Result.Success(response);
    }
}
