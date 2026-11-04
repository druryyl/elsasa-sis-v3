namespace Elsasa.Sis.Finance.Features.Payments.GetPaymentReceipt;

using Dapper;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler projecting the official receipt voucher with school letterhead, verbal amount, and cashier signature (TD-04, OQ-004).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class GetPaymentReceiptHandler(
    ISqlConnectionFactory connectionFactory,
    ILogger<GetPaymentReceiptHandler>? logger = null)
{
    static GetPaymentReceiptHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectPaymentReceiptSql = """
        SELECT
            p.id AS Id,
            p.payment_number AS PaymentNumber,
            p.receipt_number AS ReceiptNumber,
            p.student_account_id AS StudentAccountId,
            COALESCE(sfa.account_number, N'') AS AccountNumber,
            p.student_id AS StudentId,
            COALESCE(s.student_number, a.applicant_number, N'') AS StudentNumber,
            COALESCE(s.full_name, a.full_name, N'') AS StudentFullName,
            p.applicant_id AS ApplicantId,
            p.payer_id AS PayerId,
            COALESCE(pr.payer_number, N'') AS PayerNumber,
            COALESCE(pr.payer_name, N'') AS PayerName,
            pr.identification_number AS PayerIdentificationNumber,
            pr.email AS PayerEmail,
            pr.phone_number AS PayerPhone,
            p.payment_amount AS PaymentAmount,
            p.currency_code AS CurrencyCode,
            p.payment_method AS PaymentMethod,
            p.external_reference AS ExternalReference,
            p.payment_date AS PaymentDate,
            p.submission_date AS SubmissionDate,
            p.confirmation_status AS ConfirmationStatus,
            p.confirmation_date AS ConfirmationDate,
            p.confirmed_by AS ConfirmedBy,
            p.receipt_issued_at AS ReceiptIssuedAt,
            p.notes AS Notes
        FROM finance.payments p
        JOIN finance.student_financial_accounts sfa ON sfa.id = p.student_account_id
        LEFT JOIN finance.payers pr ON pr.id = p.payer_id
        LEFT JOIN students.students s ON s.id = p.student_id
        LEFT JOIN students.applicants a ON a.id = p.applicant_id
        WHERE p.id = @Id;
        """;

    private const string _selectAllocationsSql = """
        SELECT
            pa.allocation_number AS AllocationNumber,
            COALESCE(fo.obligation_number, N'') AS ObligationNumber,
            COALESCE(fo.charge_type, N'') AS ChargeType,
            COALESCE(fo.description, N'') AS Description,
            pa.allocated_amount AS Amount
        FROM finance.payment_allocations pa
        JOIN finance.financial_obligations fo ON fo.id = pa.obligation_id
        WHERE pa.payment_id = @PaymentId AND pa.status = 'active'
        ORDER BY pa.allocation_date ASC;
        """;

    private sealed record PaymentReceiptRow(
        Guid Id,
        string PaymentNumber,
        string? ReceiptNumber,
        Guid StudentAccountId,
        string AccountNumber,
        Guid? StudentId,
        string? StudentNumber,
        string? StudentFullName,
        Guid? ApplicantId,
        Guid PayerId,
        string PayerNumber,
        string PayerName,
        string? PayerIdentificationNumber,
        string? PayerEmail,
        string? PayerPhone,
        long PaymentAmount,
        string CurrencyCode,
        string PaymentMethod,
        string? ExternalReference,
        DateOnly PaymentDate,
        DateTimeOffset SubmissionDate,
        string ConfirmationStatus,
        DateTimeOffset? ConfirmationDate,
        string? ConfirmedBy,
        DateTimeOffset? ReceiptIssuedAt,
        string? Notes);

    public async Task<Result<PaymentReceiptVoucherResponse>> HandleAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        if (paymentId == Guid.Empty)
        {
            return Result.Failure<PaymentReceiptVoucherResponse>(
                DomainError.Validation("Finance.Payment.InvalidId", "Payment ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var payment = await connection.QueryFirstOrDefaultAsync<PaymentReceiptRow>(new CommandDefinition(
            _selectPaymentReceiptSql,
            new { Id = paymentId },
            cancellationToken: cancellationToken));

        if (payment is null)
        {
            return Result.Failure<PaymentReceiptVoucherResponse>(
                DomainError.NotFound("Finance.Payment.NotFound", $"Payment with ID '{paymentId}' was not found."));
        }

        if (!payment.ConfirmationStatus.Equals("confirmed", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(payment.ReceiptNumber))
        {
            return Result.Failure<PaymentReceiptVoucherResponse>(
                DomainError.Conflict(
                    "Finance.Payment.NotConfirmed",
                    $"Cannot generate receipt voucher for payment '{payment.PaymentNumber}' with status '{payment.ConfirmationStatus}'. Payment must be confirmed."));
        }

        var allocations = (await connection.QueryAsync<ReceiptAllocationLineDto>(new CommandDefinition(
            _selectAllocationsSql,
            new { PaymentId = paymentId },
            cancellationToken: cancellationToken))).ToList();

        var amountInWords = NumberToWordsConverter.ToIndonesianWords(payment.PaymentAmount);

        var schoolHeader = new SchoolHeaderDto(
            SchoolName: "Elsasa Islamic School",
            SchoolAddress: "Jl. Raya Pendidikan No. 123, Jakarta 12340",
            ContactInfo: "Telp: (021) 555-0199 | Email: finance@elsasa.sch.id",
            DocumentTitle: "OFFICIAL RECEIPT / BUKTI PEMBAYARAN SAH");

        var payerDto = new ReceiptPayerDto(
            payment.PayerId,
            payment.PayerNumber,
            payment.PayerName,
            payment.PayerIdentificationNumber,
            payment.PayerEmail,
            payment.PayerPhone);

        var candidateDto = new ReceiptCandidateDto(
            payment.StudentId,
            payment.StudentNumber,
            payment.StudentFullName,
            payment.ApplicantId,
            payment.AccountNumber);

        var cashierDto = new CashierSignatureDto(
            ConfirmedBy: payment.ConfirmedBy ?? "Finance Cashier",
            ConfirmationDate: payment.ConfirmationDate ?? payment.SubmissionDate,
            Title: "Finance Cashier / Kasir Keuangan",
            OfficialStampText: "ELSASA SIS AUTHORIZED RECEIPT");

        logger?.LogDebug(
            "Projected receipt voucher {ReceiptNumber} for payment {PaymentNumber} ({PaymentId}).",
            payment.ReceiptNumber,
            payment.PaymentNumber,
            paymentId);

        var response = new PaymentReceiptVoucherResponse(
            schoolHeader,
            payment.Id,
            payment.PaymentNumber,
            payment.ReceiptNumber,
            payment.PaymentDate,
            payment.ReceiptIssuedAt ?? payment.ConfirmationDate ?? payment.SubmissionDate,
            payment.PaymentMethod,
            payment.ExternalReference,
            payment.PaymentAmount,
            payment.CurrencyCode,
            amountInWords,
            payerDto,
            candidateDto,
            allocations.AsReadOnly(),
            cashierDto,
            payment.Notes);

        return Result.Success(response);
    }
}
