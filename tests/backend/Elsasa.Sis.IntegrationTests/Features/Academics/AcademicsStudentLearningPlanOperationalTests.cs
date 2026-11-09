namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using System.Net;
using System.Net.Http.Json;
using Dapper;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.AddAcademicPlanAdjustment;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.AddSlpLearningUnit;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ApproveStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ArchiveStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.CreateStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlanById;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlans;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ReviseStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.SubmitStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.SyncSlpParticipations;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.UpdateStudentLearningPlan;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Operational end-to-end integration test suite verifying the complete lifecycle of Student Learning Plans (IS013, P5-S15).
/// Verifies the full operational flow:
/// 1. Draft formulation (v1) for an enrolled student in an active period.
/// 2. Constituent learning units and academic adjustments setup.
/// 3. Plan submission for Academic Authority review.
/// 4. Formal Academic Authority initial approval with atomic status hand-off, immutable audit row in slp_approvals, and adjustment stamping.
/// 5. Single active plan invariant guard (activating a second draft plan for same student/period fails).
/// 6. Clone-on-revise engine (creates draft v2 with cloned units and unapproved adjustments while v1 remains active).
/// 7. Modification of draft v2 constituent units and adjustments.
/// 8. Revision approval with atomic hand-off (v1 transitions to revised, v2 transitions to active).
/// 9. Explicit participation synchronization (syncs approved required units to academic_participations with source = 'learning_plan').
/// 10. Synchronous cross-domain IAcademicsModule inquiry returning authoritative active plan DTO.
/// Also validates draft immutability guards, submission completeness rules, query filtering, and idempotency.
/// </summary>
public sealed class AcademicsStudentLearningPlanOperationalTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private IAcademicsModule CreateAcademicsModule()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IAcademicsModule>();
    }

    private async Task<(Guid AcademicYearId, Guid OperationalPeriodId, Guid Unit1Id, Guid Unit2Id, Guid Unit3Id)> SetupOperationalCurriculumAsync(string suffix)
    {
        await using var connection = await OpenConnectionAsync();
        var (ayId, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: $"Curriculum Framework {suffix}",
            version: "1.0");

        var unit1 = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: $"U1-{suffix[..Math.Min(suffix.Length, 8)]}",
            name: $"Remedial Math {suffix}",
            unitType: "course",
            creditWeight: 3.0m);

        var unit2 = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: $"U2-{suffix[..Math.Min(suffix.Length, 8)]}",
            name: $"Bahasa Indonesia {suffix}",
            unitType: "course",
            creditWeight: 3.0m);

        var unit3 = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: $"U3-{suffix[..Math.Min(suffix.Length, 8)]}",
            name: $"Adaptive PJOK {suffix}",
            unitType: "course",
            creditWeight: 2.0m);

        var approveResponse = await Client.PostAsync($"/api/v1/academics/learning-structures/{structure.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return (ayId, periodId, unit1.Id, unit2.Id, unit3.Id);
    }

    private static async Task EnsureStudentAsync(
        System.Data.Common.DbConnection connection,
        Guid studentId,
        string studentNumber,
        string fullName,
        string gradeLevel = "Grade 4")
    {
        const string insertStudentSql = """
            IF NOT EXISTS (SELECT 1 FROM students.students WHERE id = @StudentId)
            BEGIN
                INSERT INTO students.students (
                    id, student_number, full_name, government_id_number, date_of_birth,
                    enrollment_status, created_at, created_by
                ) VALUES (
                    @StudentId, @StudentNumber, @FullName, 'NISN-' + CAST(@StudentId AS NVARCHAR(36)), '2015-05-15',
                    'active', SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM students.administrative_placements WHERE student_id = @StudentId AND is_current = 1)
            BEGIN
                INSERT INTO students.administrative_placements (
                    id, student_id, school_level, grade, cohort, section,
                    academic_year, effective_date, is_current, created_at, created_by
                ) VALUES (
                    NEWID(), @StudentId, 'Elementary', @GradeLevel, @GradeLevel, 'A',
                    '2026/2027', '2026-08-01', 1, SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            """;

        await connection.ExecuteAsync(insertStudentSql, new
        {
            StudentId = studentId,
            StudentNumber = studentNumber,
            FullName = fullName,
            GradeLevel = gradeLevel
        });
    }

    [SkippableFact]
    public async Task StudentLearningPlan_OperationalLifecycle_FullEndToEndFlow_Should_SucceedAndEmitDomainEvents()
    {
        SkipIfDatabaseUnavailable();
        Factory.EventCollector.Reset();
        Factory.CurrentUser.Reset();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (_, periodId, unit1Id, unit2Id, unit3Id) = await SetupOperationalCurriculumAsync(suffix);

        var studentId = Guid.NewGuid();
        var studentNumber = $"STD-SLP-{suffix}";
        var studentName = $"SLP Persona {suffix}";

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, studentNumber, studentName);
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        // =========================================================================================================
        // Step 1. Formulate initial draft plan (v1) for student
        // =========================================================================================================
        var formulateRequest = new CreateStudentLearningPlanRequest(
            StudentId: studentId,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: $"Semester 1 {suffix}",
            Notes: "Initial draft formulated for individual learning support");

        var formulateResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-plans", formulateRequest);
        formulateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdPlan = await formulateResponse.Content.ReadFromJsonAsync<CreateStudentLearningPlanResponse>();
        createdPlan.Should().NotBeNull();
        createdPlan!.Id.Should().NotBeEmpty();
        var planV1Id = createdPlan.Id;
        createdPlan.StudentId.Should().Be(studentId);
        createdPlan.AcademicPeriodId.Should().Be(periodId);
        createdPlan.Status.Should().Be("draft");
        createdPlan.Version.Should().Be(1);

        Factory.EventCollector.StudentLearningPlanCreatedEvents
            .Should().ContainSingle(e => e.PlanId == planV1Id && e.StudentId == studentId && e.Version == 1);

        // =========================================================================================================
        // Step 2. Add constituent learning units and adjustments to draft plan
        // =========================================================================================================
        // Add Unit 1 as required
        var addUnit1Request = new AddSlpLearningUnitRequest(
            LearningUnitId: unit1Id,
            SequenceOrder: 1,
            IsRequired: true,
            Notes: "Remedial Math adaptation");
        var unit1Response = await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planV1Id}/units", addUnit1Request);
        unit1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var unit1Added = await unit1Response.Content.ReadFromJsonAsync<AddSlpLearningUnitResponse>();
        unit1Added.Should().NotBeNull();
        unit1Added!.LearningUnitId.Should().Be(unit1Id);
        unit1Added.IsRequired.Should().BeTrue();

        // Add Unit 2 as elective
        var addUnit2Request = new AddSlpLearningUnitRequest(
            LearningUnitId: unit2Id,
            SequenceOrder: 2,
            IsRequired: false,
            Notes: "Bahasa Indonesia elective");
        var unit2Response = await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planV1Id}/units", addUnit2Request);
        unit2Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var unit2Added = await unit2Response.Content.ReadFromJsonAsync<AddSlpLearningUnitResponse>();
        unit2Added.Should().NotBeNull();
        var unit2EntryId = unit2Added!.Id;

        // Add Accommodation adjustment
        var addAdj1Request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "accommodation",
            Description: "50% extra time for formal summative assessments",
            Rationale: "Documented processing speed and visual focus requirement",
            EffectiveDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2026, 12, 31),
            Notes: "Reviewed with learning specialist");
        var adj1Response = await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planV1Id}/adjustments", addAdj1Request);
        adj1Response.StatusCode.Should().Be(HttpStatusCode.Created);
        var adj1Added = await adj1Response.Content.ReadFromJsonAsync<AddAcademicPlanAdjustmentResponse>();
        adj1Added.Should().NotBeNull();
        adj1Added!.AdjustmentType.Should().Be("accommodation");
        adj1Added.ApprovedBy.Should().BeEmpty("unapproved draft adjustment must have empty approved_by");

        // Add Substitution adjustment
        var addAdj2Request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "substitution",
            Description: "Substitute Standard Physical Education with Adaptive Movement",
            Rationale: "Physician recommendation for low-impact physical activity",
            EffectiveDate: new DateOnly(2026, 8, 1),
            EndDate: null,
            Notes: null);
        var adj2Response = await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planV1Id}/adjustments", addAdj2Request);
        adj2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // =========================================================================================================
        // Step 3. Submit plan for Academic Authority review
        // =========================================================================================================
        var submitRequest = new SubmitStudentLearningPlanRequest("All units and accommodations verified by Academic Coordinator");
        var submitResponse = await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planV1Id}/submit", submitRequest);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var submittedPlan = await submitResponse.Content.ReadFromJsonAsync<SubmitStudentLearningPlanResponse>();
        submittedPlan.Should().NotBeNull();
        submittedPlan!.Status.Should().Be("draft", "submitted plan remains draft until formal authority approval");

        // =========================================================================================================
        // Step 4. Approve initial plan
        // =========================================================================================================
        var approveInitialRequest = new ApproveStudentLearningPlanRequest(
            ApprovalType: "initial_approval",
            Notes: "Formal Academic Authority approval granted for Semester 1");
        var approveInitialResponse = await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planV1Id}/approve", approveInitialRequest);
        approveInitialResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approvedInitial = await approveInitialResponse.Content.ReadFromJsonAsync<ApproveStudentLearningPlanResponse>();
        approvedInitial.Should().NotBeNull();
        approvedInitial!.Status.Should().Be("active");
        approvedInitial.Version.Should().Be(1);
        approvedInitial.ApprovalType.Should().Be("initial_approval");

        // Verify database state: status = active, slp_approvals record present, adjustments stamped
        var v1DbStatus = await connection.ExecuteScalarAsync<string>(
            "SELECT status FROM academics.student_learning_plans WHERE id = @Id",
            new { Id = planV1Id });
        v1DbStatus.Should().Be("active");

        var initialApprovalRow = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT approval_type, plan_version, approved_by FROM academics.slp_approvals WHERE student_learning_plan_id = @Id",
            new { Id = planV1Id });
        ((object?)initialApprovalRow).Should().NotBeNull();
        ((string)initialApprovalRow!.approval_type).Should().Be("initial_approval");
        ((int)initialApprovalRow.plan_version).Should().Be(1);

        var stampedAdjustments = (await connection.QueryAsync<dynamic>(
            "SELECT approved_by, approved_date FROM academics.academic_plan_adjustments WHERE student_learning_plan_id = @Id",
            new { Id = planV1Id })).ToList();
        stampedAdjustments.Should().HaveCount(2);
        foreach (var adj in stampedAdjustments)
        {
            ((string)adj.approved_by).Should().NotBeNullOrWhiteSpace();
            ((DateTime)adj.approved_date).Year.Should().BeGreaterThan(1900);
        }

        Factory.EventCollector.StudentLearningPlanApprovedEvents
            .Should().ContainSingle(e => e.PlanId == planV1Id && e.ApprovalType == "initial_approval" && e.Version == 1);

        // =========================================================================================================
        // Step 5. Attempting to activate a second draft plan for same student/period fails
        // =========================================================================================================
        // Attempting to formulate a new draft via API while an active plan exists should fail with Conflict
        var secondDraftFormulateResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-plans", formulateRequest);
        secondDraftFormulateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // If a draft somehow existed in database, attempting initial_approval on it while another active plan exists fails
        var secondDraftPlanId = Guid.NewGuid();
        await connection.ExecuteAsync("""
            INSERT INTO academics.student_learning_plans (
                id, student_id, academic_period_id, academic_period_label, status, version, notes, created_at, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, 'Conflicting Period', 'draft', 1, 'Manual concurrent draft', SYSUTCDATETIME(), 'TEST'
            );
            """, new { Id = secondDraftPlanId, StudentId = studentId, PeriodId = periodId });

        var secondApproveResponse = await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{secondDraftPlanId}/approve",
            new ApproveStudentLearningPlanRequest("initial_approval", "Should fail due to active plan"));
        secondApproveResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Clean up the manual second draft to maintain clean testing state
        await connection.ExecuteAsync("DELETE FROM academics.student_learning_plans WHERE id = @Id", new { Id = secondDraftPlanId });

        // =========================================================================================================
        // Step 6. Execute ReviseStudentLearningPlan: verify draft v2 is created with cloned units/adjustments, v1 remains active
        // =========================================================================================================
        var reviseRequest = new ReviseStudentLearningPlanRequest("Mid-semester review: adding adaptive PJOK unit and sensory accommodation");
        var reviseResponse = await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planV1Id}/revise", reviseRequest);
        reviseResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var revisedPlan = await reviseResponse.Content.ReadFromJsonAsync<ReviseStudentLearningPlanResponse>();
        revisedPlan.Should().NotBeNull();
        var planV2Id = revisedPlan!.Id;
        revisedPlan.PriorPlanId.Should().Be(planV1Id);
        revisedPlan.Version.Should().Be(2);
        revisedPlan.Status.Should().Be("draft");

        // Invariant: v1 MUST remain strictly 'active' during revision drafting
        var v1StatusAfterRevision = await connection.ExecuteScalarAsync<string>(
            "SELECT status FROM academics.student_learning_plans WHERE id = @Id",
            new { Id = planV1Id });
        v1StatusAfterRevision.Should().Be("active");

        // Verify draft v2 deep-cloned units and adjustments
        var v2Units = (await connection.QueryAsync<dynamic>(
            "SELECT learning_unit_id, sequence_order, is_required FROM academics.slp_learning_units WHERE student_learning_plan_id = @Id",
            new { Id = planV2Id })).ToList();
        v2Units.Should().HaveCount(2, "v2 must deep-clone all constituent units from v1");

        var v2Adjustments = (await connection.QueryAsync<dynamic>(
            "SELECT adjustment_type, approved_by, approved_date FROM academics.academic_plan_adjustments WHERE student_learning_plan_id = @Id",
            new { Id = planV2Id })).ToList();
        v2Adjustments.Should().HaveCount(2, "v2 must deep-clone all adjustments from v1");
        foreach (var adj in v2Adjustments)
        {
            ((string)adj.approved_by).Should().BeEmpty("cloned adjustments in draft v2 must be reset to unapproved");
        }

        Factory.EventCollector.StudentLearningPlanRevisedEvents
            .Should().ContainSingle(e => e.PriorPlanId == planV1Id && e.NewPlanId == planV2Id && e.NewVersion == 2);

        // =========================================================================================================
        // Step 7. Modify draft v2 units/adjustments
        // =========================================================================================================
        // Add Unit 3 (Adaptive PJOK) as required to v2
        var addUnit3ToV2 = new AddSlpLearningUnitRequest(
            LearningUnitId: unit3Id,
            SequenceOrder: 3,
            IsRequired: true,
            Notes: "Adaptive PJOK added in revision");
        var addUnit3Response = await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planV2Id}/units", addUnit3ToV2);
        addUnit3Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Remove the elective Unit 2 from v2
        var v2Unit2EntryId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT id FROM academics.slp_learning_units WHERE student_learning_plan_id = @PlanId AND learning_unit_id = @UnitId",
            new { PlanId = planV2Id, UnitId = unit2Id });
        var removeUnitResponse = await Client.DeleteAsync($"/api/v1/academics/learning-plans/{planV2Id}/units/{v2Unit2EntryId}");
        removeUnitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Add third adjustment: Exemption
        var addAdj3Request = new AddAcademicPlanAdjustmentRequest(
            AdjustmentType: "exemption",
            Description: "Exemption from standardized endurance fitness test",
            Rationale: "Cardiovascular pacing requirement based on specialist directive",
            EffectiveDate: new DateOnly(2026, 9, 1),
            EndDate: null,
            Notes: null);
        var addAdj3Response = await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planV2Id}/adjustments", addAdj3Request);
        addAdj3Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Submit v2 for review
        var submitV2Response = await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{planV2Id}/submit",
            new SubmitStudentLearningPlanRequest("v2 modifications finalized"));
        submitV2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // =========================================================================================================
        // Step 8. Execute ApproveStudentLearningPlan (revision_approval): v1 atomically transitions to revised, v2 to active
        // =========================================================================================================
        var approveRevisionRequest = new ApproveStudentLearningPlanRequest(
            ApprovalType: "revision_approval",
            Notes: "Academic Authority approves v2 revision");
        var approveRevisionResponse = await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planV2Id}/approve", approveRevisionRequest);
        approveRevisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedRevision = await approveRevisionResponse.Content.ReadFromJsonAsync<ApproveStudentLearningPlanResponse>();
        approvedRevision.Should().NotBeNull();
        approvedRevision!.Status.Should().Be("active");
        approvedRevision.Version.Should().Be(2);
        approvedRevision.ApprovalType.Should().Be("revision_approval");

        // Verify atomic hand-off in database: v1 is now 'revised', v2 is now 'active'
        var finalV1Status = await connection.ExecuteScalarAsync<string>(
            "SELECT status FROM academics.student_learning_plans WHERE id = @Id",
            new { Id = planV1Id });
        finalV1Status.Should().Be("revised");

        var finalV2Status = await connection.ExecuteScalarAsync<string>(
            "SELECT status FROM academics.student_learning_plans WHERE id = @Id",
            new { Id = planV2Id });
        finalV2Status.Should().Be("active");

        // Verify revision approval record added
        var v2ApprovalCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM academics.slp_approvals WHERE student_learning_plan_id = @Id AND approval_type = 'revision_approval' AND plan_version = 2",
            new { Id = planV2Id });
        v2ApprovalCount.Should().Be(1);

        // Verify v2 adjustments are now stamped
        var v2StampedCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM academics.academic_plan_adjustments WHERE student_learning_plan_id = @Id AND approved_by <> ''",
            new { Id = planV2Id });
        v2StampedCount.Should().Be(3);

        Factory.EventCollector.StudentLearningPlanApprovedEvents
            .Should().ContainSingle(e => e.PlanId == planV2Id && e.ApprovalType == "revision_approval" && e.Version == 2);

        // =========================================================================================================
        // Step 9. Execute SyncSlpParticipations: verify required units registered in academic_participations
        // =========================================================================================================
        var syncResponse = await Client.PostAsync($"/api/v1/academics/learning-plans/{planV2Id}/sync-participations", null);
        syncResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var syncResult = await syncResponse.Content.ReadFromJsonAsync<SyncSlpParticipationsResponse>();
        syncResult.Should().NotBeNull();
        syncResult!.PlanId.Should().Be(planV2Id);
        syncResult.StudentId.Should().Be(studentId);
        syncResult.SyncedCount.Should().Be(2, "v2 has two required units: Unit 1 and Unit 3");
        syncResult.TotalUnits.Should().Be(2);

        // Verify academic_participations table in database
        var syncedParticipations = (await connection.QueryAsync<dynamic>(
            """
            SELECT learning_unit_id, source, status, student_learning_plan_id
            FROM academics.academic_participations
            WHERE student_id = @StudentId AND academic_period_id = @PeriodId
            """,
            new { StudentId = studentId, PeriodId = periodId })).ToList();

        syncedParticipations.Should().HaveCount(2);
        foreach (var p in syncedParticipations)
        {
            ((string)p.source).Should().Be("learning_plan");
            ((string)p.status).Should().Be("in_progress");
            ((Guid)p.student_learning_plan_id).Should().Be(planV2Id);
        }

        var syncedUnitIds = syncedParticipations.Select(p => (Guid)p.learning_unit_id).ToList();
        syncedUnitIds.Should().Contain(unit1Id);
        syncedUnitIds.Should().Contain(unit3Id);

        Factory.EventCollector.StudentLearningPlanParticipationsSyncedEvents
            .Should().ContainSingle(e => e.PlanId == planV2Id && e.StudentId == studentId && e.SyncedCount == 2);

        // =========================================================================================================
        // Step 10. Invoke IAcademicsModule.GetActiveStudentLearningPlanAsync and verify accurate DTO return
        // =========================================================================================================
        var academicsModule = CreateAcademicsModule();
        var activePlanResult = await academicsModule.GetActiveStudentLearningPlanAsync(studentId, periodId);

        activePlanResult.IsSuccess.Should().BeTrue();
        activePlanResult.Value.Should().NotBeNull();

        var activeDto = activePlanResult.Value!;
        activeDto.PlanId.Should().Be(planV2Id);
        activeDto.StudentId.Should().Be(studentId);
        activeDto.AcademicPeriodId.Should().Be(periodId);
        activeDto.Version.Should().Be(2);
        activeDto.Status.Should().Be("active");

        activeDto.ConstituentUnits.Should().HaveCount(2);
        activeDto.ConstituentUnits.Select(u => u.LearningUnitId).Should().Contain([unit1Id, unit3Id]);

        activeDto.Adjustments.Should().HaveCount(3);
        activeDto.Adjustments.Select(a => a.AdjustmentType).Should().Contain(["accommodation", "substitution", "exemption"]);
    }

    [SkippableFact]
    public async Task StudentLearningPlan_DraftImmutability_Should_RejectModificationsToActiveOrRevisedPlan()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (_, periodId, unit1Id, _, _) = await SetupOperationalCurriculumAsync(suffix);
        var studentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, $"STD-IMM-{suffix}", $"Immutability Student {suffix}");
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        // 1. Create and approve plan v1 to make it active
        var createResponse = await Client.PostAsJsonAsync(
            "/api/v1/academics/learning-plans",
            new CreateStudentLearningPlanRequest(studentId, periodId, "Semester 1", "Active plan for immutability tests"));
        var plan = await createResponse.Content.ReadFromJsonAsync<CreateStudentLearningPlanResponse>();
        var activePlanId = plan!.Id;

        // Add 1 unit and 1 adjustment so submission/approval succeed
        await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{activePlanId}/units",
            new AddSlpLearningUnitRequest(unit1Id, 1, true));

        var adjResponse = await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{activePlanId}/adjustments",
            new AddAcademicPlanAdjustmentRequest("accommodation", "Quiet testing room", "Sensory need", new DateOnly(2026, 8, 1)));
        var adj = await adjResponse.Content.ReadFromJsonAsync<AddAcademicPlanAdjustmentResponse>();
        var adjId = adj!.Id;

        await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{activePlanId}/submit", new SubmitStudentLearningPlanRequest());
        await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{activePlanId}/approve", new ApproveStudentLearningPlanRequest("initial_approval"));

        // 2. Verifications: Active plan must reject modifications
        // 2a. Update notes
        var updateNotesResponse = await Client.PutAsJsonAsync(
            $"/api/v1/academics/learning-plans/{activePlanId}",
            new UpdateStudentLearningPlanRequest("Attempting to modify active plan notes"));
        updateNotesResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 2b. Add constituent unit
        var addUnitResponse = await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{activePlanId}/units",
            new AddSlpLearningUnitRequest(unit1Id, 2, false));
        addUnitResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 2c. Remove constituent unit
        var unitEntryId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT id FROM academics.slp_learning_units WHERE student_learning_plan_id = @PlanId",
            new { PlanId = activePlanId });
        var removeUnitResponse = await Client.DeleteAsync($"/api/v1/academics/learning-plans/{activePlanId}/units/{unitEntryId}");
        removeUnitResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 2d. Add adjustment
        var addAdjResponse = await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{activePlanId}/adjustments",
            new AddAcademicPlanAdjustmentRequest("exemption", "Exemption test", "Rationale for immutability", new DateOnly(2026, 8, 1)));
        addAdjResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 2e. Remove adjustment
        var removeAdjResponse = await Client.DeleteAsync($"/api/v1/academics/learning-plans/{activePlanId}/adjustments/{adjId}");
        removeAdjResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task StudentLearningPlan_SubmissionValidation_Should_RejectEmptyPlanWithoutUnitsOrAdjustments()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (_, periodId, _, _, _) = await SetupOperationalCurriculumAsync(suffix);
        var studentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, $"STD-EMP-{suffix}", $"Empty Plan Student {suffix}");
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        // Create empty draft plan
        var createResponse = await Client.PostAsJsonAsync(
            "/api/v1/academics/learning-plans",
            new CreateStudentLearningPlanRequest(studentId, periodId, "Semester 1", "Draft without units"));
        var plan = await createResponse.Content.ReadFromJsonAsync<CreateStudentLearningPlanResponse>();

        // Attempting to submit empty plan must be rejected (min 1 unit or adjustment required)
        var submitResponse = await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{plan!.Id}/submit",
            new SubmitStudentLearningPlanRequest());
        submitResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task StudentLearningPlan_Queries_Should_ReturnAccurateDetailAndRosterAndActiveLookup()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (_, periodId, unit1Id, _, _) = await SetupOperationalCurriculumAsync(suffix);
        var studentId = Guid.NewGuid();
        var studentNumber = $"STD-QRY-{suffix}";
        var studentName = $"Query Testing Student {suffix}";

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, studentNumber, studentName);
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        // Before plan creation: active lookup should return 404
        var preActiveResponse = await Client.GetAsync($"/api/v1/academics/students/{studentId}/learning-plans/active?academicPeriodId={periodId}");
        preActiveResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Create, populate, submit, and approve
        var createResponse = await Client.PostAsJsonAsync(
            "/api/v1/academics/learning-plans",
            new CreateStudentLearningPlanRequest(studentId, periodId, "Semester 1", "Query suite plan"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateStudentLearningPlanResponse>();
        var planId = created!.Id;

        await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{planId}/units",
            new AddSlpLearningUnitRequest(unit1Id, 1, true, "Query unit notes"));
        await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{planId}/adjustments",
            new AddAcademicPlanAdjustmentRequest("accommodation", "Extra testing time", "Processing accommodation", new DateOnly(2026, 8, 1)));
        await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planId}/submit", new SubmitStudentLearningPlanRequest());
        await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planId}/approve", new ApproveStudentLearningPlanRequest("initial_approval", "Approved"));

        // 1. Test GetStudentLearningPlanById
        var detailResponse = await Client.GetAsync($"/api/v1/academics/learning-plans/{planId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailDto = await detailResponse.Content.ReadFromJsonAsync<StudentLearningPlanDetailDto>();
        detailDto.Should().NotBeNull();
        detailDto!.Id.Should().Be(planId);
        detailDto.StudentName.Should().Be(studentName);
        detailDto.StudentNis.Should().Be(studentNumber);
        detailDto.Status.Should().Be("active");
        detailDto.ConstituentUnits.Should().HaveCount(1);
        detailDto.Adjustments.Should().HaveCount(1);
        detailDto.Approvals.Should().HaveCount(1);

        // 2. Test GetStudentLearningPlans (paginated roster)
        var rosterResponse = await Client.GetAsync($"/api/v1/academics/learning-plans?academicPeriodId={periodId}&status=active&studentSearch={studentNumber}");
        rosterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rosterResult = await rosterResponse.Content.ReadFromJsonAsync<PagedResult<StudentLearningPlanListItemDto>>();
        rosterResult.Should().NotBeNull();
        rosterResult!.Items.Should().ContainSingle(i => i.Id == planId);
        var item = rosterResult.Items.Single(i => i.Id == planId);
        item.StudentName.Should().Be(studentName);
        item.UnitsCount.Should().Be(1);
        item.AdjustmentsCount.Should().Be(1);

        // 3. Test GetActiveStudentLearningPlan via endpoint
        var postActiveResponse = await Client.GetAsync($"/api/v1/academics/students/{studentId}/learning-plans/active?academicPeriodId={periodId}");
        postActiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activeDto = await postActiveResponse.Content.ReadFromJsonAsync<ActiveStudentLearningPlanDto>();
        activeDto.Should().NotBeNull();
        activeDto!.PlanId.Should().Be(planId);
        activeDto.ConstituentUnits.Should().HaveCount(1);
        activeDto.Adjustments.Should().HaveCount(1);
    }

    [SkippableFact]
    public async Task StudentLearningPlan_SyncParticipations_Should_BeIdempotent()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (_, periodId, unit1Id, _, _) = await SetupOperationalCurriculumAsync(suffix);
        var studentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, $"STD-SYN-{suffix}", $"Sync Idempotent Student {suffix}");
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        var createResponse = await Client.PostAsJsonAsync(
            "/api/v1/academics/learning-plans",
            new CreateStudentLearningPlanRequest(studentId, periodId, "Semester 1", "Sync test"));
        var plan = await createResponse.Content.ReadFromJsonAsync<CreateStudentLearningPlanResponse>();
        var planId = plan!.Id;

        await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{planId}/units",
            new AddSlpLearningUnitRequest(unit1Id, 1, true));
        await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planId}/submit", new SubmitStudentLearningPlanRequest());
        await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planId}/approve", new ApproveStudentLearningPlanRequest("initial_approval"));

        // First sync call
        var sync1Response = await Client.PostAsync($"/api/v1/academics/learning-plans/{planId}/sync-participations", null);
        sync1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sync1 = await sync1Response.Content.ReadFromJsonAsync<SyncSlpParticipationsResponse>();
        sync1!.SyncedCount.Should().Be(1);

        // Second sync call - must be idempotent (0 new rows, 0 duplicate constraint errors)
        var sync2Response = await Client.PostAsync($"/api/v1/academics/learning-plans/{planId}/sync-participations", null);
        sync2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sync2 = await sync2Response.Content.ReadFromJsonAsync<SyncSlpParticipationsResponse>();
        sync2!.SyncedCount.Should().Be(0, "second sync must be idempotent with 0 newly inserted participations");

        var totalParticipations = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM academics.academic_participations WHERE student_id = @StudentId AND academic_period_id = @PeriodId",
            new { StudentId = studentId, PeriodId = periodId });
        totalParticipations.Should().Be(1, "only 1 participation record must exist despite repeated syncs");
    }

    [SkippableFact]
    public async Task StudentLearningPlan_CloneOnRevise_PendingDraftGuard_Should_PreventMultipleDraftRevisions()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (_, periodId, unit1Id, _, _) = await SetupOperationalCurriculumAsync(suffix);
        var studentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, $"STD-REV-{suffix}", $"Revision Guard Student {suffix}");
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        var createResponse = await Client.PostAsJsonAsync(
            "/api/v1/academics/learning-plans",
            new CreateStudentLearningPlanRequest(studentId, periodId, "Semester 1", "Plan for revision guard"));
        var plan = await createResponse.Content.ReadFromJsonAsync<CreateStudentLearningPlanResponse>();
        var planId = plan!.Id;

        await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{planId}/units",
            new AddSlpLearningUnitRequest(unit1Id, 1, true));
        await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planId}/submit", new SubmitStudentLearningPlanRequest());
        await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planId}/approve", new ApproveStudentLearningPlanRequest("initial_approval"));

        // First revision creates draft v2
        var rev1Response = await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{planId}/revise",
            new ReviseStudentLearningPlanRequest("First revision"));
        rev1Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Attempting to revise the active plan again while draft v2 is still pending MUST be rejected
        var rev2Response = await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{planId}/revise",
            new ReviseStudentLearningPlanRequest("Second concurrent revision"));
        rev2Response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task StudentLearningPlan_ArchiveLifecycle_Should_TransitionActiveAndRevisedPlansToArchivedOrCompleted()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (_, periodId, unit1Id, _, _) = await SetupOperationalCurriculumAsync(suffix);
        var studentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, $"STD-ARC-{suffix}", $"Archival Student {suffix}");
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        var createResponse = await Client.PostAsJsonAsync(
            "/api/v1/academics/learning-plans",
            new CreateStudentLearningPlanRequest(studentId, periodId, "Semester 1", "Plan for archive test"));
        var plan = await createResponse.Content.ReadFromJsonAsync<CreateStudentLearningPlanResponse>();
        var planId = plan!.Id;

        await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{planId}/units",
            new AddSlpLearningUnitRequest(unit1Id, 1, true));
        await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planId}/submit", new SubmitStudentLearningPlanRequest());
        await Client.PostAsJsonAsync($"/api/v1/academics/learning-plans/{planId}/approve", new ApproveStudentLearningPlanRequest("initial_approval"));

        // Archive plan
        var archiveResponse = await Client.PostAsJsonAsync(
            $"/api/v1/academics/learning-plans/{planId}/archive",
            new ArchiveStudentLearningPlanRequest("archived", "Archiving obsolete plan"));
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archived = await archiveResponse.Content.ReadFromJsonAsync<ArchiveStudentLearningPlanResponse>();
        archived!.Status.Should().Be("archived");

        var dbStatus = await connection.ExecuteScalarAsync<string>(
            "SELECT status FROM academics.student_learning_plans WHERE id = @Id",
            new { Id = planId });
        dbStatus.Should().Be("archived");
    }

    [SkippableFact]
    public async Task StudentLearningPlan_FormulateWithAcademicYearId_Should_Succeed()
    {
        SkipIfDatabaseUnavailable();
        Factory.EventCollector.Reset();
        Factory.CurrentUser.Reset();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (ayId, _, _, _, _) = await SetupOperationalCurriculumAsync(suffix);
        var studentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await EnsureStudentAsync(connection, studentId, $"STD-AY-{suffix}", $"AY Student {suffix}");
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        var formulateRequest = new CreateStudentLearningPlanRequest(
            StudentId: studentId,
            AcademicPeriodId: ayId,
            AcademicPeriodLabel: $"Tahun Pelajaran {suffix}",
            Notes: "Plan formulated with academic year container ID");

        var formulateResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-plans", formulateRequest);
        formulateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdPlan = await formulateResponse.Content.ReadFromJsonAsync<CreateStudentLearningPlanResponse>();
        createdPlan.Should().NotBeNull();
        createdPlan!.AcademicPeriodId.Should().Be(ayId);
        createdPlan.Status.Should().Be("draft");
    }
}
