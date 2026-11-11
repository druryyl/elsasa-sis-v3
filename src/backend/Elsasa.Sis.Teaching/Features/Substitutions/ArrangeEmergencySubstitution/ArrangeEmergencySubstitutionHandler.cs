namespace Elsasa.Sis.Teaching.Features.Substitutions.ArrangeEmergencySubstitution;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler implementing Pre-Authorized Pool Delegation (Resolution 3.8) for emergency substitutions
/// with exception escalation to Teaching Authority (TD-05, P4-S11).
/// </summary>
public sealed class ArrangeEmergencySubstitutionHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static ArrangeEmergencySubstitutionHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssignmentSql = """
        SELECT
            ta.id AS Id,
            ta.assignment_number AS AssignmentNumber,
            ta.teacher_profile_id AS OriginalTeacherId,
            ta.teaching_demand_id AS TeachingDemandId,
            ta.academic_period_id AS AcademicPeriodId,
            ta.assignment_scope AS AssignmentScope,
            ta.scope_reference_id AS ScopeReferenceId,
            ta.scope_label AS ScopeLabel,
            ta.workload_commitment_value AS WorkloadCommitmentValue,
            ta.status AS Status,
            td.required_capability_code AS RequiredCapabilityCode,
            td.campus_scope AS CampusScope
        FROM teaching.teaching_assignments ta WITH (UPDLOCK, ROWLOCK)
        LEFT JOIN teaching.teaching_demands td ON td.id = ta.teaching_demand_id
        WHERE ta.id = @OriginalAssignmentId;
        """;

    private const string _selectSubstituteProfileSql = """
        SELECT
            id AS Id,
            standing AS Standing,
            full_name AS FullName
        FROM teaching.teacher_profiles WITH (UPDLOCK, ROWLOCK)
        WHERE id = @SubstituteTeacherId;
        """;

    private const string _countMatchingAuthorizationsSql = """
        SELECT COUNT(1)
        FROM teaching.teaching_authorizations ta
        LEFT JOIN teaching.teaching_capabilities tc ON tc.id = ta.authorized_capability_id
        WHERE ta.teacher_profile_id = @SubstituteTeacherId
          AND ta.status IN ('active', 'authorized_under_supervision')
          AND (
              @RequiredCapabilityCode IS NULL
              OR @RequiredCapabilityCode = ''
              OR tc.capability_code = @RequiredCapabilityCode
              OR ta.scope_description LIKE '%' + @RequiredCapabilityCode + '%'
          )
          AND (
              ta.campus_scope = 'All'
              OR @CampusScope IS NULL
              OR @CampusScope = 'All'
              OR ta.campus_scope = @CampusScope
          )
          AND ta.effective_start_date <= @StartDate
          AND (ta.effective_end_date IS NULL OR ta.effective_end_date >= @EndDate);
        """;

    private const string _selectActiveWorkloadPolicySql = """
        SELECT TOP 1
            wp.id AS Id,
            wp.max_standard_capacity AS MaxStandardCapacity,
            wp.warning_threshold AS WarningThreshold,
            wp.prohibited_threshold AS ProhibitedThreshold
        FROM teaching.workload_policies wp
        WHERE wp.status = 'active'
        ORDER BY wp.effective_start_date DESC, wp.created_at DESC;
        """;

    private const string _selectWorkloadCommitmentSql = """
        SELECT
            wc.committed_workload_hours AS CommittedWorkloadHours
        FROM teaching.workload_commitments wc WITH (UPDLOCK, ROWLOCK)
        WHERE wc.teacher_profile_id = @SubstituteTeacherId
          AND wc.academic_period_id = @AcademicPeriodId;
        """;

    private const string _sumApprovedAssignmentsHoursSql = """
        SELECT ISNULL(SUM(ta.workload_commitment_value), 0.00)
        FROM teaching.teaching_assignments ta
        INNER JOIN teaching.assignment_roles ar ON ar.id = ta.assignment_role_id
        WHERE ta.teacher_profile_id = @SubstituteTeacherId
          AND ta.academic_period_id = @AcademicPeriodId
          AND ta.status = 'approved'
          AND ar.counts_toward_workload = 1;
        """;

    private const string _countSubstitutionNumberPrefixSql = """
        SELECT COUNT(1)
        FROM teaching.teaching_substitutions
        WHERE substitution_number LIKE @Prefix;
        """;

    private const string _checkSubstitutionNumberSql = """
        SELECT TOP 1 1
        FROM teaching.teaching_substitutions
        WHERE substitution_number = @SubstitutionNumber;
        """;

    private const string _insertSubstitutionSql = """
        INSERT INTO teaching.teaching_substitutions (
            id,
            substitution_number,
            original_assignment_id,
            original_teacher_id,
            substitute_teacher_id,
            coverage_start_date,
            coverage_end_date,
            coverage_scope_type,
            partial_scope_details,
            reason,
            workload_impact_hours,
            status,
            handover_notes,
            approved_by,
            approved_date,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @SubstitutionNumber,
            @OriginalAssignmentId,
            @OriginalTeacherId,
            @SubstituteTeacherId,
            @CoverageStartDate,
            @CoverageEndDate,
            @CoverageScopeType,
            @PartialScopeDetails,
            @Reason,
            @WorkloadImpactHours,
            @Status,
            @HandoverNotes,
            @ApprovedBy,
            @ApprovedDate,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _insertCapacityConflictSql = """
        INSERT INTO teaching.capacity_conflicts (
            id,
            teaching_assignment_id,
            teaching_demand_id,
            teacher_profile_id,
            conflict_type,
            conflict_classification,
            severity,
            description,
            detected_date,
            status,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeachingAssignmentId,
            @TeachingDemandId,
            @TeacherProfileId,
            @ConflictType,
            'substitution_exception',
            'blocking',
            @Description,
            @DetectedDate,
            'unresolved',
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _insertTeachingHistorySql = """
        INSERT INTO teaching.teaching_history_entries (
            id,
            teacher_profile_id,
            event_type,
            entity_type,
            entity_id,
            effective_date,
            event_timestamp,
            actor_role,
            actor_id,
            event_summary,
            change_payload,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeacherProfileId,
            @EventType,
            @EntityType,
            @EntityId,
            @EffectiveDate,
            SYSUTCDATETIME(),
            @ActorRole,
            @ActorId,
            @EventSummary,
            @ChangePayload,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<ArrangeEmergencySubstitutionResponse>> HandleAsync(
        ArrangeEmergencySubstitutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.OriginalAssignmentId == Guid.Empty)
        {
            return Result.Failure<ArrangeEmergencySubstitutionResponse>(DomainError.Validation(
                "Teaching.InvalidAssignmentId",
                "Original assignment ID cannot be empty."));
        }

        if (request.SubstituteTeacherId == Guid.Empty)
        {
            return Result.Failure<ArrangeEmergencySubstitutionResponse>(DomainError.Validation(
                "Teaching.InvalidSubstituteId",
                "Substitute teacher ID cannot be empty."));
        }

        if (request.StartDate > request.EndDate)
        {
            return Result.Failure<ArrangeEmergencySubstitutionResponse>(DomainError.Validation(
                "Teaching.InvalidDateRange",
                "Start date cannot be after end date."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        if (connection is not SqlConnection sqlConnection)
        {
            return Result.Failure<ArrangeEmergencySubstitutionResponse>(DomainError.Failure(
                "Teaching.InvalidConnection",
                "Expected a SQL Server connection for transactional operations."));
        }

        await using var tx = (SqlTransaction)await sqlConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Fetch original assignment with exclusive lock
            var original = await connection.QuerySingleOrDefaultAsync<OriginalAssignmentRecord>(new CommandDefinition(
                _selectAssignmentSql,
                new { OriginalAssignmentId = request.OriginalAssignmentId },
                transaction: tx,
                cancellationToken: cancellationToken));

            if (original is null)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure<ArrangeEmergencySubstitutionResponse>(DomainError.NotFound(
                    "Teaching.OriginalAssignmentNotFound",
                    $"Original teaching assignment with ID '{request.OriginalAssignmentId}' was not found."));
            }

            if (original.Status != "approved")
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure<ArrangeEmergencySubstitutionResponse>(DomainError.Conflict(
                    "Teaching.AssignmentNotApproved",
                    $"Emergency substitution can only be arranged for approved assignments. Current status is '{original.Status}'."));
            }

            if (original.OriginalTeacherId == request.SubstituteTeacherId)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure<ArrangeEmergencySubstitutionResponse>(DomainError.Conflict(
                    "Teaching.SelfSubstitutionNotAllowed",
                    "A teacher cannot be arranged as an emergency substitute for their own assignment."));
            }

            // 2. Fetch proposed substitute teacher profile with lock
            var substituteProfile = await connection.QuerySingleOrDefaultAsync<SubstituteProfileRecord>(new CommandDefinition(
                _selectSubstituteProfileSql,
                new { SubstituteTeacherId = request.SubstituteTeacherId },
                transaction: tx,
                cancellationToken: cancellationToken));

            if (substituteProfile is null)
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure<ArrangeEmergencySubstitutionResponse>(DomainError.NotFound(
                    "Teaching.SubstituteProfileNotFound",
                    $"Substitute teacher profile with ID '{request.SubstituteTeacherId}' was not found."));
            }

            if (substituteProfile.Standing != "active")
            {
                await tx.RollbackAsync(cancellationToken);
                return Result.Failure<ArrangeEmergencySubstitutionResponse>(DomainError.Conflict(
                    "Teaching.SubstituteProfileInactive",
                    $"Substitute teacher '{substituteProfile.FullName}' must have 'active' standing. Current standing is '{substituteProfile.Standing}'."));
            }

            // 3. Determine workload impact hours
            var workloadImpactHours = request.EffectiveHours > 0m
                ? request.EffectiveHours
                : original.WorkloadCommitmentValue;

            // 4. Resolution 3.8 Pre-Authorized Pool Check 1: Active matching authorization
            var authCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countMatchingAuthorizationsSql,
                new
                {
                    SubstituteTeacherId = request.SubstituteTeacherId,
                    RequiredCapabilityCode = original.RequiredCapabilityCode ?? string.Empty,
                    CampusScope = original.CampusScope ?? "All",
                    StartDate = request.StartDate,
                    EndDate = request.EndDate
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            var hasMatchingAuthorization = authCount > 0;

            // 5. Resolution 3.8 Pre-Authorized Pool Check 2: Workload capacity & prohibited threshold
            var policy = await connection.QuerySingleOrDefaultAsync<WorkloadPolicyRecord>(new CommandDefinition(
                _selectActiveWorkloadPolicySql,
                transaction: tx,
                cancellationToken: cancellationToken));

            var prohibitedThreshold = policy?.ProhibitedThreshold ?? 28.00m;

            var existingCommitmentHours = await connection.QuerySingleOrDefaultAsync<decimal?>(new CommandDefinition(
                _selectWorkloadCommitmentSql,
                new
                {
                    SubstituteTeacherId = request.SubstituteTeacherId,
                    AcademicPeriodId = original.AcademicPeriodId
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            var currentCommittedHours = existingCommitmentHours ?? await connection.ExecuteScalarAsync<decimal>(new CommandDefinition(
                _sumApprovedAssignmentsHoursSql,
                new
                {
                    SubstituteTeacherId = request.SubstituteTeacherId,
                    AcademicPeriodId = original.AcademicPeriodId
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            var projectedWorkload = currentCommittedHours + workloadImpactHours;
            var isWithinCapacity = projectedWorkload <= prohibitedThreshold;

            var passesPoolDelegation = hasMatchingAuthorization && isWithinCapacity;

            // 6. Generate sequential substitution number SUB-YYYY-XXXX
            var year = DateTime.UtcNow.Year;
            var prefix = $"SUB-{year}-";
            var existingCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countSubstitutionNumberPrefixSql,
                new { Prefix = prefix + "%" },
                transaction: tx,
                cancellationToken: cancellationToken));

            var seq = existingCount + 1;
            string substitutionNumber;
            while (true)
            {
                var candidate = $"{prefix}{seq:D4}";
                var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                    _checkSubstitutionNumberSql,
                    new { SubstitutionNumber = candidate },
                    transaction: tx,
                    cancellationToken: cancellationToken));

                if (!exists)
                {
                    substitutionNumber = candidate;
                    break;
                }

                seq++;
            }

            var substitutionId = Guid.NewGuid();
            var actorUser = currentUser.Username ?? "coordinator";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            string status;
            string approvedBy;
            DateOnly approvedDate;
            bool isDelegatedApproval;
            bool requiresAuthorityApproval;
            Guid? capacityConflictId = null;

            if (passesPoolDelegation)
            {
                // Direct delegation path (Resolution 3.8)
                status = "approved";
                approvedBy = actorUser;
                approvedDate = today;
                isDelegatedApproval = true;
                requiresAuthorityApproval = false;

                // Append history entry for substitute
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertTeachingHistorySql,
                    new
                    {
                        Id = Guid.NewGuid(),
                        TeacherProfileId = request.SubstituteTeacherId,
                        EventType = "SUBSTITUTION_APPROVED",
                        EntityType = "teaching_substitution",
                        EntityId = substitutionId,
                        EffectiveDate = request.StartDate,
                        ActorRole = "Teaching Coordinator",
                        ActorId = actorUser,
                        EventSummary = $"Emergency substitution {substitutionNumber} arranged for assignment {original.AssignmentNumber}. Substitute committed under Pre-Authorized Pool Delegation (Resolution 3.8).",
                        ChangePayload = JsonSerializer.Serialize(new
                        {
                            SubstitutionId = substitutionId,
                            SubstitutionNumber = substitutionNumber,
                            OriginalAssignmentId = original.Id,
                            OriginalAssignmentNumber = original.AssignmentNumber,
                            WorkloadImpactHours = workloadImpactHours,
                            DelegatedApproval = true
                        }),
                        Notes = request.Reason,
                        CreatedBy = actorUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));
            }
            else
            {
                // Exception escalation path
                status = "proposed";
                approvedBy = "PENDING";
                approvedDate = today;
                isDelegatedApproval = false;
                requiresAuthorityApproval = true;

                capacityConflictId = Guid.NewGuid();
                string conflictType;
                string conflictDescription;

                if (!hasMatchingAuthorization && !isWithinCapacity)
                {
                    conflictType = "UNAUTHORIZED_AND_CAPACITY_BREACH";
                    conflictDescription = $"Candidate substitute lacks active authorization for '{original.RequiredCapabilityCode ?? "general"}' and temporary substitution ({workloadImpactHours:F2} hrs) causes projected workload ({projectedWorkload:F2} hrs) to breach prohibited ceiling ({prohibitedThreshold:F2} hrs).";
                }
                else if (!hasMatchingAuthorization)
                {
                    conflictType = "UNAUTHORIZED_SUBSTITUTE";
                    conflictDescription = $"Candidate substitute lacks active matching authorization for capability '{original.RequiredCapabilityCode ?? "general"}' and campus '{original.CampusScope ?? "All"}'.";
                }
                else
                {
                    conflictType = "WORKLOAD_CAPACITY_BREACH";
                    conflictDescription = $"Temporary substitution ({workloadImpactHours:F2} hrs) causes projected workload ({projectedWorkload:F2} hrs) to breach prohibited capacity ceiling ({prohibitedThreshold:F2} hrs).";
                }

                // Insert blocking capacity conflict
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertCapacityConflictSql,
                    new
                    {
                        Id = capacityConflictId.Value,
                        TeachingAssignmentId = original.Id,
                        TeachingDemandId = original.TeachingDemandId,
                        TeacherProfileId = request.SubstituteTeacherId,
                        ConflictType = conflictType,
                        Description = conflictDescription,
                        DetectedDate = today,
                        CreatedBy = actorUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

                // Append history entry for escalation
                await connection.ExecuteAsync(new CommandDefinition(
                    _insertTeachingHistorySql,
                    new
                    {
                        Id = Guid.NewGuid(),
                        TeacherProfileId = request.SubstituteTeacherId,
                        EventType = "SUBSTITUTION_ESCALATED",
                        EntityType = "teaching_substitution",
                        EntityId = substitutionId,
                        EffectiveDate = request.StartDate,
                        ActorRole = "Teaching Coordinator",
                        ActorId = actorUser,
                        EventSummary = $"Emergency substitution {substitutionNumber} escalated to Teaching Authority due to blocking conflict: {conflictDescription}",
                        ChangePayload = JsonSerializer.Serialize(new
                        {
                            SubstitutionId = substitutionId,
                            SubstitutionNumber = substitutionNumber,
                            ConflictId = capacityConflictId.Value,
                            ConflictType = conflictType,
                            WorkloadImpactHours = workloadImpactHours,
                            RequiresAuthorityApproval = true
                        }),
                        Notes = request.Reason,
                        CreatedBy = actorUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));
            }

            // 7. Insert teaching.teaching_substitutions row
            await connection.ExecuteAsync(new CommandDefinition(
                _insertSubstitutionSql,
                new
                {
                    Id = substitutionId,
                    SubstitutionNumber = substitutionNumber,
                    OriginalAssignmentId = original.Id,
                    OriginalTeacherId = original.OriginalTeacherId,
                    SubstituteTeacherId = request.SubstituteTeacherId,
                    CoverageStartDate = request.StartDate,
                    CoverageEndDate = request.EndDate,
                    CoverageScopeType = request.CoverageScopeType.Trim().ToLowerInvariant(),
                    PartialScopeDetails = request.PartialScopeDetails,
                    Reason = request.Reason.Trim(),
                    WorkloadImpactHours = workloadImpactHours,
                    Status = status,
                    HandoverNotes = request.HandoverNotes,
                    ApprovedBy = approvedBy,
                    ApprovedDate = approvedDate,
                    CreatedBy = actorUser
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);

            // 8. Publish post-commit domain event if committed under direct delegation
            if (status == "approved")
            {
                await publisher.Publish(new TeachingSubstitutionAssignedEvent(
                    substitutionId: substitutionId,
                    substitutionNumber: substitutionNumber,
                    originalAssignmentId: original.Id,
                    originalTeacherId: original.OriginalTeacherId,
                    substituteTeacherId: request.SubstituteTeacherId,
                    coverageStartDate: request.StartDate,
                    coverageEndDate: request.EndDate,
                    workloadImpactHours: workloadImpactHours,
                    approvedBy: approvedBy,
                    isDelegatedApproval: true),
                    cancellationToken);
            }

            return Result.Success(new ArrangeEmergencySubstitutionResponse(
                Id: substitutionId,
                SubstitutionNumber: substitutionNumber,
                OriginalAssignmentId: original.Id,
                OriginalTeacherId: original.OriginalTeacherId,
                SubstituteTeacherId: request.SubstituteTeacherId,
                CoverageStartDate: request.StartDate,
                CoverageEndDate: request.EndDate,
                CoverageScopeType: request.CoverageScopeType.Trim().ToLowerInvariant(),
                PartialScopeDetails: request.PartialScopeDetails,
                Reason: request.Reason.Trim(),
                WorkloadImpactHours: workloadImpactHours,
                Status: status,
                HandoverNotes: request.HandoverNotes,
                ApprovedBy: approvedBy,
                ApprovedDate: approvedDate,
                IsDelegatedApproval: isDelegatedApproval,
                RequiresAuthorityApproval: requiresAuthorityApproval,
                CapacityConflictId: capacityConflictId,
                CreatedAt: DateTimeOffset.UtcNow));
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed record OriginalAssignmentRecord(
        Guid Id,
        string AssignmentNumber,
        Guid OriginalTeacherId,
        Guid? TeachingDemandId,
        Guid AcademicPeriodId,
        string AssignmentScope,
        Guid ScopeReferenceId,
        string ScopeLabel,
        decimal WorkloadCommitmentValue,
        string Status,
        string? RequiredCapabilityCode,
        string? CampusScope);

    private sealed record SubstituteProfileRecord(
        Guid Id,
        string Standing,
        string FullName);

    private sealed record WorkloadPolicyRecord(
        Guid Id,
        decimal MaxStandardCapacity,
        decimal WarningThreshold,
        decimal ProhibitedThreshold);
}
