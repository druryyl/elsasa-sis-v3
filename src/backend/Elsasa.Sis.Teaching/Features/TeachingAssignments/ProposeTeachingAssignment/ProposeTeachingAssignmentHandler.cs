namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ProposeTeachingAssignment;

using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler proposing a new teaching assignment with atomic planned workload increment and domain event publication (TD-01, TD-04, P3-S06).
/// </summary>
public sealed class ProposeTeachingAssignmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ProposeTeachingAssignmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkDemandSql = """
        SELECT TOP 1 id, status
        FROM teaching.teaching_demands
        WHERE id = @DemandId;
        """;

    private const string _checkTeacherSql = """
        SELECT TOP 1 id, standing
        FROM teaching.teacher_profiles
        WHERE id = @TeacherProfileId;
        """;

    private const string _checkRoleSql = """
        SELECT TOP 1 id, name, counts_toward_workload AS CountsTowardWorkload, is_active AS IsActive
        FROM teaching.assignment_roles
        WHERE id = @RoleId;
        """;

    private const string _checkPredecessorSql = """
        SELECT TOP 1 1
        FROM teaching.teaching_assignments
        WHERE id = @PredecessorId;
        """;

    private const string _checkAssignmentNumberSql = """
        SELECT TOP 1 1
        FROM teaching.teaching_assignments
        WHERE assignment_number = @AssignmentNumber;
        """;

    private const string _countAssignmentNumberPrefixSql = """
        SELECT COUNT(1)
        FROM teaching.teaching_assignments
        WHERE assignment_number LIKE @Prefix;
        """;

    private const string _insertAssignmentSql = """
        INSERT INTO teaching.teaching_assignments (
            id,
            assignment_number,
            teaching_demand_id,
            teacher_profile_id,
            assignment_role_id,
            assignment_scope,
            scope_reference_id,
            scope_label,
            academic_period_id,
            effective_start_date,
            effective_end_date,
            workload_commitment_value,
            workload_unit,
            status,
            approved_by,
            approved_date,
            teacher_acknowledged,
            teacher_acknowledged_date,
            predecessor_assignment_id,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @AssignmentNumber,
            @TeachingDemandId,
            @TeacherProfileId,
            @AssignmentRoleId,
            @AssignmentScope,
            @ScopeReferenceId,
            @ScopeLabel,
            @AcademicPeriodId,
            @EffectiveStartDate,
            @EffectiveEndDate,
            @WorkloadCommitmentValue,
            @WorkloadUnit,
            'proposed',
            NULL,
            NULL,
            0,
            NULL,
            @PredecessorAssignmentId,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _selectCommitmentForUpdateSql = """
        SELECT TOP 1 id, planned_workload_hours AS PlannedWorkloadHours
        FROM teaching.workload_commitments WITH (UPDLOCK, ROWLOCK)
        WHERE teacher_profile_id = @TeacherProfileId AND academic_period_id = @AcademicPeriodId;
        """;

    private const string _updateCommitmentPlannedSql = """
        UPDATE teaching.workload_commitments
        SET planned_workload_hours = planned_workload_hours + @WorkloadValue,
            as_of_timestamp = SYSUTCDATETIME(),
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @CommitmentId;
        """;

    private const string _selectActivePolicySql = """
        SELECT TOP 1 id AS Id, max_standard_capacity AS MaxStandardCapacity
        FROM teaching.workload_policies
        WHERE status = 'active'
        ORDER BY effective_start_date DESC, created_at DESC;
        """;

    private const string _insertCommitmentSql = """
        INSERT INTO teaching.workload_commitments (
            id,
            teacher_profile_id,
            academic_period_id,
            workload_policy_id,
            planned_workload_hours,
            committed_workload_hours,
            max_capacity_limit_hours,
            remaining_capacity_hours,
            status,
            active_assignment_count,
            approved_exception_count,
            as_of_timestamp,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeacherProfileId,
            @AcademicPeriodId,
            @WorkloadPolicyId,
            @PlannedWorkloadHours,
            0.00,
            @MaxCapacityLimitHours,
            @RemainingCapacityHours,
            'under_allocated',
            0,
            0,
            SYSUTCDATETIME(),
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<ProposeTeachingAssignmentResponse>> HandleAsync(
        ProposeTeachingAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify demand exists and is in staffable status
        var demand = await connection.QuerySingleOrDefaultAsync<DemandRow>(new CommandDefinition(
            _checkDemandSql,
            new { DemandId = request.TeachingDemandId },
            cancellationToken: cancellationToken));

        if (demand is null)
        {
            return Result.Failure<ProposeTeachingAssignmentResponse>(DomainError.NotFound(
                "Teaching.DemandNotFound",
                $"Teaching demand with ID '{request.TeachingDemandId}' was not found."));
        }

        if (demand.Status is "cancelled" or "declined")
        {
            return Result.Failure<ProposeTeachingAssignmentResponse>(DomainError.Conflict(
                "Teaching.DemandNotStaffable",
                $"Teaching demand is in '{demand.Status}' status and cannot accept assignment proposals."));
        }

        // 2. Verify teacher profile exists and is active
        var teacher = await connection.QuerySingleOrDefaultAsync<TeacherProfileRow>(new CommandDefinition(
            _checkTeacherSql,
            new { TeacherProfileId = request.TeacherProfileId },
            cancellationToken: cancellationToken));

        if (teacher is null)
        {
            return Result.Failure<ProposeTeachingAssignmentResponse>(DomainError.NotFound(
                "Teaching.TeacherNotFound",
                $"Teacher profile with ID '{request.TeacherProfileId}' was not found."));
        }

        if (!string.Equals(teacher.Standing, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ProposeTeachingAssignmentResponse>(DomainError.Conflict(
                "Teaching.TeacherNotActive",
                $"Teacher profile has standing '{teacher.Standing}'. Only active faculty members can be assigned."));
        }

        // 3. Verify assignment role exists and is active
        var role = await connection.QuerySingleOrDefaultAsync<AssignmentRoleRow>(new CommandDefinition(
            _checkRoleSql,
            new { RoleId = request.AssignmentRoleId },
            cancellationToken: cancellationToken));

        if (role is null || !role.IsActive)
        {
            return Result.Failure<ProposeTeachingAssignmentResponse>(DomainError.NotFound(
                "Teaching.AssignmentRoleNotFound",
                $"Assignment role with ID '{request.AssignmentRoleId}' does not exist or is inactive."));
        }

        // 4. Verify predecessor assignment if provided
        if (request.PredecessorAssignmentId.HasValue && request.PredecessorAssignmentId.Value != Guid.Empty)
        {
            var predecessorExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkPredecessorSql,
                new { PredecessorId = request.PredecessorAssignmentId.Value },
                cancellationToken: cancellationToken));

            if (!predecessorExists)
            {
                return Result.Failure<ProposeTeachingAssignmentResponse>(DomainError.NotFound(
                    "Teaching.PredecessorAssignmentNotFound",
                    $"Predecessor assignment with ID '{request.PredecessorAssignmentId.Value}' was not found."));
            }
        }

        // 5. Generate or validate unique assignment number
        string assignmentNumber;
        if (!string.IsNullOrWhiteSpace(request.AssignmentNumber))
        {
            assignmentNumber = request.AssignmentNumber.Trim();
            var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkAssignmentNumberSql,
                new { AssignmentNumber = assignmentNumber },
                cancellationToken: cancellationToken));

            if (exists)
            {
                return Result.Failure<ProposeTeachingAssignmentResponse>(DomainError.Conflict(
                    "Teaching.DuplicateAssignmentNumber",
                    $"A teaching assignment with number '{assignmentNumber}' already exists."));
            }
        }
        else
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"ASN-{year}-";
            var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countAssignmentNumberPrefixSql,
                new { Prefix = prefix + "%" },
                cancellationToken: cancellationToken));

            var seq = count + 1;
            while (true)
            {
                var candidate = $"{prefix}{seq:D4}";
                var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    _checkAssignmentNumberSql,
                    new { AssignmentNumber = candidate },
                    cancellationToken: cancellationToken));

                if (!exists)
                {
                    assignmentNumber = candidate;
                    break;
                }

                seq++;
            }
        }

        var assignmentId = Guid.NewGuid();
        var auditUser = currentUser.Username ?? "system";
        var assignmentScope = request.AssignmentScope.Trim().ToLowerInvariant();
        var workloadUnit = string.IsNullOrWhiteSpace(request.WorkloadUnit) ? "Teaching Hours" : request.WorkloadUnit.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        // 6. Execute atomic multi-table write within a single transaction
        if (connection is SqlConnection sqlConn && sqlConn.State != System.Data.ConnectionState.Open)
        {
            await sqlConn.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // 6a. Insert teaching assignment
            await connection.ExecuteAsync(new CommandDefinition(
                _insertAssignmentSql,
                new
                {
                    Id = assignmentId,
                    AssignmentNumber = assignmentNumber,
                    TeachingDemandId = request.TeachingDemandId,
                    TeacherProfileId = request.TeacherProfileId,
                    AssignmentRoleId = request.AssignmentRoleId,
                    AssignmentScope = assignmentScope,
                    ScopeReferenceId = request.ScopeReferenceId,
                    ScopeLabel = request.ScopeLabel.Trim(),
                    AcademicPeriodId = request.AcademicPeriodId,
                    EffectiveStartDate = request.EffectiveStartDate,
                    EffectiveEndDate = request.EffectiveEndDate,
                    WorkloadCommitmentValue = request.WorkloadCommitmentValue,
                    WorkloadUnit = workloadUnit,
                    PredecessorAssignmentId = request.PredecessorAssignmentId,
                    Notes = notes,
                    CreatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 6b. Atomically increment planned_workload_hours in teaching.workload_commitments if role counts toward workload
            if (role.CountsTowardWorkload && request.WorkloadCommitmentValue > 0)
            {
                var commitment = await connection.QuerySingleOrDefaultAsync<CommitmentRow>(new CommandDefinition(
                    _selectCommitmentForUpdateSql,
                    new
                    {
                        TeacherProfileId = request.TeacherProfileId,
                        AcademicPeriodId = request.AcademicPeriodId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                if (commitment is not null)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        _updateCommitmentPlannedSql,
                        new
                        {
                            CommitmentId = commitment.Id,
                            WorkloadValue = request.WorkloadCommitmentValue,
                            UpdatedBy = auditUser
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                }
                else
                {
                    var policy = await connection.QuerySingleOrDefaultAsync<WorkloadPolicyRow>(new CommandDefinition(
                        _selectActivePolicySql,
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                    if (policy is not null)
                    {
                        await connection.ExecuteAsync(new CommandDefinition(
                            _insertCommitmentSql,
                            new
                            {
                                Id = Guid.NewGuid(),
                                TeacherProfileId = request.TeacherProfileId,
                                AcademicPeriodId = request.AcademicPeriodId,
                                WorkloadPolicyId = policy.Id,
                                PlannedWorkloadHours = request.WorkloadCommitmentValue,
                                MaxCapacityLimitHours = policy.MaxStandardCapacity,
                                RemainingCapacityHours = policy.MaxStandardCapacity,
                                CreatedBy = auditUser
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                    }
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        // 7. Publish domain event
        var domainEvent = new TeachingAssignmentProposedEvent(
            assignmentId,
            assignmentNumber,
            request.TeachingDemandId,
            request.TeacherProfileId,
            request.AssignmentRoleId,
            assignmentScope,
            request.ScopeReferenceId,
            request.AcademicPeriodId,
            request.WorkloadCommitmentValue,
            auditUser);

        await publisher.Publish(domainEvent, cancellationToken);

        return Result.Success(new ProposeTeachingAssignmentResponse(
            assignmentId,
            assignmentNumber,
            "proposed",
            request.TeachingDemandId,
            request.TeacherProfileId,
            request.WorkloadCommitmentValue,
            DateTimeOffset.UtcNow));
    }

    private sealed record DemandRow(Guid Id, string Status);
    private sealed record TeacherProfileRow(Guid Id, string Standing);
    private sealed record AssignmentRoleRow(Guid Id, string Name, bool CountsTowardWorkload, bool IsActive);
    private sealed record CommitmentRow(Guid Id, decimal PlannedWorkloadHours);
    private sealed record WorkloadPolicyRow(Guid Id, decimal MaxStandardCapacity);
}
