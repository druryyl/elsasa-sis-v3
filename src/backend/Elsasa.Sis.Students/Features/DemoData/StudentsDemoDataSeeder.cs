namespace Elsasa.Sis.Students.Features.DemoData;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous seeder for the Students domain (ADR-002, ADR-004, TD-04, OQ-003, OQ-006, OQ-010).
/// Seeds 6 staged admission applicants across intake gates, 48 enrolled students with
/// authentic Indonesian identities across ELC, JHLC, and HSLC, deterministic flagship personas
/// (Jonathan Pratama, Grace Santoso, Budi Wijaya), linked guardians, administrative placements,
/// and student statuses.
/// </summary>
public sealed class StudentsDemoDataSeeder(
    ISqlConnectionFactory connectionFactory) : IStudentsDemoDataSeeder
{
    static StudentsDemoDataSeeder()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _createdBy = "demo-data-seeder";
    private const string _campusMain = "campus-main";

    private const string _insertApplicantSql = """
        INSERT INTO students.applicants (
            id, first_name, last_name, full_name, preferred_name,
            date_of_birth, place_of_birth, gender, nationality,
            government_id_type, government_id_number, address,
            email, phone, notes, created_at, created_by
        )
        VALUES (
            @Id, @FirstName, @LastName, @FullName, @PreferredName,
            @DateOfBirth, @PlaceOfBirth, @Gender, @Nationality,
            @GovernmentIdType, @GovernmentIdNumber, @Address,
            @Email, @Phone, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertApplicationSql = """
        INSERT INTO students.admission_applications (
            id, applicant_id, academic_year_id, admission_period_id,
            application_number, application_type, requested_entry_level,
            requested_campus, requested_program, registration_date,
            status, submitted_at, notes, created_at, created_by
        )
        VALUES (
            @Id, @ApplicantId, @AcademicYearId, @AdmissionPeriodId,
            @ApplicationNumber, @ApplicationType, @RequestedEntryLevel,
            @RequestedCampus, @RequestedProgram, @RegistrationDate,
            @Status, @SubmittedAt, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertDecisionSql = """
        INSERT INTO students.admission_decisions (
            id, application_id, decision_type, decision_date,
            effective_date, decided_by, reason, supporting_reference,
            deferred_admission_period_id, created_at, created_by
        )
        VALUES (
            @Id, @ApplicationId, @DecisionType, @DecisionDate,
            @EffectiveDate, @DecidedBy, @Reason, @SupportingReference,
            @DeferredAdmissionPeriodId, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertAssessmentEvidenceSql = """
        INSERT INTO students.entrance_assessment_evidence (
            id, application_id, assessment_type, score,
            source, assessment_date, notes, created_at, created_by
        )
        VALUES (
            @Id, @ApplicationId, @AssessmentType, @Score,
            @Source, @AssessmentDate, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertApplicantDocSql = """
        INSERT INTO students.applicant_documents (
            id, application_id, document_type, submission_status,
            submitted_date, verified_date, verified_by,
            file_reference, file_name, file_size_bytes, content_type,
            notes, created_at, created_by
        )
        VALUES (
            @Id, @ApplicationId, @DocumentType, @SubmissionStatus,
            @SubmittedDate, @VerifiedDate, @VerifiedBy,
            @FileReference, @FileName, @FileSizeBytes, @ContentType,
            @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertContactSql = """
        INSERT INTO students.admission_application_contacts (
            id, application_id, full_name, relationship,
            phone, email, address, is_primary, created_at, created_by
        )
        VALUES (
            @Id, @ApplicationId, @FullName, @Relationship,
            @Phone, @Email, @Address, @IsPrimary, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertStudentSql = """
        INSERT INTO students.students (
            id, student_number, applicant_id, full_name, preferred_name,
            date_of_birth, place_of_birth, gender, nationality,
            government_id_type, government_id_number, address,
            phone, email, is_independent, current_status,
            enrollment_date, notes, created_at, created_by
        )
        VALUES (
            @Id, @StudentNumber, @ApplicantId, @FullName, @PreferredName,
            @DateOfBirth, @PlaceOfBirth, @Gender, @Nationality,
            @GovernmentIdType, @GovernmentIdNumber, @Address,
            @Phone, @Email, @IsIndependent, @CurrentStatus,
            @EnrollmentDate, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertEnrollmentSql = """
        INSERT INTO students.enrollments (
            id, student_id, application_id, enrollment_type,
            enrollment_date, initial_campus, initial_level,
            initial_academic_year, initial_program, initial_cohort,
            approved_by, enrollment_payment_confirmed, payment_reference,
            payment_waived, waiver_reference, notes, created_at, created_by
        )
        VALUES (
            @Id, @StudentId, @ApplicationId, @EnrollmentType,
            @EnrollmentDate, @InitialCampus, @InitialLevel,
            @InitialAcademicYear, @InitialProgram, @InitialCohort,
            @ApprovedBy, @EnrollmentPaymentConfirmed, @PaymentReference,
            @PaymentWaived, @WaiverReference, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertGuardianSql = """
        INSERT INTO students.guardians (
            id, full_name, government_id_type, government_id_number,
            phone, email, address, occupation, notes, created_at, created_by
        )
        VALUES (
            @Id, @FullName, @GovernmentIdType, @GovernmentIdNumber,
            @Phone, @Email, @Address, @Occupation, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertRelationshipSql = """
        INSERT INTO students.student_guardian_relationships (
            id, student_id, guardian_id, relationship_type,
            is_primary_contact, is_emergency_contact, is_financial_contact,
            is_authorized_pickup, is_active, created_at, created_by
        )
        VALUES (
            @Id, @StudentId, @GuardianId, @RelationshipType,
            @IsPrimaryContact, @IsEmergencyContact, @IsFinancialContact,
            @IsAuthorizedPickup, @IsActive, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertPlacementSql = """
        INSERT INTO students.administrative_placements (
            id, student_id, campus, school_level, grade,
            academic_year, effective_date, program, stream,
            cohort, homeroom, end_date, is_current, reason,
            authorized_by, decision_reference, created_at, created_by
        )
        VALUES (
            @Id, @StudentId, @Campus, @SchoolLevel, @Grade,
            @AcademicYear, @EffectiveDate, @Program, @Stream,
            @Cohort, @Homeroom, @EndDate, @IsCurrent, @Reason,
            @AuthorizedBy, @DecisionReference, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertStatusSql = """
        INSERT INTO students.student_statuses (
            id, student_id, status, effective_date, end_date,
            is_current, reason, authorized_by, decision_reference,
            created_at, created_by
        )
        VALUES (
            @Id, @StudentId, @Status, @EffectiveDate, @EndDate,
            @IsCurrent, @Reason, @AuthorizedBy, @DecisionReference,
            @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertHistorySql = """
        INSERT INTO students.administrative_history_entries (
            id, student_id, event_type, event_date, detail,
            authorized_by, decision_reference, is_correction,
            corrects_entry_id, created_at, created_by
        )
        VALUES (
            @Id, @StudentId, @EventType, @EventDate, @Detail,
            @AuthorizedBy, @DecisionReference, @IsCorrection,
            @CorrectsEntryId, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertStudentDocSql = """
        INSERT INTO students.student_documents (
            id, student_id, document_type, submission_status,
            submitted_date, verified_date, verified_by,
            file_reference, file_name, file_size_bytes, content_type,
            valid_until, notes, created_at, created_by
        )
        VALUES (
            @Id, @StudentId, @DocumentType, @SubmissionStatus,
            @SubmittedDate, @VerifiedDate, @VerifiedBy,
            @FileReference, @FileName, @FileSizeBytes, @ContentType,
            @ValidUntil, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    public async Task<StudentsSeedResult> SeedStudentsAsync(
        OperationsSeedResult operationsContext,
        AcademicsStructureSeedResult academicsContext,
        FinancePolicySeedResult financeContext,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var baseDate = operationsContext.BaseDate;
        var academicYearId = operationsContext.AcademicYearId;
        var academicYearCode = operationsContext.AcademicYearCode;
        var activePeriodId = operationsContext.ActivePeriodId;

        // Deterministic IDs for flagship personas
        var jonathanStudentId = DemoDataConstants.JonathanStudentId;
        var jonathanApplicantId = DemoDataConstants.JonathanApplicantId;
        var hendraParentId = DemoDataConstants.HendraParentId;

        var graceStudentId = DemoDataConstants.GraceStudentId;
        var graceApplicantId = DemoDataConstants.GraceApplicantId;
        var graceGuardianId = DemoDataConstants.GraceGuardianId;

        var budiStudentId = DemoDataConstants.BudiStudentId;
        var budiApplicantId = new Guid("00000000-0000-0003-0000-000000000002");
        var budiGuardianId = DemoDataConstants.BudiGuardianId;

        // 6 Staged applicant deterministic IDs
        var danielApplicantId = DemoDataConstants.DanielApplicantId;
        var mariaApplicantId = DemoDataConstants.MariaApplicantId;
        var samuelApplicantId = DemoDataConstants.SamuelApplicantId;
        var estherApplicantId = DemoDataConstants.EstherApplicantId;
        var ruthApplicantId = DemoDataConstants.RuthApplicantId;
        var timotiusApplicantId = DemoDataConstants.TimotiusApplicantId;

        // Build Indonesian names roster for the 45 non-flagship enrolled students
        // Total students: 3 flagships + 45 = 48 students (18 ELC, 15 JHLC, 15 HSLC)
        var additionalProfiles = GetAdditionalStudentProfiles(baseDate);

        var enrolledStudentIds = new List<Guid> { jonathanStudentId, graceStudentId, budiStudentId };
        var elcList = new List<Guid> { budiStudentId };
        var jhlcList = new List<Guid> { graceStudentId };
        var hslcList = new List<Guid> { jonathanStudentId };

        foreach (var p in additionalProfiles)
        {
            enrolledStudentIds.Add(p.StudentId);
            switch (p.SchoolLevel)
            {
                case "ELC":
                    elcList.Add(p.StudentId);
                    break;
                case "JHLC":
                    jhlcList.Add(p.StudentId);
                    break;
                default:
                    hslcList.Add(p.StudentId);
                    break;
            }
        }

        var cohortDict = new Dictionary<string, IReadOnlyList<Guid>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ELC"] = elcList.AsReadOnly(),
            ["JHLC"] = jhlcList.AsReadOnly(),
            ["HSLC"] = hslcList.AsReadOnly()
        };

        var stagedApplicantIdsDict = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            ["DanielApplicantId"] = danielApplicantId,
            ["MariaApplicantId"] = mariaApplicantId,
            ["SamuelApplicantId"] = samuelApplicantId,
            ["EstherApplicantId"] = estherApplicantId,
            ["RuthApplicantId"] = ruthApplicantId,
            ["TimotiusApplicantId"] = timotiusApplicantId
        };

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // =========================================================================
            // 1. SEED 6 STAGED APPLICANTS (Daniel, Maria, Samuel, Esther, Ruth, Timotius)
            // =========================================================================
            var stagedApplicants = new[]
            {
                new
                {
                    Id = danielApplicantId,
                    FirstName = "Daniel",
                    LastName = "Kurniawan",
                    FullName = "Daniel Kurniawan",
                    PreferredName = "Daniel",
                    DateOfBirth = new DateOnly(2011, 3, 12),
                    PlaceOfBirth = "Yogyakarta",
                    Gender = "Male",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471011234560001",
                    Address = "Jl. Malioboro No. 42, Yogyakarta 55111",
                    Email = "daniel.kurniawan@demo.hagios.sch.id",
                    Phone = "+6281211110001",
                    Notes = (string?)null,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = mariaApplicantId,
                    FirstName = "Maria",
                    LastName = "Sitorus",
                    FullName = "Maria Sitorus",
                    PreferredName = "Maria",
                    DateOfBirth = new DateOnly(2013, 7, 8),
                    PlaceOfBirth = "Bantul, DI Yogyakarta",
                    Gender = "Female",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471015678900002",
                    Address = "Jl. Sosrowijayan No. 15, Yogyakarta 55231",
                    Email = "maria.sitorus@demo.hagios.sch.id",
                    Phone = "+6281211110002",
                    Notes = (string?)null,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = samuelApplicantId,
                    FirstName = "Samuel",
                    LastName = "Simanjuntak",
                    FullName = "Samuel Simanjuntak",
                    PreferredName = "Samuel",
                    DateOfBirth = new DateOnly(2014, 11, 21),
                    PlaceOfBirth = "Sleman, DI Yogyakarta",
                    Gender = "Male",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471019876540003",
                    Address = "Jl. Parangtritis No. 78, Yogyakarta 55251",
                    Email = "samuel.simanjuntak@demo.hagios.sch.id",
                    Phone = "+6281211110003",
                    Notes = (string?)null,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = estherApplicantId,
                    FirstName = "Esther",
                    LastName = "Nugroho",
                    FullName = "Esther Nugroho",
                    PreferredName = "Esther",
                    DateOfBirth = new DateOnly(2017, 2, 14),
                    PlaceOfBirth = "Klaten, Jawa Tengah",
                    Gender = "Female",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471012345670004",
                    Address = "Jl. Gejayan No. 3, Yogyakarta 55177",
                    Email = "esther.nugroho@demo.hagios.sch.id",
                    Phone = "+6281211110004",
                    Notes = (string?)null,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = ruthApplicantId,
                    FirstName = "Ruth",
                    LastName = "Halim",
                    FullName = "Ruth Halim",
                    PreferredName = "Ruth",
                    DateOfBirth = new DateOnly(2011, 5, 3),
                    PlaceOfBirth = "Cirebon, Jawa Barat",
                    Gender = "Female",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471013456780005",
                    Address = "Jl. Raya Cirebon No. 88, Cirebon 45111",
                    Email = "ruth.halim@demo.hagios.sch.id",
                    Phone = "+6281211110005",
                    Notes = (string?)null,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = timotiusApplicantId,
                    FirstName = "Timotius",
                    LastName = "Hartono",
                    FullName = "Timotius Hartono",
                    PreferredName = "Timotius",
                    DateOfBirth = new DateOnly(2019, 9, 30),
                    PlaceOfBirth = "Banyumas, Jawa Tengah",
                    Gender = "Male",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471014567890006",
                    Address = "Jl. Veteran No. 123, Banyumas 53111",
                    Email = "timotius.hartono@demo.hagios.sch.id",
                    Phone = "+6281211110006",
                    Notes = (string?)"Family relocation to external school",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                }
            };

            await connection.ExecuteAsync(new CommandDefinition(
                _insertApplicantSql, stagedApplicants, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 2. SEED APPLICANTS FOR ALL 48 ENROLLED STUDENTS
            // =========================================================================
            var enrolledApplicants = new List<object>
            {
                // Jonathan Pratama
                new
                {
                    Id = jonathanApplicantId,
                    FirstName = "Jonathan",
                    LastName = "Pratama",
                    FullName = "Jonathan Pratama",
                    PreferredName = "Jonathan",
                    DateOfBirth = new DateOnly(2009, 8, 15),
                    PlaceOfBirth = "Yogyakarta",
                    Gender = "Male",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471011508090001",
                    Address = "Jl. Malioboro No. 42, Yogyakarta 55111",
                    Email = "jonathan.pratama@student.hagios.sch.id",
                    Phone = "+6281200000001",
                    Notes = "Flagship High Honors Student",
                    CreatedAt = now.AddDays(-190),
                    CreatedBy = _createdBy
                },
                // Grace Santoso
                new
                {
                    Id = graceApplicantId,
                    FirstName = "Grace",
                    LastName = "Santoso",
                    FullName = "Grace Santoso",
                    PreferredName = "Grace",
                    DateOfBirth = new DateOnly(2011, 4, 20),
                    PlaceOfBirth = "Semarang",
                    Gender = "Female",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471022004110002",
                    Address = "Jl. Kaliurang Km 5 No. 12, Sleman 55281",
                    Email = "grace.santoso@student.hagios.sch.id",
                    Phone = "+6281200000002",
                    Notes = "Flagship Retake & Overdue Student",
                    CreatedAt = now.AddDays(-190),
                    CreatedBy = _createdBy
                },
                // Budi Wijaya
                new
                {
                    Id = budiApplicantId,
                    FirstName = "Budi",
                    LastName = "Wijaya",
                    FullName = "Budi Wijaya",
                    PreferredName = "Budi",
                    DateOfBirth = new DateOnly(2016, 11, 10),
                    PlaceOfBirth = "Yogyakarta",
                    Gender = "Male",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471031011160003",
                    Address = "Jl. Gejayan No. 25, Yogyakarta 55222",
                    Email = "budi.wijaya@student.hagios.sch.id",
                    Phone = "+6281200000003",
                    Notes = "Flagship Live Grading Student",
                    CreatedAt = now.AddDays(-190),
                    CreatedBy = _createdBy
                }
            };

            foreach (var p in additionalProfiles)
            {
                enrolledApplicants.Add(new
                {
                    Id = p.ApplicantId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    FullName = $"{p.FirstName} {p.LastName}",
                    PreferredName = p.FirstName,
                    DateOfBirth = p.DateOfBirth,
                    PlaceOfBirth = p.PlaceOfBirth,
                    Gender = p.Gender,
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = p.Nik,
                    Address = p.Address,
                    Email = p.Email,
                    Phone = p.Phone,
                    Notes = (string?)null,
                    CreatedAt = now.AddDays(-190),
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertApplicantSql, enrolledApplicants, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 3. SEED ADMISSION APPLICATIONS (6 staged + 48 enrolled)
            // =========================================================================
            var danielAppId = Guid.NewGuid();
            var mariaAppId = Guid.NewGuid();
            var samuelAppId = Guid.NewGuid();
            var estherAppId = Guid.NewGuid();
            var ruthAppId = Guid.NewGuid();
            var timotiusAppId = Guid.NewGuid();

            var stagedApplications = new[]
            {
                new
                {
                    Id = danielAppId,
                    ApplicantId = danielApplicantId,
                    AcademicYearId = academicYearId,
                    AdmissionPeriodId = activePeriodId,
                    ApplicationNumber = "APP-2026-0001",
                    ApplicationType = "new",
                    RequestedEntryLevel = "Grade 10",
                    RequestedCampus = _campusMain,
                    RequestedProgram = "ACE High School Curriculum",
                    RegistrationDate = DateOnly.FromDateTime(baseDate.AddDays(-50)),
                    Status = "submitted",
                    SubmittedAt = (DateTimeOffset?)now.AddDays(-50),
                    Notes = "Complete dossier awaiting review queue",
                    CreatedAt = now.AddDays(-50),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = mariaAppId,
                    ApplicantId = mariaApplicantId,
                    AcademicYearId = academicYearId,
                    AdmissionPeriodId = activePeriodId,
                    ApplicationNumber = "APP-2026-0002",
                    ApplicationType = "new",
                    RequestedEntryLevel = "Grade 8",
                    RequestedCampus = _campusMain,
                    RequestedProgram = "ACE Junior High Curriculum",
                    RegistrationDate = DateOnly.FromDateTime(baseDate.AddDays(-40)),
                    Status = "under_review",
                    SubmittedAt = (DateTimeOffset?)now.AddDays(-40),
                    Notes = "Under administrative review by admission committee",
                    CreatedAt = now.AddDays(-40),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = samuelAppId,
                    ApplicantId = samuelApplicantId,
                    AcademicYearId = academicYearId,
                    AdmissionPeriodId = activePeriodId,
                    ApplicationNumber = "APP-2026-0003",
                    ApplicationType = "new",
                    RequestedEntryLevel = "Grade 7",
                    RequestedCampus = _campusMain,
                    RequestedProgram = "ACE Junior High Curriculum",
                    RegistrationDate = DateOnly.FromDateTime(baseDate.AddDays(-30)),
                    Status = "entrance_assessed",
                    SubmittedAt = (DateTimeOffset?)now.AddDays(-30),
                    Notes = "Diagnostic assessment score 88.5 recorded",
                    CreatedAt = now.AddDays(-30),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = estherAppId,
                    ApplicantId = estherApplicantId,
                    AcademicYearId = academicYearId,
                    AdmissionPeriodId = activePeriodId,
                    ApplicationNumber = "APP-2026-0004",
                    ApplicationType = "new",
                    RequestedEntryLevel = "Grade 4",
                    RequestedCampus = _campusMain,
                    RequestedProgram = "ACE Elementary Curriculum",
                    RegistrationDate = DateOnly.FromDateTime(baseDate.AddDays(-25)),
                    Status = "accepted",
                    SubmittedAt = (DateTimeOffset?)now.AddDays(-25),
                    Notes = "Accepted - awaiting admission fee confirmation",
                    CreatedAt = now.AddDays(-25),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = ruthAppId,
                    ApplicantId = ruthApplicantId,
                    AcademicYearId = academicYearId,
                    AdmissionPeriodId = activePeriodId,
                    ApplicationNumber = "APP-2026-0005",
                    ApplicationType = "new",
                    RequestedEntryLevel = "Grade 10",
                    RequestedCampus = _campusMain,
                    RequestedProgram = "ACE High School Curriculum",
                    RegistrationDate = DateOnly.FromDateTime(baseDate.AddDays(-20)),
                    Status = "accepted",
                    SubmittedAt = (DateTimeOffset?)now.AddDays(-20),
                    Notes = "Accepted & admission fee verified paid. Pre-staged for 1-click live enrollment demo.",
                    CreatedAt = now.AddDays(-20),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = timotiusAppId,
                    ApplicantId = timotiusApplicantId,
                    AcademicYearId = academicYearId,
                    AdmissionPeriodId = activePeriodId,
                    ApplicationNumber = "APP-2026-0006",
                    ApplicationType = "new",
                    RequestedEntryLevel = "Grade 2",
                    RequestedCampus = _campusMain,
                    RequestedProgram = "ACE Elementary Curriculum",
                    RegistrationDate = DateOnly.FromDateTime(baseDate.AddDays(-60)),
                    Status = "withdrawn",
                    SubmittedAt = (DateTimeOffset?)now.AddDays(-60),
                    Notes = "Withdrawn by parent due to out-of-province family relocation",
                    CreatedAt = now.AddDays(-60),
                    CreatedBy = _createdBy
                }
            };

            await connection.ExecuteAsync(new CommandDefinition(
                _insertApplicationSql, stagedApplications, transaction: transaction, cancellationToken: cancellationToken));

            // Applications for the 48 enrolled students
            var enrolledApplications = new List<object>();
            var jonathanAppId = Guid.NewGuid();
            var graceAppId = Guid.NewGuid();
            var budiAppId = Guid.NewGuid();

            enrolledApplications.Add(new
            {
                Id = jonathanAppId,
                ApplicantId = jonathanApplicantId,
                AcademicYearId = academicYearId,
                AdmissionPeriodId = activePeriodId,
                ApplicationNumber = "APP-2026-1001",
                ApplicationType = "new",
                RequestedEntryLevel = "Grade 10",
                RequestedCampus = _campusMain,
                RequestedProgram = "ACE High School Curriculum",
                RegistrationDate = DateOnly.FromDateTime(baseDate.AddDays(-190)),
                Status = "enrolled",
                SubmittedAt = (DateTimeOffset?)now.AddDays(-190),
                Notes = "Admission complete and enrolled",
                CreatedAt = now.AddDays(-190),
                CreatedBy = _createdBy
            });

            enrolledApplications.Add(new
            {
                Id = graceAppId,
                ApplicantId = graceApplicantId,
                AcademicYearId = academicYearId,
                AdmissionPeriodId = activePeriodId,
                ApplicationNumber = "APP-2026-1002",
                ApplicationType = "new",
                RequestedEntryLevel = "Grade 8",
                RequestedCampus = _campusMain,
                RequestedProgram = "ACE Junior High Curriculum",
                RegistrationDate = DateOnly.FromDateTime(baseDate.AddDays(-190)),
                Status = "enrolled",
                SubmittedAt = (DateTimeOffset?)now.AddDays(-190),
                Notes = "Admission complete and enrolled",
                CreatedAt = now.AddDays(-190),
                CreatedBy = _createdBy
            });

            enrolledApplications.Add(new
            {
                Id = budiAppId,
                ApplicantId = budiApplicantId,
                AcademicYearId = academicYearId,
                AdmissionPeriodId = activePeriodId,
                ApplicationNumber = "APP-2026-1003",
                ApplicationType = "new",
                RequestedEntryLevel = "Grade 3",
                RequestedCampus = _campusMain,
                RequestedProgram = "ACE Elementary Curriculum",
                RegistrationDate = DateOnly.FromDateTime(baseDate.AddDays(-190)),
                Status = "enrolled",
                SubmittedAt = (DateTimeOffset?)now.AddDays(-190),
                Notes = "Admission complete and enrolled",
                CreatedAt = now.AddDays(-190),
                CreatedBy = _createdBy
            });

            var studentApplicationMap = new Dictionary<Guid, Guid>
            {
                [jonathanStudentId] = jonathanAppId,
                [graceStudentId] = graceAppId,
                [budiStudentId] = budiAppId
            };

            for (int i = 0; i < additionalProfiles.Count; i++)
            {
                var p = additionalProfiles[i];
                var appId = Guid.NewGuid();
                studentApplicationMap[p.StudentId] = appId;

                enrolledApplications.Add(new
                {
                    Id = appId,
                    ApplicantId = p.ApplicantId,
                    AcademicYearId = academicYearId,
                    AdmissionPeriodId = activePeriodId,
                    ApplicationNumber = $"APP-2026-{1004 + i:D4}",
                    ApplicationType = "new",
                    RequestedEntryLevel = p.Grade,
                    RequestedCampus = _campusMain,
                    RequestedProgram = $"ACE {p.SchoolLevel} Curriculum",
                    RegistrationDate = DateOnly.FromDateTime(baseDate.AddDays(-190)),
                    Status = "enrolled",
                    SubmittedAt = (DateTimeOffset?)now.AddDays(-190),
                    Notes = "Admission complete and enrolled",
                    CreatedAt = now.AddDays(-190),
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertApplicationSql, enrolledApplications, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 4. SEED ADMISSION DECISIONS (Samuel, Esther, Ruth, and 48 Enrolled)
            // =========================================================================
            var admissionDecisions = new List<object>
            {
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = samuelAppId,
                    DecisionType = "conditional_accept",
                    DecisionDate = DateOnly.FromDateTime(baseDate.AddDays(-22)),
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-22)),
                    DecidedBy = "demo-admission-officer",
                    Reason = "Diagnostic entrance assessment passed with score 88.5. Conditional acceptance granted.",
                    SupportingReference = "DEC-ADM-SAMUEL",
                    DeferredAdmissionPeriodId = (Guid?)null,
                    CreatedAt = now.AddDays(-22),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = estherAppId,
                    DecisionType = "accepted",
                    DecisionDate = DateOnly.FromDateTime(baseDate.AddDays(-20)),
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-20)),
                    DecidedBy = "demo-admission-officer",
                    Reason = "Academic prerequisites verified. Full acceptance granted.",
                    SupportingReference = "DEC-ADM-ESTHER",
                    DeferredAdmissionPeriodId = (Guid?)null,
                    CreatedAt = now.AddDays(-20),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = ruthAppId,
                    DecisionType = "accepted",
                    DecisionDate = DateOnly.FromDateTime(baseDate.AddDays(-15)),
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-15)),
                    DecidedBy = "demo-admission-officer",
                    Reason = "Full acceptance granted. Admission fee payment verified.",
                    SupportingReference = "DEC-ADM-RUTH",
                    DeferredAdmissionPeriodId = (Guid?)null,
                    CreatedAt = now.AddDays(-15),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = jonathanAppId,
                    DecisionType = "accepted",
                    DecisionDate = DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    DecidedBy = "demo-admission-officer",
                    Reason = "High Honors academic profile. Unconditional admission.",
                    SupportingReference = "DEC-ADM-JONATHAN",
                    DeferredAdmissionPeriodId = (Guid?)null,
                    CreatedAt = now.AddDays(-185),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = graceAppId,
                    DecisionType = "accepted",
                    DecisionDate = DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    DecidedBy = "demo-admission-officer",
                    Reason = "Prerequisites satisfied. Unconditional admission.",
                    SupportingReference = "DEC-ADM-GRACE",
                    DeferredAdmissionPeriodId = (Guid?)null,
                    CreatedAt = now.AddDays(-185),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = budiAppId,
                    DecisionType = "accepted",
                    DecisionDate = DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    DecidedBy = "demo-admission-officer",
                    Reason = "Elementary intake readiness confirmed.",
                    SupportingReference = "DEC-ADM-BUDI",
                    DeferredAdmissionPeriodId = (Guid?)null,
                    CreatedAt = now.AddDays(-185),
                    CreatedBy = _createdBy
                }
            };

            foreach (var p in additionalProfiles)
            {
                admissionDecisions.Add(new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = studentApplicationMap[p.StudentId],
                    DecisionType = "accepted",
                    DecisionDate = DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    DecidedBy = "demo-admission-officer",
                    Reason = "Intake credentials verified.",
                    SupportingReference = $"DEC-ADM-{p.StudentNumber}",
                    DeferredAdmissionPeriodId = (Guid?)null,
                    CreatedAt = now.AddDays(-185),
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertDecisionSql, admissionDecisions, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 5. SEED ENTRANCE ASSESSMENT EVIDENCE (for Samuel)
            // =========================================================================
            var assessmentEvidence = new
            {
                Id = Guid.NewGuid(),
                ApplicationId = samuelAppId,
                AssessmentType = "Diagnostic Math & English PACE Readiness",
                Score = "88.5",
                Source = "Hagios Assessment Center",
                AssessmentDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-25)),
                Notes = "Strong foundational readiness across ACE PACE Level 7 curriculum.",
                CreatedAt = now.AddDays(-25),
                CreatedBy = _createdBy
            };

            await connection.ExecuteAsync(new CommandDefinition(
                _insertAssessmentEvidenceSql, assessmentEvidence, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 6. SEED APPLICANT DOCUMENTS & CONTACTS
            // =========================================================================
            var applicantDocs = new List<object>
            {
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = danielAppId,
                    DocumentType = "Birth Certificate",
                    SubmissionStatus = "submitted",
                    SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-50)),
                    VerifiedDate = (DateOnly?)null,
                    VerifiedBy = (string?)null,
                    FileReference = $"documents/applicants/{danielApplicantId:N}/akta_kelahiran.pdf",
                    FileName = "akta_kelahiran_daniel.pdf",
                    FileSizeBytes = (long?)245760,
                    ContentType = "application/pdf",
                    Notes = "Pending registrar document verification",
                    CreatedAt = now.AddDays(-50),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = mariaAppId,
                    DocumentType = "Birth Certificate",
                    SubmissionStatus = "submitted",
                    SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-40)),
                    VerifiedDate = (DateOnly?)null,
                    VerifiedBy = (string?)null,
                    FileReference = $"documents/applicants/{mariaApplicantId:N}/akta_kelahiran.pdf",
                    FileName = "akta_kelahiran_maria.pdf",
                    FileSizeBytes = (long?)312400,
                    ContentType = "application/pdf",
                    Notes = "Under verification",
                    CreatedAt = now.AddDays(-40),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = samuelAppId,
                    DocumentType = "Birth Certificate",
                    SubmissionStatus = "verified",
                    SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-30)),
                    VerifiedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-28)),
                    VerifiedBy = "demo-admission-officer",
                    FileReference = $"documents/applicants/{samuelApplicantId:N}/akta_kelahiran.pdf",
                    FileName = "akta_kelahiran_samuel.pdf",
                    FileSizeBytes = (long?)198600,
                    ContentType = "application/pdf",
                    Notes = "Verified against civil registry",
                    CreatedAt = now.AddDays(-30),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = estherAppId,
                    DocumentType = "Birth Certificate",
                    SubmissionStatus = "verified",
                    SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-25)),
                    VerifiedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-22)),
                    VerifiedBy = "demo-admission-officer",
                    FileReference = $"documents/applicants/{estherApplicantId:N}/akta_kelahiran.pdf",
                    FileName = "akta_kelahiran_esther.pdf",
                    FileSizeBytes = (long?)225400,
                    ContentType = "application/pdf",
                    Notes = "Verified",
                    CreatedAt = now.AddDays(-25),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = ruthAppId,
                    DocumentType = "Birth Certificate",
                    SubmissionStatus = "verified",
                    SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-20)),
                    VerifiedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-18)),
                    VerifiedBy = "demo-admission-officer",
                    FileReference = $"documents/applicants/{ruthApplicantId:N}/akta_kelahiran.pdf",
                    FileName = "akta_kelahiran_ruth.pdf",
                    FileSizeBytes = (long?)280100,
                    ContentType = "application/pdf",
                    Notes = "Official verified certified copy",
                    CreatedAt = now.AddDays(-20),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = timotiusAppId,
                    DocumentType = "Birth Certificate",
                    SubmissionStatus = "submitted",
                    SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-60)),
                    VerifiedDate = (DateOnly?)null,
                    VerifiedBy = (string?)null,
                    FileReference = $"documents/applicants/{timotiusApplicantId:N}/akta_kelahiran.pdf",
                    FileName = "akta_kelahiran_timotius.pdf",
                    FileSizeBytes = (long?)210400,
                    ContentType = "application/pdf",
                    Notes = "Archived due to withdrawal",
                    CreatedAt = now.AddDays(-60),
                    CreatedBy = _createdBy
                }
            };

            // Verified docs for the 3 flagship enrolled applicants
            applicantDocs.Add(new
            {
                Id = Guid.NewGuid(),
                ApplicationId = jonathanAppId,
                DocumentType = "Birth Certificate",
                SubmissionStatus = "verified",
                SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-190)),
                VerifiedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-185)),
                VerifiedBy = "demo-admission-officer",
                FileReference = $"documents/applicants/{jonathanApplicantId:N}/akta_kelahiran.pdf",
                FileName = "akta_kelahiran_jonathan.pdf",
                FileSizeBytes = (long?)256000,
                ContentType = "application/pdf",
                Notes = "Verified official birth certificate",
                CreatedAt = now.AddDays(-190),
                CreatedBy = _createdBy
            });
            applicantDocs.Add(new
            {
                Id = Guid.NewGuid(),
                ApplicationId = graceAppId,
                DocumentType = "Birth Certificate",
                SubmissionStatus = "verified",
                SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-190)),
                VerifiedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-185)),
                VerifiedBy = "demo-admission-officer",
                FileReference = $"documents/applicants/{graceApplicantId:N}/akta_kelahiran.pdf",
                FileName = "akta_kelahiran_grace.pdf",
                FileSizeBytes = (long?)240000,
                ContentType = "application/pdf",
                Notes = "Verified official birth certificate",
                CreatedAt = now.AddDays(-190),
                CreatedBy = _createdBy
            });
            applicantDocs.Add(new
            {
                Id = Guid.NewGuid(),
                ApplicationId = budiAppId,
                DocumentType = "Birth Certificate",
                SubmissionStatus = "verified",
                SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-190)),
                VerifiedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-185)),
                VerifiedBy = "demo-admission-officer",
                FileReference = $"documents/applicants/{budiApplicantId:N}/akta_kelahiran.pdf",
                FileName = "akta_kelahiran_budi.pdf",
                FileSizeBytes = (long?)235000,
                ContentType = "application/pdf",
                Notes = "Verified official birth certificate",
                CreatedAt = now.AddDays(-190),
                CreatedBy = _createdBy
            });

            await connection.ExecuteAsync(new CommandDefinition(
                _insertApplicantDocSql, applicantDocs, transaction: transaction, cancellationToken: cancellationToken));

            var applicantContacts = new[]
            {
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = danielAppId,
                    FullName = "Bambang Kurniawan",
                    Relationship = "Father",
                    Phone = "+628113456001",
                    Email = "bambang.kurniawan@demo.hagios.sch.id",
                    Address = "Jl. Malioboro No. 42, Yogyakarta 55111",
                    IsPrimary = true,
                    CreatedAt = now.AddDays(-50),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = mariaAppId,
                    FullName = "Rina Sitorus",
                    Relationship = "Mother",
                    Phone = "+628113456002",
                    Email = "rina.sitorus@demo.hagios.sch.id",
                    Address = "Jl. Sosrowijayan No. 15, Yogyakarta 55231",
                    IsPrimary = true,
                    CreatedAt = now.AddDays(-40),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = samuelAppId,
                    FullName = "Togar Simanjuntak",
                    Relationship = "Father",
                    Phone = "+628113456003",
                    Email = "togar.simanjuntak@demo.hagios.sch.id",
                    Address = "Jl. Parangtritis No. 78, Yogyakarta 55251",
                    IsPrimary = true,
                    CreatedAt = now.AddDays(-30),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = estherAppId,
                    FullName = "Dewi Nugroho",
                    Relationship = "Mother",
                    Phone = "+628113456004",
                    Email = "dewi.nugroho@demo.hagios.sch.id",
                    Address = "Jl. Gejayan No. 3, Yogyakarta 55177",
                    IsPrimary = true,
                    CreatedAt = now.AddDays(-25),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = ruthAppId,
                    FullName = "David Halim",
                    Relationship = "Father",
                    Phone = "+628113456005",
                    Email = "david.halim@demo.hagios.sch.id",
                    Address = "Jl. Raya Cirebon No. 88, Cirebon 45111",
                    IsPrimary = true,
                    CreatedAt = now.AddDays(-20),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = timotiusAppId,
                    FullName = "Hadi Hartono",
                    Relationship = "Father",
                    Phone = "+628113456006",
                    Email = "hadi.hartono@demo.hagios.sch.id",
                    Address = "Jl. Veteran No. 123, Banyumas 53111",
                    IsPrimary = true,
                    CreatedAt = now.AddDays(-60),
                    CreatedBy = _createdBy
                }
            };

            await connection.ExecuteAsync(new CommandDefinition(
                _insertContactSql, applicantContacts, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 7. SEED 48 ENROLLED STUDENTS (students.students)
            // =========================================================================
            var studentsToInsert = new List<object>
            {
                // Jonathan Pratama (Flagship High Honors Student)
                new
                {
                    Id = jonathanStudentId,
                    StudentNumber = "261001",
                    ApplicantId = jonathanApplicantId,
                    FullName = "Jonathan Pratama",
                    PreferredName = "Jonathan",
                    DateOfBirth = (DateOnly?)new DateOnly(2009, 8, 15),
                    PlaceOfBirth = "Yogyakarta",
                    Gender = "Male",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471011508090001",
                    Address = "Jl. Malioboro No. 42, Yogyakarta 55111",
                    Phone = "+6281200000001",
                    Email = "jonathan.pratama@student.hagios.sch.id",
                    IsIndependent = false,
                    CurrentStatus = "active",
                    EnrollmentDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Notes = "Flagship High Honors Student - High School Grade 10",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                // Grace Santoso (Flagship Retake & Overdue Student)
                new
                {
                    Id = graceStudentId,
                    StudentNumber = "260801",
                    ApplicantId = graceApplicantId,
                    FullName = "Grace Santoso",
                    PreferredName = "Grace",
                    DateOfBirth = (DateOnly?)new DateOnly(2011, 4, 20),
                    PlaceOfBirth = "Semarang",
                    Gender = "Female",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471022004110002",
                    Address = "Jl. Kaliurang Km 5 No. 12, Sleman 55281",
                    Phone = "+6281200000002",
                    Email = "grace.santoso@student.hagios.sch.id",
                    IsIndependent = false,
                    CurrentStatus = "active",
                    EnrollmentDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Notes = "Flagship Retake & Overdue Student - Junior High Grade 8",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                // Budi Wijaya (Flagship Live Grading Student)
                new
                {
                    Id = budiStudentId,
                    StudentNumber = "260301",
                    ApplicantId = budiApplicantId,
                    FullName = "Budi Wijaya",
                    PreferredName = "Budi",
                    DateOfBirth = (DateOnly?)new DateOnly(2016, 11, 10),
                    PlaceOfBirth = "Yogyakarta",
                    Gender = "Male",
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471031011160003",
                    Address = "Jl. Gejayan No. 25, Yogyakarta 55222",
                    Phone = "+6281200000003",
                    Email = "budi.wijaya@student.hagios.sch.id",
                    IsIndependent = false,
                    CurrentStatus = "active",
                    EnrollmentDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Notes = "Flagship Live Grading Student - Elementary Grade 3",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                }
            };

            foreach (var p in additionalProfiles)
            {
                studentsToInsert.Add(new
                {
                    Id = p.StudentId,
                    StudentNumber = p.StudentNumber,
                    ApplicantId = p.ApplicantId,
                    FullName = $"{p.FirstName} {p.LastName}",
                    PreferredName = p.FirstName,
                    DateOfBirth = (DateOnly?)p.DateOfBirth,
                    PlaceOfBirth = p.PlaceOfBirth,
                    Gender = p.Gender,
                    Nationality = "Indonesian",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = p.Nik,
                    Address = p.Address,
                    Phone = p.Phone,
                    Email = p.Email,
                    IsIndependent = false,
                    CurrentStatus = p.Status,
                    EnrollmentDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Notes = (string?)null,
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertStudentSql, studentsToInsert, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 8. SEED ENROLLMENTS (students.enrollments)
            // =========================================================================
            var enrollmentsToInsert = new List<object>
            {
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = jonathanStudentId,
                    ApplicationId = jonathanAppId,
                    EnrollmentType = "initial",
                    EnrollmentDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    InitialCampus = _campusMain,
                    InitialLevel = "HSLC",
                    InitialAcademicYear = academicYearCode,
                    InitialProgram = "ACE High School Curriculum",
                    InitialCohort = "HSLC",
                    ApprovedBy = "demo-admission-officer",
                    EnrollmentPaymentConfirmed = (bool?)true,
                    PaymentReference = "PAY-ADM-0001",
                    PaymentWaived = false,
                    WaiverReference = (string?)null,
                    Notes = "Enrolled with full academic standing",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = graceStudentId,
                    ApplicationId = graceAppId,
                    EnrollmentType = "initial",
                    EnrollmentDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    InitialCampus = _campusMain,
                    InitialLevel = "JHLC",
                    InitialAcademicYear = academicYearCode,
                    InitialProgram = "ACE Junior High Curriculum",
                    InitialCohort = "JHLC",
                    ApprovedBy = "demo-admission-officer",
                    EnrollmentPaymentConfirmed = (bool?)true,
                    PaymentReference = "PAY-ADM-0002",
                    PaymentWaived = false,
                    WaiverReference = (string?)null,
                    Notes = "Enrolled with regular academic standing",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = budiStudentId,
                    ApplicationId = budiAppId,
                    EnrollmentType = "initial",
                    EnrollmentDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    InitialCampus = _campusMain,
                    InitialLevel = "ELC",
                    InitialAcademicYear = academicYearCode,
                    InitialProgram = "ACE Elementary Curriculum",
                    InitialCohort = "ELC",
                    ApprovedBy = "demo-admission-officer",
                    EnrollmentPaymentConfirmed = (bool?)true,
                    PaymentReference = "PAY-ADM-0003",
                    PaymentWaived = false,
                    WaiverReference = (string?)null,
                    Notes = "Enrolled into Elementary Learning Center",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                }
            };

            for (int i = 0; i < additionalProfiles.Count; i++)
            {
                var p = additionalProfiles[i];
                enrollmentsToInsert.Add(new
                {
                    Id = Guid.NewGuid(),
                    StudentId = p.StudentId,
                    ApplicationId = studentApplicationMap[p.StudentId],
                    EnrollmentType = "initial",
                    EnrollmentDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    InitialCampus = _campusMain,
                    InitialLevel = p.SchoolLevel,
                    InitialAcademicYear = academicYearCode,
                    InitialProgram = $"ACE {p.SchoolLevel} Curriculum",
                    InitialCohort = p.SchoolLevel,
                    ApprovedBy = "demo-admission-officer",
                    EnrollmentPaymentConfirmed = (bool?)true,
                    PaymentReference = $"PAY-ADM-{1004 + i:D4}",
                    PaymentWaived = false,
                    WaiverReference = (string?)null,
                    Notes = (string?)null,
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertEnrollmentSql, enrollmentsToInsert, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 9. SEED GUARDIANS (students.guardians)
            // =========================================================================
            var guardiansToInsert = new List<object>
            {
                // Hendra Pratama (Jonathan's father)
                new
                {
                    Id = hendraParentId,
                    FullName = "Hendra Pratama",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471010101750001",
                    Phone = "+628112345678",
                    Email = "hendra.pratama@parent.hagios.sch.id",
                    Address = "Jl. Malioboro No. 42, Yogyakarta 55111",
                    Occupation = "Entrepreneur",
                    Notes = "Father and primary emergency contact for Jonathan Pratama",
                    CreatedAt = now.AddDays(-190),
                    CreatedBy = _createdBy
                },
                // Grace's Guardian (Agus Santoso)
                new
                {
                    Id = graceGuardianId,
                    FullName = "Agus Santoso",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471010202760002",
                    Phone = "+628112345679",
                    Email = "agus.santoso@parent.hagios.sch.id",
                    Address = "Jl. Kaliurang Km 5 No. 12, Sleman 55281",
                    Occupation = "Management Consultant",
                    Notes = "Father and primary guardian for Grace Santoso",
                    CreatedAt = now.AddDays(-190),
                    CreatedBy = _createdBy
                },
                // Budi's Guardian (Bambang Wijaya)
                new
                {
                    Id = budiGuardianId,
                    FullName = "Bambang Wijaya",
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = "3471010303770003",
                    Phone = "+628112345680",
                    Email = "bambang.wijaya@parent.hagios.sch.id",
                    Address = "Jl. Gejayan No. 25, Yogyakarta 55222",
                    Occupation = "Civil Engineer",
                    Notes = "Father and primary guardian for Budi Wijaya",
                    CreatedAt = now.AddDays(-190),
                    CreatedBy = _createdBy
                }
            };

            foreach (var p in additionalProfiles)
            {
                guardiansToInsert.Add(new
                {
                    Id = p.GuardianId,
                    FullName = p.GuardianName,
                    GovernmentIdType = "NIK",
                    GovernmentIdNumber = p.GuardianNik,
                    Phone = p.GuardianPhone,
                    Email = p.GuardianEmail,
                    Address = p.Address,
                    Occupation = p.GuardianOccupation,
                    Notes = (string?)null,
                    CreatedAt = now.AddDays(-190),
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertGuardianSql, guardiansToInsert, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 10. SEED STUDENT-GUARDIAN RELATIONSHIPS (students.student_guardian_relationships)
            // =========================================================================
            var relationshipsToInsert = new List<object>
            {
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = jonathanStudentId,
                    GuardianId = hendraParentId,
                    RelationshipType = "Father",
                    IsPrimaryContact = true,
                    IsEmergencyContact = true, // emergency_contact = 1
                    IsFinancialContact = true,
                    IsAuthorizedPickup = true,
                    IsActive = true,
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = graceStudentId,
                    GuardianId = graceGuardianId,
                    RelationshipType = "Father",
                    IsPrimaryContact = true,
                    IsEmergencyContact = true, // emergency_contact = 1
                    IsFinancialContact = true,
                    IsAuthorizedPickup = true,
                    IsActive = true,
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = budiStudentId,
                    GuardianId = budiGuardianId,
                    RelationshipType = "Father",
                    IsPrimaryContact = true,
                    IsEmergencyContact = true, // emergency_contact = 1
                    IsFinancialContact = true,
                    IsAuthorizedPickup = true,
                    IsActive = true,
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                }
            };

            foreach (var p in additionalProfiles)
            {
                relationshipsToInsert.Add(new
                {
                    Id = Guid.NewGuid(),
                    StudentId = p.StudentId,
                    GuardianId = p.GuardianId,
                    RelationshipType = p.GuardianRelationship,
                    IsPrimaryContact = true,
                    IsEmergencyContact = true, // emergency_contact = 1
                    IsFinancialContact = true,
                    IsAuthorizedPickup = true,
                    IsActive = true,
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertRelationshipSql, relationshipsToInsert, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 11. SEED ADMINISTRATIVE PLACEMENTS (students.administrative_placements)
            // =========================================================================
            var placementsToInsert = new List<object>
            {
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = jonathanStudentId,
                    Campus = _campusMain,
                    SchoolLevel = "HSLC",
                    Grade = "Grade 10",
                    AcademicYear = academicYearCode,
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Program = "ACE High School Curriculum",
                    Stream = (string?)null,
                    Cohort = "HSLC",
                    Homeroom = "Room 301",
                    EndDate = (DateOnly?)null,
                    IsCurrent = true,
                    Reason = "Initial intake grade placement",
                    AuthorizedBy = "demo-academic-coordinator",
                    DecisionReference = "DEC-ADM-JONATHAN",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = graceStudentId,
                    Campus = _campusMain,
                    SchoolLevel = "JHLC",
                    Grade = "Grade 8",
                    AcademicYear = academicYearCode,
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Program = "ACE Junior High Curriculum",
                    Stream = (string?)null,
                    Cohort = "JHLC",
                    Homeroom = "Room 201",
                    EndDate = (DateOnly?)null,
                    IsCurrent = true,
                    Reason = "Initial intake grade placement",
                    AuthorizedBy = "demo-academic-coordinator",
                    DecisionReference = "DEC-ADM-GRACE",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = budiStudentId,
                    Campus = _campusMain,
                    SchoolLevel = "ELC",
                    Grade = "Grade 3",
                    AcademicYear = academicYearCode,
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Program = "ACE Elementary Curriculum",
                    Stream = (string?)null,
                    Cohort = "ELC",
                    Homeroom = "Room 103",
                    EndDate = (DateOnly?)null,
                    IsCurrent = true,
                    Reason = "Initial intake grade placement",
                    AuthorizedBy = "demo-academic-coordinator",
                    DecisionReference = "DEC-ADM-BUDI",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                }
            };

            foreach (var p in additionalProfiles)
            {
                placementsToInsert.Add(new
                {
                    Id = Guid.NewGuid(),
                    StudentId = p.StudentId,
                    Campus = _campusMain,
                    SchoolLevel = p.SchoolLevel,
                    Grade = p.Grade,
                    AcademicYear = academicYearCode,
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Program = $"ACE {p.SchoolLevel} Curriculum",
                    Stream = (string?)null,
                    Cohort = p.SchoolLevel,
                    Homeroom = p.Homeroom,
                    EndDate = (DateOnly?)null,
                    IsCurrent = true,
                    Reason = "Initial intake grade placement",
                    AuthorizedBy = "demo-academic-coordinator",
                    DecisionReference = $"DEC-ADM-{p.StudentNumber}",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertPlacementSql, placementsToInsert, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 12. SEED STUDENT STATUSES (students.student_statuses: active, 1 on_leave, 1 transferred)
            // =========================================================================
            var statusesToInsert = new List<object>
            {
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = jonathanStudentId,
                    Status = "active",
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    EndDate = (DateOnly?)null,
                    IsCurrent = true,
                    Reason = "Active academic enrollment",
                    AuthorizedBy = "demo-academic-coordinator",
                    DecisionReference = "DEC-ADM-JONATHAN",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = graceStudentId,
                    Status = "active",
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    EndDate = (DateOnly?)null,
                    IsCurrent = true,
                    Reason = "Active academic enrollment",
                    AuthorizedBy = "demo-academic-coordinator",
                    DecisionReference = "DEC-ADM-GRACE",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = budiStudentId,
                    Status = "active",
                    EffectiveDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    EndDate = (DateOnly?)null,
                    IsCurrent = true,
                    Reason = "Active academic enrollment",
                    AuthorizedBy = "demo-academic-coordinator",
                    DecisionReference = "DEC-ADM-BUDI",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                }
            };

            foreach (var p in additionalProfiles)
            {
                var effectiveDate = p.Status switch
                {
                    "on_leave" => DateOnly.FromDateTime(baseDate.AddDays(-30)),
                    "transferred" => DateOnly.FromDateTime(baseDate.AddDays(-20)),
                    _ => DateOnly.FromDateTime(baseDate.AddDays(-180))
                };

                var reason = p.Status switch
                {
                    "on_leave" => "Medical leave approved for Semester 2",
                    "transferred" => "Relocated with family to external institution",
                    _ => "Active academic enrollment"
                };

                statusesToInsert.Add(new
                {
                    Id = Guid.NewGuid(),
                    StudentId = p.StudentId,
                    Status = p.Status,
                    EffectiveDate = effectiveDate,
                    EndDate = (DateOnly?)null,
                    IsCurrent = true,
                    Reason = reason,
                    AuthorizedBy = "demo-academic-coordinator",
                    DecisionReference = $"STATUS-{p.StudentNumber}",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertStatusSql, statusesToInsert, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 13. SEED ADMINISTRATIVE HISTORY ENTRIES
            // =========================================================================
            var historyEntries = new List<object>
            {
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = jonathanStudentId,
                    EventType = "enrollment_completed",
                    EventDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Detail = "Officially enrolled with student number 261001 into Grade 10 (HSLC)",
                    AuthorizedBy = "demo-admission-officer",
                    DecisionReference = "DEC-ADM-JONATHAN",
                    IsCorrection = false,
                    CorrectsEntryId = (Guid?)null,
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = graceStudentId,
                    EventType = "enrollment_completed",
                    EventDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Detail = "Officially enrolled with student number 260801 into Grade 8 (JHLC)",
                    AuthorizedBy = "demo-admission-officer",
                    DecisionReference = "DEC-ADM-GRACE",
                    IsCorrection = false,
                    CorrectsEntryId = (Guid?)null,
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = budiStudentId,
                    EventType = "enrollment_completed",
                    EventDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Detail = "Officially enrolled with student number 260301 into Grade 3 (ELC)",
                    AuthorizedBy = "demo-admission-officer",
                    DecisionReference = "DEC-ADM-BUDI",
                    IsCorrection = false,
                    CorrectsEntryId = (Guid?)null,
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                }
            };

            foreach (var p in additionalProfiles)
            {
                historyEntries.Add(new
                {
                    Id = Guid.NewGuid(),
                    StudentId = p.StudentId,
                    EventType = "enrollment_completed",
                    EventDate = DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    Detail = $"Officially enrolled with student number {p.StudentNumber} into {p.Grade} ({p.SchoolLevel})",
                    AuthorizedBy = "demo-admission-officer",
                    DecisionReference = $"DEC-ADM-{p.StudentNumber}",
                    IsCorrection = false,
                    CorrectsEntryId = (Guid?)null,
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                });

                if (p.Status == "on_leave")
                {
                    historyEntries.Add(new
                    {
                        Id = Guid.NewGuid(),
                        StudentId = p.StudentId,
                        EventType = "status_change",
                        EventDate = DateOnly.FromDateTime(baseDate.AddDays(-30)),
                        Detail = "Student status changed from active to on_leave: Medical leave approved for Semester 2",
                        AuthorizedBy = "demo-academic-coordinator",
                        DecisionReference = $"LEAVE-{p.StudentNumber}",
                        IsCorrection = false,
                        CorrectsEntryId = (Guid?)null,
                        CreatedAt = now.AddDays(-30),
                        CreatedBy = _createdBy
                    });
                }
                else if (p.Status == "transferred")
                {
                    historyEntries.Add(new
                    {
                        Id = Guid.NewGuid(),
                        StudentId = p.StudentId,
                        EventType = "status_change",
                        EventDate = DateOnly.FromDateTime(baseDate.AddDays(-20)),
                        Detail = "Student status changed from active to transferred: Relocated with family to external institution",
                        AuthorizedBy = "demo-academic-coordinator",
                        DecisionReference = $"TRANS-{p.StudentNumber}",
                        IsCorrection = false,
                        CorrectsEntryId = (Guid?)null,
                        CreatedAt = now.AddDays(-20),
                        CreatedBy = _createdBy
                    });
                }
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertHistorySql, historyEntries, transaction: transaction, cancellationToken: cancellationToken));

            // =========================================================================
            // 14. SEED STUDENT DOCUMENTS (students.student_documents)
            // =========================================================================
            var studentDocs = new List<object>
            {
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = jonathanStudentId,
                    DocumentType = "Birth Certificate",
                    SubmissionStatus = "verified",
                    SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    VerifiedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    VerifiedBy = "demo-admission-officer",
                    FileReference = $"documents/students/{jonathanStudentId:N}/birth_certificate.pdf",
                    FileName = "birth_certificate_261001.pdf",
                    FileSizeBytes = (long?)256000,
                    ContentType = "application/pdf",
                    ValidUntil = (DateOnly?)null,
                    Notes = "Official certified civil registry document",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = graceStudentId,
                    DocumentType = "Birth Certificate",
                    SubmissionStatus = "verified",
                    SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    VerifiedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    VerifiedBy = "demo-admission-officer",
                    FileReference = $"documents/students/{graceStudentId:N}/birth_certificate.pdf",
                    FileName = "birth_certificate_260801.pdf",
                    FileSizeBytes = (long?)240000,
                    ContentType = "application/pdf",
                    ValidUntil = (DateOnly?)null,
                    Notes = "Official certified civil registry document",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                },
                new
                {
                    Id = Guid.NewGuid(),
                    StudentId = budiStudentId,
                    DocumentType = "Birth Certificate",
                    SubmissionStatus = "verified",
                    SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    VerifiedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    VerifiedBy = "demo-admission-officer",
                    FileReference = $"documents/students/{budiStudentId:N}/birth_certificate.pdf",
                    FileName = "birth_certificate_260301.pdf",
                    FileSizeBytes = (long?)235000,
                    ContentType = "application/pdf",
                    ValidUntil = (DateOnly?)null,
                    Notes = "Official certified civil registry document",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                }
            };

            foreach (var p in additionalProfiles)
            {
                studentDocs.Add(new
                {
                    Id = Guid.NewGuid(),
                    StudentId = p.StudentId,
                    DocumentType = "Birth Certificate",
                    SubmissionStatus = "verified",
                    SubmittedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-185)),
                    VerifiedDate = (DateOnly?)DateOnly.FromDateTime(baseDate.AddDays(-180)),
                    VerifiedBy = "demo-admission-officer",
                    FileReference = $"documents/students/{p.StudentId:N}/birth_certificate.pdf",
                    FileName = $"birth_certificate_{p.StudentNumber}.pdf",
                    FileSizeBytes = (long?)240000,
                    ContentType = "application/pdf",
                    ValidUntil = (DateOnly?)null,
                    Notes = "Official certified civil registry document",
                    CreatedAt = now.AddDays(-180),
                    CreatedBy = _createdBy
                });
            }

            await connection.ExecuteAsync(new CommandDefinition(
                _insertStudentDocSql, studentDocs, transaction: transaction, cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new StudentsSeedResult(
            EnrolledStudentIds: enrolledStudentIds.AsReadOnly(),
            CohortStudentIds: cohortDict,
            Personas: new FlagshipStudentPersonas(
                JonathanStudentId: jonathanStudentId,
                GraceStudentId: graceStudentId,
                BudiStudentId: budiStudentId,
                StagedApplicantIds: stagedApplicantIdsDict
            )
        );
    }

    private sealed record EnrolledProfile(
        Guid StudentId,
        Guid ApplicantId,
        Guid GuardianId,
        string StudentNumber,
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        string PlaceOfBirth,
        string Gender,
        string Nik,
        string Address,
        string Phone,
        string Email,
        string SchoolLevel,
        string Grade,
        string Homeroom,
        string Status,
        string GuardianName,
        string GuardianNik,
        string GuardianPhone,
        string GuardianEmail,
        string GuardianOccupation,
        string GuardianRelationship);

    private static List<EnrolledProfile> GetAdditionalStudentProfiles(DateTime baseDate)
    {
        // 45 students total across 3 cohorts:
        // ELC: 17 additional (grades 1-6) + Budi (Grade 3) = 18 total
        // JHLC: 14 additional (grades 7-9) + Grace (Grade 8) = 15 total
        // HSLC: 14 additional (grades 10-12) + Jonathan (Grade 10) = 15 total
        var firstNames = new[]
        {
            "Kevin", "Dimas", "Arya", "Farhan", "Rizky", "Aditya", "Yoga", "Bagus", "Fajar", "Hendra",
            "Satria", "Bayu", "Gilang", "Ilham", "Wahyu", "Indra", "Agung", "Dwi", "Eko", "Tri",
            "Taufik", "Rian", "Putri", "Siti", "Ayu", "Dewi", "Ratna", "Maya", "Sari", "Indah",
            "Wulandari", "Rina", "Nita", "Tari", "Lestari", "Dian", "Fitri", "Gita", "Mega", "Tiara",
            "Melati", "Jessica", "Amanda", "Stefani", "Michelle"
        };

        var lastNames = new[]
        {
            "Kurniawan", "Setiawan", "Nugroho", "Kusuma", "Pratama", "Halim", "Sitorus", "Simanjuntak", "Hartono", "Gunawan",
            "Siregar", "Tanuwidjaja", "Wibowo", "Hidayat", "Susanto", "Saputra", "Utomo", "Raharjo", "Subagyo", "Prasetyo",
            "Surya", "Wardhana", "Sinaga", "Pasaribu", "Kurniawan", "Santoso", "Wijaya", "Halim", "Kusuma", "Sitorus",
            "Simanjuntak", "Setiawan", "Nugroho", "Hartono", "Gunawan", "Siregar", "Hidayat", "Tanuwidjaja", "Wibowo", "Susanto",
            "Saputra", "Utomo", "Raharjo", "Subagyo", "Prasetyo"
        };

        var occupations = new[]
        {
            "Civil Servant (PNS)", "Private Sector Employee", "Software Engineer", "Accountant", "Physician",
            "University Lecturer", "Architect", "Bank Manager", "Business Owner", "Pharmacist",
            "Dentist", "Veterinarian", "Legal Counsel", "Graphic Designer", "Civil Engineer"
        };

        var profiles = new List<EnrolledProfile>(45);

        for (int i = 0; i < 45; i++)
        {
            string level;
            string grade;
            string homeroom;
            int gradeNum;

            if (i < 17)
            {
                level = "ELC";
                // Distribute across grades 1-6 (Budi is Grade 3)
                gradeNum = (i % 6) + 1;
                grade = $"Grade {gradeNum}";
                homeroom = $"Room 10{gradeNum}";
            }
            else if (i < 31) // 17 + 14 = 31
            {
                level = "JHLC";
                // Distribute across grades 7-9 (Grace is Grade 8)
                gradeNum = 7 + ((i - 17) % 3);
                grade = $"Grade {gradeNum}";
                homeroom = $"Room 20{gradeNum - 6}";
            }
            else
            {
                level = "HSLC";
                // Distribute across grades 10-12 (Jonathan is Grade 10)
                gradeNum = 10 + ((i - 31) % 3);
                grade = $"Grade {gradeNum}";
                homeroom = $"Room 30{gradeNum - 9}";
            }

            var studentId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();
            var guardianId = Guid.NewGuid();

            var fn = firstNames[i];
            var ln = lastNames[i];
            var gender = i >= 22 ? "Female" : "Male";
            var birthYear = baseDate.Year - (5 + gradeNum);
            var dob = new DateOnly(birthYear, ((i * 3) % 12) + 1, ((i * 7) % 28) + 1);

            // Exactly 1 on_leave and 1 transferred student
            var status = i == 20 ? "on_leave" : (i == 35 ? "transferred" : "active");

            var studentNum = $"26{gradeNum:D2}{((i % 5) + 2):D2}";
            var nik = $"34710{i + 10:D2}{dob:ddMMyy}{i:D4}";
            var phone = $"+62812{10000000 + i:D8}";
            var email = $"{fn.ToLowerInvariant()}.{ln.ToLowerInvariant()}{i + 1}@student.hagios.sch.id";

            var gfn = gender == "Male" ? $"Bapak {ln}" : $"Ibu {ln}";
            var gnik = $"34710{i + 50:D2}{dob.AddYears(-25):ddMMyy}{i:D4}";
            var gphone = $"+62811{20000000 + i:D8}";
            var gemail = $"{fn.ToLowerInvariant()}.guardian{i + 1}@parent.hagios.sch.id";
            var occ = occupations[i % occupations.Length];
            var rel = gender == "Male" ? "Father" : "Mother";

            profiles.Add(new EnrolledProfile(
                StudentId: studentId,
                ApplicantId: applicantId,
                GuardianId: guardianId,
                StudentNumber: studentNum,
                FirstName: fn,
                LastName: ln,
                DateOfBirth: dob,
                PlaceOfBirth: i % 2 == 0 ? "Yogyakarta" : "Sleman",
                Gender: gender,
                Nik: nik,
                Address: $"Jl. Kaliurang Km {((i % 15) + 1)} No. {i + 10}, Yogyakarta",
                Phone: phone,
                Email: email,
                SchoolLevel: level,
                Grade: grade,
                Homeroom: homeroom,
                Status: status,
                GuardianName: gfn,
                GuardianNik: gnik,
                GuardianPhone: gphone,
                GuardianEmail: gemail,
                GuardianOccupation: occ,
                GuardianRelationship: rel
            ));
        }

        return profiles;
    }
}
