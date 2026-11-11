namespace Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemand;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler updating demand parameters when in mutable status ('received', 'returned_for_clarification') (TD-02, P2-S04).
/// </summary>
public sealed class UpdateTeachingDemandHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static UpdateTeachingDemandHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectDemandSql = """
        SELECT
            id AS Id,
            demand_code AS DemandCode,
            status AS Status
        FROM teaching.teaching_demands
        WHERE id = @Id;
        """;

    private const string _checkRoleSql = """
        SELECT TOP 1 1
        FROM teaching.assignment_roles
        WHERE id = @RoleId AND is_active = 1;
        """;

    private const string _updateDemandSql = """
        UPDATE teaching.teaching_demands
        SET
            source_work_label = @SourceWorkLabel,
            required_capability_code = @RequiredCapabilityCode,
            required_assignment_role_id = @RequiredAssignmentRoleId,
            expected_workload_hours = @ExpectedWorkloadHours,
            workload_unit = @WorkloadUnit,
            requires_assessment_authority = @RequiresAssessmentAuthority,
            requested_start_date = @RequestedStartDate,
            requested_end_date = @RequestedEndDate,
            campus_scope = @CampusScope,
            required_teachers_count = @RequiredTeachersCount,
            clarification_notes = COALESCE(@ClarificationNotes, clarification_notes),
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        OUTPUT inserted.updated_at
        WHERE id = @Id;
        """;

    public async Task<Result<UpdateTeachingDemandResponse>> HandleAsync(
        Guid id,
        UpdateTeachingDemandRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify demand exists
        var existing = await connection.QuerySingleOrDefaultAsync<DemandSummaryRow>(new CommandDefinition(
            _selectDemandSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<UpdateTeachingDemandResponse>(DomainError.NotFound(
                "Teaching.DemandNotFound",
                $"Teaching demand with ID '{id}' was not found."));
        }

        // 2. Verify demand is in a mutable status
        if (!existing.Status.Equals("received", StringComparison.OrdinalIgnoreCase) &&
            !existing.Status.Equals("returned_for_clarification", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateTeachingDemandResponse>(DomainError.Conflict(
                "Teaching.DemandImmutable",
                $"Teaching demand with ID '{id}' cannot be modified because its current status is '{existing.Status}'. Only demands with status 'received' or 'returned_for_clarification' can be modified."));
        }

        // 3. Verify required assignment role exists and is active
        var roleExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkRoleSql,
            new { RoleId = request.RequiredAssignmentRoleId },
            cancellationToken: cancellationToken));

        if (!roleExists)
        {
            return Result.Failure<UpdateTeachingDemandResponse>(DomainError.NotFound(
                "Teaching.AssignmentRoleNotFound",
                $"Assignment role with ID '{request.RequiredAssignmentRoleId}' does not exist or is inactive."));
        }

        var auditUser = currentUser.Username ?? "system";
        var workloadUnit = string.IsNullOrWhiteSpace(request.WorkloadUnit) ? "Teaching Hours" : request.WorkloadUnit.Trim();
        var campusScope = string.IsNullOrWhiteSpace(request.CampusScope) ? "Kampus Utama" : request.CampusScope.Trim();
        var requiredTeachersCount = request.RequiredTeachersCount < 1 ? 1 : request.RequiredTeachersCount;

        var updatedAt = await connection.ExecuteScalarAsync<DateTimeOffset>(new CommandDefinition(
            _updateDemandSql,
            new
            {
                Id = id,
                SourceWorkLabel = request.SourceWorkLabel.Trim(),
                RequiredCapabilityCode = request.RequiredCapabilityCode.Trim(),
                RequiredAssignmentRoleId = request.RequiredAssignmentRoleId,
                ExpectedWorkloadHours = request.ExpectedWorkloadHours,
                WorkloadUnit = workloadUnit,
                RequiresAssessmentAuthority = request.RequiresAssessmentAuthority,
                RequestedStartDate = request.RequestedStartDate,
                RequestedEndDate = request.RequestedEndDate,
                CampusScope = campusScope,
                RequiredTeachersCount = requiredTeachersCount,
                ClarificationNotes = string.IsNullOrWhiteSpace(request.ClarificationNotes) ? null : request.ClarificationNotes.Trim(),
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        return Result.Success(new UpdateTeachingDemandResponse(
            id,
            existing.DemandCode,
            existing.Status,
            updatedAt));
    }

    private sealed record DemandSummaryRow(
        Guid Id,
        string DemandCode,
        string Status);
}
