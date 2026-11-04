namespace Elsasa.Sis.Finance.Features.Payments.GetPaymentById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler querying detailed payment inspection record by primary key (SCR-FIN-02, TD-04).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class GetPaymentByIdHandler(
    ISqlConnectionFactory connectionFactory,
    ILogger<GetPaymentByIdHandler>? logger = null)
{
    static GetPaymentByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectPaymentSql = """
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
            COALESCE(pr.payer_type, N'parent_guardian') AS PayerType,
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
            p.rejection_reason AS RejectionReason,
            p.notes AS Notes,
            p.created_at AS CreatedAt,
            p.created_by AS CreatedBy,
            p.updated_at AS UpdatedAt,
            p.updated_by AS UpdatedBy
        FROM finance.payments p
        JOIN finance.student_financial_accounts sfa ON sfa.id = p.student_account_id
        LEFT JOIN finance.payers pr ON pr.id = p.payer_id
        LEFT JOIN students.students s ON s.id = p.student_id
        LEFT JOIN students.applicants a ON a.id = p.applicant_id
        WHERE p.id = @Id;
        """;

    private const string _selectAllocationsSql = """
        SELECT
            pa.id AS Id,
            pa.allocation_number AS AllocationNumber,
            pa.obligation_id AS ObligationId,
            COALESCE(fo.obligation_number, N'') AS ObligationNumber,
            COALESCE(fo.charge_type, N'') AS ChargeType,
            COALESCE(fo.description, N'') AS Description,
            pa.allocated_amount AS AllocatedAmount,
            pa.allocation_date AS AllocationDate,
            pa.allocated_by AS AllocatedBy,
            pa.status AS Status,
            pa.superseded_by_id AS SupersededById,
            pa.reversal_reason AS ReversalReason
        FROM finance.payment_allocations pa
        JOIN finance.financial_obligations fo ON fo.id = pa.obligation_id
        WHERE pa.payment_id = @PaymentId
        ORDER BY pa.allocation_date ASC;
        """;

    private sealed record PaymentRow(
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
        string PayerType,
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
        string? RejectionReason,
        string? Notes,
        DateTimeOffset CreatedAt,
        string? CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy);

    public async Task<Result<GetPaymentByIdResponse>> HandleAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        if (paymentId == Guid.Empty)
        {
            return Result.Failure<GetPaymentByIdResponse>(
                DomainError.Validation("Finance.Payment.InvalidId", "Payment ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var payment = await connection.QueryFirstOrDefaultAsync<PaymentRow>(new CommandDefinition(
            _selectPaymentSql,
            new { Id = paymentId },
            cancellationToken: cancellationToken));

        if (payment is null)
        {
            return Result.Failure<GetPaymentByIdResponse>(
                DomainError.NotFound("Finance.Payment.NotFound", $"Payment with ID '{paymentId}' was not found."));
        }

        var allocations = (await connection.QueryAsync<PaymentAllocationItemDto>(new CommandDefinition(
            _selectAllocationsSql,
            new { PaymentId = paymentId },
            cancellationToken: cancellationToken))).ToList();

        var totalAllocated = allocations
            .Where(a => a.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            .Sum(a => a.AllocatedAmount);

        var unallocatedAmount = Math.Max(0, payment.PaymentAmount - totalAllocated);

        logger?.LogDebug("Inspected payment {PaymentNumber} ({PaymentId}) with {AllocationCount} allocations.",
            payment.PaymentNumber, paymentId, allocations.Count);

        var response = new GetPaymentByIdResponse(
            payment.Id,
            payment.PaymentNumber,
            payment.ReceiptNumber,
            payment.StudentAccountId,
            payment.AccountNumber,
            payment.StudentId,
            payment.StudentNumber,
            payment.StudentFullName,
            payment.ApplicantId,
            payment.PayerId,
            payment.PayerNumber,
            payment.PayerName,
            payment.PayerType,
            payment.PayerIdentificationNumber,
            payment.PayerEmail,
            payment.PayerPhone,
            payment.PaymentAmount,
            payment.CurrencyCode,
            payment.PaymentMethod,
            payment.ExternalReference,
            payment.PaymentDate,
            payment.SubmissionDate,
            payment.ConfirmationStatus,
            payment.ConfirmationDate,
            payment.ConfirmedBy,
            payment.ReceiptIssuedAt,
            payment.RejectionReason,
            payment.Notes,
            payment.CreatedAt,
            payment.CreatedBy,
            payment.UpdatedAt,
            payment.UpdatedBy,
            totalAllocated,
            unallocatedAmount,
            allocations.AsReadOnly());

        return Result.Success(response);
    }
}
