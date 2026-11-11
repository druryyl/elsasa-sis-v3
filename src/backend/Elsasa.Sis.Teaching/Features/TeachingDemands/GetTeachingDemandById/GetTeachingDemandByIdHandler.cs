namespace Elsasa.Sis.Teaching.Features.TeachingDemands.GetTeachingDemandById;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying a teaching demand by its unique identifier (TD-02, P2-S04).
/// </summary>
public sealed class GetTeachingDemandByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeachingDemandByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectDemandByIdSql = """
        SELECT
            td.id AS Id,
            td.demand_code AS DemandCode,
            td.source_domain AS SourceDomain,
            td.source_work_reference AS SourceWorkReference,
            td.source_work_label AS SourceWorkLabel,
            td.academic_period_id AS AcademicPeriodId,
            td.required_capability_code AS RequiredCapabilityCode,
            td.required_assignment_role_id AS RequiredAssignmentRoleId,
            ar.name AS RequiredAssignmentRoleName,
            td.expected_workload_hours AS ExpectedWorkloadHours,
            td.workload_unit AS WorkloadUnit,
            td.requires_assessment_authority AS RequiresAssessmentAuthority,
            td.requested_start_date AS RequestedStartDate,
            td.requested_end_date AS RequestedEndDate,
            td.campus_scope AS CampusScope,
            td.required_teachers_count AS RequiredTeachersCount,
            td.status AS Status,
            td.requested_by AS RequestedBy,
            td.requested_at AS RequestedAt,
            td.decline_reason AS DeclineReason,
            td.clarification_notes AS ClarificationNotes,
            td.created_at AS CreatedAt
        FROM teaching.teaching_demands td
        LEFT JOIN teaching.assignment_roles ar ON ar.id = td.required_assignment_role_id
        WHERE td.id = @Id;
        """;

    public async Task<Result<TeachingDemandDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var demand = await connection.QuerySingleOrDefaultAsync<TeachingDemandDto>(new CommandDefinition(
            _selectDemandByIdSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (demand is null)
        {
            return Result.Failure<TeachingDemandDto>(DomainError.NotFound(
                "Teaching.DemandNotFound",
                $"Teaching demand with ID '{id}' was not found."));
        }

        return Result.Success(demand);
    }
}
