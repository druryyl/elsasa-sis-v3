namespace Elsasa.Sis.IntegrationTests.Features.Teaching;

using System.Net;
using System.Net.Http.Json;
using Dapper;
using Elsasa.Sis.Api.Features.DemoData;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Teaching.Features.AssignmentCompletions.ConcludeTeachingAssignment;
using Elsasa.Sis.Teaching.Features.Reassignments.ApprovePermanentReassignment;
using Elsasa.Sis.Teaching.Features.Reassignments.ConfirmReassignmentHandover;
using Elsasa.Sis.Teaching.Features.Reassignments.ProposePermanentReassignment;
using Elsasa.Sis.Teaching.Features.Substitutions.ApproveSubstitutionException;
using Elsasa.Sis.Teaching.Features.Substitutions.ArrangeEmergencySubstitution;
using Elsasa.Sis.Teaching.Features.Substitutions.CancelSubstitution;
using Elsasa.Sis.Teaching.Features.Workload.CancelUnavailabilityWindow;
using Elsasa.Sis.Teaching.Features.Workload.CreateUnavailabilityWindow;
using Elsasa.Sis.Teaching.Features.Workload.DeclareTeacherAvailability;
using Elsasa.Sis.Teaching.Features.Workload.RecalculatePeriodWorkload;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Integration test suite verifying Workload commitments, on-demand period recalculation,
/// Pre-Authorized Pool Delegation vs exception escalation (Resolution 3.8), Permanent reassignments
/// with handover, Assignment completions releasing capacity atomically, and two-phase demo reset
/// (P6-S20, Criteria 7 &amp; 8).
/// </summary>
public sealed class TeachingWorkloadAndSubstitutionsIntegrationTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private const string _teachersUri = "/api/v1/teaching/teachers";
    private const string _unavailabilityUri = "/api/v1/teaching/unavailability";
    private const string _workloadUri = "/api/v1/teaching/workload";
    private const string _substitutionsUri = "/api/v1/teaching/substitutions";
    private const string _reassignmentsUri = "/api/v1/teaching/reassignments";
    private const string _assignmentsUri = "/api/v1/teaching/assignments";

    private sealed record AvailabilityDbRecord(
        Guid Id,
        Guid TeacherProfileId,
        Guid AcademicPeriodId,
        decimal MaxAvailableHours,
        DateOnly EffectiveStartDate,
        DateOnly EffectiveEndDate,
        string CampusScope,
        bool IsActive);

    private sealed record UnavailabilityDbRecord(
        Guid Id,
        Guid TeacherProfileId,
        DateOnly StartDate,
        DateOnly EndDate,
        string UnavailabilityType,
        string Reason,
        string Status);

    private sealed record WorkloadCommitmentDbRecord(
        Guid Id,
        decimal PlannedWorkloadHours,
        decimal CommittedWorkloadHours,
        decimal MaxCapacityLimitHours,
        decimal RemainingCapacityHours,
        int ActiveAssignmentCount,
        string CommitmentStatus);

    private sealed record SubstitutionDbRecord(
        Guid Id,
        string SubstitutionNumber,
        Guid OriginalAssignmentId,
        Guid SubstituteTeacherProfileId,
        DateOnly StartDate,
        DateOnly EndDate,
        decimal WorkloadImpactHours,
        string Status,
        string ApprovedBy,
        DateOnly? ApprovedDate);

    private sealed record CapacityConflictDbRecord(
        Guid Id,
        string Severity,
        string Status,
        string ConflictType,
        Guid? TeacherProfileId,
        Guid? TeachingAssignmentId,
        string? ResolutionAction);

    private sealed record AssignmentDbRecord(
        Guid Id,
        string AssignmentNumber,
        string Status,
        Guid? PredecessorAssignmentId,
        decimal WorkloadCommitmentValue);

    private sealed record CompletionDbRecord(
        Guid Id,
        Guid TeachingAssignmentId,
        DateOnly CompletionDate,
        bool AcademicConfirmationReceived,
        bool HandoverConfirmed,
        decimal ReleasedWorkloadHours,
        string Status);

    #region 1. Teacher Availability & Unavailability Windows Tests

    [SkippableFact]
    public async Task DeclareTeacherAvailability_Should_PersistActiveAvailability_And_SupportSubsequentQueries()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");
        var academicPeriodId = Guid.NewGuid();

        var request = new DeclareTeacherAvailabilityRequest(
            AcademicPeriodId: academicPeriodId,
            MaxAvailableHours: 20.00m,
            EffectiveStartDate: new DateOnly(2026, 8, 1),
            EffectiveEndDate: new DateOnly(2026, 12, 15),
            CampusScope: "Kampus Utama",
            AvailableDeliveryModes: "in_person",
            RecurringAvailableSchedule: "{\"monday\":[\"morning\",\"afternoon\"]}",
            Notes: "Prefers morning slots");

        // Act 1: Declare Availability
        var response = await Client.PostAsJsonAsync($"{_teachersUri}/{teacherId}/availability", request);

        // Assert 1
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<DeclareTeacherAvailabilityResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.TeacherProfileId.Should().Be(teacherId);
        created.AcademicPeriodId.Should().Be(academicPeriodId);
        created.MaxAvailableHours.Should().Be(20.00m);
        created.Status.Should().Be("active");

        // Verify database persistence
        const string checkSql = """
            SELECT id AS Id, teacher_profile_id AS TeacherProfileId, academic_period_id AS AcademicPeriodId,
                   max_available_hours AS MaxAvailableHours, effective_start_date AS EffectiveStartDate,
                   effective_end_date AS EffectiveEndDate, campus_scope AS CampusScope, is_active AS IsActive
            FROM teaching.teacher_availabilities
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<AvailabilityDbRecord>(checkSql, new { created.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.MaxAvailableHours.Should().Be(20.00m);
        dbRecord.IsActive.Should().BeTrue();
        dbRecord.CampusScope.Should().Be("Kampus Utama");

        // Act 2: Query availability via GET endpoint
        var queryResp = await Client.GetAsync($"{_teachersUri}/{teacherId}/availability?academicPeriodId={academicPeriodId}");
        queryResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var availabilityDto = await queryResp.Content.ReadFromJsonAsync<TeacherAvailabilityDto>();
        availabilityDto.Should().NotBeNull();
        availabilityDto!.Id.Should().Be(created.Id);
        availabilityDto.MaxAvailableHours.Should().Be(20.00m);
    }

    [SkippableFact]
    public async Task CreateUnavailabilityWindow_Should_RecordLeaveWindow_And_SupportCancellation()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");

        var createRequest = new CreateUnavailabilityWindowRequest(
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2026, 9, 10),
            UnavailabilityType: "medical_leave",
            Reason: "Post-surgery convalescence",
            EmergencyCoverageRequired: true,
            Notes: "Requires substitute teacher coverage for 10 days");

        // Act 1: Create Unavailability Window
        var response = await Client.PostAsJsonAsync($"{_teachersUri}/{teacherId}/unavailability", createRequest);

        // Assert 1
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateUnavailabilityWindowResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.TeacherProfileId.Should().Be(teacherId);
        created.UnavailabilityType.Should().Be("medical_leave");
        created.Status.Should().Be("active");

        // Verify database persistence
        const string checkSql = """
            SELECT id AS Id, teacher_profile_id AS TeacherProfileId, start_date AS StartDate,
                   end_date AS EndDate, unavailability_type AS UnavailabilityType,
                   reason AS Reason, status AS Status
            FROM teaching.teacher_unavailability_windows
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<UnavailabilityDbRecord>(checkSql, new { created.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.Status.Should().Be("active");
        dbRecord.UnavailabilityType.Should().Be("medical_leave");

        // Act 2: Cancel Unavailability Window
        var cancelResp = await Client.PostAsync($"{_unavailabilityUri}/{created.Id}/cancel", content: null);
        cancelResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelResult = await cancelResp.Content.ReadFromJsonAsync<CancelUnavailabilityWindowResponse>();
        cancelResult.Should().NotBeNull();
        cancelResult!.Status.Should().Be("cancelled");

        // Verify updated database state
        var dbCancelled = await QuerySingleOrDefaultAsync<UnavailabilityDbRecord>(checkSql, new { created.Id });
        dbCancelled.Should().NotBeNull();
        dbCancelled!.Status.Should().Be("cancelled");
    }

    #endregion

    #region 2. On-Demand Workload Period Recalculation Tests

    [SkippableFact]
    public async Task RecalculatePeriodWorkload_Should_ReconcilePlannedAndCommittedHours_And_CorrectDrift()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);
        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");

        // Deliberate drift: baseline record says 0.00 planned, 0.00 committed, 0 active count
        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, teacherId, academicPeriodId, plannedHours: 0.00m, committedHours: 0.00m, activeCount: 0);

        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);

        // 1 Approved assignment (5.00 hours)
        var approvedAsnId = Guid.NewGuid();
        const string insertApprovedAsn = """
            INSERT INTO teaching.teaching_assignments (
                id, assignment_number, teaching_demand_id, teacher_profile_id,
                assignment_role_id, assignment_scope, scope_reference_id, scope_label,
                academic_period_id, effective_start_date, effective_end_date,
                workload_commitment_value, workload_unit, status, teacher_acknowledged,
                created_at, created_by
            ) VALUES (
                @Id, 'ASN-RECALC-0001', @DemandId, @TeacherId,
                @RoleId, 'learning_unit', NEWID(), 'Matematika SD Kelas 5',
                @PeriodId, '2026-08-01', '2026-12-15',
                5.00, 'Teaching Hours', 'approved', 1,
                SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertApprovedAsn, new
        {
            Id = approvedAsnId,
            DemandId = demandId,
            TeacherId = teacherId,
            RoleId = assignmentRoleId,
            PeriodId = academicPeriodId
        });

        // 1 Proposed assignment (3.00 hours)
        var proposedAsnId = Guid.NewGuid();
        const string insertProposedAsn = """
            INSERT INTO teaching.teaching_assignments (
                id, assignment_number, teaching_demand_id, teacher_profile_id,
                assignment_role_id, assignment_scope, scope_reference_id, scope_label,
                academic_period_id, effective_start_date, effective_end_date,
                workload_commitment_value, workload_unit, status, teacher_acknowledged,
                created_at, created_by
            ) VALUES (
                @Id, 'ASN-RECALC-0002', @DemandId, @TeacherId,
                @RoleId, 'learning_unit', NEWID(), 'Matematika Tambahan',
                @PeriodId, '2026-08-01', '2026-12-15',
                3.00, 'Teaching Hours', 'proposed', 0,
                SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertProposedAsn, new
        {
            Id = proposedAsnId,
            DemandId = demandId,
            TeacherId = teacherId,
            RoleId = assignmentRoleId,
            PeriodId = academicPeriodId
        });

        var recalcRequest = new RecalculatePeriodWorkloadRequest(academicPeriodId);

        // Act
        var response = await Client.PostAsJsonAsync($"{_workloadUri}/recalculate", recalcRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var recalcResult = await response.Content.ReadFromJsonAsync<RecalculatePeriodWorkloadResponse>();
        recalcResult.Should().NotBeNull();
        recalcResult!.RecalculatedCount.Should().BeGreaterOrEqualTo(1);

        // Verify corrected database commitment values
        const string wcomSql = """
            SELECT id AS Id, planned_workload_hours AS PlannedWorkloadHours, committed_workload_hours AS CommittedWorkloadHours,
                   max_capacity_limit_hours AS MaxCapacityLimitHours, remaining_capacity_hours AS RemainingCapacityHours,
                   active_assignment_count AS ActiveAssignmentCount, commitment_status AS CommitmentStatus
            FROM teaching.workload_commitments
            WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId;
            """;
        var correctedCommitment = await QuerySingleOrDefaultAsync<WorkloadCommitmentDbRecord>(
            wcomSql, new { TeacherId = teacherId, PeriodId = academicPeriodId });

        correctedCommitment.Should().NotBeNull();
        correctedCommitment!.CommittedWorkloadHours.Should().Be(5.00m, "approved assignments aggregate into committed workload");
        correctedCommitment.PlannedWorkloadHours.Should().Be(3.00m, "proposed assignments aggregate into planned workload");
        correctedCommitment.ActiveAssignmentCount.Should().Be(1, "exactly one approved active assignment exists");
        correctedCommitment.RemainingCapacityHours.Should().Be(19.00m, "24.00 standard capacity minus 5.00 committed equals 19.00 remaining");
    }

    #endregion

    #region 3. Pre-Authorized Pool Delegation & Emergency Substitutions Tests (Resolution 3.8)

    [SkippableFact]
    public async Task EmergencySubstitution_Should_DirectlyApprove_WhenSubstituteIsQualifiedAndWithinCapacity()
    {
        // Arrange (Resolution 3.8: Qualified candidate within capacity commits directly with status 'approved')
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);
        var academicPeriodId = Guid.NewGuid();

        var primaryTeacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");
        var substituteTeacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");

        // Substitute holds active matching capability authorization
        var capabilityId = await TeachingIntegrationTestHelpers.EnsureTeachingCapabilityAsync(connection, "MATH_PRIMARY", "Primary Mathematics Instruction");
        await TeachingIntegrationTestHelpers.EnsureTeachingAuthorizationAsync(
            connection, substituteTeacherId, capabilityId, scopeDescription: "MATH_PRIMARY Grades 1-6");

        // Substitute has 0 hours committed (well within standard capacity limit 24.00 / prohibited 28.00)
        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, substituteTeacherId, academicPeriodId, plannedHours: 0.00m, committedHours: 0.00m, maxCapacityHours: 24.00m);

        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);
        var originalAsnId = await TeachingIntegrationTestHelpers.EnsureTeachingAssignmentAsync(
            connection, demandId, primaryTeacherId, assignmentRoleId, academicPeriodId, status: "approved");

        var request = new ArrangeEmergencySubstitutionRequest(
            OriginalAssignmentId: originalAsnId,
            SubstituteTeacherId: substituteTeacherId,
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2026, 9, 15),
            CoverageScopeType: "full",
            CoveredHoursPerWeek: 4.00m,
            Reason: "medical_leave",
            HandoverNotes: "Lesson plans in Google Drive folder");

        // Act
        var response = await Client.PostAsJsonAsync(_substitutionsUri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ArrangeEmergencySubstitutionResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Status.Should().Be("approved", "qualified substitute within capacity commits directly under Pre-Authorized Pool Delegation");
        result.IsDelegatedApproval.Should().BeTrue();
        result.RequiresAuthorityApproval.Should().BeFalse();
        result.CapacityConflictId.Should().BeNull();
        result.SubstitutionNumber.Should().StartWith("SUB-");

        // Verify database persistence
        const string checkSql = """
            SELECT id AS Id, substitution_number AS SubstitutionNumber, original_assignment_id AS OriginalAssignmentId,
                   substitute_teacher_profile_id AS SubstituteTeacherProfileId, start_date AS StartDate,
                   end_date AS EndDate, workload_impact_hours AS WorkloadImpactHours, status AS Status,
                   approved_by AS ApprovedBy, approved_date AS ApprovedDate
            FROM teaching.teaching_substitutions
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<SubstitutionDbRecord>(checkSql, new { result.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.Status.Should().Be("approved");
        dbRecord.SubstituteTeacherProfileId.Should().Be(substituteTeacherId);
        dbRecord.WorkloadImpactHours.Should().Be(4.00m);

        // Verify domain event publication
        Factory.EventCollector.TeachingSubstitutionAssignedEvents
            .Should().Contain(e => e.SubstitutionId == result.Id && e.IsDelegatedApproval == true);
    }

    [SkippableFact]
    public async Task EmergencySubstitution_Should_EscalateToProposedWithBlockingConflict_WhenSubstituteLacksAuthorization_And_ResolveUponApproval()
    {
        // Arrange (Resolution 3.8: Unqualified substitute raises blocking conflict requiring authority approval)
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);
        var academicPeriodId = Guid.NewGuid();

        var primaryTeacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");
        var unqualifiedSubstituteId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");

        // Note: unqualifiedSubstituteId intentionally NOT granted MATH_PRIMARY teaching authorization
        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, unqualifiedSubstituteId, academicPeriodId, plannedHours: 0.00m, committedHours: 0.00m);

        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);
        var originalAsnId = await TeachingIntegrationTestHelpers.EnsureTeachingAssignmentAsync(
            connection, demandId, primaryTeacherId, assignmentRoleId, academicPeriodId, status: "approved");

        var request = new ArrangeEmergencySubstitutionRequest(
            OriginalAssignmentId: originalAsnId,
            SubstituteTeacherId: unqualifiedSubstituteId,
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2026, 9, 15),
            CoverageScopeType: "full",
            CoveredHoursPerWeek: 4.00m,
            Reason: "emergency_leave",
            HandoverNotes: "Substitute lacks math certification, emergency only");

        // Act 1: Arrange substitution (should escalate)
        var response = await Client.PostAsJsonAsync(_substitutionsUri, request);

        // Assert 1: Escalated to 'proposed' with blocking capacity conflict
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var escalated = await response.Content.ReadFromJsonAsync<ArrangeEmergencySubstitutionResponse>();
        escalated.Should().NotBeNull();
        escalated!.Status.Should().Be("proposed", "unauthorized substitute requires Teaching Authority exception approval");
        escalated.IsDelegatedApproval.Should().BeFalse();
        escalated.RequiresAuthorityApproval.Should().BeTrue();
        escalated.CapacityConflictId.Should().NotBeNull();

        // Verify blocking capacity conflict row exists
        const string conflictSql = """
            SELECT id AS Id, severity AS Severity, status AS Status, conflict_type AS ConflictType,
                   teacher_profile_id AS TeacherProfileId, teaching_assignment_id AS TeachingAssignmentId,
                   resolution_action AS ResolutionAction
            FROM teaching.capacity_conflicts
            WHERE id = @ConflictId;
            """;
        var conflict = await QuerySingleOrDefaultAsync<CapacityConflictDbRecord>(conflictSql, new { ConflictId = escalated.CapacityConflictId });
        conflict.Should().NotBeNull();
        conflict!.Severity.Should().Be("blocking");
        conflict.Status.Should().Be("unresolved");
        conflict.TeacherProfileId.Should().Be(unqualifiedSubstituteId);

        // Act 2: Teaching Authority approves exception
        var approveReq = new ApproveSubstitutionExceptionRequest("Dean approved exception due to acute staffing shortage");
        var approveResp = await Client.PostAsJsonAsync($"{_substitutionsUri}/{escalated.Id}/approve", approveReq);

        // Assert 2: Substitution committed with 'approved' and conflict resolved
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var approvedResult = await approveResp.Content.ReadFromJsonAsync<ApproveSubstitutionExceptionResponse>();
        approvedResult.Should().NotBeNull();
        approvedResult!.Status.Should().Be("approved");
        approvedResult.ApprovedBy.Should().NotBeNullOrWhiteSpace();

        // Verify conflict resolved in DB
        var resolvedConflict = await QuerySingleOrDefaultAsync<CapacityConflictDbRecord>(conflictSql, new { ConflictId = escalated.CapacityConflictId });
        resolvedConflict.Should().NotBeNull();
        resolvedConflict!.Status.Should().Be("resolved");
        resolvedConflict.ResolutionAction.Should().Be("exception_approved");

        // Verify domain event published with IsDelegatedApproval = false
        Factory.EventCollector.TeachingSubstitutionAssignedEvents
            .Should().Contain(e => e.SubstitutionId == escalated.Id && e.IsDelegatedApproval == false);
    }

    [SkippableFact]
    public async Task EmergencySubstitution_Should_EscalateToProposed_WhenSubstituteBreachesCapacityCeiling()
    {
        // Arrange: Substitute has matching authorization, but committed hours + substitution breaches prohibited ceiling (28.00m)
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);
        var academicPeriodId = Guid.NewGuid();

        var primaryTeacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");
        var overloadedSubstituteId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");

        var capabilityId = await TeachingIntegrationTestHelpers.EnsureTeachingCapabilityAsync(connection, "MATH_PRIMARY", "Primary Mathematics Instruction");
        await TeachingIntegrationTestHelpers.EnsureTeachingAuthorizationAsync(
            connection, overloadedSubstituteId, capabilityId, scopeDescription: "MATH_PRIMARY Grades 1-6");

        // Substitute is already near ceiling: 26.00 hours committed (prohibited threshold is 28.00)
        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, overloadedSubstituteId, academicPeriodId, plannedHours: 0.00m, committedHours: 26.00m, maxCapacityHours: 24.00m);

        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);
        var originalAsnId = await TeachingIntegrationTestHelpers.EnsureTeachingAssignmentAsync(
            connection, demandId, primaryTeacherId, assignmentRoleId, academicPeriodId, status: "approved");

        // Adding 4.00 hours -> 26 + 4 = 30 > 28 (prohibited ceiling breach)
        var request = new ArrangeEmergencySubstitutionRequest(
            OriginalAssignmentId: originalAsnId,
            SubstituteTeacherId: overloadedSubstituteId,
            StartDate: new DateOnly(2026, 9, 1),
            EndDate: new DateOnly(2026, 9, 15),
            CoverageScopeType: "full",
            CoveredHoursPerWeek: 4.00m,
            Reason: "medical_leave");

        // Act
        var response = await Client.PostAsJsonAsync(_substitutionsUri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ArrangeEmergencySubstitutionResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("proposed", "breaching prohibited capacity ceiling requires authority approval");
        result.IsDelegatedApproval.Should().BeFalse();
        result.RequiresAuthorityApproval.Should().BeTrue();
        result.CapacityConflictId.Should().NotBeNull();
    }

    #endregion

    #region 4. Permanent Reassignments & Handover Management Tests

    [SkippableFact]
    public async Task PermanentReassignment_Should_LinkPredecessor_RequireHandover_And_TransferWorkloadAtomically()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);
        var academicPeriodId = Guid.NewGuid();

        var predecessorTeacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");
        var replacementTeacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");

        // Predecessor has 5.00 committed hours
        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, predecessorTeacherId, academicPeriodId, plannedHours: 0.00m, committedHours: 5.00m, activeCount: 1);

        // Replacement has 0.00 committed hours
        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, replacementTeacherId, academicPeriodId, plannedHours: 0.00m, committedHours: 0.00m, activeCount: 0);

        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);

        // Predecessor original assignment (5.00 hours)
        var originalAsnId = Guid.NewGuid();
        const string insertOriginalAsn = """
            INSERT INTO teaching.teaching_assignments (
                id, assignment_number, teaching_demand_id, teacher_profile_id,
                assignment_role_id, assignment_scope, scope_reference_id, scope_label,
                academic_period_id, effective_start_date, effective_end_date,
                workload_commitment_value, workload_unit, status, teacher_acknowledged,
                created_at, created_by
            ) VALUES (
                @Id, 'ASN-PRED-0001', @DemandId, @TeacherId,
                @RoleId, 'learning_unit', NEWID(), 'Matematika SD Kelas 4B',
                @PeriodId, '2026-08-01', '2026-12-15',
                5.00, 'Teaching Hours', 'approved', 1,
                SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertOriginalAsn, new
        {
            Id = originalAsnId,
            DemandId = demandId,
            TeacherId = predecessorTeacherId,
            RoleId = assignmentRoleId,
            PeriodId = academicPeriodId
        });

        // Act 1: Propose Permanent Reassignment
        var proposeReq = new ProposePermanentReassignmentRequest(
            OriginalAssignmentId: originalAsnId,
            NewTeacherId: replacementTeacherId,
            EffectiveDate: new DateOnly(2026, 9, 1),
            Reason: "Mid-term medical leave requiring full permanent transfer",
            RequiresHandover: true);

        var proposeResp = await Client.PostAsJsonAsync(_reassignmentsUri, proposeReq);
        proposeResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var proposed = await proposeResp.Content.ReadFromJsonAsync<ProposePermanentReassignmentResponse>();
        proposed.Should().NotBeNull();
        proposed!.Reassignment.Status.Should().Be("proposed");
        proposed.Reassignment.ReassignmentNumber.Should().StartWith("REA-");
        var reassignmentId = proposed.Reassignment.Id;

        // Act 2: Teaching Authority approves reassignment
        var approveReq = new ApprovePermanentReassignmentRequest("Approved permanent transfer; handover mandated");
        var approveResp = await Client.PostAsJsonAsync($"{_reassignmentsUri}/{reassignmentId}/approve", approveReq);
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approveResp.Content.ReadFromJsonAsync<ApprovePermanentReassignmentResponse>();
        approved.Should().NotBeNull();
        approved!.Reassignment.Status.Should().Be("approved");
        approved.ReplacementAssignment.PredecessorAssignmentId.Should().Be(originalAsnId, "new assignment links to predecessor");
        approved.ReplacementAssignment.TeacherProfileId.Should().Be(replacementTeacherId);
        approved.ReplacementAssignment.WorkloadCommitmentValue.Should().Be(5.00m);

        // Verify original assignment marked 'completed' (superseded)
        const string asnCheckSql = "SELECT id AS Id, assignment_number AS AssignmentNumber, status AS Status, predecessor_assignment_id AS PredecessorAssignmentId, workload_commitment_value AS WorkloadCommitmentValue FROM teaching.teaching_assignments WHERE id = @Id;";
        var originalAsnDb = await QuerySingleOrDefaultAsync<AssignmentDbRecord>(asnCheckSql, new { Id = originalAsnId });
        originalAsnDb.Should().NotBeNull();
        originalAsnDb!.Status.Should().Be("completed");

        // Verify workload commitment transferred atomically
        const string wcomSql = "SELECT id AS Id, planned_workload_hours AS PlannedWorkloadHours, committed_workload_hours AS CommittedWorkloadHours, max_capacity_limit_hours AS MaxCapacityLimitHours, remaining_capacity_hours AS RemainingCapacityHours, active_assignment_count AS ActiveAssignmentCount, commitment_status AS CommitmentStatus FROM teaching.workload_commitments WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId;";
        var predWcom = await QuerySingleOrDefaultAsync<WorkloadCommitmentDbRecord>(wcomSql, new { TeacherId = predecessorTeacherId, PeriodId = academicPeriodId });
        predWcom.Should().NotBeNull();
        predWcom!.CommittedWorkloadHours.Should().Be(0.00m, "predecessor workload deducted by 5.00m");
        predWcom.ActiveAssignmentCount.Should().Be(0);

        var replWcom = await QuerySingleOrDefaultAsync<WorkloadCommitmentDbRecord>(wcomSql, new { TeacherId = replacementTeacherId, PeriodId = academicPeriodId });
        replWcom.Should().NotBeNull();
        replWcom!.CommittedWorkloadHours.Should().Be(5.00m, "replacement workload incremented by 5.00m");
        replWcom.ActiveAssignmentCount.Should().Be(1);

        // Act 3: Confirm instructional handover
        var handoverReq = new ConfirmReassignmentHandoverRequest(
            HandoverStatus: "completed",
            Notes: "Curriculum materials, student portfolios, and mid-term grade records transferred cleanly");
        var handoverResp = await Client.PostAsJsonAsync($"{_reassignmentsUri}/{reassignmentId}/handover", handoverReq);
        handoverResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var handoverResult = await handoverResp.Content.ReadFromJsonAsync<ConfirmReassignmentHandoverResponse>();
        handoverResult.Should().NotBeNull();
        handoverResult!.Reassignment.HandoverStatus.Should().Be("completed");
        handoverResult.Reassignment.HandoverCompletedDate.Should().NotBeNull();
    }

    #endregion

    #region 5. Assignment Completion & Workload Capacity Release Tests

    [SkippableFact]
    public async Task AssignmentCompletion_Should_ConcludeAssignment_And_ReleaseCommittedCapacityAtomically()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);
        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");

        // Teacher baseline commitment: 4.00 committed, 20.00 remaining, 1 active count
        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, teacherId, academicPeriodId, plannedHours: 0.00m, committedHours: 4.00m, maxCapacityHours: 24.00m, activeCount: 1);

        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);

        // Approved assignment with 4.00 hours workload
        var assignmentId = Guid.NewGuid();
        const string insertApprovedAsn = """
            INSERT INTO teaching.teaching_assignments (
                id, assignment_number, teaching_demand_id, teacher_profile_id,
                assignment_role_id, assignment_scope, scope_reference_id, scope_label,
                academic_period_id, effective_start_date, effective_end_date,
                workload_commitment_value, workload_unit, status, teacher_acknowledged,
                created_at, created_by
            ) VALUES (
                @Id, 'ASN-CMPL-0001', @DemandId, @TeacherId,
                @RoleId, 'learning_unit', NEWID(), 'Matematika SD Kelas 6 Semester 1',
                @PeriodId, '2026-08-01', '2026-12-15',
                4.00, 'Teaching Hours', 'approved', 1,
                SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;
        await connection.ExecuteAsync(insertApprovedAsn, new
        {
            Id = assignmentId,
            DemandId = demandId,
            TeacherId = teacherId,
            RoleId = assignmentRoleId,
            PeriodId = academicPeriodId
        });

        var completionDate = new DateOnly(2026, 12, 18);
        var concludeRequest = new ConcludeTeachingAssignmentRequest(
            CompletionDate: completionDate,
            AcademicConfirmationReceived: true,
            AcademicConfirmationSource: "Academics Dean Gradebook Sign-off",
            HandoverConfirmed: true,
            UnresolvedItemsStatus: "none",
            UnresolvedItemsDescription: null,
            Notes: "Course successfully finished with 100% grade submission");

        // Act
        var response = await Client.PostAsJsonAsync($"{_assignmentsUri}/{assignmentId}/conclude", concludeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConcludeTeachingAssignmentResponse>();
        result.Should().NotBeNull();
        result!.TeachingAssignmentId.Should().Be(assignmentId);
        result.Status.Should().Be("completed");
        result.ReleasedWorkloadHours.Should().Be(4.00m);
        result.AcademicConfirmationReceived.Should().BeTrue();
        result.HandoverConfirmed.Should().BeTrue();

        // Verify assignment status updated in DB
        const string asnCheckSql = "SELECT id AS Id, assignment_number AS AssignmentNumber, status AS Status, predecessor_assignment_id AS PredecessorAssignmentId, workload_commitment_value AS WorkloadCommitmentValue FROM teaching.teaching_assignments WHERE id = @Id;";
        var dbAsn = await QuerySingleOrDefaultAsync<AssignmentDbRecord>(asnCheckSql, new { Id = assignmentId });
        dbAsn.Should().NotBeNull();
        dbAsn!.Status.Should().Be("completed");

        // Verify completion audit record exists
        const string cmplCheckSql = """
            SELECT id AS Id, teaching_assignment_id AS TeachingAssignmentId, completion_date AS CompletionDate,
                   academic_confirmation_received AS AcademicConfirmationReceived, handover_confirmed AS HandoverConfirmed,
                   released_workload_hours AS ReleasedWorkloadHours, status AS Status
            FROM teaching.assignment_completions
            WHERE teaching_assignment_id = @Id;
            """;
        var dbCmpl = await QuerySingleOrDefaultAsync<CompletionDbRecord>(cmplCheckSql, new { Id = assignmentId });
        dbCmpl.Should().NotBeNull();
        dbCmpl!.ReleasedWorkloadHours.Should().Be(4.00m);
        dbCmpl.Status.Should().Be("completed");

        // Verify committed capacity released atomically in workload_commitments
        const string wcomSql = "SELECT id AS Id, planned_workload_hours AS PlannedWorkloadHours, committed_workload_hours AS CommittedWorkloadHours, max_capacity_limit_hours AS MaxCapacityLimitHours, remaining_capacity_hours AS RemainingCapacityHours, active_assignment_count AS ActiveAssignmentCount, commitment_status AS CommitmentStatus FROM teaching.workload_commitments WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId;";
        var wcom = await QuerySingleOrDefaultAsync<WorkloadCommitmentDbRecord>(wcomSql, new { TeacherId = teacherId, PeriodId = academicPeriodId });
        wcom.Should().NotBeNull();
        wcom!.CommittedWorkloadHours.Should().Be(0.00m, "committed hours released back to teacher pool");
        wcom.ActiveAssignmentCount.Should().Be(0, "active assignment count decremented");
        wcom.RemainingCapacityHours.Should().Be(24.00m, "remaining capacity restored to full capacity limit");

        // Verify domain event publication
        Factory.EventCollector.TeachingAssignmentCompletedEvents
            .Should().Contain(e => e.AssignmentId == assignmentId && e.ReleasedWorkloadHours == 4.00m);
    }

    #endregion

    #region 6. Demo Data Reset & Two-Phase Seeder Integration Tests

    [SkippableFact]
    public async Task DemoDataResetCycle_Should_ExecuteBothPhases_And_PurgerCleansAll24TeachingTables()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        using var scope = Factory.Services.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<DemoDataOrchestrator>();

        // Act 1: Execute complete reset and topological seeding (both Phase 1 & Phase 2)
        var seedSummary = await orchestrator.ResetAndSeedAsync();

        // Assert 1: Orchestrator executed both purge and seed cycles
        seedSummary.Should().NotBeNull();
        seedSummary.PurgedSchemas.Should().BeGreaterOrEqualTo(4, "purges Academics, Finance, Students, Teaching, Operations");
        seedSummary.SeededDomains.Should().BeGreaterOrEqualTo(7, "seeds across all core domains including Phase 1 & 2 Teaching");

        // Verify Phase 1 and Phase 2 data exist in teaching.* tables
        await using (var connection = await OpenConnectionAsync())
        {
            // Verify Hagios personas seeded (Budi Hartono, Siti Nurhaliza)
            var teacherCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM teaching.teacher_profiles WHERE display_name IN ('Budi Hartono, M.Pd.', 'Siti Nurhaliza, S.Pd.');");
            teacherCount.Should().Be(2, "Flagship Hagios personas are seeded");

            // Verify assignment roles and workload policies exist
            var roleCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.assignment_roles;");
            roleCount.Should().BeGreaterThan(0);

            var policyCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.workload_policies;");
            policyCount.Should().BeGreaterThan(0);

            // Verify Stage 2 assignments and workload snapshots exist
            var assignmentCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.teaching_assignments;");
            assignmentCount.Should().BeGreaterThan(0, "Phase 2 seeder binds assignments to learning units and cohorts");

            var commitmentCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.workload_commitments;");
            commitmentCount.Should().BeGreaterThan(0, "workload commitments synchronized for faculty");

            var substitutionCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.teaching_substitutions;");
            substitutionCount.Should().BeGreaterThan(0, "emergency substitution seeded for Budi Hartono absence");

            var historyCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.teaching_history_entries;");
            historyCount.Should().BeGreaterThan(0, "history audit log populated during seeding");
        }

        // Act 2: Execute Clean-Only purge
        var purgeSummary = await orchestrator.ExecuteResetAsync(cleanOnly: true);

        // Assert 2: Purger executed without error and cleaned all 24 schema teaching tables
        purgeSummary.Should().NotBeNull();
        purgeSummary.PurgedSchemas.Should().BeGreaterOrEqualTo(4);
        purgeSummary.SeededDomains.Should().Be(0);

        await using (var connection = await OpenConnectionAsync())
        {
            // Strict verification: all 24 authoritative tables in schema teaching must be empty (0 rows)
            string[] teachingTables =
            [
                "teaching.teaching_history_entries",
                "teaching.assignment_completions",
                "teaching.teaching_reassignments",
                "teaching.teaching_substitutions",
                "teaching.workload_exceptions",
                "teaching.workload_commitments",
                "teaching.teacher_unavailability_windows",
                "teaching.teacher_availabilities",
                "teaching.capacity_conflicts",
                "teaching.teaching_assignments",
                "teaching.teacher_eligibility_evaluations",
                "teaching.teaching_demands",
                "teaching.assignment_roles",
                "teaching.workload_policies",
                "teaching.teaching_record_corrections",
                "teaching.supervision_requirements",
                "teaching.authorization_evidence",
                "teaching.teaching_authorizations",
                "teaching.qualification_evidence",
                "teaching.teaching_capabilities",
                "teaching.teacher_profiles",
                "teaching.qualification_types",
                "teaching.capability_types",
                "teaching.teaching_roles",
            ];

            foreach (var table in teachingTables)
            {
                var rowCount = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM {table};");
                rowCount.Should().Be(0, $"Purger must delete all records from table '{table}' in reverse-dependency order");
            }
        }
    }

    #endregion
}
