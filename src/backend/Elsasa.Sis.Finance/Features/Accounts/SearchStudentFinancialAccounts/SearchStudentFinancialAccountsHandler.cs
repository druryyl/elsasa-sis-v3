namespace Elsasa.Sis.Finance.Features.Accounts.SearchStudentFinancialAccounts;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing paginated student financial account search with dynamically derived balance (ADR-004, ADR-005, TD-02).
/// </summary>
public sealed class SearchStudentFinancialAccountsHandler(ISqlConnectionFactory connectionFactory)
{
    static SearchStudentFinancialAccountsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _searchSql = """
        SELECT COUNT(1)
        FROM finance.student_financial_accounts sfa
        LEFT JOIN students.students s ON s.id = sfa.student_id
        OUTER APPLY (
            SELECT TOP (1)
                p.campus,
                p.grade,
                p.school_level,
                p.program
            FROM students.administrative_placements p
            WHERE p.student_id = s.id AND p.is_current = 1
            ORDER BY p.effective_date DESC, p.created_at DESC
        ) current_placement
        OUTER APPLY (
            SELECT TOP (1)
                e.initial_campus,
                e.initial_level
            FROM students.enrollments e
            WHERE e.student_id = s.id
            ORDER BY e.enrollment_date DESC, e.created_at DESC
        ) latest_enrollment
        WHERE (@Status IS NULL OR sfa.account_status = @Status)
          AND (@Campus IS NULL OR COALESCE(current_placement.campus, latest_enrollment.initial_campus) = @Campus)
          AND (@Grade IS NULL OR COALESCE(current_placement.grade, latest_enrollment.initial_level) = @Grade)
          AND (@SearchPattern IS NULL OR (
                s.full_name LIKE @SearchPattern
                OR s.student_number LIKE @SearchPattern
                OR sfa.account_number LIKE @SearchPattern));

        SELECT
            sfa.id AS AccountId,
            sfa.account_number AS AccountNumber,
            sfa.student_id AS StudentId,
            COALESCE(s.student_number, N'') AS StudentNumber,
            COALESCE(s.full_name, N'') AS StudentFullName,
            COALESCE(current_placement.campus, latest_enrollment.initial_campus, N'') AS Campus,
            COALESCE(current_placement.grade, latest_enrollment.initial_level, N'') AS Grade,
            sfa.currency_code AS CurrencyCode,
            sfa.account_status AS AccountStatus,
            sfa.opened_date AS OpenedDate,
            ISNULL(bal.total_assessed, 0) AS TotalAssessed,
            ISNULL(bal.total_settled, 0) AS TotalSettled,
            ISNULL(bal.current_balance, 0) AS CurrentBalance
        FROM finance.student_financial_accounts sfa
        LEFT JOIN students.students s ON s.id = sfa.student_id
        OUTER APPLY (
            SELECT TOP (1)
                p.campus,
                p.grade,
                p.school_level,
                p.program
            FROM students.administrative_placements p
            WHERE p.student_id = s.id AND p.is_current = 1
            ORDER BY p.effective_date DESC, p.created_at DESC
        ) current_placement
        OUTER APPLY (
            SELECT TOP (1)
                e.initial_campus,
                e.initial_level
            FROM students.enrollments e
            WHERE e.student_id = s.id
            ORDER BY e.enrollment_date DESC, e.created_at DESC
        ) latest_enrollment
        OUTER APPLY (
            SELECT 
                SUM(CASE WHEN fo.status NOT IN ('cancelled') THEN fo.original_amount ELSE 0 END) AS total_assessed,
                SUM(CASE WHEN fo.status = 'settled' THEN fo.original_amount ELSE 0 END) AS total_settled,
                (SUM(CASE WHEN fo.status NOT IN ('cancelled') THEN fo.original_amount ELSE 0 END) -
                 SUM(CASE WHEN fo.status = 'settled' THEN fo.original_amount ELSE 0 END)) AS current_balance
            FROM finance.financial_obligations fo
            WHERE fo.student_account_id = sfa.id
        ) bal
        WHERE (@Status IS NULL OR sfa.account_status = @Status)
          AND (@Campus IS NULL OR COALESCE(current_placement.campus, latest_enrollment.initial_campus) = @Campus)
          AND (@Grade IS NULL OR COALESCE(current_placement.grade, latest_enrollment.initial_level) = @Grade)
          AND (@SearchPattern IS NULL OR (
                s.full_name LIKE @SearchPattern
                OR s.student_number LIKE @SearchPattern
                OR sfa.account_number LIKE @SearchPattern))
        ORDER BY sfa.opened_date DESC, sfa.created_at DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    /// <summary>
    /// Executes paginated student financial accounts search with filtering and dynamic balance calculation.
    /// </summary>
    public async Task<Result<SearchStudentFinancialAccountsResponse>> HandleAsync(
        string? searchTerm = null,
        string? campus = null,
        string? grade = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var effectivePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var effectivePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);
        var offset = (effectivePageNumber - 1) * effectivePageSize;
        var searchPattern = string.IsNullOrWhiteSpace(searchTerm) ? null : $"%{searchTerm.Trim()}%";

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _searchSql,
            new
            {
                Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
                Campus = string.IsNullOrWhiteSpace(campus) ? null : campus.Trim(),
                Grade = string.IsNullOrWhiteSpace(grade) ? null : grade.Trim(),
                SearchPattern = searchPattern,
                Offset = offset,
                PageSize = effectivePageSize
            },
            cancellationToken: cancellationToken));

        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<StudentFinancialAccountListItemDto>()).AsList();

        var response = SearchStudentFinancialAccountsResponse.Create(items, totalCount, effectivePageNumber, effectivePageSize);
        return Result.Success(response);
    }
}
