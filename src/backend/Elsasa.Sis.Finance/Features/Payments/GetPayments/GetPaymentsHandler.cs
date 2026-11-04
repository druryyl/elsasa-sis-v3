namespace Elsasa.Sis.Finance.Features.Payments.GetPayments;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler querying payments with filters and pagination for verification queues and transaction history (SCR-FIN-02, TD-04).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class GetPaymentsHandler(
    ISqlConnectionFactory connectionFactory,
    ILogger<GetPaymentsHandler>? logger = null)
{
    static GetPaymentsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _countSql = """
        SELECT COUNT(1)
        FROM finance.payments p
        LEFT JOIN finance.payers pr ON pr.id = p.payer_id
        LEFT JOIN students.students s ON s.id = p.student_id
        LEFT JOIN students.applicants a ON a.id = p.applicant_id
        WHERE (@Status IS NULL OR p.confirmation_status = @Status)
          AND (@StudentId IS NULL OR p.student_id = @StudentId)
          AND (@ApplicantId IS NULL OR p.applicant_id = @ApplicantId)
          AND (@PayerId IS NULL OR p.payer_id = @PayerId)
          AND (@StartDate IS NULL OR p.payment_date >= @StartDate)
          AND (@EndDate IS NULL OR p.payment_date <= @EndDate)
          AND (@SearchPattern IS NULL OR (
              p.payment_number LIKE @SearchPattern
              OR p.receipt_number LIKE @SearchPattern
              OR p.external_reference LIKE @SearchPattern
              OR pr.payer_name LIKE @SearchPattern
              OR s.full_name LIKE @SearchPattern
              OR a.full_name LIKE @SearchPattern
          ));
        """;

    private const string _selectPaymentsSql = """
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
            p.payment_amount AS PaymentAmount,
            p.currency_code AS CurrencyCode,
            p.payment_method AS PaymentMethod,
            p.external_reference AS ExternalReference,
            p.payment_date AS PaymentDate,
            p.submission_date AS SubmissionDate,
            p.confirmation_status AS ConfirmationStatus,
            p.confirmation_date AS ConfirmationDate,
            p.confirmed_by AS ConfirmedBy,
            p.rejection_reason AS RejectionReason,
            p.notes AS Notes,
            ISNULL(alloc.total_allocated, 0) AS TotalAllocated,
            CASE
                WHEN p.payment_amount - ISNULL(alloc.total_allocated, 0) > 0
                THEN p.payment_amount - ISNULL(alloc.total_allocated, 0)
                ELSE 0
            END AS UnallocatedAmount
        FROM finance.payments p
        JOIN finance.student_financial_accounts sfa ON sfa.id = p.student_account_id
        LEFT JOIN finance.payers pr ON pr.id = p.payer_id
        LEFT JOIN students.students s ON s.id = p.student_id
        LEFT JOIN students.applicants a ON a.id = p.applicant_id
        LEFT JOIN (
            SELECT payment_id, SUM(allocated_amount) AS total_allocated
            FROM finance.payment_allocations
            WHERE status = 'active'
            GROUP BY payment_id
        ) alloc ON alloc.payment_id = p.id
        WHERE (@Status IS NULL OR p.confirmation_status = @Status)
          AND (@StudentId IS NULL OR p.student_id = @StudentId)
          AND (@ApplicantId IS NULL OR p.applicant_id = @ApplicantId)
          AND (@PayerId IS NULL OR p.payer_id = @PayerId)
          AND (@StartDate IS NULL OR p.payment_date >= @StartDate)
          AND (@EndDate IS NULL OR p.payment_date <= @EndDate)
          AND (@SearchPattern IS NULL OR (
              p.payment_number LIKE @SearchPattern
              OR p.receipt_number LIKE @SearchPattern
              OR p.external_reference LIKE @SearchPattern
              OR pr.payer_name LIKE @SearchPattern
              OR s.full_name LIKE @SearchPattern
              OR a.full_name LIKE @SearchPattern
          ))
        ORDER BY p.submission_date DESC, p.created_at DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    public async Task<Result<GetPaymentsResponse>> HandleAsync(
        string? status = null,
        Guid? studentId = null,
        Guid? applicantId = null,
        Guid? payerId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var resolvedPage = page < 1 ? 1 : page;
        var resolvedPageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var offset = (resolvedPage - 1) * resolvedPageSize;

        var cleanStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();
        var searchPattern = string.IsNullOrWhiteSpace(searchTerm) ? null : $"%{searchTerm.Trim()}%";

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var parameters = new
        {
            Status = cleanStatus,
            StudentId = studentId == Guid.Empty ? null : studentId,
            ApplicantId = applicantId == Guid.Empty ? null : applicantId,
            PayerId = payerId == Guid.Empty ? null : payerId,
            StartDate = startDate,
            EndDate = endDate,
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = resolvedPageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countSql,
            parameters,
            cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<PaymentListItemDto>(new CommandDefinition(
            _selectPaymentsSql,
            parameters,
            cancellationToken: cancellationToken))).ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / resolvedPageSize);

        logger?.LogDebug(
            "Retrieved {Count}/{Total} payments (Page {Page} of {TotalPages}).",
            items.Count,
            totalCount,
            resolvedPage,
            totalPages);

        return Result.Success(new GetPaymentsResponse(
            items.AsReadOnly(),
            totalCount,
            resolvedPage,
            resolvedPageSize,
            totalPages));
    }
}
