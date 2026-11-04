namespace Elsasa.Sis.Finance.Features.Payers.GetPayerById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying detailed payer attributes and associated transaction history (SCR-FIN-02, TD-03).
/// </summary>
public sealed class GetPayerByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetPayerByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectPayerSql = """
        SELECT
            p.id AS Id,
            p.payer_number AS PayerNumber,
            p.payer_name AS PayerName,
            p.payer_type AS PayerType,
            p.identification_number AS IdentificationNumber,
            p.email AS Email,
            p.phone_number AS PhoneNumber,
            p.billing_address AS BillingAddress,
            p.bank_account_name AS BankAccountName,
            p.bank_account_number AS BankAccountNumber,
            p.bank_name AS BankName,
            p.notes AS Notes,
            p.created_at AS CreatedAt,
            p.created_by AS CreatedBy,
            p.updated_at AS UpdatedAt,
            p.updated_by AS UpdatedBy
        FROM finance.payers p
        WHERE p.id = @Id;
        """;

    private const string _selectPaymentsSql = """
        SELECT
            pm.id AS Id,
            pm.payment_number AS PaymentNumber,
            pm.student_account_id AS StudentAccountId,
            pm.student_id AS StudentId,
            pm.applicant_id AS ApplicantId,
            pm.payment_amount AS PaymentAmount,
            pm.currency_code AS CurrencyCode,
            pm.payment_method AS PaymentMethod,
            pm.external_reference AS ExternalReference,
            pm.payment_date AS PaymentDate,
            pm.confirmation_status AS ConfirmationStatus,
            pm.receipt_number AS ReceiptNumber,
            pm.submission_date AS SubmissionDate
        FROM finance.payments pm
        WHERE pm.payer_id = @PayerId
        ORDER BY pm.payment_date DESC, pm.submission_date DESC;
        """;

    public async Task<Result<GetPayerByIdResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<GetPayerByIdResponse>(DomainError.Validation(
                "Finance.Payer.InvalidId",
                "Payer ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var payer = await connection.QuerySingleOrDefaultAsync<PayerDetailRow>(new CommandDefinition(
            _selectPayerSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (payer is null)
        {
            return Result.Failure<GetPayerByIdResponse>(DomainError.NotFound(
                "Finance.Payer.NotFound",
                $"Payer with ID '{id}' was not found."));
        }

        var payments = (await connection.QueryAsync<PayerPaymentSummaryDto>(new CommandDefinition(
            _selectPaymentsSql,
            new { PayerId = id },
            cancellationToken: cancellationToken))).ToList();

        var totalPaymentsCount = payments.Count;
        var totalPaidAmount = payments
            .Where(p => string.Equals(p.ConfirmationStatus, "confirmed", StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.PaymentAmount);

        var response = new GetPayerByIdResponse(
            payer.Id,
            payer.PayerNumber,
            payer.PayerName,
            payer.PayerType,
            payer.IdentificationNumber,
            payer.Email,
            payer.PhoneNumber,
            payer.BillingAddress,
            payer.BankAccountName,
            payer.BankAccountNumber,
            payer.BankName,
            payer.Notes,
            payer.CreatedAt,
            payer.CreatedBy,
            payer.UpdatedAt,
            payer.UpdatedBy,
            totalPaymentsCount,
            totalPaidAmount,
            payments);

        return Result.Success(response);
    }

    private sealed record PayerDetailRow(
        Guid Id,
        string PayerNumber,
        string PayerName,
        string PayerType,
        string? IdentificationNumber,
        string? Email,
        string? PhoneNumber,
        string? BillingAddress,
        string? BankAccountName,
        string? BankAccountNumber,
        string? BankName,
        string? Notes,
        DateTimeOffset CreatedAt,
        string? CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy);
}
