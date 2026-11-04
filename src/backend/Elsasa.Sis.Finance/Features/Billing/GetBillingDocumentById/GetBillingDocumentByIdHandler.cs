namespace Elsasa.Sis.Finance.Features.Billing.GetBillingDocumentById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler projecting detailed invoice snapshot with frozen itemized line items list (FIN-WF-04, UC-FIN-03, GAP-005, TD-05).
/// </summary>
public sealed class GetBillingDocumentByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetBillingDocumentByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectHeaderSql = """
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
            bd.created_at AS CreatedAt,
            bd.created_by AS CreatedBy
        FROM finance.billing_documents bd
        LEFT JOIN students.students s ON s.id = bd.student_id
        WHERE bd.id = @Id;
        """;

    private const string _selectItemsSql = """
        SELECT
            id AS Id,
            line_sequence AS LineSequence,
            item_type AS ItemType,
            reference_entity_type AS ReferenceEntityType,
            reference_entity_id AS ReferenceEntityId,
            description AS Description,
            amount AS Amount,
            currency_code AS CurrencyCode,
            created_at AS CreatedAt
        FROM finance.billing_document_items
        WHERE billing_document_id = @Id
        ORDER BY line_sequence ASC;
        """;

    public async Task<Result<BillingDocumentDetailResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var header = await connection.QueryFirstOrDefaultAsync<HeaderRow>(new CommandDefinition(
            _selectHeaderSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (header is null)
        {
            return Result.Failure<BillingDocumentDetailResponse>(
                DomainError.NotFound(
                    "Finance.Billing.DocumentNotFound",
                    $"Billing document with ID '{id}' was not found."));
        }

        var items = (await connection.QueryAsync<BillingDocumentDetailItemDto>(new CommandDefinition(
            _selectItemsSql,
            new { Id = id },
            cancellationToken: cancellationToken))).ToList();

        var response = new BillingDocumentDetailResponse(
            header.Id,
            header.DocumentNumber,
            header.StudentAccountId,
            header.StudentId,
            header.StudentNumber,
            header.StudentFullName,
            header.DocumentType,
            header.RecipientName,
            header.RecipientRelationship,
            header.RecipientContact,
            header.BillingPeriodStart,
            header.BillingPeriodEnd,
            header.IssueDate,
            header.PaymentDueDate,
            header.TotalBilledAmount,
            header.CurrencyCode,
            header.DeliveryChannel,
            header.DeliveryStatus,
            header.SentAt,
            header.Notes,
            items,
            header.CreatedAt,
            header.CreatedBy);

        return Result.Success(response);
    }

    private sealed record HeaderRow(
        Guid Id,
        string DocumentNumber,
        Guid StudentAccountId,
        Guid StudentId,
        string StudentNumber,
        string StudentFullName,
        string DocumentType,
        string RecipientName,
        string RecipientRelationship,
        string RecipientContact,
        DateOnly? BillingPeriodStart,
        DateOnly? BillingPeriodEnd,
        DateOnly IssueDate,
        DateOnly? PaymentDueDate,
        long TotalBilledAmount,
        string CurrencyCode,
        string DeliveryChannel,
        string DeliveryStatus,
        DateTimeOffset? SentAt,
        string? Notes,
        DateTimeOffset CreatedAt,
        string? CreatedBy);
}
