namespace Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying the 360-degree student financial account position and deriving balance dynamically (ADR-004, ADR-005, TD-02).
/// </summary>
public sealed class GetStudentFinancialAccountHandler(ISqlConnectionFactory connectionFactory)
{
    static GetStudentFinancialAccountHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _queryMultiSql = """
        SELECT
            sfa.id AS AccountId,
            sfa.account_number AS AccountNumber,
            sfa.student_id AS StudentId,
            s.student_number AS StudentNumber,
            s.full_name AS StudentFullName,
            sfa.currency_code AS CurrencyCode,
            sfa.account_status AS AccountStatus,
            sfa.opened_date AS OpenedDate,
            sfa.closed_date AS ClosedDate,
            sfa.notes AS Notes,
            sfa.created_at AS CreatedAt,
            ISNULL(SUM(CASE WHEN fo.status NOT IN ('cancelled') THEN fo.original_amount ELSE 0 END), 0) AS TotalAssessed,
            ISNULL(SUM(CASE WHEN fo.status = 'settled' THEN fo.original_amount ELSE 0 END), 0) AS TotalSettled,
            (ISNULL(SUM(CASE WHEN fo.status NOT IN ('cancelled') THEN fo.original_amount ELSE 0 END), 0) -
             ISNULL(SUM(CASE WHEN fo.status = 'settled' THEN fo.original_amount ELSE 0 END), 0)) AS CurrentBalance
        FROM finance.student_financial_accounts sfa
        LEFT JOIN students.students s ON s.id = sfa.student_id
        LEFT JOIN finance.financial_obligations fo ON fo.student_account_id = sfa.id
        WHERE sfa.student_id = @StudentId
        GROUP BY sfa.id, sfa.account_number, sfa.student_id, s.student_number, s.full_name, sfa.currency_code, sfa.account_status, sfa.opened_date, sfa.closed_date, sfa.notes, sfa.created_at;

        SELECT
            fo.id AS Id,
            fo.obligation_number AS ObligationNumber,
            fo.student_account_id AS StudentAccountId,
            fo.student_id AS StudentId,
            fo.charge_rule_id AS ChargeRuleId,
            fo.policy_id AS PolicyId,
            fo.description AS Description,
            fo.charge_type AS ChargeType,
            fo.is_mandatory AS IsMandatory,
            fo.original_amount AS OriginalAmount,
            fo.currency_code AS CurrencyCode,
            fo.issue_date AS IssueDate,
            fo.due_date AS DueDate,
            fo.academic_period_id AS AcademicPeriodId,
            fo.academic_period_label AS AcademicPeriodLabel,
            fo.status AS Status,
            fo.authorized_by AS AuthorizedBy,
            fo.created_at AS CreatedAt
        FROM finance.financial_obligations fo
        WHERE fo.student_id = @StudentId
          AND fo.status IN ('assessed', 'issued')
        ORDER BY fo.due_date ASC, fo.created_at ASC;

        SELECT TOP (20)
            bd.id AS Id,
            bd.document_number AS DocumentNumber,
            bd.student_account_id AS StudentAccountId,
            bd.student_id AS StudentId,
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
            bd.created_at AS CreatedAt
        FROM finance.billing_documents bd
        WHERE bd.student_id = @StudentId
        ORDER BY bd.issue_date DESC, bd.created_at DESC;
        """;

    private sealed record AccountHeaderRow(
        Guid AccountId,
        string AccountNumber,
        Guid StudentId,
        string? StudentNumber,
        string? StudentFullName,
        string CurrencyCode,
        string AccountStatus,
        DateOnly OpenedDate,
        DateOnly? ClosedDate,
        string? Notes,
        DateTimeOffset CreatedAt,
        long TotalAssessed,
        long TotalSettled,
        long CurrentBalance);

    /// <summary>
    /// Executes account lookup by student ID and builds the dossier with dynamic balance calculation.
    /// </summary>
    public async Task<Result<GetStudentFinancialAccountResponse>> HandleAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _queryMultiSql,
            new { StudentId = studentId },
            cancellationToken: cancellationToken));

        var header = await multi.ReadSingleOrDefaultAsync<AccountHeaderRow>();
        if (header is null)
        {
            return Result.Failure<GetStudentFinancialAccountResponse>(
                DomainError.NotFound(
                    "Finance.Accounts.AccountNotFound",
                    $"Financial account for student ID '{studentId}' was not found."));
        }

        var openObligations = (await multi.ReadAsync<OpenObligationDto>()).AsList();
        var recentBillingDocuments = (await multi.ReadAsync<RecentBillingDocumentDto>()).AsList();

        var response = new GetStudentFinancialAccountResponse(
            header.AccountId,
            header.AccountNumber,
            header.StudentId,
            header.StudentNumber,
            header.StudentFullName,
            header.CurrencyCode,
            header.AccountStatus,
            header.OpenedDate,
            header.ClosedDate,
            header.Notes,
            header.CreatedAt,
            header.TotalAssessed,
            header.TotalSettled,
            header.CurrentBalance,
            openObligations,
            recentBillingDocuments);

        return Result.Success(response);
    }
}
