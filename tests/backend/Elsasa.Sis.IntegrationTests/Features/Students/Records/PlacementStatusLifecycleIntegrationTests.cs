namespace Elsasa.Sis.IntegrationTests.Features.Students.Records;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;
using Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CreateOperationalPeriod;
using Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;
using Elsasa.Sis.Students.Features.Admission.RegisterApplication;
using Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;
using Elsasa.Sis.Students.Features.Records.GetStudentById;
using Elsasa.Sis.Students.Features.Records.History.GetStudentAdministrativeHistory;
using Elsasa.Sis.Students.Features.Records.Placements.ChangeAdministrativePlacement;
using Elsasa.Sis.Students.Features.Records.Status.ChangeStudentStatus;
using FluentAssertions;
using Xunit;

/// <summary>
/// End-to-end integration tests for Student Placement & Status lifecycle flows (IS005, CR-STU-PLACEMENT-STATUS, P4-S11).
/// Validates:
/// 1. Enrollment intake provisioning (initial placement, sequential statuses enrolled->active, 4 intake audit entries).
/// 2. Placement reassignment (POST /api/students/{id}/placements, closure of prior placement, new active row, history logging, event publishing, effective date constraint).
/// 3. Status adjustment (POST /api/students/{id}/status, closure of prior status, new active status, lockstep sync of students.current_status, history logging, validation).
/// 4. 360° Dossier & Administrative History queries (GET /api/students/{id}, GET /api/students/{id}/history with chronological descending order).
/// </summary>
public sealed class PlacementStatusLifecycleIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private async Task<(Guid AcademicYearId, Guid AdmissionPeriodId, Guid ApplicationId, Guid ApplicantId, string GovId)> SetupAcceptedApplicationAsync(string suffix)
    {
        var ayRequest = new CreateAcademicYearRequest(
            Code: $"AY-PLC-{suffix}",
            Name: $"Placement AY {suffix}",
            StartDate: new DateOnly(2026, 8, 1),
            EndDate: new DateOnly(2027, 7, 31));

        var ayResponse = await Client.PostAsJsonAsync("/api/operations/academic-years", ayRequest);
        ayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ay = await ayResponse.Content.ReadFromJsonAsync<CreateAcademicYearResponse>();

        await Client.PostAsync($"/api/operations/academic-years/{ay!.Id}/approve", null);
        await Client.PostAsync($"/api/operations/academic-years/{ay.Id}/activate", null);

        var periodRequest = new CreateOperationalPeriodRequest(
            PeriodCode: $"ADM-PLC-{suffix}",
            Name: $"Placement Window {suffix}",
            PeriodType: "admission_window",
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)));

        var periodResponse = await Client.PostAsJsonAsync($"/api/operations/academic-years/{ay.Id}/periods", periodRequest);
        periodResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var period = await periodResponse.Content.ReadFromJsonAsync<CreateOperationalPeriodResponse>();

        await ExecuteAsync("UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;", new { Id = period!.Id });

        var fullName = $"Placement Candidate {suffix}";
        var govId = $"3174{Random.Shared.Next(10000000, 99999999)}0004";
        var regRequest = new RegisterApplicationRequest(
            ApplicantId: null,
            FullName: fullName,
            DateOfBirth: new DateOnly(2018, 6, 10),
            GovernmentIdType: "NIK",
            GovernmentIdNumber: govId,
            AdmissionPeriodId: period!.Id,
            RequestedEntryLevel: "Grade 1",
            RequestedCampus: "Main Campus",
            Contacts:
            [
                new RegisterApplicationContactRequest("Father " + suffix, "Father", "+6281233334444", IsPrimary: true)
            ]);

        var regResponse = await Client.PostAsJsonAsync("/api/students/admission/applications", regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var reg = await regResponse.Content.ReadFromJsonAsync<RegisterApplicationResponse>();

        await Client.PostAsync($"/api/students/admission/applications/{reg!.Id}/start-review", null);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var decisionRequest = new RecordAdmissionDecisionRequest(
            DecisionType: "accepted",
            DecisionDate: today,
            EffectiveDate: today,
            Reason: "Candidate passed assessment for placement testing",
            SupportingReference: $"DEC-PLC-{suffix}");

        var decisionResponse = await Client.PostAsJsonAsync($"/api/students/admission/applications/{reg.Id}/decision", decisionRequest);
        decisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return (ay.Id, period.Id, reg.Id, reg.ApplicantId, govId);
    }

    private async Task<(Guid StudentId, string StudentNumber, Guid AcademicYearId, string Campus, string Level, DateOnly EnrollmentDate)> SetupEnrolledStudentAsync(string suffix)
    {
        var (academicYearId, _, applicationId, applicantId, _) = await SetupAcceptedApplicationAsync(suffix);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var enrollRequest = new EnrollAcceptedApplicantRequest(
            ApplicantId: applicantId,
            AcademicYearId: academicYearId,
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            InitialProgram: "National Curriculum",
            InitialCohort: "2026-A",
            PaymentWaived: true,
            WaiverReference: $"SK-WAIVE-{suffix}",
            Guardians:
            [
                new EnrollGuardianRequest(
                    FullName: $"Guardian of {suffix}",
                    RelationshipType: "mother",
                    GovernmentIdType: "NIK",
                    GovernmentIdNumber: $"3175{Random.Shared.Next(10000000, 99999999)}0005",
                    Phone: "+6281255556666",
                    Email: $"guardian.{suffix}@example.com",
                    IsPrimaryContact: true)
            ]);

        var enrollResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseBody = await enrollResponse.Content.ReadFromJsonAsync<EnrollAcceptedApplicantResponse>();
        responseBody.Should().NotBeNull();

        return (responseBody!.StudentId, responseBody.StudentNumber, academicYearId, "Main Campus", "Grade 1", today);
    }

    [SkippableFact]
    public async Task EnrollmentIntake_Should_CreateActivePlacement_SequentialStatuses_AndIntakeHistory()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"INTAKE-{Guid.NewGuid():N}"[..8];
        var (academicYearId, _, applicationId, applicantId, _) = await SetupAcceptedApplicationAsync(suffix);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var enrollRequest = new EnrollAcceptedApplicantRequest(
            ApplicantId: applicantId,
            AcademicYearId: academicYearId,
            InitialCampus: "West Campus",
            InitialLevel: "Grade 3",
            InitialAcademicYear: "2026/2027",
            InitialProgram: "Bilingual Program",
            InitialCohort: "2026-B",
            PaymentWaived: false,
            PaymentReference: $"PAY-REF-{suffix}",
            Guardians:
            [
                new EnrollGuardianRequest(
                    FullName: $"Primary Parent {suffix}",
                    RelationshipType: "father",
                    GovernmentIdType: "NIK",
                    GovernmentIdNumber: $"3176{Random.Shared.Next(10000000, 99999999)}0006",
                    Phone: "+6281277778888",
                    Email: $"parent.{suffix}@example.com",
                    IsPrimaryContact: true)
            ]);

        // Act: Enroll the accepted applicant
        var enrollResponse = await Client.PostAsJsonAsync("/api/students/enrollments", enrollRequest);

        // Assert HTTP 201 Created
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseBody = await enrollResponse.Content.ReadFromJsonAsync<EnrollAcceptedApplicantResponse>();
        responseBody.Should().NotBeNull();
        var studentId = responseBody!.StudentId;

        // 1. Assert students.students record has current_status = 'active'
        const string studentSql = "SELECT current_status, student_number FROM students.students WHERE id = @Id;";
        var student = (await QueryAsync<dynamic>(studentSql, new { Id = studentId })).Single();
        ((string)student.current_status).Should().Be("active");
        ((string)student.student_number).Should().Be(responseBody.StudentNumber);

        // 2. Assert active row in students.administrative_placements
        const string placementSql = "SELECT * FROM students.administrative_placements WHERE student_id = @StudentId;";
        var placements = (await QueryAsync<dynamic>(placementSql, new { StudentId = studentId })).ToList();
        placements.Should().HaveCount(1);
        var activePlacement = placements.Single();
        ((bool)activePlacement.is_current).Should().BeTrue();
        ((object?)activePlacement.end_date).Should().BeNull();
        ((string)activePlacement.campus).Should().Be("West Campus");
        ((string)activePlacement.school_level).Should().Be("Grade 3");
        ((string)activePlacement.grade).Should().Be("Grade 3");
        ((string)activePlacement.academic_year).Should().Be("2026/2027");
        ((string)activePlacement.program).Should().Be("Bilingual Program");
        ((string)activePlacement.cohort).Should().Be("2026-B");
        ((string)activePlacement.reason).Should().Be("Initial enrollment placement");
        ((string)activePlacement.decision_reference).Should().Be(responseBody.StudentNumber);

        // 3. Assert two sequential status rows in students.student_statuses
        const string statusSql = "SELECT * FROM students.student_statuses WHERE student_id = @StudentId ORDER BY is_current ASC;";
        var statuses = (await QueryAsync<dynamic>(statusSql, new { StudentId = studentId })).ToList();
        statuses.Should().HaveCount(2);

        // Row 1: closed enrolled status
        var enrolledStatus = statuses[0];
        ((string)enrolledStatus.status).Should().Be("enrolled");
        ((bool)enrolledStatus.is_current).Should().BeFalse();
        ((object?)enrolledStatus.end_date).Should().NotBeNull();
        ((string)enrolledStatus.reason).Should().Be("Initial enrollment completed");

        // Row 2: active current status
        var activeStatus = statuses[1];
        ((string)activeStatus.status).Should().Be("active");
        ((bool)activeStatus.is_current).Should().BeTrue();
        ((object?)activeStatus.end_date).Should().BeNull();
        ((string)activeStatus.reason).Should().Be("Activated upon enrollment completion");

        // 4. Assert 4 chronological intake history entries in students.administrative_history_entries
        const string historySql = """
            SELECT event_type, detail, is_correction, corrects_entry_id
            FROM students.administrative_history_entries
            WHERE student_id = @StudentId
            ORDER BY created_at ASC;
            """;
        var historyEntries = (await QueryAsync<dynamic>(historySql, new { StudentId = studentId })).ToList();
        historyEntries.Should().HaveCount(4);

        var eventTypes = historyEntries.Select(h => (string)h.event_type).ToList();
        eventTypes.Should().Equal(
            "application_submitted",
            "application_accepted",
            "enrollment_completed",
            "student_activated");

        ((string)historyEntries[2].detail).Should().Be("Enrolled at West Campus Grade 3");
        ((string)historyEntries[3].detail).Should().Be("Official student status activated");
        foreach (var entry in historyEntries)
        {
            ((bool)entry.is_correction).Should().BeFalse();
            ((object?)entry.corrects_entry_id).Should().BeNull();
        }

        // 5. Assert domain event published
        Factory.EventCollector.StudentEnrolledEvents.Should().ContainSingle(e =>
            e.StudentId == studentId &&
            e.ApplicantId == applicantId &&
            e.AcademicYearId == academicYearId);
    }

    [SkippableFact]
    public async Task PlacementReassignment_Should_ClosePriorPlacement_InsertNewActivePlacement_AppendHistory_AndPublishEvent()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"REASSIGN-{Guid.NewGuid():N}"[..8];
        var (studentId, studentNumber, _, _, _, enrollmentDate) = await SetupEnrolledStudentAsync(suffix);
        var effectiveDate = enrollmentDate.AddDays(1);

        var reassignRequest = new ChangeAdministrativePlacementRequest(
            Campus: "North Campus",
            SchoolLevel: "Secondary",
            Grade: "Grade 7",
            AcademicYear: "2027/2028",
            EffectiveDate: effectiveDate,
            Program: "Accelerated Science",
            Stream: "STEM",
            Cohort: "2027-SEC",
            Homeroom: "Room 7-B",
            Reason: "School level promotion and campus transfer",
            AuthorizedBy: "Academic Director",
            DecisionReference: $"DEC-REASSIGN-{suffix}");

        // Act: POST /api/students/{id}/placements
        var response = await Client.PostAsJsonAsync($"/api/students/{studentId}/placements", reassignRequest);

        // Assert HTTP 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadFromJsonAsync<ChangeAdministrativePlacementResponse>();
        responseBody.Should().NotBeNull();
        responseBody!.StudentId.Should().Be(studentId);
        responseBody.PlacementId.Should().NotBeEmpty();
        responseBody.IsCurrent.Should().BeTrue();

        // Database assertions: verify prior placement closed and new placement active
        const string placementsSql = """
            SELECT id, campus, school_level, grade, academic_year, effective_date, end_date,
                   is_current, program, stream, cohort, homeroom, reason, authorized_by, decision_reference
            FROM students.administrative_placements
            WHERE student_id = @StudentId
            ORDER BY created_at ASC;
            """;
        var placements = (await QueryAsync<dynamic>(placementsSql, new { StudentId = studentId })).ToList();
        placements.Should().HaveCount(2);

        // Prior placement
        var priorPlacement = placements[0];
        ((bool)priorPlacement.is_current).Should().BeFalse();
        ((object?)priorPlacement.end_date).Should().NotBeNull();
        DateOnly.FromDateTime((DateTime)priorPlacement.end_date).Should().Be(effectiveDate);
        ((string)priorPlacement.grade).Should().Be("Grade 1");

        // New active placement
        var newPlacement = placements[1];
        ((Guid)newPlacement.id).Should().Be(responseBody.PlacementId);
        ((bool)newPlacement.is_current).Should().BeTrue();
        ((object?)newPlacement.end_date).Should().BeNull();
        DateOnly.FromDateTime((DateTime)newPlacement.effective_date).Should().Be(effectiveDate);
        ((string)newPlacement.campus).Should().Be("North Campus");
        ((string)newPlacement.school_level).Should().Be("Secondary");
        ((string)newPlacement.grade).Should().Be("Grade 7");
        ((string)newPlacement.academic_year).Should().Be("2027/2028");
        ((string)newPlacement.program).Should().Be("Accelerated Science");
        ((string)newPlacement.stream).Should().Be("STEM");
        ((string)newPlacement.cohort).Should().Be("2027-SEC");
        ((string)newPlacement.homeroom).Should().Be("Room 7-B");
        ((string)newPlacement.reason).Should().Be("School level promotion and campus transfer");
        ((string)newPlacement.authorized_by).Should().Be("Academic Director");
        ((string)newPlacement.decision_reference).Should().Be($"DEC-REASSIGN-{suffix}");

        // History entry appended
        const string historySql = """
            SELECT event_type, event_date, detail, authorized_by, decision_reference
            FROM students.administrative_history_entries
            WHERE student_id = @StudentId AND event_type = 'placement_changed';
            """;
        var historyEntries = (await QueryAsync<dynamic>(historySql, new { StudentId = studentId })).ToList();
        historyEntries.Should().HaveCount(1);
        var historyEntry = historyEntries.Single();
        ((string)historyEntry.detail).Should().Be("Reassigned to North Campus, Secondary, Grade 7, Cohort: 2027-SEC, Homeroom: Room 7-B");
        ((string)historyEntry.authorized_by).Should().Be("Academic Director");
        ((string)historyEntry.decision_reference).Should().Be($"DEC-REASSIGN-{suffix}");

        // Event published
        Factory.EventCollector.PlacementChangedEvents.Should().ContainSingle(e =>
            e.StudentId == studentId &&
            e.PlacementId == responseBody.PlacementId &&
            e.Campus == "North Campus" &&
            e.SchoolLevel == "Secondary" &&
            e.Grade == "Grade 7" &&
            e.EffectiveDate == effectiveDate);
    }

    [SkippableFact]
    public async Task PlacementReassignment_Should_FailValidation_WhenEffectiveDatePrecedesCurrent()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"PRIOR-EFF-{Guid.NewGuid():N}"[..8];
        var (studentId, _, _, _, _, enrollmentDate) = await SetupEnrolledStudentAsync(suffix);

        // Effective date strictly prior to current placement effective date (enrollment date)
        var invalidEffectiveDate = enrollmentDate.AddDays(-5);

        var invalidRequest = new ChangeAdministrativePlacementRequest(
            Campus: "East Campus",
            SchoolLevel: "Primary",
            Grade: "Grade 2",
            AcademicYear: "2026/2027",
            EffectiveDate: invalidEffectiveDate,
            Reason: "Backdated placement attempt");

        // Act: POST /api/students/{id}/placements
        var response = await Client.PostAsJsonAsync($"/api/students/{studentId}/placements", invalidRequest);

        // Assert 422 UnprocessableEntity
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("Students.Placements.EffectiveDatePriorToCurrent");

        // Verify database remains unchanged: still only 1 active placement
        const string countSql = "SELECT COUNT(1) FROM students.administrative_placements WHERE student_id = @StudentId;";
        var count = await ExecuteScalarAsync<int>(countSql, new { StudentId = studentId });
        count.Should().Be(1);

        // Verify no placement_changed history entry was appended
        const string historySql = "SELECT COUNT(1) FROM students.administrative_history_entries WHERE student_id = @StudentId AND event_type = 'placement_changed';";
        var historyCount = await ExecuteScalarAsync<int>(historySql, new { StudentId = studentId });
        historyCount.Should().Be(0);
    }

    [SkippableFact]
    public async Task StatusAdjustment_Should_ClosePriorStatus_InsertNewActiveStatus_SyncDenormalizedColumn_AndLogHistory()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"STATUS-{Guid.NewGuid():N}"[..8];
        var (studentId, _, _, _, _, enrollmentDate) = await SetupEnrolledStudentAsync(suffix);
        var effectiveDate = enrollmentDate.AddDays(2);

        var statusRequest = new ChangeStudentStatusRequest(
            Status: "enrolled",
            EffectiveDate: effectiveDate,
            Reason: "Administrative verification hold",
            AuthorizedBy: "Compliance Officer",
            DecisionReference: $"DEC-STATUS-{suffix}");

        // Act 1: POST /api/students/{id}/status to 'enrolled'
        var response = await Client.PostAsJsonAsync($"/api/students/{studentId}/status", statusRequest);

        // Assert HTTP 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadFromJsonAsync<ChangeStudentStatusResponse>();
        responseBody.Should().NotBeNull();
        responseBody!.StudentId.Should().Be(studentId);
        responseBody.Status.Should().Be("enrolled");
        responseBody.IsCurrent.Should().BeTrue();

        // Verify students.students.current_status updated in lockstep
        const string studentSql = "SELECT current_status FROM students.students WHERE id = @Id;";
        var currentStatus = await ExecuteScalarAsync<string>(studentSql, new { Id = studentId });
        currentStatus.Should().Be("enrolled");

        // Verify students.student_statuses: prior active closed, new enrolled active
        const string statusesSql = """
            SELECT id, status, effective_date, end_date, is_current, reason, authorized_by, decision_reference
            FROM students.student_statuses
            WHERE student_id = @StudentId
            ORDER BY created_at ASC;
            """;
        var statuses = (await QueryAsync<dynamic>(statusesSql, new { StudentId = studentId })).ToList();
        statuses.Should().HaveCount(3); // 2 intake (enrolled, active) + 1 new (enrolled)

        // Middle status (intake active) should now be closed
        var priorActive = statuses[1];
        ((string)priorActive.status).Should().Be("active");
        ((bool)priorActive.is_current).Should().BeFalse();
        ((object?)priorActive.end_date).Should().NotBeNull();
        DateOnly.FromDateTime((DateTime)priorActive.end_date).Should().Be(effectiveDate);

        // Latest status (enrolled) should be current
        var newStatus = statuses[2];
        ((Guid)newStatus.id).Should().Be(responseBody.StatusId);
        ((string)newStatus.status).Should().Be("enrolled");
        ((bool)newStatus.is_current).Should().BeTrue();
        ((object?)newStatus.end_date).Should().BeNull();
        ((string)newStatus.reason).Should().Be("Administrative verification hold");
        ((string)newStatus.authorized_by).Should().Be("Compliance Officer");
        ((string)newStatus.decision_reference).Should().Be($"DEC-STATUS-{suffix}");

        // Verify history entry logged
        const string historySql = """
            SELECT event_type, event_date, detail, authorized_by, decision_reference
            FROM students.administrative_history_entries
            WHERE student_id = @StudentId AND event_type = 'status_adjusted';
            """;
        var historyEntries = (await QueryAsync<dynamic>(historySql, new { StudentId = studentId })).ToList();
        historyEntries.Should().HaveCount(1);
        var entry = historyEntries.Single();
        ((string)entry.detail).Should().Contain("Status adjusted to 'enrolled'. Reason: Administrative verification hold");
        ((string)entry.authorized_by).Should().Be("Compliance Officer");
        ((string)entry.decision_reference).Should().Be($"DEC-STATUS-{suffix}");

        // Act 2: Transition back to 'active'
        var reactivateDate = effectiveDate.AddDays(3);
        var reactivateRequest = new ChangeStudentStatusRequest(
            Status: "active",
            EffectiveDate: reactivateDate,
            Reason: "Verification hold cleared",
            AuthorizedBy: "Registrar",
            DecisionReference: $"REG-CLEAR-{suffix}");

        var reactivateResponse = await Client.PostAsJsonAsync($"/api/students/{studentId}/status", reactivateRequest);
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reactivatedStatus = await ExecuteScalarAsync<string>(studentSql, new { Id = studentId });
        reactivatedStatus.Should().Be("active");

        // Verify student_activated history entry added
        const string activatedHistorySql = """
            SELECT detail
            FROM students.administrative_history_entries
            WHERE student_id = @StudentId AND event_type = 'student_activated'
            ORDER BY created_at DESC;
            """;
        var activatedEntries = (await QueryAsync<string>(activatedHistorySql, new { StudentId = studentId })).ToList();
        activatedEntries.Should().HaveCount(2); // 1 from intake, 1 from reactivation
        activatedEntries[0].Should().Contain("Status adjusted to 'active'. Reason: Verification hold cleared");
    }

    [SkippableFact]
    public async Task StatusAdjustment_Should_FailValidation_WhenStatusIsInvalid()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"INV-STAT-{Guid.NewGuid():N}"[..8];
        var (studentId, _, _, _, _, enrollmentDate) = await SetupEnrolledStudentAsync(suffix);

        var invalidRequest = new ChangeStudentStatusRequest(
            Status: "expelled", // Stage 1 allows only 'active' or 'enrolled'
            EffectiveDate: enrollmentDate.AddDays(1),
            Reason: "Invalid status test",
            AuthorizedBy: "Admin",
            DecisionReference: null);

        var response = await Client.PostAsJsonAsync($"/api/students/{studentId}/status", invalidRequest);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // Verify status remains unchanged
        const string studentSql = "SELECT current_status FROM students.students WHERE id = @Id;";
        var currentStatus = await ExecuteScalarAsync<string>(studentSql, new { Id = studentId });
        currentStatus.Should().Be("active");
    }

    [SkippableFact]
    public async Task DossierAndHistoryQueries_Should_ProjectCurrentPlacement_Histories_AndDescendingTimeline()
    {
        SkipIfDatabaseUnavailable();
        var suffix = $"QUERY-{Guid.NewGuid():N}"[..8];
        var (studentId, studentNumber, _, _, _, enrollmentDate) = await SetupEnrolledStudentAsync(suffix);

        // 1. Perform placement reassignment
        var placementDate = enrollmentDate.AddDays(2);
        var reassignRequest = new ChangeAdministrativePlacementRequest(
            Campus: "Central Campus",
            SchoolLevel: "Primary",
            Grade: "Grade 2",
            AcademicYear: "2027/2028",
            EffectiveDate: placementDate,
            Cohort: "2027-C",
            Homeroom: "HR-102",
            Reason: "Advancement",
            AuthorizedBy: "Principal",
            DecisionReference: $"REF-ADV-{suffix}");

        var reassignResponse = await Client.PostAsJsonAsync($"/api/students/{studentId}/placements", reassignRequest);
        reassignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reassignBody = await reassignResponse.Content.ReadFromJsonAsync<ChangeAdministrativePlacementResponse>();

        // 2. Perform status change
        var statusDate = placementDate.AddDays(1);
        var statusRequest = new ChangeStudentStatusRequest(
            Status: "enrolled",
            EffectiveDate: statusDate,
            Reason: "Review hold",
            AuthorizedBy: "Registrar",
            DecisionReference: $"REF-HOLD-{suffix}");

        var statusResponse = await Client.PostAsJsonAsync($"/api/students/{studentId}/status", statusRequest);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Act: GET /api/students/{id} (Dossier Projection)
        var dossierResponse = await Client.GetAsync($"/api/students/{studentId}");
        dossierResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dossier = await dossierResponse.Content.ReadFromJsonAsync<StudentDetailResponse>();

        dossier.Should().NotBeNull();
        dossier!.Id.Should().Be(studentId);
        dossier.StudentNumber.Should().Be(studentNumber);
        dossier.CurrentStatus.Should().Be("enrolled");

        // Current placement assertion
        dossier.CurrentPlacement.Should().NotBeNull();
        dossier.CurrentPlacement!.Id.Should().Be(reassignBody!.PlacementId);
        dossier.CurrentPlacement.Campus.Should().Be("Central Campus");
        dossier.CurrentPlacement.Grade.Should().Be("Grade 2");
        dossier.CurrentPlacement.IsCurrent.Should().BeTrue();
        dossier.CurrentPlacement.Cohort.Should().Be("2027-C");
        dossier.CurrentPlacement.Homeroom.Should().Be("HR-102");

        // Placement history assertion: ordered effective_date DESC, created_at DESC
        dossier.PlacementHistory.Should().HaveCount(2);
        dossier.PlacementHistory[0].IsCurrent.Should().BeTrue();
        dossier.PlacementHistory[0].Grade.Should().Be("Grade 2");
        dossier.PlacementHistory[1].IsCurrent.Should().BeFalse();
        dossier.PlacementHistory[1].Grade.Should().Be("Grade 1");

        // Status history assertion: ordered effective_date DESC, created_at DESC
        dossier.StatusHistory.Should().HaveCount(3);
        dossier.StatusHistory[0].IsCurrent.Should().BeTrue();
        dossier.StatusHistory[0].Status.Should().Be("enrolled");
        dossier.StatusHistory[1].IsCurrent.Should().BeFalse();
        dossier.StatusHistory[1].Status.Should().Be("active");
        dossier.StatusHistory[2].IsCurrent.Should().BeFalse();
        dossier.StatusHistory[2].Status.Should().Be("enrolled");

        // 4. Act: GET /api/students/{id}/history (Chronological Administrative History)
        var historyResponse = await Client.GetAsync($"/api/students/{studentId}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await historyResponse.Content.ReadFromJsonAsync<GetStudentAdministrativeHistoryResponse>();

        history.Should().NotBeNull();
        history!.StudentId.Should().Be(studentId);
        history.Entries.Should().HaveCount(6); // 4 intake + 1 placement_changed + 1 status_adjusted

        // Verify entries are chronologically descending by EventDate / CreatedAt
        for (var i = 0; i < history.Entries.Count - 1; i++)
        {
            var current = history.Entries[i];
            var next = history.Entries[i + 1];

            (current.EventDate >= next.EventDate).Should().BeTrue(
                $"entry at index {i} ({current.EventType}: {current.EventDate}) should be >= entry at index {i + 1} ({next.EventType}: {next.EventDate})");
        }

        var entryTypes = history.Entries.Select(e => e.EventType).ToList();
        entryTypes.Should().Contain([
            "application_submitted",
            "application_accepted",
            "enrollment_completed",
            "student_activated",
            "placement_changed",
            "status_adjusted"
        ]);
    }

    [SkippableFact]
    public async Task Endpoints_Should_ReturnNotFound_WhenStudentDoesNotExist()
    {
        SkipIfDatabaseUnavailable();
        var nonExistentId = Guid.NewGuid();

        // 1. History query
        var historyResponse = await Client.GetAsync($"/api/students/{nonExistentId}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 2. Placement mutation
        var placementRequest = new ChangeAdministrativePlacementRequest(
            Campus: "Main Campus",
            SchoolLevel: "Primary",
            Grade: "Grade 1",
            AcademicYear: "2026/2027",
            EffectiveDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var placementResponse = await Client.PostAsJsonAsync($"/api/students/{nonExistentId}/placements", placementRequest);
        placementResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 3. Status mutation
        var statusRequest = new ChangeStudentStatusRequest(
            Status: "active",
            EffectiveDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Reason: "Non-existent student test",
            AuthorizedBy: null,
            DecisionReference: null);

        var statusResponse = await Client.PostAsJsonAsync($"/api/students/{nonExistentId}/status", statusRequest);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
