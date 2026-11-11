namespace Elsasa.Sis.Teaching.Features.Workload.GetWorkloadPolicies;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying institutional workload policies (TD-04, P3-S09).
/// </summary>
public sealed class GetWorkloadPoliciesHandler(ISqlConnectionFactory connectionFactory)
{
    static GetWorkloadPoliciesHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    public async Task<Result<IReadOnlyList<WorkloadPolicyDto>>> HandleAsync(
        Guid? academicYearId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (academicYearId.HasValue && academicYearId.Value != Guid.Empty)
        {
            conditions.Add("wp.academic_year_id = @AcademicYearId");
            parameters.Add("AcademicYearId", academicYearId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("wp.status = @Status");
            parameters.Add("Status", status.Trim().ToLowerInvariant());
        }

        var whereSql = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        var querySql = $"""
            SELECT
                wp.id AS Id,
                wp.code AS Code,
                wp.name AS Name,
                wp.academic_year_id AS AcademicYearId,
                wp.version AS Version,
                wp.workload_unit AS WorkloadUnit,
                wp.max_standard_capacity AS MaxStandardCapacity,
                wp.warning_threshold AS WarningThreshold,
                wp.prohibited_threshold AS ProhibitedThreshold,
                wp.includes_assessment_effort AS IncludesAssessmentEffort,
                wp.includes_advising_effort AS IncludesAdvisingEffort,
                wp.includes_preparation_time AS IncludesPreparationTime,
                wp.calculation_rules_summary AS CalculationRulesSummary,
                wp.effective_start_date AS EffectiveStartDate,
                wp.effective_end_date AS EffectiveEndDate,
                wp.status AS Status,
                wp.approved_by AS ApprovedBy,
                wp.approved_date AS ApprovedDate,
                wp.created_at AS CreatedAt
            FROM teaching.workload_policies wp
            {whereSql}
            ORDER BY wp.effective_start_date DESC, wp.created_at DESC;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var items = await connection.QueryAsync<WorkloadPolicyDto>(new CommandDefinition(
            querySql,
            parameters,
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<WorkloadPolicyDto>>(items.ToList().AsReadOnly());
    }
}
