namespace Elsasa.Sis.IntegrationTests.Features.Teaching;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Teaching.Features.TeacherEligibility.EvaluateCandidateEligibility;
using Elsasa.Sis.Teaching.Features.TeacherEligibility.GetCandidateEvaluations;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.AcknowledgeTeachingAssignment;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.ApproveTeachingAssignment;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.ProposeTeachingAssignment;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.ResolveCapacityConflict;
using Elsasa.Sis.Teaching.Features.TeachingAssignments.UpdateTeachingAssignmentStatus;
using Elsasa.Sis.Teaching.Features.TeachingDemands.CreateTeachingDemand;
using Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemand;
using Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemandStatus;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration test suite verifying Teaching Demands intake, the 5-check automated Candidate Eligibility Evaluation Engine,
/// Assignment proposal, authority approval with atomic workload commitment updates, teacher acknowledgement,
/// homeroom advisory assignment integration, and capacity conflict resolution (P6-S19, TD-01..TD-05).
/// </summary>
public sealed class TeachingAssignmentsIntegrationTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private const string _demandsUri = "/api/v1/teaching/demands";
    private const string _eligibilityUri = "/api/v1/teaching/eligibility";
    private const string _assignmentsUri = "/api/v1/teaching/assignments";
    private const string _conflictsUri = "/api/v1/teaching/conflicts";

    private sealed record DemandDbRecord(
        Guid Id,
        string DemandCode,
        string SourceDomain,
        string SourceWorkLabel,
        decimal ExpectedWorkloadHours,
        string Status,
        string? DeclineReason,
        string? ClarificationNotes);

    private sealed record AssignmentDbRecord(
        Guid Id,
        string AssignmentNumber,
        string Status,
        string AssignmentScope,
        string ScopeLabel,
        decimal WorkloadCommitmentValue,
        bool TeacherAcknowledged,
        DateOnly? TeacherAcknowledgedDate,
        string? ApprovedBy,
        DateOnly? ApprovedDate);

    private sealed record WorkloadCommitmentDbRecord(
        Guid Id,
        decimal PlannedWorkloadHours,
        decimal CommittedWorkloadHours,
        decimal MaxCapacityLimitHours,
        decimal RemainingCapacityHours,
        int ActiveAssignmentCount,
        string CommitmentStatus);

    private sealed record CapacityConflictDbRecord(
        Guid Id,
        string Status,
        string? ResolutionAction,
        string? ResolvedBy,
        DateOnly? ResolvedDate,
        string? ResolutionNotes);

    #region 1. Demands Intake & Management Tests

    [SkippableFact]
    public async Task DemandCreation_Should_Succeed_WithGeneratedCode_And_ReceivedStatus_And_PublishEvent()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var academicPeriodId = Guid.NewGuid();
        var sourceWorkRef = Guid.NewGuid();

        var request = new CreateTeachingDemandRequest(
            SourceDomain: "Academics",
            SourceWorkReference: sourceWorkRef,
            SourceWorkLabel: "Matematika SD Kelas 4A",
            AcademicPeriodId: academicPeriodId,
            RequiredCapabilityCode: "MATH_PRIMARY",
            RequiredAssignmentRoleId: assignmentRoleId,
            ExpectedWorkloadHours: 5.00m,
            RequestedStartDate: new DateOnly(2026, 8, 1),
            RequestedEndDate: new DateOnly(2026, 12, 15),
            CampusScope: "Kampus Utama",
            RequiredTeachersCount: 1,
            ClarificationNotes: "Initial demand intake for Grade 4 Math");

        // Act
        var response = await Client.PostAsJsonAsync(_demandsUri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateTeachingDemandResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.DemandCode.Should().StartWith("DEM-");
        created.Status.Should().Be("received");
        created.SourceDomain.Should().Be("Academics");
        created.SourceWorkLabel.Should().Be("Matematika SD Kelas 4A");

        // Verify database persistence
        const string sql = """
            SELECT id AS Id, demand_code AS DemandCode, source_domain AS SourceDomain,
                   source_work_label AS SourceWorkLabel, expected_workload_hours AS ExpectedWorkloadHours,
                   status AS Status, decline_reason AS DeclineReason, clarification_notes AS ClarificationNotes
            FROM teaching.teaching_demands
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<DemandDbRecord>(sql, new { created.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.Status.Should().Be("received");
        dbRecord.ExpectedWorkloadHours.Should().Be(5.00m);
        dbRecord.SourceWorkLabel.Should().Be("Matematika SD Kelas 4A");

        // Verify domain event publication
        Factory.EventCollector.TeachingDemandReceivedEvents
            .Should().Contain(e => e.DemandId == created.Id && e.DemandCode == created.DemandCode);
    }

    [SkippableFact]
    public async Task DemandUpdate_Should_ModifyParameters_WhenInMutableStatus()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, status: "received");

        var updateRequest = new UpdateTeachingDemandRequest(
            SourceWorkLabel: "Matematika SD Kelas 4A (Revised Scope)",
            RequiredCapabilityCode: "MATH_PRIMARY",
            RequiredAssignmentRoleId: assignmentRoleId,
            ExpectedWorkloadHours: 6.00m,
            RequestedStartDate: new DateOnly(2026, 8, 15),
            RequestedEndDate: new DateOnly(2026, 12, 20),
            WorkloadUnit: "Teaching Hours",
            RequiresAssessmentAuthority: true,
            CampusScope: "Kampus Utama",
            RequiredTeachersCount: 2,
            ClarificationNotes: "Scope increased to 6 hours for co-teaching");

        // Act
        var response = await Client.PutAsJsonAsync($"{_demandsUri}/{demandId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<UpdateTeachingDemandResponse>();
        updated.Should().NotBeNull();
        updated!.Id.Should().Be(demandId);
        updated.Status.Should().Be("received");

        // Verify database state
        const string checkSql = """
            SELECT id AS Id, demand_code AS DemandCode, source_domain AS SourceDomain,
                   source_work_label AS SourceWorkLabel, expected_workload_hours AS ExpectedWorkloadHours,
                   status AS Status, decline_reason AS DeclineReason, clarification_notes AS ClarificationNotes
            FROM teaching.teaching_demands
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<DemandDbRecord>(checkSql, new { Id = demandId });
        dbRecord.Should().NotBeNull();
        dbRecord!.SourceWorkLabel.Should().Be("Matematika SD Kelas 4A (Revised Scope)");
        dbRecord.ExpectedWorkloadHours.Should().Be(6.00m);
    }

    [SkippableFact]
    public async Task DemandStatusTransitions_Should_Succeed_AcrossValidLifecycle()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, status: "received");

        // Act & Assert 1: Accept for staffing
        var acceptReq = new UpdateTeachingDemandStatusRequest("accepted_for_staffing");
        var acceptResp = await Client.PostAsJsonAsync($"{_demandsUri}/{demandId}/status", acceptReq);
        acceptResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var acceptBody = await acceptResp.Content.ReadFromJsonAsync<UpdateTeachingDemandStatusResponse>();
        acceptBody!.NewStatus.Should().Be("accepted_for_staffing");

        // Act & Assert 2: Return for clarification
        var clarifyReq = new UpdateTeachingDemandStatusRequest("returned_for_clarification", ClarificationNotes: "Need confirmation on schedule");
        var clarifyResp = await Client.PostAsJsonAsync($"{_demandsUri}/{demandId}/status", clarifyReq);
        clarifyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var clarifyBody = await clarifyResp.Content.ReadFromJsonAsync<UpdateTeachingDemandStatusResponse>();
        clarifyBody!.NewStatus.Should().Be("returned_for_clarification");

        // Act & Assert 3: Decline without reason should fail validation (422)
        var invalidDeclineReq = new UpdateTeachingDemandStatusRequest("declined");
        var invalidDeclineResp = await Client.PostAsJsonAsync($"{_demandsUri}/{demandId}/status", invalidDeclineReq);
        invalidDeclineResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // Act & Assert 4: Decline with reason should succeed
        var declineReq = new UpdateTeachingDemandStatusRequest("declined", DeclineReason: "Faculty shortage in mathematics");
        var declineResp = await Client.PostAsJsonAsync($"{_demandsUri}/{demandId}/status", declineReq);
        declineResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var declineBody = await declineResp.Content.ReadFromJsonAsync<UpdateTeachingDemandStatusResponse>();
        declineBody!.NewStatus.Should().Be("declined");

        // Verify DB
        const string checkSql = "SELECT status FROM teaching.teaching_demands WHERE id = @Id;";
        var status = await ExecuteScalarAsync<string>(checkSql, new { Id = demandId });
        status.Should().Be("declined");
    }

    [SkippableFact]
    public async Task DemandQueries_Should_FilterByPeriodAndStatusAndSearch_And_GetById()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var targetPeriodId = Guid.NewGuid();
        var otherPeriodId = Guid.NewGuid();

        var d1 = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(
            connection, assignmentRoleId, targetPeriodId, demandCode: "DEM-FLT-001", status: "accepted_for_staffing");
        var d2 = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(
            connection, assignmentRoleId, targetPeriodId, demandCode: "DEM-FLT-002", status: "received");
        var d3 = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(
            connection, assignmentRoleId, otherPeriodId, demandCode: "DEM-FLT-003", status: "accepted_for_staffing");

        // Act 1: Filter by target academicPeriodId and status = accepted_for_staffing
        var filterResp = await Client.GetAsync($"{_demandsUri}?academicPeriodId={targetPeriodId}&status=accepted_for_staffing");
        filterResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await filterResp.Content.ReadFromJsonAsync<PaginatedList<TeachingDemandDto>>();
        pagedList.Should().NotBeNull();
        pagedList!.Items.Should().Contain(d => d.Id == d1);
        pagedList.Items.Should().NotContain(d => d.Id == d2);
        pagedList.Items.Should().NotContain(d => d.Id == d3);

        // Act 2: Get demand by ID
        var getByIdResp = await Client.GetAsync($"{_demandsUri}/{d1}");
        getByIdResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var demandDto = await getByIdResp.Content.ReadFromJsonAsync<TeachingDemandDto>();
        demandDto.Should().NotBeNull();
        demandDto!.Id.Should().Be(d1);
        demandDto.DemandCode.Should().Be("DEM-FLT-001");
        demandDto.Status.Should().Be("accepted_for_staffing");
    }

    #endregion

    #region 2. Candidate Eligibility Evaluation Engine Tests

    [SkippableFact]
    public async Task CandidateEligibility_Should_ReturnEligible_WhenAllChecksPass()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection, "TEACHER_EXACT", "Subject Teacher", requiresQualificationCheck: false);
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);

        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active", roleId);
        var capabilityId = await TeachingIntegrationTestHelpers.EnsureTeachingCapabilityAsync(connection, "MATH_PRIMARY", "Primary Mathematics Instruction");
        await TeachingIntegrationTestHelpers.EnsureTeachingAuthorizationAsync(
            connection, teacherId, capabilityId, scopeDescription: "MATH_PRIMARY Grades 1-6", campusScope: "Kampus Utama");

        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, teacherId, academicPeriodId, plannedHours: 0m, committedHours: 10.00m, maxCapacityHours: 24.00m);

        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(
            connection, assignmentRoleId, academicPeriodId, status: "accepted_for_staffing");

        var evalRequest = new EvaluateCandidateEligibilityRequest(
            TeachingDemandId: demandId,
            TeacherProfileId: teacherId,
            CoordinatorRemarks: "Pre-assignment candidate verification");

        // Act
        var response = await Client.PostAsJsonAsync($"{_eligibilityUri}/evaluate", evalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateCandidateEligibilityResponse>();
        result.Should().NotBeNull();
        result!.IsEligible.Should().BeTrue();
        result.EligibilityStatus.Should().Be("eligible");
        result.ProfileActiveCheck.Should().BeTrue();
        result.AuthorizationCheckPassed.Should().BeTrue();
        result.QualificationCheckPassed.Should().BeTrue();
        result.AvailabilityCheckPassed.Should().BeTrue();
        result.CapacityCheckPassed.Should().BeTrue();
        result.Explanation.Should().NotBeNullOrWhiteSpace();
        result.Explanation.Should().Contain("Kandidat memenuhi seluruh persyaratan");

        // Verify record persisted in database
        const string checkSql = """
            SELECT is_eligible AS IsEligible, eligibility_status AS EligibilityStatus,
                   profile_active_check AS ProfileActiveCheck, authorization_check_passed AS AuthorizationCheckPassed
            FROM teaching.teacher_eligibility_evaluations
            WHERE id = @Id;
            """;
        var persisted = await QuerySingleOrDefaultAsync<EvaluateCandidateEligibilityResponse>(checkSql, new { result.Id });
        persisted.Should().NotBeNull();
        persisted!.IsEligible.Should().BeTrue();
        persisted.EligibilityStatus.Should().Be("eligible");
    }

    [SkippableFact]
    public async Task CandidateEligibility_Should_ReturnIneligibleUnqualified_WhenLackingAuthorization()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection, "TEACHER_EXACT_2", requiresQualificationCheck: false);
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);

        var academicPeriodId = Guid.NewGuid();
        // Teacher has standing active, but NO authorizations at all
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active", roleId);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);

        var evalRequest = new EvaluateCandidateEligibilityRequest(
            TeachingDemandId: demandId,
            TeacherProfileId: teacherId,
            CoordinatorRemarks: "Checking unqualified teacher candidate");

        // Act
        var response = await Client.PostAsJsonAsync($"{_eligibilityUri}/evaluate", evalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateCandidateEligibilityResponse>();
        result.Should().NotBeNull();
        result!.IsEligible.Should().BeFalse();
        result.EligibilityStatus.Should().Be("ineligible_unqualified");
        result.ProfileActiveCheck.Should().BeTrue();
        result.AuthorizationCheckPassed.Should().BeFalse();
        result.Explanation.Should().Contain("otorisasi mengajar aktif");
    }

    [SkippableFact]
    public async Task CandidateEligibility_Should_ReturnIneligibleConflicted_WhenLeaveConflictExists()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection, "TEACHER_EXACT_3", requiresQualificationCheck: false);
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);

        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active", roleId);
        var capabilityId = await TeachingIntegrationTestHelpers.EnsureTeachingCapabilityAsync(connection, "MATH_PRIMARY", "Primary Mathematics Instruction");
        await TeachingIntegrationTestHelpers.EnsureTeachingAuthorizationAsync(
            connection, teacherId, capabilityId, scopeDescription: "MATH_PRIMARY Grades 1-6");

        // Overlapping leave window covering demand date range (2026-08-01 to 2026-12-15)
        await TeachingIntegrationTestHelpers.EnsureUnavailabilityWindowAsync(
            connection, teacherId, new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), reason: "Medical Leave Window");

        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);

        var evalRequest = new EvaluateCandidateEligibilityRequest(
            TeachingDemandId: demandId,
            TeacherProfileId: teacherId,
            CoordinatorRemarks: "Checking leave conflict evaluation");

        // Act
        var response = await Client.PostAsJsonAsync($"{_eligibilityUri}/evaluate", evalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateCandidateEligibilityResponse>();
        result.Should().NotBeNull();
        result!.IsEligible.Should().BeFalse();
        result.EligibilityStatus.Should().Be("ineligible_conflicted");
        result.AuthorizationCheckPassed.Should().BeTrue();
        result.AvailabilityCheckPassed.Should().BeFalse();
        result.Explanation.Should().Contain("ketidaktersediaan");
    }

    [SkippableFact]
    public async Task CandidateEligibility_Should_ReturnIneligibleCapacityExceeded_WhenWorkloadExceedsLimit()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection, "TEACHER_EXACT_4", requiresQualificationCheck: false);
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);

        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active", roleId);
        var capabilityId = await TeachingIntegrationTestHelpers.EnsureTeachingCapabilityAsync(connection, "MATH_PRIMARY", "Primary Mathematics Instruction");
        await TeachingIntegrationTestHelpers.EnsureTeachingAuthorizationAsync(
            connection, teacherId, capabilityId, scopeDescription: "MATH_PRIMARY Grades 1-6");

        // Teacher committed hours = 22.00, max limit = 24.00, demand is 4.00 hours -> total 26 > 24
        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, teacherId, academicPeriodId, plannedHours: 0m, committedHours: 22.00m, maxCapacityHours: 24.00m);

        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);

        var evalRequest = new EvaluateCandidateEligibilityRequest(
            TeachingDemandId: demandId,
            TeacherProfileId: teacherId,
            CoordinatorRemarks: "Checking capacity overload evaluation");

        // Act
        var response = await Client.PostAsJsonAsync($"{_eligibilityUri}/evaluate", evalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateCandidateEligibilityResponse>();
        result.Should().NotBeNull();
        result!.IsEligible.Should().BeFalse();
        result.EligibilityStatus.Should().Be("ineligible_capacity_exceeded");
        result.AuthorizationCheckPassed.Should().BeTrue();
        result.AvailabilityCheckPassed.Should().BeTrue();
        result.CapacityCheckPassed.Should().BeFalse();
        result.Explanation.Should().Contain("melebihi batas kapasitas");
    }

    [SkippableFact]
    public async Task CandidateEvaluations_Should_SupportQueryByDemandOrTeacher()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection, "TEACHER_EXACT_5", requiresQualificationCheck: false);
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);

        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active", roleId);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);

        // Evaluate candidate
        var evalRequest = new EvaluateCandidateEligibilityRequest(demandId, teacherId, "Initial query test evaluation");
        var evalResp = await Client.PostAsJsonAsync($"{_eligibilityUri}/evaluate", evalRequest);
        evalResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 1: Query evaluations by demand ID
        var demandQueryResp = await Client.GetAsync($"{_eligibilityUri}/evaluations?teachingDemandId={demandId}");
        demandQueryResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listByDemand = await demandQueryResp.Content.ReadFromJsonAsync<IReadOnlyList<CandidateEvaluationSummaryDto>>();
        listByDemand.Should().NotBeNull();
        listByDemand!.Should().Contain(e => e.TeachingDemandId == demandId && e.TeacherProfileId == teacherId);

        // Act 2: Query evaluations by teacher ID
        var teacherQueryResp = await Client.GetAsync($"{_eligibilityUri}/evaluations?teacherProfileId={teacherId}");
        teacherQueryResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listByTeacher = await teacherQueryResp.Content.ReadFromJsonAsync<IReadOnlyList<CandidateEvaluationSummaryDto>>();
        listByTeacher.Should().NotBeNull();
        listByTeacher!.Should().Contain(e => e.TeacherProfileId == teacherId);
    }

    #endregion

    #region 3. Teaching Assignments Proposal, Approval & Lifecycle Tests

    [SkippableFact]
    public async Task ProposeAssignment_Should_CreateAssignment_And_IncrementPlannedWorkload_And_PublishEvent()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);

        // Ensure baseline workload commitment row exists (0 planned, 0 committed)
        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, teacherId, academicPeriodId, plannedHours: 0.00m, committedHours: 0.00m);

        var request = new ProposeTeachingAssignmentRequest(
            TeachingDemandId: demandId,
            TeacherProfileId: teacherId,
            AssignmentRoleId: assignmentRoleId,
            AssignmentScope: "learning_unit",
            ScopeReferenceId: Guid.NewGuid(),
            ScopeLabel: "Matematika Kelas 4 SD",
            AcademicPeriodId: academicPeriodId,
            EffectiveStartDate: new DateOnly(2026, 8, 1),
            EffectiveEndDate: new DateOnly(2026, 12, 15),
            WorkloadCommitmentValue: 4.00m,
            Notes: "Proposed candidate assignment");

        // Act
        var response = await Client.PostAsJsonAsync($"{_assignmentsUri}/propose", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ProposeTeachingAssignmentResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.AssignmentNumber.Should().StartWith("ASN-");
        created.Status.Should().Be("proposed");

        // Verify DB assignment row
        const string sql = """
            SELECT id AS Id, assignment_number AS AssignmentNumber, status AS Status,
                   assignment_scope AS AssignmentScope, scope_label AS ScopeLabel,
                   workload_commitment_value AS WorkloadCommitmentValue, teacher_acknowledged AS TeacherAcknowledged
            FROM teaching.teaching_assignments
            WHERE id = @Id;
            """;
        var dbAssignment = await QuerySingleOrDefaultAsync<AssignmentDbRecord>(sql, new { created.Id });
        dbAssignment.Should().NotBeNull();
        dbAssignment!.Status.Should().Be("proposed");
        dbAssignment.WorkloadCommitmentValue.Should().Be(4.00m);

        // Verify planned workload incremented in workload_commitments
        const string wcomSql = """
            SELECT id AS Id, planned_workload_hours AS PlannedWorkloadHours, committed_workload_hours AS CommittedWorkloadHours,
                   max_capacity_limit_hours AS MaxCapacityLimitHours, remaining_capacity_hours AS RemainingCapacityHours,
                   active_assignment_count AS ActiveAssignmentCount, commitment_status AS CommitmentStatus
            FROM teaching.workload_commitments
            WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId;
            """;
        var commitment = await QuerySingleOrDefaultAsync<WorkloadCommitmentDbRecord>(wcomSql, new { TeacherId = teacherId, PeriodId = academicPeriodId });
        commitment.Should().NotBeNull();
        commitment!.PlannedWorkloadHours.Should().Be(4.00m);
        commitment.CommittedWorkloadHours.Should().Be(0.00m);

        // Verify domain event
        Factory.EventCollector.TeachingAssignmentProposedEvents
            .Should().Contain(e => e.AssignmentId == created.Id && e.AssignmentNumber == created.AssignmentNumber);
    }

    [SkippableFact]
    public async Task ApproveAssignment_Should_AtomicallyUpdateCommitments_And_StaffDemand_And_PublishEvent()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);
        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(
            connection, assignmentRoleId, academicPeriodId, status: "accepted_for_staffing");

        // Propose assignment via endpoint
        var proposeReq = new ProposeTeachingAssignmentRequest(
            TeachingDemandId: demandId,
            TeacherProfileId: teacherId,
            AssignmentRoleId: assignmentRoleId,
            AssignmentScope: "learning_unit",
            ScopeReferenceId: Guid.NewGuid(),
            ScopeLabel: "Matematika Kelas 4 SD",
            AcademicPeriodId: academicPeriodId,
            EffectiveStartDate: new DateOnly(2026, 8, 1),
            EffectiveEndDate: new DateOnly(2026, 12, 15),
            WorkloadCommitmentValue: 4.00m);

        var propResp = await Client.PostAsJsonAsync($"{_assignmentsUri}/propose", proposeReq);
        propResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var proposed = await propResp.Content.ReadFromJsonAsync<ProposeTeachingAssignmentResponse>();

        // Act: Authority approves assignment
        var approveReq = new ApproveTeachingAssignmentRequest(
            ApprovedDate: new DateOnly(2026, 8, 1),
            Notes: "Formal authority assignment approval");

        var approveResp = await Client.PostAsJsonAsync($"{_assignmentsUri}/{proposed!.Id}/approve", approveReq);

        // Assert
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await approveResp.Content.ReadFromJsonAsync<ApproveTeachingAssignmentResponse>();
        approved.Should().NotBeNull();
        approved!.Id.Should().Be(proposed.Id);
        approved.Status.Should().Be("approved");
        approved.ApprovedBy.Should().NotBeNullOrWhiteSpace();

        // Verify DB: assignment status updated
        const string asnSql = """
            SELECT id AS Id, assignment_number AS AssignmentNumber, status AS Status,
                   approved_by AS ApprovedBy, approved_date AS ApprovedDate
            FROM teaching.teaching_assignments
            WHERE id = @Id;
            """;
        var dbAsn = await QuerySingleOrDefaultAsync<AssignmentDbRecord>(asnSql, new { proposed.Id });
        dbAsn.Should().NotBeNull();
        dbAsn!.Status.Should().Be("approved");
        dbAsn.ApprovedBy.Should().NotBeNull();
        dbAsn.ApprovedDate.Should().Be(new DateOnly(2026, 8, 1));

        // Verify DB: workload_commitments atomically updated:
        // planned_workload_hours decremented (4 -> 0), committed_workload_hours incremented (0 -> 4), active_count (0 -> 1)
        const string wcomSql = """
            SELECT id AS Id, planned_workload_hours AS PlannedWorkloadHours, committed_workload_hours AS CommittedWorkloadHours,
                   max_capacity_limit_hours AS MaxCapacityLimitHours, remaining_capacity_hours AS RemainingCapacityHours,
                   active_assignment_count AS ActiveAssignmentCount, commitment_status AS CommitmentStatus
            FROM teaching.workload_commitments
            WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId;
            """;
        var dbWcom = await QuerySingleOrDefaultAsync<WorkloadCommitmentDbRecord>(wcomSql, new { TeacherId = teacherId, PeriodId = academicPeriodId });
        dbWcom.Should().NotBeNull();
        dbWcom!.PlannedWorkloadHours.Should().Be(0.00m);
        dbWcom.CommittedWorkloadHours.Should().Be(4.00m);
        dbWcom.ActiveAssignmentCount.Should().Be(1);

        // Verify demand marked 'staffed' (headcount requirement of 1 satisfied)
        const string demSql = "SELECT status FROM teaching.teaching_demands WHERE id = @Id;";
        var demandStatus = await ExecuteScalarAsync<string>(demSql, new { Id = demandId });
        demandStatus.Should().Be("staffed");

        // Verify audit log appended to teaching.teaching_history_entries
        const string histSql = """
            SELECT COUNT(1) FROM teaching.teaching_history_entries
            WHERE teacher_profile_id = @TeacherId AND event_type = 'AssignmentApproved';
            """;
        var histCount = await ExecuteScalarAsync<int>(histSql, new { TeacherId = teacherId });
        histCount.Should().BeGreaterThanOrEqualTo(1);

        // Verify domain event
        Factory.EventCollector.TeachingAssignmentApprovedEvents
            .Should().Contain(e => e.AssignmentId == proposed.Id && e.AssignmentNumber == proposed.AssignmentNumber);
    }

    [SkippableFact]
    public async Task AcknowledgeAssignment_Should_StampAcknowledgedFlagAndDate()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, assignmentRoleId, academicPeriodId);

        var assignmentId = await TeachingIntegrationTestHelpers.EnsureTeachingAssignmentAsync(
            connection, demandId, teacherId, assignmentRoleId, academicPeriodId, status: "approved");

        // Act: Teacher acknowledges assignment
        var response = await Client.PostAsync($"{_assignmentsUri}/{assignmentId}/acknowledge", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ackResult = await response.Content.ReadFromJsonAsync<AcknowledgeTeachingAssignmentResponse>();
        ackResult.Should().NotBeNull();
        ackResult!.Id.Should().Be(assignmentId);
        ackResult.TeacherAcknowledged.Should().BeTrue();
        ackResult.TeacherAcknowledgedDate.Year.Should().BeGreaterThan(2020);

        // Verify DB state
        const string checkSql = """
            SELECT teacher_acknowledged AS TeacherAcknowledged, teacher_acknowledged_date AS TeacherAcknowledgedDate
            FROM teaching.teaching_assignments
            WHERE id = @Id;
            """;
        var dbRow = await QuerySingleOrDefaultAsync<AssignmentDbRecord>(checkSql, new { Id = assignmentId });
        dbRow.Should().NotBeNull();
        dbRow!.TeacherAcknowledged.Should().BeTrue();
        dbRow.TeacherAcknowledgedDate.HasValue.Should().BeTrue();
    }

    [SkippableFact]
    public async Task UpdateAssignmentStatus_Should_DeductCommittedWorkload_WhenCancelled_And_PublishEvent()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var assignmentRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);
        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(
            connection, assignmentRoleId, academicPeriodId, status: "staffed");

        // Approved assignment with 4.00 workload commitment
        var assignmentId = await TeachingIntegrationTestHelpers.EnsureTeachingAssignmentAsync(
            connection, demandId, teacherId, assignmentRoleId, academicPeriodId, status: "approved");

        await TeachingIntegrationTestHelpers.EnsureWorkloadCommitmentAsync(
            connection, teacherId, academicPeriodId, plannedHours: 0.00m, committedHours: 4.00m, activeCount: 1);

        // Act: Cancel the assignment
        var cancelReq = new UpdateTeachingAssignmentStatusRequest("cancelled", Reason: "Course section cancelled by academics");
        var response = await Client.PostAsJsonAsync($"{_assignmentsUri}/{assignmentId}/status", cancelReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateResult = await response.Content.ReadFromJsonAsync<UpdateTeachingAssignmentStatusResponse>();
        updateResult.Should().NotBeNull();
        updateResult!.Status.Should().Be("cancelled");

        // Verify committed workload hours deducted from workload_commitments (4 -> 0, active count 1 -> 0)
        const string wcomSql = """
            SELECT id AS Id, planned_workload_hours AS PlannedWorkloadHours, committed_workload_hours AS CommittedWorkloadHours,
                   max_capacity_limit_hours AS MaxCapacityLimitHours, remaining_capacity_hours AS RemainingCapacityHours,
                   active_assignment_count AS ActiveAssignmentCount, commitment_status AS CommitmentStatus
            FROM teaching.workload_commitments
            WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId;
            """;
        var commitment = await QuerySingleOrDefaultAsync<WorkloadCommitmentDbRecord>(wcomSql, new { TeacherId = teacherId, PeriodId = academicPeriodId });
        commitment.Should().NotBeNull();
        commitment!.CommittedWorkloadHours.Should().Be(0.00m);
        commitment.ActiveAssignmentCount.Should().Be(0);

        // Verify demand reopened to 'accepted_for_staffing' since headcount dropped below required count
        const string demSql = "SELECT status FROM teaching.teaching_demands WHERE id = @Id;";
        var demandStatus = await ExecuteScalarAsync<string>(demSql, new { Id = demandId });
        demandStatus.Should().Be("accepted_for_staffing");

        // Verify domain event
        Factory.EventCollector.TeachingAssignmentCancelledEvents
            .Should().Contain(e => e.AssignmentId == assignmentId);
    }

    #endregion

    #region 4. Homeroom Advisory Assignment Tests

    [SkippableFact]
    public async Task HomeroomAdvisoryAssignment_Should_CountTowardWorkload_WithCohortScope()
    {
        // Arrange: Role HOMEROOM_ADVISOR with counts_toward_workload = 1
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var homeroomRoleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(
            connection, code: "HOMEROOM_ADVISOR", name: "Wali Kelas", isLeadRole: true, countsTowardWorkload: true);
        await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);

        var academicPeriodId = Guid.NewGuid();
        var studentCohortId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(
            connection, homeroomRoleId, academicPeriodId, status: "accepted_for_staffing");

        // Act 1: Propose homeroom advisory assignment with scope = cohort_group
        var proposeReq = new ProposeTeachingAssignmentRequest(
            TeachingDemandId: demandId,
            TeacherProfileId: teacherId,
            AssignmentRoleId: homeroomRoleId,
            AssignmentScope: "cohort_group",
            ScopeReferenceId: studentCohortId,
            ScopeLabel: "Kelas 4A (Wali Kelas)",
            AcademicPeriodId: academicPeriodId,
            EffectiveStartDate: new DateOnly(2026, 8, 1),
            EffectiveEndDate: new DateOnly(2026, 12, 15),
            WorkloadCommitmentValue: 2.00m,
            Notes: "Homeroom advisory staffing decoupled from student placement");

        var propResp = await Client.PostAsJsonAsync($"{_assignmentsUri}/propose", proposeReq);
        propResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var proposed = await propResp.Content.ReadFromJsonAsync<ProposeTeachingAssignmentResponse>();

        // Act 2: Authority approves homeroom advisory assignment
        var approveReq = new ApproveTeachingAssignmentRequest(ApprovedDate: new DateOnly(2026, 8, 1));
        var approveResp = await Client.PostAsJsonAsync($"{_assignmentsUri}/{proposed!.Id}/approve", approveReq);
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: Database persistence verifies scope category and label
        const string checkSql = """
            SELECT id AS Id, assignment_number AS AssignmentNumber, status AS Status,
                   assignment_scope AS AssignmentScope, scope_label AS ScopeLabel,
                   workload_commitment_value AS WorkloadCommitmentValue
            FROM teaching.teaching_assignments
            WHERE id = @Id;
            """;
        var dbAsn = await QuerySingleOrDefaultAsync<AssignmentDbRecord>(checkSql, new { proposed.Id });
        dbAsn.Should().NotBeNull();
        dbAsn!.AssignmentScope.Should().Be("cohort_group");
        dbAsn.ScopeLabel.Should().Be("Kelas 4A (Wali Kelas)");
        dbAsn.WorkloadCommitmentValue.Should().Be(2.00m);
        dbAsn.Status.Should().Be("approved");

        // Assert: Workload commitments correctly accounts for the 2.00 hours
        const string wcomSql = """
            SELECT committed_workload_hours AS CommittedWorkloadHours, active_assignment_count AS ActiveAssignmentCount
            FROM teaching.workload_commitments
            WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId;
            """;
        var wcom = await QuerySingleOrDefaultAsync<WorkloadCommitmentDbRecord>(wcomSql, new { TeacherId = teacherId, PeriodId = academicPeriodId });
        wcom.Should().NotBeNull();
        wcom!.CommittedWorkloadHours.Should().Be(2.00m);
        wcom.ActiveAssignmentCount.Should().Be(1);
    }

    #endregion

    #region 5. Capacity Conflicts Query & Resolution Tests

    [SkippableFact]
    public async Task CapacityConflicts_Should_SupportQueryFiltering_And_Resolution()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var academicPeriodId = Guid.NewGuid();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection, "active");

        var conflictId = await TeachingIntegrationTestHelpers.EnsureCapacityConflictAsync(
            connection,
            academicPeriodId: academicPeriodId,
            teacherProfileId: teacherId,
            conflictType: "schedule_overlap",
            severity: "blocking",
            status: "unresolved",
            description: "Teacher scheduled concurrently in two separate cohorts");

        // Act 1: Query conflicts filtering by academicPeriodId, severity, and status
        var queryResp = await Client.GetAsync(
            $"{_conflictsUri}?academicPeriodId={academicPeriodId}&severity=blocking&status=unresolved");
        queryResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedConflicts = await queryResp.Content.ReadFromJsonAsync<PaginatedList<CapacityConflictDto>>();
        pagedConflicts.Should().NotBeNull();
        pagedConflicts!.Items.Should().Contain(c => c.Id == conflictId && c.Severity == "blocking");

        // Act 2: Resolve the conflict
        var resolveReq = new ResolveCapacityConflictRequest(
            Status: "resolved",
            ResolutionAction: "workload_rebalanced",
            ResolutionNotes: "Adjusted timetable cohort schedule to eliminate overlap");

        var resolveResp = await Client.PostAsJsonAsync($"{_conflictsUri}/{conflictId}/resolve", resolveReq);

        // Assert
        resolveResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolvedResult = await resolveResp.Content.ReadFromJsonAsync<ResolveCapacityConflictResponse>();
        resolvedResult.Should().NotBeNull();
        resolvedResult!.Id.Should().Be(conflictId);
        resolvedResult.Status.Should().Be("resolved");
        resolvedResult.ResolutionAction.Should().Be("workload_rebalanced");
        resolvedResult.ResolutionNotes.Should().Be(resolveReq.ResolutionNotes);

        // Verify DB persistence
        const string checkSql = """
            SELECT id AS Id, status AS Status, resolution_action AS ResolutionAction,
                   resolved_by AS ResolvedBy, resolved_date AS ResolvedDate, resolution_notes AS ResolutionNotes
            FROM teaching.capacity_conflicts
            WHERE id = @Id;
            """;
        var dbConflict = await QuerySingleOrDefaultAsync<CapacityConflictDbRecord>(checkSql, new { Id = conflictId });
        dbConflict.Should().NotBeNull();
        dbConflict!.Status.Should().Be("resolved");
        dbConflict.ResolutionAction.Should().Be("workload_rebalanced");
        dbConflict.ResolvedBy.Should().NotBeNullOrWhiteSpace();
        dbConflict.ResolvedDate.Should().NotBeNull();
    }

    #endregion
}
