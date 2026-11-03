namespace Elsasa.Sis.IntegrationTests.Features.Students.Admission;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationById;
using Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;
using Elsasa.Sis.Students.Features.Admission.RegisterApplication;
using Elsasa.Sis.Students.Features.Admission.StartApplicationReview;
using Elsasa.Sis.Students.Features.Admission.WithdrawAdmissionApplication;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests verifying the admission decision lifecycle, decision immutability protection,
/// deferral period validation, and post-commit event publishing (P5-S08, P5-S09, ADR-003, ADR-004).
/// </summary>
public sealed class AdmissionDecisionIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private async Task<(Guid AcademicYearId, Guid AdmissionPeriodId, Guid ApplicationId, Guid ApplicantId)> SetupRegisteredApplicationAsync(string suffix)
    {
        var ayRequest = new CreateAcademicYearRequest(
            Code: $"AY-{suffix}",
            Name: "Decision Test AY",
            StartDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2027, 7, 31));

        var ayResponse = await Client.PostAsJsonAsync("/api/operations/academic-years", ayRequest);
        ayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        await Client.PostAsync($"/api/operations/academic-years/{ay!.Id}/approve", null);
        await Client.PostAsync($"/api/operations/academic-years/{ay.Id}/activate", null);

        var periodRequest = new CreateOperationalPeriodRequest(
            PeriodCode: $"ADM-{suffix}",
            Name: "Decision Admission Window",
            PeriodType: "admission_window",
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25)));

        var periodResponse = await Client.PostAsJsonAsync($"/api/operations/academic-years/{ay.Id}/periods", periodRequest);
        periodResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var period = await periodResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();

        await ExecuteAsync("UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;", new { Id = period!.Id });

        var regRequest = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: "Rizky Ramadhan",
            DateOfBirth: new DateOnly(2018, 10, 1),
            GovernmentIdType: "NIK",
            GovernmentIdNumber: $"3171{Random.Shared.Next(10000000, 99999999)}0006",
            AdmissionPeriodId: period!.Id,
            RequestedEntryLevel: "Grade 1",
            Contacts:
            [
                new RegisterApplicationContactRequest("Hasan Ramadhan", "Father", "+6281255556666", IsPrimary: true)
            ]);

        var regResponse = await Client.PostAsJsonAsync("/api/students/admission/applications", regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var reg = await regResponse.Content.ReadFromJsonAsync<RegisterApplicationResponse>();

        return (ay.Id, period.Id, reg!.Id, reg.ApplicantId);
    }

    [SkippableFact]
    public async Task AdmissionLifecycle_Should_ProgressFromRegisteredToUnderReviewToDecided_And_PublishAcceptedEvent()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (academicYearId, _, applicationId, applicantId) = await SetupRegisteredApplicationAsync($"DEC-{Guid.NewGuid():N}"[..8]);

        // 1. Act: Transition from registered -> under_review
        var startReviewResponse = await Client.PostAsync($"/api/students/admission/applications/{applicationId}/start-review", null);

        // Assert review transition
        startReviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviewResult = await startReviewResponse.Content.ReadFromJsonAsync<StartApplicationReviewResponse>();
        reviewResult.Should().NotBeNull();
        reviewResult!.Status.Should().Be("under_review");

        // Verify status in Database
        const string checkStatusSql = "SELECT status FROM students.admission_applications WHERE id = @Id;";
        var currentStatus = await ExecuteScalarAsync<string>(checkStatusSql, new { Id = applicationId });
        currentStatus.Should().Be("under_review");

        // 2. Act: Record Decision -> Accepted
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var decisionRequest = new RecordAdmissionDecisionRequest(
            DecisionType: "accepted",
            DecisionDate: today,
            EffectiveDate: today,
            Reason: "Candidate met all entrance criteria and diagnostic assessment benchmarks.",
            SupportingReference: "MIN-ADMIS-2026-088");

        var decisionResponse = await Client.PostAsJsonAsync(
            $"/api/students/admission/applications/{applicationId}/decisions",
            decisionRequest);

        // Assert Decision
        decisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var decisionResult = await decisionResponse.Content.ReadFromJsonAsync<RecordAdmissionDecisionResponse>();
        decisionResult.Should().NotBeNull();
        decisionResult!.DecisionType.Should().Be("accepted");
        decisionResult.ApplicationId.Should().Be(applicationId);

        // 3. Verify Database state: application status = 'decided'
        var decidedStatus = await ExecuteScalarAsync<string>(checkStatusSql, new { Id = applicationId });
        decidedStatus.Should().Be("decided");

        const string checkDecisionSql = """
            SELECT decision_type AS DecisionType, supporting_reference AS SupportingReference
            FROM students.admission_decisions
            WHERE application_id = @Id;
            """;
        var dbDecision = await QuerySingleOrDefaultAsync<DecisionDbRow>(checkDecisionSql, new { Id = applicationId });
        dbDecision.Should().NotBeNull();
        dbDecision!.DecisionType.Should().Be("accepted");
        dbDecision.SupportingReference.Should().Be("MIN-ADMIS-2026-088");

        // 4. Verify Post-Commit Event Notification via MediatR (ADR-003)
        Factory.EventCollector.ApplicantAcceptedEvents.Should().ContainSingle(evt =>
            evt.ApplicantId == applicantId &&
            evt.ApplicationId == applicationId &&
            evt.AcademicYearId == academicYearId);

        // 5. Verify Dossier query returns the recorded decision
        var dossierResponse = await Client.GetAsync($"/api/students/admission/applications/{applicationId}");
        dossierResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dossier = await dossierResponse.Content.ReadFromJsonAsync<AdmissionApplicationDetailResponse>();
        dossier.Should().NotBeNull();
        dossier!.Status.Should().Be("decided");
        dossier.Decision.Should().NotBeNull();
        dossier.Decision!.DecisionType.Should().Be("accepted");
    }

    [SkippableFact]
    public async Task RecordAdmissionDecision_Should_ReturnConflict_When_DecisionAlreadyExists()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (_, _, applicationId, _) = await SetupRegisteredApplicationAsync($"IMM-{Guid.NewGuid():N}"[..8]);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var decisionRequest1 = new RecordAdmissionDecisionRequest(
            DecisionType: "rejected",
            DecisionDate: today,
            EffectiveDate: today,
            Reason: "Did not meet entrance criteria.");

        var response1 = await Client.PostAsJsonAsync(
            $"/api/students/admission/applications/{applicationId}/decisions",
            decisionRequest1);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Attempt to record second decision on already decided application
        var decisionRequest2 = new RecordAdmissionDecisionRequest(
            DecisionType: "accepted",
            DecisionDate: today,
            EffectiveDate: today,
            Reason: "Attempting duplicate/revised decision.");

        var response2 = await Client.PostAsJsonAsync(
            $"/api/students/admission/applications/{applicationId}/decisions",
            decisionRequest2);

        // Assert: 409 Conflict
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Attempting to withdraw after decided also returns 409 Conflict
        var withdrawRequest = new WithdrawAdmissionApplicationRequest("Late withdrawal attempt");
        var withdrawResponse = await Client.PostAsJsonAsync(
            $"/api/students/admission/applications/{applicationId}/withdraw",
            withdrawRequest);
        withdrawResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task RecordAdmissionDecision_Should_SupportDeferredDecision_WithValidOperationalPeriod()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (academicYearId, _, applicationId, _) = await SetupRegisteredApplicationAsync($"DEF-{Guid.NewGuid():N}"[..8]);

        // Create target deferred operational period
        var deferredPeriodRequest = new CreateOperationalPeriodRequest(
            PeriodCode: $"DEF-{Guid.NewGuid():N}"[..14],
            Name: "Deferred Target Semester",
            PeriodType: "semester",
            StartDate: new DateOnly(2027, 2, 1),
            EndDate: new DateOnly(2027, 6, 30));

        var deferredPeriodResponse = await Client.PostAsJsonAsync(
            $"/api/operations/academic-years/{academicYearId}/periods",
            deferredPeriodRequest);
        deferredPeriodResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var deferredPeriod = await deferredPeriodResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();

        await ExecuteAsync("UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;", new { Id = deferredPeriod!.Id });

        // Act: Record deferred decision pointing to valid deferred period
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var deferredDecisionRequest = new RecordAdmissionDecisionRequest(
            DecisionType: "deferred",
            DecisionDate: today,
            EffectiveDate: new DateOnly(2027, 2, 1),
            Reason: "Family relocation deferred to second semester.",
            DeferredAdmissionPeriodId: deferredPeriod!.Id);

        var response = await Client.PostAsJsonAsync(
            $"/api/students/admission/applications/{applicationId}/decisions",
            deferredDecisionRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RecordAdmissionDecisionResponse>();
        result.Should().NotBeNull();
        result!.DecisionType.Should().Be("deferred");

        // Accepted event should NOT be published for deferred decisions
        Factory.EventCollector.ApplicantAcceptedEvents.Should().NotContain(evt => evt.ApplicationId == applicationId);
    }

    private sealed record DecisionDbRow(string DecisionType, string SupportingReference);
}
