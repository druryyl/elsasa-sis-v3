namespace Elsasa.Sis.Finance.Features.Billing.GetBillingDocuments;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing filtered and paginated billing documents query (FIN-WF-04, SCR-FIN-03, GAP-005, TD-05).
/// </summary>
public sealed class GetBillingDocumentsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetBillingDocumentsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _countSql = """
        SELECT COUNT(1)
        FROM finance.billing_documents bd
        WHERE (@StudentId IS NULL OR bd.student_id = @StudentId)
          AND (@DocumentType IS NULL OR bd.document_type = @DocumentType)
          AND (@DeliveryStatus IS NULL OR bd.delivery_status = @DeliveryStatus);
        """;

    private const string _selectSql = """
        SELECT
            bd.id AS Id,
            bd.document_number AS DocumentNumber,
            bd.student_account_id AS StudentAccountId,
            bd.student_id AS StudentId,
            COALESCE(s.student_number, N'') AS StudentNumber,
            COALESCE(s.full_name, N'') AS StudentFullName,
            bd.document_type AS DocumentType,
            bd.recipient_name AS RecipientName,
            bd.recipient_relationship AS RecipientRelationship,
            bd.recipient_contact AS RecipientContact,
            bd.billing_period_start AS BillingPeriodStart,
            bd.billing_period_end AS BillingPeriodEnd,
            bd.issue_date AS IssueDate,
            bd.payment_due_date AS PaymentDueDate,
            bd.total_billed_amount AS TotalBilledAmount,
            bd.currency_code AS CurrencyCode,
            bd.delivery_channel AS DeliveryChannel,
            bd.delivery_status AS DeliveryStatus,
            bd.sent_at AS SentAt,
            bd.notes AS Notes,
            bd.created_at AS CreatedAt
        FROM finance.billing_documents bd
        LEFT JOIN students.students s ON s.id = bd.student_id
        WHERE (@StudentId IS NULL OR bd.student_id = @StudentId)
          AND (@DocumentType IS NULL OR bd.document_type = @DocumentType)
          AND (@DeliveryStatus IS NULL OR bd.delivery_status = @DeliveryStatus)
        ORDER BY bd.issue_date DESC, bd.created_at DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    public async Task<Result<PaginatedList<BillingDocumentListItemDto>>> HandleAsync(
        Guid? studentId = null,
        string? documentType = null,
        string? deliveryStatus = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);
        var offset = (safePage - 1) * safePageSize;

        var cleanDocumentType = string.IsNullOrWhiteSpace(documentType) ? null : documentType.Trim().ToLowerInvariant();
        var cleanDeliveryStatus = string.IsNullOrWhiteSpace(deliveryStatus) ? null : deliveryStatus.Trim().ToLowerInvariant();

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countSql,
            new
            {
                StudentId = studentId,
                DocumentType = cleanDocumentType,
                DeliveryStatus = cleanDeliveryStatus
            },
            cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<BillingDocumentListItemDto>(new CommandDefinition(
            _selectSql,
            new
            {
                StudentId = studentId,
                DocumentType = cleanDocumentType,
                DeliveryStatus = cleanDeliveryStatus,
                Offset = offset,
                PageSize = safePageSize
            },
            cancellationToken: cancellationToken))).ToList();

        var result = PaginatedList<BillingDocumentListItemDto>.Create(
            items,
            totalCount,
            safePage,
            safePageSize);

        return Result.Success(result);
    }
}
