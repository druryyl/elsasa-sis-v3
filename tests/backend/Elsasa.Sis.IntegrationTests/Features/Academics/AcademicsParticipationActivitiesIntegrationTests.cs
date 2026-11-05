namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Academics.Features.AcademicParticipations.BatchRegisterCohortParticipation;
using Elsasa.Sis.Academics.Features.AcademicParticipations.ConcludeParticipation;
using Elsasa.Sis.Academics.Features.AcademicParticipations.RecognizePriorCredit;
using Elsasa.Sis.Academics.Features.AcademicParticipations.RegisterParticipation;
using Elsasa.Sis.Academics.Features.Assignments.CloseAssignment;
using Elsasa.Sis.Academics.Features.Assignments.CreateAssignment;
using Elsasa.Sis.Academics.Features.Assignments.GetAssignmentSubmissions;
using Elsasa.Sis.Academics.Features.Assignments.ReleaseAssignment;
using Elsasa.Sis.Academics.Features.Assignments.SubmitCoursework;
using Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance;
using Elsasa.Sis.Academics.Features.Attendance.RecordSessionAttendance;
using Elsasa.Sis.Academics.Features.LearningActivities.ConductLearningActivity;
using Elsasa.Sis.Academics.Features.LearningActivities.PlanLearningActivity;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Operational end-to-end integration test suite verifying Submodule 2 workflows (IS009, P6-S16).
/// Covers cohort batch registration, repeat attempt counters, prior credit recognition,
/// activity conduction with provisional teacher context, attendance roll-call and immutable corrections,
/// coursework assignment lifecycle, resubmission lineage, dynamic missing projections, and cross-domain inquiries.
/// </summary>
public sealed class AcademicsParticipationActivitiesIntegrationTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private IAcademicsModule CreateAcademicsModule()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IAcademicsModule>();
    }

    private async Task<(Guid PeriodId, Guid UnitId)> SetupActiveCurriculumUnitAsync(string testSuffix)
    {
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, testSuffix);

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: $"Curriculum Framework {testSuffix}",
            version: "1.0");

        var unit = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: $"U-{testSuffix[..Math.Min(testSuffix.Length, 8)]}",
            name: $"Foundational Unit {testSuffix}",
            unitType: "course",
            creditWeight: 4.0m);

        var approveResponse = await Client.PostAsync($"/api/v1/academics/learning-structures/{structure.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return (periodId, unit.Id);
    }

    [SkippableFact]
    public async Task Criterion01_BatchRegisterCohortParticipation_Should_EnforceEnrollment_And_PublishEvents()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT01");

        var student1 = Guid.NewGuid();
        var student2 = Guid.NewGuid();
        var unenrolledStudent = Guid.NewGuid();

        // Simulate enrollment: student1 and student2 enrolled, unenrolledStudent explicitly not enrolled
        Factory.StudentsModule.MarkStudentEnrolled(student1);
        Factory.StudentsModule.MarkStudentEnrolled(student2);
        Factory.StudentsModule.MarkStudentUnenrolled(unenrolledStudent);

        // 1. Attempt batch registration with unenrolled student -> should fail with validation error
        var invalidRequest = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodLabel: "2026-Fall",
            AcademicPeriodId: periodId,
            LearningUnitIds: [unitId],
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 8, 15),
            StudentIds: [student1, unenrolledStudent]);

        var failResponse = await Client.PostAsJsonAsync("/api/v1/academics/participations/batch-cohort", invalidRequest);
        failResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // 2. Successful batch registration with active enrolled students
        var validRequest = new BatchRegisterCohortParticipationRequest(
            AcademicPeriodLabel: "2026-Fall",
            AcademicPeriodId: periodId,
            LearningUnitIds: [unitId],
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 8, 15),
            StudentIds: [student1, student2]);

        var successResponse = await Client.PostAsJsonAsync("/api/v1/academics/participations/batch-cohort", validRequest);
        successResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseData = await successResponse.Content.ReadFromJsonAsync<BatchRegisterCohortParticipationResponse>();
        responseData.Should().NotBeNull();
        responseData!.RegisteredCount.Should().Be(2);
        responseData.SkippedCount.Should().Be(0);

        // 3. Assert AcademicParticipationRegisteredEvent emissions
        Factory.EventCollector.AcademicParticipationRegisteredEvents
            .Should().Contain(e => e.StudentId == student1 && e.LearningUnitId == unitId)
            .And.Contain(e => e.StudentId == student2 && e.LearningUnitId == unitId);
    }

    [SkippableFact]
    public async Task Criterion02_RegisterParticipation_ForRepeatCourse_Should_AutoIncrementAttemptNumber()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT02");
        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        // 1. Initial attempt registration
        var firstRequest = new RegisterParticipationRequest(
            StudentId: studentId,
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "2026-Fall",
            ParticipationType: "standard",
            Source: "assigned",
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 8, 15));

        var firstResponse = await Client.PostAsJsonAsync("/api/v1/academics/participations", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstCreated = await firstResponse.Content.ReadFromJsonAsync<RegisterParticipationResponse>();
        firstCreated.Should().NotBeNull();
        firstCreated!.AttemptNumber.Should().Be(1);
        firstCreated.Status.Should().Be("in_progress");

        // 2. Conclude the initial attempt so it is in a terminal state
        var concludeRequest = new ConcludeParticipationRequest(
            CompletionDate: new DateOnly(2026, 12, 15),
            Notes: "Initial attempt completed");

        var concludeResponse = await Client.PostAsJsonAsync($"/api/v1/academics/participations/{firstCreated.Id}/conclude", concludeRequest);
        concludeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Register repeat attempt for same student and learning unit
        var repeatRequest = new RegisterParticipationRequest(
            StudentId: studentId,
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "2027-Spring",
            ParticipationType: "repeat",
            Source: "assigned",
            SectionLabel: "10-A",
            StartDate: new DateOnly(2027, 1, 15));

        var repeatResponse = await Client.PostAsJsonAsync("/api/v1/academics/participations", repeatRequest);
        repeatResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var repeatCreated = await repeatResponse.Content.ReadFromJsonAsync<RegisterParticipationResponse>();
        repeatCreated.Should().NotBeNull();
        repeatCreated!.AttemptNumber.Should().Be(2, "repeat attempt must auto-increment from prior maximum attempt 1");
        repeatCreated.ParticipationType.Should().Be("repeat");
        repeatCreated.Status.Should().Be("in_progress");
    }

    [SkippableFact]
    public async Task Criterion03_RecognizePriorCredit_Should_InsertCompletedParticipation_And_PublishEvents()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT03");
        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        var request = new RecognizePriorCreditRequest(
            StudentId: studentId,
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "2026-Fall",
            ParticipationType: "transferred_in",
            StartDate: new DateOnly(2026, 8, 15),
            CompletionDate: new DateOnly(2026, 8, 15),
            SourceInstitution: "International Academy of Science",
            ExternalGradeOrCredit: "A (4.0)",
            RecognitionBasis: "Certified Official Transcript #TR-9981",
            StudentLearningPlanId: null,
            Notes: "Verified and approved by Academic Authority");

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/academics/participations/recognize-prior", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RecognizePriorCreditResponse>();
        created.Should().NotBeNull();
        created!.Status.Should().Be("completed");
        created.ParticipationType.Should().Be("transferred_in");
        created.SourceInstitution.Should().Be("International Academy of Science");
        created.ExternalGradeOrCredit.Should().Be("A (4.0)");
        created.RecognitionBasis.Should().Be("Certified Official Transcript #TR-9981");

        // Assert both events emitted
        Factory.EventCollector.AcademicParticipationRegisteredEvents
            .Should().Contain(e => e.ParticipationId == created.Id && e.StudentId == studentId);

        Factory.EventCollector.ParticipationConcludedEvents
            .Should().Contain(e => e.ParticipationId == created.Id && e.StudentId == studentId && e.CompletionDate == new DateOnly(2026, 8, 15));
    }

    [SkippableFact]
    public async Task Criterion04_ConcludeParticipation_Should_CompleteUnit_And_UpdateCompletionDate()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT04");
        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        var registerRequest = new RegisterParticipationRequest(
            StudentId: studentId,
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "2026-Fall",
            ParticipationType: "standard",
            Source: "assigned",
            SectionLabel: "10-B",
            StartDate: new DateOnly(2026, 8, 15));

        var regResponse = await Client.PostAsJsonAsync("/api/v1/academics/participations", registerRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registered = await regResponse.Content.ReadFromJsonAsync<RegisterParticipationResponse>();

        // Act: Conclude participation
        var concludeRequest = new ConcludeParticipationRequest(
            CompletionDate: new DateOnly(2026, 12, 22),
            Notes: "Successfully mastered curriculum requirements");

        var response = await Client.PostAsJsonAsync($"/api/v1/academics/participations/{registered!.Id}/conclude", concludeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var concluded = await response.Content.ReadFromJsonAsync<ConcludeParticipationResponse>();
        concluded.Should().NotBeNull();
        concluded!.Status.Should().Be("completed");
        concluded.CompletionDate.Should().Be(new DateOnly(2026, 12, 22));

        Factory.EventCollector.ParticipationConcludedEvents
            .Should().Contain(e => e.ParticipationId == registered.Id && e.CompletionDate == new DateOnly(2026, 12, 22));
    }

    [SkippableFact]
    public async Task Criterion05_PlanAndConductLearningActivity_Should_RecordProvisionalTeacher_And_ConductSession()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT05");

        // 1. Plan learning activity with provisional teacher display context
        var planRequest = new PlanLearningActivityRequest(
            LearningUnitId: unitId,
            ActivityType: "laboratory",
            Title: "Physics Laboratory: Optics & Diffraction",
            Description: "Hands-on diffraction experiments",
            SectionLabel: "10-A",
            ActivityDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(11, 30),
            TeacherAssignmentId: null,
            TeacherName: "Prof. Rosalind Franklin",
            IsAttendanceRequired: true);

        var planResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-activities", planRequest);
        planResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var planned = await planResponse.Content.ReadFromJsonAsync<PlanLearningActivityResponse>();
        planned.Should().NotBeNull();
        planned!.Status.Should().Be("scheduled");
        planned.TeacherName.Should().Be("Prof. Rosalind Franklin");
        planned.ActivityType.Should().Be("laboratory");

        // 2. Conduct learning activity with delivery notes
        var conductRequest = new ConductLearningActivityRequest(
            DeliveryNotes: "Students completed interference pattern observations.",
            ActualTeacherName: "Prof. Rosalind Franklin");

        var conductResponse = await Client.PostAsJsonAsync($"/api/v1/academics/learning-activities/{planned.Id}/conduct", conductRequest);
        conductResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var conducted = await conductResponse.Content.ReadFromJsonAsync<ConductLearningActivityResponse>();
        conducted.Should().NotBeNull();
        conducted!.Status.Should().Be("conducted");
        conducted.DeliveryNotes.Should().Be("Students completed interference pattern observations.");

        Factory.EventCollector.LearningActivityConductedEvents
            .Should().Contain(e => e.ActivityId == planned.Id && e.LearningUnitId == unitId);
    }

    [SkippableFact]
    public async Task Criterion06_RecordSessionAttendance_Should_RecordRollCall_And_PublishEvent()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT06");
        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        // Plan activity for today so it's within same-day grace window
        var planRequest = new PlanLearningActivityRequest(
            LearningUnitId: unitId,
            ActivityType: "homeroom_session",
            Title: "Homeroom Morning Roll-Call",
            Description: "Homeroom morning briefing",
            SectionLabel: "10-A",
            ActivityDate: DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(8, 30),
            TeacherAssignmentId: null,
            TeacherName: "Teacher Maria",
            IsAttendanceRequired: true);

        var planResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-activities", planRequest);
        planResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var planned = await planResponse.Content.ReadFromJsonAsync<PlanLearningActivityResponse>();

        // Act: Record session attendance
        var attendanceRequest = new RecordSessionAttendanceRequest(
            Entries:
            [
                new StudentAttendanceEntry(
                    StudentId: studentId,
                    Status: "present",
                    Evidence: null,
                    Notes: "Prompt arrival")
            ]);

        var response = await Client.PostAsJsonAsync($"/api/v1/academics/learning-activities/{planned!.Id}/attendance", attendanceRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RecordSessionAttendanceResponse>();
        result.Should().NotBeNull();
        result!.TotalRecorded.Should().Be(1);
        result.TotalPresent.Should().Be(1);

        Factory.EventCollector.AttendanceRecordedEvents
            .Should().Contain(e => e.LearningActivityId == planned.Id && e.TotalPresent == 1);
    }

    [SkippableFact]
    public async Task Criterion07_CorrectAttendance_Should_RejectWithoutReason_And_WriteAuditRowOnSuccess()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT07");
        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        // 1. Create activity and record initial attendance
        var planRequest = new PlanLearningActivityRequest(
            LearningUnitId: unitId,
            ActivityType: "lesson",
            Title: "Chemistry Seminar",
            Description: "General chemistry lecture",
            SectionLabel: "10-A",
            ActivityDate: DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(10, 0),
            TeacherAssignmentId: null,
            TeacherName: "Prof. Mendeleev",
            IsAttendanceRequired: true);

        var planResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-activities", planRequest);
        var planned = await planResponse.Content.ReadFromJsonAsync<PlanLearningActivityResponse>();

        var attendanceRequest = new RecordSessionAttendanceRequest(
            Entries: [new StudentAttendanceEntry(studentId, "absent", null, "Unexcused initial mark")]);

        await Client.PostAsJsonAsync($"/api/v1/academics/learning-activities/{planned!.Id}/attendance", attendanceRequest);

        // Retrieve recorded attendance record ID directly from database
        const string selectRecordSql = """
            SELECT id FROM academics.attendance_records
            WHERE learning_activity_id = @ActivityId AND student_id = @StudentId;
            """;
        var recordId = await ExecuteScalarAsync<Guid>(selectRecordSql, new { ActivityId = planned.Id, StudentId = studentId });
        recordId.Should().NotBeEmpty();

        // 2. Attempt correction with blank / short reason -> rejects with validation failure
        var invalidCorrection = new CorrectAttendanceRequest(
            NewStatus: "excused_absence",
            Reason: "med"); // Too short (min 5 characters)

        var failResponse = await Client.PostAsJsonAsync($"/api/v1/academics/attendance-records/{recordId}/correct", invalidCorrection);
        failResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // 3. Perform valid correction with mandatory justification reason
        var validCorrection = new CorrectAttendanceRequest(
            NewStatus: "excused_absence",
            Reason: "Official medical certificate submitted by parent",
            Evidence: "Doctor certificate #MC-4412");

        var successResponse = await Client.PostAsJsonAsync($"/api/v1/academics/attendance-records/{recordId}/correct", validCorrection);
        successResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var corrected = await successResponse.Content.ReadFromJsonAsync<CorrectAttendanceResponse>();
        corrected.Should().NotBeNull();
        corrected!.Status.Should().Be("excused_absence");
        corrected.LatestCorrection.Reason.Should().Be("Official medical certificate submitted by parent");
        corrected.LatestCorrection.PreviousStatus.Should().Be("absent");
        corrected.LatestCorrection.NewStatus.Should().Be("excused_absence");

        // 4. Verify immutable row written into academics.attendance_corrections
        const string selectCorrectionSql = """
            SELECT COUNT(1) FROM academics.attendance_corrections
            WHERE attendance_record_id = @RecordId
              AND previous_status = 'absent'
              AND new_status = 'excused_absence'
              AND reason = @Reason;
            """;
        var auditRows = await ExecuteScalarAsync<int>(selectCorrectionSql, new
        {
            RecordId = recordId,
            Reason = "Official medical certificate submitted by parent"
        });
        auditRows.Should().Be(1, "immutable audit correction row must be recorded in academics.attendance_corrections");

        // 5. Assert AttendanceCorrectedEvent emission
        Factory.EventCollector.AttendanceCorrectedEvents
            .Should().Contain(e => e.AttendanceRecordId == recordId && e.PreviousStatus == "absent" && e.NewStatus == "excused_absence");
    }

    [SkippableFact]
    public async Task Criterion08_AssignmentLifecycle_CreateReleaseClose_Should_TransitionStates()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT08");

        var planRequest = new PlanLearningActivityRequest(
            LearningUnitId: unitId,
            ActivityType: "lesson",
            Title: "Literature Lecture",
            Description: "Class lecture on modern literature",
            SectionLabel: "10-C",
            ActivityDate: DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime: new TimeOnly(13, 0),
            EndTime: new TimeOnly(14, 0),
            TeacherAssignmentId: null,
            TeacherName: "Ms. Austen",
            IsAttendanceRequired: true);

        var planResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-activities", planRequest);
        var planned = await planResponse.Content.ReadFromJsonAsync<PlanLearningActivityResponse>();

        // 1. Create Assignment -> status = draft
        var createRequest = new CreateAssignmentRequest(
            Title: "Essay: Symbolism in 1984",
            Description: "Write a 1500-word analysis of Orwell's political symbolism.",
            ReleaseDate: DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            Notes: "Draft assignment");

        var createResponse = await Client.PostAsJsonAsync($"/api/v1/academics/learning-activities/{planned!.Id}/assignments", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateAssignmentResponse>();
        created.Should().NotBeNull();
        created!.Status.Should().Be("draft");
        created.LearningActivityId.Should().Be(planned.Id);

        // 2. Release Assignment -> status = released, emits AssignmentReleasedEvent
        var releaseResponse = await Client.PostAsync($"/api/v1/academics/assignments/{created.Id}/release", null);
        releaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var released = await releaseResponse.Content.ReadFromJsonAsync<ReleaseAssignmentResponse>();
        released.Should().NotBeNull();
        released!.Status.Should().Be("released");

        Factory.EventCollector.AssignmentReleasedEvents
            .Should().Contain(e => e.AssignmentId == created.Id && e.LearningActivityId == planned.Id);

        // 3. Close Assignment -> status = closed
        var closeResponse = await Client.PostAsync($"/api/v1/academics/assignments/{created.Id}/close", null);
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var closed = await closeResponse.Content.ReadFromJsonAsync<CloseAssignmentResponse>();
        closed.Should().NotBeNull();
        closed!.Status.Should().Be("closed");
    }

    [SkippableFact]
    public async Task Criterion09_SubmitCoursework_Should_HandleResubmissionLineage_And_LateStatus()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT09");
        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        // Create and release assignment with a past due date to verify late evaluation
        var planRequest = new PlanLearningActivityRequest(
            LearningUnitId: unitId,
            ActivityType: "discussion",
            Title: "History Discussion",
            Description: "Ancient civilizations forum",
            SectionLabel: "10-A",
            ActivityDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            StartTime: new TimeOnly(11, 0),
            EndTime: new TimeOnly(12, 0),
            TeacherAssignmentId: null,
            TeacherName: "Mr. Herodotus",
            IsAttendanceRequired: true);

        var planResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-activities", planRequest);
        var planned = await planResponse.Content.ReadFromJsonAsync<PlanLearningActivityResponse>();

        var pastDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var createRequest = new CreateAssignmentRequest(
            Title: "Past Due Homework",
            Description: "Homework due in the past",
            ReleaseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)),
            DueDate: pastDueDate,
            Notes: "Past due test");

        var createResponse = await Client.PostAsJsonAsync($"/api/v1/academics/learning-activities/{planned!.Id}/assignments", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAssignmentResponse>();

        await Client.PostAsync($"/api/v1/academics/assignments/{created!.Id}/release", null);

        // 1. Initial submission (past due date -> status = late)
        var firstSubmit = new SubmitCourseworkRequest(
            StudentId: studentId,
            SubmissionReference: "https://cloud.elsasa.edu/docs/history-v1.pdf",
            Notes: "First submission delivered late");

        var firstResponse = await Client.PostAsJsonAsync($"/api/v1/academics/assignments/{created.Id}/submissions", firstSubmit);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstCreated = await firstResponse.Content.ReadFromJsonAsync<SubmitCourseworkResponse>();
        firstCreated.Should().NotBeNull();
        firstCreated!.Status.Should().Be("late", "submission after due date must be evaluated as late");
        firstCreated.IsResubmission.Should().BeFalse();
        firstCreated.PreviousSubmissionId.Should().BeNull();

        Factory.EventCollector.CourseworkSubmittedEvents
            .Should().Contain(e => e.SubmissionId == firstCreated.Id && e.StudentId == studentId && e.Status == "late");

        // 2. Resubmission by same student
        var secondSubmit = new SubmitCourseworkRequest(
            StudentId: studentId,
            SubmissionReference: "https://cloud.elsasa.edu/docs/history-v2.pdf",
            Notes: "Resubmission with extra bibliography");

        var secondResponse = await Client.PostAsJsonAsync($"/api/v1/academics/assignments/{created.Id}/submissions", secondSubmit);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondCreated = await secondResponse.Content.ReadFromJsonAsync<SubmitCourseworkResponse>();
        secondCreated.Should().NotBeNull();
        secondCreated!.IsResubmission.Should().BeTrue("subsequent submission on same assignment must be flagged as resubmission");
        secondCreated.PreviousSubmissionId.Should().Be(firstCreated.Id, "resubmission must link to previous submission ID");

        Factory.EventCollector.CourseworkSubmittedEvents
            .Should().Contain(e => e.SubmissionId == secondCreated.Id && e.IsResubmission);
    }

    [SkippableFact]
    public async Task Criterion10_DynamicMissingProjection_Should_ProjectMissingStatus_WithoutDatabaseRowBloat()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT10");
        var activeStudentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(activeStudentId);

        // 1. Register student in the learning unit with section label '10-A'
        var regRequest = new RegisterParticipationRequest(
            StudentId: activeStudentId,
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "2026-Fall",
            ParticipationType: "standard",
            Source: "assigned",
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 8, 15));

        await Client.PostAsJsonAsync("/api/v1/academics/participations", regRequest);

        // 2. Plan activity for section '10-A' and create assignment with past due date
        var planRequest = new PlanLearningActivityRequest(
            LearningUnitId: unitId,
            ActivityType: "lesson",
            Title: "Algebra Workshop",
            Description: "Algebra problem solving session",
            SectionLabel: "10-A",
            ActivityDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            StartTime: new TimeOnly(14, 0),
            EndTime: new TimeOnly(15, 0),
            TeacherAssignmentId: null,
            TeacherName: "Prof. Gauss",
            IsAttendanceRequired: true);

        var planResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-activities", planRequest);
        var planned = await planResponse.Content.ReadFromJsonAsync<PlanLearningActivityResponse>();

        var pastDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var createAssignment = new CreateAssignmentRequest(
            Title: "Quadratic Equations Homework",
            Description: "Exercises 1-10",
            ReleaseDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            DueDate: pastDueDate,
            Notes: "Past due task");

        var assignResponse = await Client.PostAsJsonAsync($"/api/v1/academics/learning-activities/{planned!.Id}/assignments", createAssignment);
        var assignment = await assignResponse.Content.ReadFromJsonAsync<CreateAssignmentResponse>();

        await Client.PostAsync($"/api/v1/academics/assignments/{assignment!.Id}/release", null);

        // 3. Query assignment submissions roster: student has submitted NOTHING
        var rosterResponse = await Client.GetAsync($"/api/v1/academics/assignments/{assignment.Id}/submissions");
        rosterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var roster = await rosterResponse.Content.ReadFromJsonAsync<IReadOnlyList<AssignmentSubmissionRosterItemDto>>();
        roster.Should().NotBeNull();
        roster.Should().Contain(s => s.StudentId == activeStudentId && s.DisplayStatus == "missing");

        // 4. Assert ZERO database rows exist in academics.submissions for this student on this assignment
        const string checkPhysicalRowsSql = """
            SELECT COUNT(1) FROM academics.submissions
            WHERE assignment_id = @AssignmentId AND student_id = @StudentId;
            """;
        var physicalRows = await ExecuteScalarAsync<int>(checkPhysicalRowsSql, new
        {
            AssignmentId = assignment.Id,
            StudentId = activeStudentId
        });
        physicalRows.Should().Be(0, "dynamic missing projection must NOT insert synthetic rows into academics.submissions");
    }

    [SkippableFact]
    public async Task Criterion11_AcademicsModuleCrossDomainQueries_Should_ReturnAccurateResults()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (periodId, unitId) = await SetupActiveCurriculumUnitAsync("CRIT11");
        var studentId = Guid.NewGuid();
        Factory.StudentsModule.MarkStudentEnrolled(studentId);

        var academicsModule = CreateAcademicsModule();

        // 1. Initial check: student is not participating
        var notParticipating = await academicsModule.IsStudentParticipatingAsync(studentId, unitId, periodId);
        notParticipating.IsSuccess.Should().BeTrue();
        notParticipating.Value.Should().BeFalse();

        // 2. Register participation -> now participating is true
        var regRequest = new RegisterParticipationRequest(
            StudentId: studentId,
            LearningUnitId: unitId,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "2026-Fall",
            ParticipationType: "standard",
            Source: "assigned",
            SectionLabel: "10-A",
            StartDate: new DateOnly(2026, 8, 15));

        var regResponse = await Client.PostAsJsonAsync("/api/v1/academics/participations", regRequest);
        var registered = await regResponse.Content.ReadFromJsonAsync<RegisterParticipationResponse>();

        var isParticipating = await academicsModule.IsStudentParticipatingAsync(studentId, unitId, periodId);
        isParticipating.IsSuccess.Should().BeTrue();
        isParticipating.Value.Should().BeTrue();

        // 3. Plan activity, record attendance, and test GetStudentAttendanceSummaryAsync
        var planRequest = new PlanLearningActivityRequest(
            LearningUnitId: unitId,
            ActivityType: "tutorial",
            Title: "Module Seminar",
            Description: "Weekly module review seminar",
            SectionLabel: "10-A",
            ActivityDate: DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime: new TimeOnly(15, 0),
            EndTime: new TimeOnly(16, 0),
            TeacherAssignmentId: null,
            TeacherName: "Dr. Turing",
            IsAttendanceRequired: true);

        var planResponse = await Client.PostAsJsonAsync("/api/v1/academics/learning-activities", planRequest);
        var planned = await planResponse.Content.ReadFromJsonAsync<PlanLearningActivityResponse>();

        var attendanceRequest = new RecordSessionAttendanceRequest(
            Entries: [new StudentAttendanceEntry(studentId, "present")]);

        await Client.PostAsJsonAsync($"/api/v1/academics/learning-activities/{planned!.Id}/attendance", attendanceRequest);

        var attendanceSummary = await academicsModule.GetStudentAttendanceSummaryAsync(studentId, periodId);
        attendanceSummary.IsSuccess.Should().BeTrue();
        attendanceSummary.Value.Should().NotBeNull();
        attendanceSummary.Value!.TotalSessions.Should().Be(1);
        attendanceSummary.Value.TotalPresent.Should().Be(1);
        attendanceSummary.Value.AttendanceRatePercentage.Should().Be(100.00m);

        // 4. Conclude participation and test GetStudentCompletedUnitsAsync
        var concludeRequest = new ConcludeParticipationRequest(
            CompletionDate: new DateOnly(2026, 12, 18),
            Notes: "Completed course unit");

        await Client.PostAsJsonAsync($"/api/v1/academics/participations/{registered!.Id}/conclude", concludeRequest);

        var completedUnits = await academicsModule.GetStudentCompletedUnitsAsync(studentId);
        completedUnits.IsSuccess.Should().BeTrue();
        completedUnits.Value.Should().NotBeNull();
        completedUnits.Value.Should().Contain(u => u.LearningUnitId == unitId && u.CreditWeight == 4.0m);
    }
}
