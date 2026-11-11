namespace Elsasa.Sis.Teaching.Features.TeachingDemands.CreateTeachingDemand;

using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler creating a formal teaching capacity demand and publishing TeachingDemandReceivedEvent (TD-02, P2-S04).
/// </summary>
public sealed class CreateTeachingDemandHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static CreateTeachingDemandHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkRoleSql = """
        SELECT TOP 1 1
        FROM teaching.assignment_roles
        WHERE id = @RoleId AND is_active = 1;
        """;

    private const string _checkDemandCodeSql = """
        SELECT TOP 1 1
        FROM teaching.teaching_demands
        WHERE demand_code = @DemandCode;
        """;

    private const string _countDemandCodePrefixSql = """
        SELECT COUNT(1)
        FROM teaching.teaching_demands
        WHERE demand_code LIKE @Prefix;
        """;

    private const string _insertDemandSql = """
        INSERT INTO teaching.teaching_demands (
            id,
            demand_code,
            source_domain,
            source_work_reference,
            source_work_label,
            academic_period_id,
            required_capability_code,
            required_assignment_role_id,
            expected_workload_hours,
            workload_unit,
            requires_assessment_authority,
            requested_start_date,
            requested_end_date,
            campus_scope,
            required_teachers_count,
            status,
            requested_by,
            requested_at,
            decline_reason,
            clarification_notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @DemandCode,
            @SourceDomain,
            @SourceWorkReference,
            @SourceWorkLabel,
            @AcademicPeriodId,
            @RequiredCapabilityCode,
            @RequiredAssignmentRoleId,
            @ExpectedWorkloadHours,
            @WorkloadUnit,
            @RequiresAssessmentAuthority,
            @RequestedStartDate,
            @RequestedEndDate,
            @CampusScope,
            @RequiredTeachersCount,
            'received',
            @RequestedBy,
            SYSUTCDATETIME(),
            NULL,
            @ClarificationNotes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<CreateTeachingDemandResponse>> HandleAsync(
        CreateTeachingDemandRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify required assignment role exists and is active
        var roleExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkRoleSql,
            new { RoleId = request.RequiredAssignmentRoleId },
            cancellationToken: cancellationToken));

        if (!roleExists)
        {
            return Result.Failure<CreateTeachingDemandResponse>(DomainError.NotFound(
                "Teaching.AssignmentRoleNotFound",
                $"Assignment role with ID '{request.RequiredAssignmentRoleId}' does not exist or is inactive."));
        }

        // 2. Generate or validate demand_code
        string demandCode;
        if (!string.IsNullOrWhiteSpace(request.DemandCode))
        {
            demandCode = request.DemandCode.Trim();
            var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkDemandCodeSql,
                new { DemandCode = demandCode },
                cancellationToken: cancellationToken));

            if (exists)
            {
                return Result.Failure<CreateTeachingDemandResponse>(DomainError.Conflict(
                    "Teaching.DuplicateDemandCode",
                    $"A teaching demand with code '{demandCode}' already exists."));
            }
        }
        else
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"DEM-{year}-";
            var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countDemandCodePrefixSql,
                new { Prefix = prefix + "%" },
                cancellationToken: cancellationToken));

            var seq = count + 1;
            while (true)
            {
                var candidate = $"{prefix}{seq:D4}";
                var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    _checkDemandCodeSql,
                    new { DemandCode = candidate },
                    cancellationToken: cancellationToken));

                if (!exists)
                {
                    demandCode = candidate;
                    break;
                }

                seq++;
            }
        }

        var demandId = Guid.NewGuid();
        var auditUser = currentUser.Username ?? "system";
        var workloadUnit = string.IsNullOrWhiteSpace(request.WorkloadUnit) ? "Teaching Hours" : request.WorkloadUnit.Trim();
        var campusScope = string.IsNullOrWhiteSpace(request.CampusScope) ? "Kampus Utama" : request.CampusScope.Trim();
        var requiredTeachersCount = request.RequiredTeachersCount < 1 ? 1 : request.RequiredTeachersCount;

        await connection.ExecuteAsync(new CommandDefinition(
            _insertDemandSql,
            new
            {
                Id = demandId,
                DemandCode = demandCode,
                SourceDomain = request.SourceDomain.Trim(),
                SourceWorkReference = request.SourceWorkReference,
                SourceWorkLabel = request.SourceWorkLabel.Trim(),
                AcademicPeriodId = request.AcademicPeriodId,
                RequiredCapabilityCode = request.RequiredCapabilityCode.Trim(),
                RequiredAssignmentRoleId = request.RequiredAssignmentRoleId,
                ExpectedWorkloadHours = request.ExpectedWorkloadHours,
                WorkloadUnit = workloadUnit,
                RequiresAssessmentAuthority = request.RequiresAssessmentAuthority,
                RequestedStartDate = request.RequestedStartDate,
                RequestedEndDate = request.RequestedEndDate,
                CampusScope = campusScope,
                RequiredTeachersCount = requiredTeachersCount,
                RequestedBy = auditUser,
                ClarificationNotes = string.IsNullOrWhiteSpace(request.ClarificationNotes) ? null : request.ClarificationNotes.Trim(),
                CreatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        // 3. Publish domain event
        var domainEvent = new TeachingDemandReceivedEvent(
            demandId,
            demandCode,
            request.SourceDomain.Trim(),
            request.SourceWorkReference,
            request.SourceWorkLabel.Trim(),
            request.AcademicPeriodId,
            request.RequiredCapabilityCode.Trim(),
            request.ExpectedWorkloadHours,
            requiredTeachersCount,
            auditUser);

        await publisher.Publish(domainEvent, cancellationToken);

        return Result.Success(new CreateTeachingDemandResponse(
            demandId,
            demandCode,
            "received",
            request.SourceDomain.Trim(),
            request.SourceWorkLabel.Trim(),
            DateTimeOffset.UtcNow));
    }
}
