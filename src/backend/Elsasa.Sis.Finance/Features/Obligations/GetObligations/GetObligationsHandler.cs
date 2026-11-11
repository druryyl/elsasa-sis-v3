namespace Elsasa.Sis.Finance.Features.Obligations.GetObligations;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing filtered and paginated financial obligations query with schedule projections (FIN-WF-02, SCR-FIN-03, GAP-004, TD-04).
/// </summary>
public sealed class GetObligationsHandler(ISqlConnectionFactory connectionFactory)
{
    static GetObligationsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _countSql = """
        SELECT COUNT(1)
        FROM finance.financial_obligations fo
        LEFT JOIN finance.v_student_receivables r ON r.obligation_id = fo.id
        LEFT JOIN students.students s ON s.id = fo.student_id
        WHERE (@StudentId IS NULL OR fo.student_id = @StudentId)
          AND (
            @Status IS NULL
            OR (@Status = 'overdue' AND r.is_overdue = 1)
            OR (@Status = 'unpaid' AND r.outstanding_amount > 0 AND fo.status NOT IN ('settled', 'waived', 'cancelled'))
            OR (@Status = 'paid' AND (fo.status = 'settled' OR r.outstanding_amount = 0))
            OR (@Status NOT IN ('overdue', 'unpaid', 'paid') AND fo.status = @Status)
          )
          AND (@Aging IS NULL OR r.aging_bracket = @Aging)
          AND (@AcademicPeriodId IS NULL OR fo.academic_period_id = @AcademicPeriodId)
          AND (@ChargeType IS NULL OR fo.charge_type = @ChargeType)
          AND (
            @SearchPattern IS NULL
            OR fo.obligation_number LIKE @SearchPattern
            OR fo.description LIKE @SearchPattern
            OR s.full_name LIKE @SearchPattern
            OR s.student_number LIKE @SearchPattern
          );
        """;

    private const string _selectObligationsSql = """
        SELECT
            fo.id AS Id,
            fo.obligation_number AS ObligationNumber,
            fo.student_account_id AS StudentAccountId,
            fo.student_id AS StudentId,
            COALESCE(s.student_number, N'') AS StudentNumber,
            COALESCE(s.full_name, N'') AS StudentFullName,
            fo.policy_id AS PolicyId,
            fp.policy_code AS PolicyCode,
            fo.charge_rule_id AS ChargeRuleId,
            cr.rule_code AS ChargeRuleCode,
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
            fo.notes AS Notes,
            fo.created_at AS CreatedAt,
            r.outstanding_amount AS OutstandingAmount,
            r.aging_bracket AS AgingBracket
        FROM finance.financial_obligations fo
        LEFT JOIN finance.v_student_receivables r ON r.obligation_id = fo.id
        LEFT JOIN students.students s ON s.id = fo.student_id
        LEFT JOIN finance.financial_policies fp ON fp.id = fo.policy_id
        LEFT JOIN finance.charge_rules cr ON cr.id = fo.charge_rule_id
        WHERE (@StudentId IS NULL OR fo.student_id = @StudentId)
          AND (
            @Status IS NULL
            OR (@Status = 'overdue' AND r.is_overdue = 1)
            OR (@Status = 'unpaid' AND r.outstanding_amount > 0 AND fo.status NOT IN ('settled', 'waived', 'cancelled'))
            OR (@Status = 'paid' AND (fo.status = 'settled' OR r.outstanding_amount = 0))
            OR (@Status NOT IN ('overdue', 'unpaid', 'paid') AND fo.status = @Status)
          )
          AND (@Aging IS NULL OR r.aging_bracket = @Aging)
          AND (@AcademicPeriodId IS NULL OR fo.academic_period_id = @AcademicPeriodId)
          AND (@ChargeType IS NULL OR fo.charge_type = @ChargeType)
          AND (
            @SearchPattern IS NULL
            OR fo.obligation_number LIKE @SearchPattern
            OR fo.description LIKE @SearchPattern
            OR s.full_name LIKE @SearchPattern
            OR s.student_number LIKE @SearchPattern
          )
        ORDER BY fo.issue_date DESC, fo.created_at DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    private const string _selectSchedulesSql = """
        SELECT
            id AS Id,
            obligation_id AS ObligationId,
            installment_sequence AS InstallmentSequence,
            due_date AS DueDate,
            scheduled_amount AS ScheduledAmount,
            currency_code AS CurrencyCode,
            status AS Status,
            notes AS Notes
        FROM finance.obligation_due_schedules
        WHERE obligation_id IN @ObligationIds
        ORDER BY installment_sequence ASC;
        """;

    private sealed record ObligationRow(
        Guid Id,
        string ObligationNumber,
        Guid StudentAccountId,
        Guid StudentId,
        string StudentNumber,
        string StudentFullName,
        Guid PolicyId,
        string? PolicyCode,
        Guid? ChargeRuleId,
        string? ChargeRuleCode,
        string Description,
        string ChargeType,
        bool IsMandatory,
        long OriginalAmount,
        string CurrencyCode,
        DateOnly IssueDate,
        DateOnly DueDate,
        Guid AcademicPeriodId,
        string AcademicPeriodLabel,
        string Status,
        string AuthorizedBy,
        string? Notes,
        DateTimeOffset CreatedAt,
        long? OutstandingAmount = null,
        string? AgingBracket = null);

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return status.Trim().ToLowerInvariant();
    }

    private static string? NormalizeAgingBracket(string? aging)
    {
        if (string.IsNullOrWhiteSpace(aging) || aging.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var clean = aging.Trim();
        if (clean.Contains("%2B", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean.Replace("%2B", "+", StringComparison.OrdinalIgnoreCase);
        }

        if (clean.Contains("%20", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean.Replace("%20", " ", StringComparison.OrdinalIgnoreCase);
        }

        return clean.ToLowerInvariant() switch
        {
            "current" => "Current",
            "1-30" or "1-30 days" or "1-30-days" => "1-30 Days",
            "31-60" or "31-60 days" or "31-60-days" => "31-60 Days",
            "61-90" or "61-90 days" or "61-90-days" => "61-90 Days",
            "90+" or "90+ days" or "90-plus-days" or "90 plus days" => "90+ Days",
            _ => clean
        };
    }

    public async Task<Result<GetObligationsResponse>> HandleAsync(
        Guid? studentId = null,
        string? status = null,
        Guid? academicPeriodId = null,
        string? chargeType = null,
        string? aging = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var effectivePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var effectivePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);
        var offset = (effectivePageNumber - 1) * effectivePageSize;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var normalizedStatus = NormalizeStatus(status);
        var normalizedAging = NormalizeAgingBracket(aging);
        var trimmedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var searchPattern = trimmedSearch is not null ? $"%{trimmedSearch}%" : null;

        var filterParams = new
        {
            StudentId = studentId,
            Status = normalizedStatus,
            Aging = normalizedAging,
            AcademicPeriodId = academicPeriodId,
            ChargeType = string.IsNullOrWhiteSpace(chargeType) || chargeType.Equals("all", StringComparison.OrdinalIgnoreCase) ? null : chargeType.Trim(),
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = effectivePageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countSql,
            filterParams,
            cancellationToken: cancellationToken));

        var rows = (await connection.QueryAsync<ObligationRow>(new CommandDefinition(
            _selectObligationsSql,
            filterParams,
            cancellationToken: cancellationToken))).AsList();

        if (rows.Count == 0)
        {
            return Result.Success(GetObligationsResponse.Create(
                [],
                totalCount,
                effectivePageNumber,
                effectivePageSize));
        }

        var obligationIds = rows.Select(r => r.Id).Distinct().ToArray();

        var schedules = (await connection.QueryAsync<ObligationScheduleDto>(new CommandDefinition(
            _selectSchedulesSql,
            new { ObligationIds = obligationIds },
            cancellationToken: cancellationToken))).AsList();

        var schedulesByObligation = schedules
            .GroupBy(s => s.ObligationId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ObligationScheduleDto>)g.ToList());

        var items = rows.Select(row => new ObligationListItemDto(
            row.Id,
            row.ObligationNumber,
            row.StudentAccountId,
            row.StudentId,
            row.StudentNumber,
            row.StudentFullName,
            row.PolicyId,
            row.PolicyCode,
            row.ChargeRuleId,
            row.ChargeRuleCode,
            row.Description,
            row.ChargeType,
            row.IsMandatory,
            row.OriginalAmount,
            row.CurrencyCode,
            row.IssueDate,
            row.DueDate,
            row.AcademicPeriodId,
            row.AcademicPeriodLabel,
            row.Status,
            row.AuthorizedBy,
            row.Notes,
            row.CreatedAt,
            schedulesByObligation.TryGetValue(row.Id, out var schedList) ? schedList : [],
            row.OutstandingAmount,
            row.AgingBracket
        )).ToList();

        var response = GetObligationsResponse.Create(
            items,
            totalCount,
            effectivePageNumber,
            effectivePageSize);

        return Result.Success(response);
    }
}
