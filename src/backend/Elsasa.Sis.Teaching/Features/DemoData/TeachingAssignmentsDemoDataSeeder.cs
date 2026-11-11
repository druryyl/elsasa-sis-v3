namespace Elsasa.Sis.Teaching.Features.DemoData;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous demo data seeder for Stage 2 Teaching domain (demands, evaluations, assignments,
/// homerooms, availability, commitments, exceptions, substitutions, reassignments, completions, history)
/// binding real Academics Learning Units and Student Cohorts to Hagios faculty personas (TD-08, TD-09, P5-S16).
/// </summary>
public sealed class TeachingAssignmentsDemoDataSeeder(
    ISqlConnectionFactory connectionFactory) : ITeachingAssignmentsDemoDataSeeder
{
    private const string _createdBy = "demo-data-seeder";
    private const string _campusMain = "Kampus Utama";
    private const string _kepalaSekolah = "Kepala Sekolah";
    private const string _coordinator = "Teaching Coordinator";

    // ─────────────────────────────────────────────────────────────────────────
    // Deterministic Assignment Roles & Workload Policies GUIDs
    // ─────────────────────────────────────────────────────────────────────────
    private static readonly Guid _roleGuruUtamaId = new("00000000-0000-0000-0007-000000000101");
    private static readonly Guid _roleGuruPendampingId = new("00000000-0000-0000-0007-000000000102");
    private static readonly Guid _roleWaliKelasId = new("00000000-0000-0000-0007-000000000103");
    private static readonly Guid _roleGuruPenggantiId = new("00000000-0000-0000-0007-000000000104");
    private static readonly Guid _roleDosenTamuId = new("00000000-0000-0000-0007-000000000105");
    private static readonly Guid _roleAsistenGuruId = new("00000000-0000-0000-0007-000000000106");
    private static readonly Guid _rolePenilaiId = new("00000000-0000-0000-0007-000000000107");

    private static readonly Guid _workloadPolicyId = new("00000000-0000-0000-0007-000000000201");

    // Deterministic Cohort GUID
    private static readonly Guid _cohort4AGuid = new("00000000-0000-0000-0007-000000000301");

    // Deterministic Demands GUIDs
    private static readonly Guid _demandBudiMathId = new("00000000-0000-0000-0007-000000000401");
    private static readonly Guid _demandBudiHomeroomId = new("00000000-0000-0000-0007-000000000402");
    private static readonly Guid _demandSitiEngId = new("00000000-0000-0000-0007-000000000403");
    private static readonly Guid _demandJohnImmersionId = new("00000000-0000-0000-0007-000000000404");
    private static readonly Guid _demandHendraPendingId = new("00000000-0000-0000-0007-000000000405");
    private static readonly Guid _demandHistoricalConcludedId = new("00000000-0000-0000-0007-000000000406");

    // Deterministic Assignments GUIDs
    private static readonly Guid _assignmentBudiMathId = new("00000000-0000-0000-0007-000000000501");
    private static readonly Guid _assignmentBudiHomeroomId = new("00000000-0000-0000-0007-000000000502");
    private static readonly Guid _assignmentSitiEngId = new("00000000-0000-0000-0007-000000000503");
    private static readonly Guid _assignmentJohnImmersionId = new("00000000-0000-0000-0007-000000000504");
    private static readonly Guid _assignmentHistoricalConcludedId = new("00000000-0000-0000-0007-000000000505");
    private static readonly Guid _assignmentBudiHandoverReplacedId = new("00000000-0000-0000-0007-000000000506");

    // Deterministic Operations & Workflow GUIDs
    private static readonly Guid _subBudiMedicalId = new("00000000-0000-0000-0007-000000000601");
    private static readonly Guid _wexJohnMillerId = new("00000000-0000-0000-0007-000000000602");
    private static readonly Guid _reaBudiToSitiId = new("00000000-0000-0000-0007-000000000603");
    private static readonly Guid _acmpHistoricalId = new("00000000-0000-0000-0007-000000000604");

    /// <inheritdoc/>
    public async Task<TeachingAssignmentsSeedResult> SeedTeachingAssignmentsAsync(
        OperationsSeedResult operationsContext,
        AcademicsStructureSeedResult academicsContext,
        StudentsSeedResult studentsContext,
        TeachingSeedResult teachingContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationsContext);
        ArgumentNullException.ThrowIfNull(academicsContext);
        ArgumentNullException.ThrowIfNull(studentsContext);
        ArgumentNullException.ThrowIfNull(teachingContext);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var baseDate = operationsContext.BaseDate.Date;
        var now = DateTimeOffset.UtcNow;
        var activePeriodId = operationsContext.ActivePeriodId;
        var historicalPeriodId = operationsContext.HistoricalPeriodId;
        var academicYearId = operationsContext.AcademicYearId;

        // Resolve Learning Units from Academics Structure or Database fallback
        var mathUnitId = academicsContext.SubjectUnitIds.TryGetValue("MATH", out var mId) ? mId : await ResolveLearningUnitIdAsync(connection, "MATH", cancellationToken);
        var engUnitId = academicsContext.SubjectUnitIds.TryGetValue("ENG", out var eId) ? eId : await ResolveLearningUnitIdAsync(connection, "ENG", cancellationToken);
        var sciUnitId = academicsContext.SubjectUnitIds.TryGetValue("SCI", out var sId) ? sId : await ResolveLearningUnitIdAsync(connection, "SCI", cancellationToken);

        // 1. Seed Assignment Roles Catalog
        await SeedAssignmentRolesAsync(connection, now, cancellationToken);

        // 2. Seed Standard Workload Policy
        await SeedWorkloadPolicyAsync(connection, academicYearId, baseDate, now, cancellationToken);

        // 3. Seed Persona 1: Budi Hartono (Math SD 4A 5 hrs + Homeroom Advisor 2 hrs + Medical Leave + Commitments)
        await SeedBudiHartonoStage2Async(
            connection,
            baseDate,
            now,
            activePeriodId,
            historicalPeriodId,
            mathUnitId,
            cancellationToken);

        // 4. Seed Persona 2: Siti Nurhaliza (English SMP Co-Teacher 4 hrs + Emergency Substitute for Budi 5 hrs)
        await SeedSitiNurhalizaStage2Async(
            connection,
            baseDate,
            now,
            activePeriodId,
            engUnitId,
            cancellationToken);

        // 5. Seed Persona 3: John Miller (Visiting Lecturer 30 hrs + Approved Workload Exception +3 hrs)
        await SeedJohnMillerStage2Async(
            connection,
            baseDate,
            now,
            activePeriodId,
            engUnitId,
            cancellationToken);

        // 6. Seed Persona 4: Hendra Setiawan (Ineligible Candidate Evaluation Demo)
        await SeedHendraSetiawanStage2Async(
            connection,
            baseDate,
            now,
            activePeriodId,
            mathUnitId,
            cancellationToken);

        // 7. Seed Permanent Reassignment & Handover Example (Budi to Siti for Science SD)
        await SeedPermanentReassignmentExampleAsync(
            connection,
            baseDate,
            now,
            activePeriodId,
            sciUnitId,
            cancellationToken);

        // 8. Aggregate Counts for summary result
        var demandsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teaching_demands;", cancellationToken: cancellationToken));
        var evaluationsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teacher_eligibility_evaluations;", cancellationToken: cancellationToken));
        var assignmentsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teaching_assignments;", cancellationToken: cancellationToken));
        var conflictsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.capacity_conflicts;", cancellationToken: cancellationToken));
        var availabilitiesCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teacher_availabilities;", cancellationToken: cancellationToken));
        var unavailabilityCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teacher_unavailability_windows;", cancellationToken: cancellationToken));
        var commitmentsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.workload_commitments;", cancellationToken: cancellationToken));
        var exceptionsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.workload_exceptions;", cancellationToken: cancellationToken));
        var substitutionsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teaching_substitutions;", cancellationToken: cancellationToken));
        var reassignmentsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teaching_reassignments;", cancellationToken: cancellationToken));
        var completionsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.assignment_completions;", cancellationToken: cancellationToken));
        var historyCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teaching_history_entries;", cancellationToken: cancellationToken));

        return new TeachingAssignmentsSeedResult(
            DemandsCreated: demandsCount,
            EvaluationsCreated: evaluationsCount,
            AssignmentsCreated: assignmentsCount,
            ConflictsCreated: conflictsCount,
            AvailabilitiesCreated: availabilitiesCount,
            UnavailabilityWindowsCreated: unavailabilityCount,
            WorkloadCommitmentsCreated: commitmentsCount,
            WorkloadExceptionsCreated: exceptionsCount,
            SubstitutionsCreated: substitutionsCount,
            ReassignmentsCreated: reassignmentsCount,
            CompletionsCreated: completionsCount,
            HistoryEntriesCreated: historyCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: Query Learning Unit ID fallback
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task<Guid> ResolveLearningUnitIdAsync(
        System.Data.Common.DbConnection connection,
        string code,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT TOP (1) id FROM academics.learning_units WHERE code = @Code;";
        var id = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql, new { Code = code }, cancellationToken: cancellationToken));
        return id ?? Guid.NewGuid();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Step 1: Assignment Roles Catalog
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedAssignmentRolesAsync(
        System.Data.Common.DbConnection connection,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var roles = new (Guid Id, string Code, string Name, string Description, bool IsLead, bool Counts)[]
        {
            (_roleGuruUtamaId, "GURU_UTAMA", "Guru Utama", "Guru pemegang otoritas instruksional utama mata pelajaran", true, true),
            (_roleGuruPendampingId, "GURU_PENDAMPING", "Guru Pendamping (Co-Teacher)", "Guru pendamping instruksional atau co-teacher dalam kelas", false, true),
            (_roleWaliKelasId, "WALI_KELAS", "Wali Kelas (Homeroom Advisor)", "Pembimbing akademik, administrasi kesiswaan, dan pastoral kelompok siswa", true, true),
            (_roleGuruPenggantiId, "GURU_PENGGANTI", "Guru Pengganti (Substitute)", "Guru pengganti darurat untuk jadwal tertentu", false, true),
            (_roleDosenTamuId, "DOSEN_TAMU", "Dosen Tamu (Visiting Faculty)", "Instruktur ahli tamu untuk program dan lokakarya khusus", true, true),
            (_roleAsistenGuruId, "ASISTEN_GURU", "Asisten Guru", "Asisten pembelajaran yang tidak memegang tanggung jawab beban ajar utama", false, false),
            (_rolePenilaiId, "PENILAI", "Penilai / Asesor", "Penguji atau asesor portofolio ujian independen", false, false)
        };

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.assignment_roles WHERE id = @Id OR code = @Code)
            BEGIN
                INSERT INTO teaching.assignment_roles (
                    id, code, name, description, is_lead_role, counts_toward_workload, is_active, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, @Description, @IsLead, @Counts, 1, @CreatedAt, @CreatedBy
                );
            END;
            """;

        foreach (var r in roles)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    r.Id,
                    r.Code,
                    r.Name,
                    r.Description,
                    r.IsLead,
                    r.Counts,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                cancellationToken: cancellationToken));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Step 2: Workload Policy
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedWorkloadPolicyAsync(
        System.Data.Common.DbConnection connection,
        Guid academicYearId,
        DateTime baseDate,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.workload_policies WHERE id = @Id OR code = @Code)
            BEGIN
                INSERT INTO teaching.workload_policies (
                    id, code, name, academic_year_id, version, workload_unit,
                    max_standard_capacity, warning_threshold, prohibited_threshold,
                    includes_assessment_effort, includes_advising_effort, includes_preparation_time,
                    calculation_rules_summary, effective_start_date, effective_end_date,
                    status, approved_by, approved_date, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, @AcademicYearId, '2026.1', 'Teaching Hours',
                    24.00, 20.00, 28.00,
                    1, 1, 0,
                    'Standar beban kerja Hagios: Maksimum standar 24 jam/minggu, batas peringatan 20 jam, batas absolut 28 jam. Mengakomodasi beban penilaian dan pembimbingan wali kelas.',
                    @EffectiveStart, NULL,
                    'active', @ApprovedBy, @ApprovedDate, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = _workloadPolicyId,
                Code = "WPOL-2026-STD",
                Name = "Kebijakan Standar Beban Kerja Pendidik Hagios 2026/2027",
                AcademicYearId = academicYearId,
                EffectiveStart = baseDate.AddMonths(-6),
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = baseDate.AddMonths(-6),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Persona 1: Budi Hartono Stage 2 Seeding
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedBudiHartonoStage2Async(
        System.Data.Common.DbConnection connection,
        DateTime baseDate,
        DateTimeOffset now,
        Guid activePeriodId,
        Guid historicalPeriodId,
        Guid mathUnitId,
        CancellationToken cancellationToken)
    {
        var budiId = DemoDataConstants.BudiHartonoTeacherId;
        var startTerm = baseDate.AddMonths(-1);
        var endTerm = baseDate.AddMonths(5);

        // 1. Demand 1: Math SD 4A (5 hrs)
        const string insertDemandSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_demands WHERE id = @Id OR demand_code = @DemandCode)
            BEGIN
                INSERT INTO teaching.teaching_demands (
                    id, demand_code, source_domain, source_work_reference, source_work_label,
                    academic_period_id, required_capability_code, required_assignment_role_id,
                    expected_workload_hours, workload_unit, requires_assessment_authority,
                    requested_start_date, requested_end_date, campus_scope, required_teachers_count,
                    status, requested_by, requested_at, created_at, created_by
                ) VALUES (
                    @Id, @DemandCode, @SourceDomain, @SourceWorkReference, @SourceWorkLabel,
                    @AcademicPeriodId, @RequiredCapabilityCode, @RequiredAssignmentRoleId,
                    @ExpectedHours, 'Teaching Hours', 1,
                    @StartDate, @EndDate, @CampusScope, 1,
                    @Status, @RequestedBy, @RequestedAt, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertDemandSql,
            new
            {
                Id = _demandBudiMathId,
                DemandCode = "DEM-2026-0001",
                SourceDomain = "Academics",
                SourceWorkReference = mathUnitId,
                SourceWorkLabel = "Matematika SD Kelas 4",
                AcademicPeriodId = activePeriodId,
                RequiredCapabilityCode = "MATH_PRIMARY",
                RequiredAssignmentRoleId = _roleGuruUtamaId,
                ExpectedHours = 5.00m,
                StartDate = startTerm,
                EndDate = endTerm,
                CampusScope = _campusMain,
                Status = "staffed",
                RequestedBy = _coordinator,
                RequestedAt = now.AddDays(-40),
                CreatedAt = now.AddDays(-40),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 2. Evaluation: Budi for Math SD 4A (Eligible)
        const string insertEvalSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teacher_eligibility_evaluations WHERE teaching_demand_id = @DemandId AND teacher_profile_id = @TeacherId)
            BEGIN
                INSERT INTO teaching.teacher_eligibility_evaluations (
                    id, teaching_demand_id, teacher_profile_id, evaluation_date, evaluated_by,
                    is_eligible, eligibility_status, profile_active_check, authorization_check_passed,
                    qualification_check_passed, availability_check_passed, capacity_check_passed,
                    explanation, notes, created_at, created_by
                ) VALUES (
                    NEWID(), @DemandId, @TeacherId, @EvaluationDate, @EvaluatedBy,
                    @IsEligible, @EligibilityStatus, @StandingCheck, @AuthCheck,
                    @QualCheck, @AvailCheck, @CapCheck,
                    @Explanation, @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertEvalSql,
            new
            {
                DemandId = _demandBudiMathId,
                TeacherId = budiId,
                EvaluationDate = startTerm.AddDays(-5),
                EvaluatedBy = _coordinator,
                IsEligible = true,
                EligibilityStatus = "eligible",
                StandingCheck = true,
                AuthCheck = true,
                QualCheck = true,
                AvailCheck = true,
                CapCheck = true,
                Explanation = "Kandidat Budi Hartono memenuhi seluruh kualifikasi (Serdik verified), memiliki otorisasi aktif untuk MATH_PRIMARY, ketersediaan jadwal mencukupi, dan total beban kerja berada dalam batas aman.",
                Notes = "Rekomendasi guru utama kelas 4A.",
                CreatedAt = now.AddDays(-35),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 3. Assignment 1: Budi Math SD 4A (5 hrs, approved)
        const string insertAsnSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_assignments WHERE id = @Id OR assignment_number = @AssignmentNumber)
            BEGIN
                INSERT INTO teaching.teaching_assignments (
                    id, assignment_number, teaching_demand_id, teacher_profile_id, assignment_role_id,
                    assignment_scope, scope_reference_id, scope_label, academic_period_id,
                    effective_start_date, effective_end_date, workload_commitment_value, workload_unit,
                    status, approved_by, approved_date, teacher_acknowledged, teacher_acknowledged_date,
                    notes, created_at, created_by
                ) VALUES (
                    @Id, @AssignmentNumber, @TeachingDemandId, @TeacherProfileId, @AssignmentRoleId,
                    @AssignmentScope, @ScopeReferenceId, @ScopeLabel, @AcademicPeriodId,
                    @EffectiveStartDate, @EffectiveEndDate, @WorkloadHours, 'Teaching Hours',
                    @Status, @ApprovedBy, @ApprovedDate, @TeacherAcknowledged, @TeacherAcknowledgedDate,
                    @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAsnSql,
            new
            {
                Id = _assignmentBudiMathId,
                AssignmentNumber = "ASN-2026-0001",
                TeachingDemandId = _demandBudiMathId,
                TeacherProfileId = budiId,
                AssignmentRoleId = _roleGuruUtamaId,
                AssignmentScope = "learning_unit",
                ScopeReferenceId = mathUnitId,
                ScopeLabel = "Matematika SD Kelas 4A",
                AcademicPeriodId = activePeriodId,
                EffectiveStartDate = startTerm,
                EffectiveEndDate = endTerm,
                WorkloadHours = 5.00m,
                Status = "approved",
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = startTerm,
                TeacherAcknowledged = true,
                TeacherAcknowledgedDate = startTerm,
                Notes = "Penugasan guru utama pengajaran Matematika kurikulum ACE.",
                CreatedAt = now.AddDays(-30),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 4. Demand 2: Homeroom Advisor Kelas 4A (2 hrs)
        await connection.ExecuteAsync(new CommandDefinition(
            insertDemandSql,
            new
            {
                Id = _demandBudiHomeroomId,
                DemandCode = "DEM-2026-0002",
                SourceDomain = "Students",
                SourceWorkReference = _cohort4AGuid,
                SourceWorkLabel = "Kelas 4A (ELC Room 104)",
                AcademicPeriodId = activePeriodId,
                RequiredCapabilityCode = "MATH_PRIMARY",
                RequiredAssignmentRoleId = _roleWaliKelasId,
                ExpectedHours = 2.00m,
                StartDate = startTerm,
                EndDate = endTerm,
                CampusScope = _campusMain,
                Status = "staffed",
                RequestedBy = _coordinator,
                RequestedAt = now.AddDays(-40),
                CreatedAt = now.AddDays(-40),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 5. Assignment 2: Budi Homeroom Advisor Kelas 4A (2 hrs, approved)
        await connection.ExecuteAsync(new CommandDefinition(
            insertAsnSql,
            new
            {
                Id = _assignmentBudiHomeroomId,
                AssignmentNumber = "ASN-2026-0002",
                TeachingDemandId = _demandBudiHomeroomId,
                TeacherProfileId = budiId,
                AssignmentRoleId = _roleWaliKelasId,
                AssignmentScope = "cohort_group",
                ScopeReferenceId = _cohort4AGuid,
                ScopeLabel = "Wali Kelas 4A (ELC Room 104)",
                AcademicPeriodId = activePeriodId,
                EffectiveStartDate = startTerm,
                EffectiveEndDate = endTerm,
                WorkloadHours = 2.00m,
                Status = "approved",
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = startTerm,
                TeacherAcknowledged = true,
                TeacherAcknowledgedDate = startTerm,
                Notes = "Pembimbing akademik dan pastoral siswa kelas 4A.",
                CreatedAt = now.AddDays(-30),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 6. Medical Leave Unavailability Window for Budi
        const string insertUnavSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teacher_unavailability_windows WHERE teacher_profile_id = @TeacherId AND start_date = @StartDate)
            BEGIN
                INSERT INTO teaching.teacher_unavailability_windows (
                    id, teacher_profile_id, start_date, end_date, start_time, end_time,
                    unavailability_type, external_source_reference, reason, status, notes,
                    created_at, created_by
                ) VALUES (
                    NEWID(), @TeacherId, @StartDate, @EndDate, NULL, NULL,
                    @UnavType, @ExtRef, @Reason, 'active', @Notes,
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertUnavSql,
            new
            {
                TeacherId = budiId,
                StartDate = baseDate.AddDays(7),
                EndDate = baseDate.AddDays(14),
                UnavType = "medical_leave",
                ExtRef = "MED-LEAVE-2026-081",
                Reason = "Izin medis rawat inap dan pemulihan pasca operasi",
                Notes = "Surat keterangan dokter RS St. Carolus terlampir.",
                CreatedAt = now.AddDays(-2),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 7. Weekly Availability for Budi
        const string insertAvailSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teacher_availabilities WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId AND status = 'active')
            BEGIN
                INSERT INTO teaching.teacher_availabilities (
                    id, teacher_profile_id, academic_period_id, max_available_hours, campus_scope,
                    available_delivery_modes, recurring_available_schedule, effective_start_date,
                    effective_end_date, status, notes, created_at, created_by
                ) VALUES (
                    NEWID(), @TeacherId, @PeriodId, @MaxHours, @CampusScope,
                    'face_to_face', @ScheduleJson, @StartDate,
                    @EndDate, 'active', @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAvailSql,
            new
            {
                TeacherId = budiId,
                PeriodId = activePeriodId,
                MaxHours = 24.00m,
                CampusScope = _campusMain,
                ScheduleJson = "{\"Monday\":[\"07:30-15:00\"],\"Tuesday\":[\"07:30-15:00\"],\"Wednesday\":[\"07:30-15:00\"],\"Thursday\":[\"07:30-15:00\"],\"Friday\":[\"07:30-13:30\"]}",
                StartDate = startTerm,
                EndDate = endTerm,
                Notes = "Jadwal mengajar tetap hari kerja reguler.",
                CreatedAt = now.AddDays(-30),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 8. Workload Commitment for Budi (7 hrs total: 5 Math + 2 Homeroom)
        const string insertCommitmentSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.workload_commitments WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId)
            BEGIN
                INSERT INTO teaching.workload_commitments (
                    id, teacher_profile_id, academic_period_id, workload_policy_id,
                    planned_workload_hours, committed_workload_hours, max_capacity_limit_hours,
                    remaining_capacity_hours, status, active_assignment_count, approved_exception_count,
                    as_of_timestamp, created_at, created_by
                ) VALUES (
                    NEWID(), @TeacherId, @PeriodId, @PolicyId,
                    0.00, @CommittedHours, 24.00,
                    @RemainingHours, @Status, @ActiveCount, @ExceptionCount,
                    SYSUTCDATETIME(), @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertCommitmentSql,
            new
            {
                TeacherId = budiId,
                PeriodId = activePeriodId,
                PolicyId = _workloadPolicyId,
                CommittedHours = 7.00m,
                RemainingHours = 17.00m,
                Status = "normal",
                ActiveCount = 2,
                ExceptionCount = 0,
                CreatedAt = now.AddDays(-30),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 9. Historical Concluded Assignment example for Budi (Capacity Released)
        await connection.ExecuteAsync(new CommandDefinition(
            insertDemandSql,
            new
            {
                Id = _demandHistoricalConcludedId,
                DemandCode = "DEM-2025-0099",
                SourceDomain = "Academics",
                SourceWorkReference = mathUnitId,
                SourceWorkLabel = "Matematika SD Semester Lampau",
                AcademicPeriodId = historicalPeriodId,
                RequiredCapabilityCode = "MATH_PRIMARY",
                RequiredAssignmentRoleId = _roleGuruUtamaId,
                ExpectedHours = 5.00m,
                StartDate = baseDate.AddMonths(-8),
                EndDate = baseDate.AddMonths(-2),
                CampusScope = _campusMain,
                Status = "staffed",
                RequestedBy = _coordinator,
                RequestedAt = now.AddDays(-200),
                CreatedAt = now.AddDays(-200),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            insertAsnSql,
            new
            {
                Id = _assignmentHistoricalConcludedId,
                AssignmentNumber = "ASN-2025-0099",
                TeachingDemandId = _demandHistoricalConcludedId,
                TeacherProfileId = budiId,
                AssignmentRoleId = _roleGuruUtamaId,
                AssignmentScope = "learning_unit",
                ScopeReferenceId = mathUnitId,
                ScopeLabel = "Matematika SD Kelas 4 (Semester Selesai)",
                AcademicPeriodId = historicalPeriodId,
                EffectiveStartDate = baseDate.AddMonths(-8),
                EffectiveEndDate = baseDate.AddMonths(-2),
                WorkloadHours = 5.00m,
                Status = "completed",
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = baseDate.AddMonths(-8),
                TeacherAcknowledged = true,
                TeacherAcknowledgedDate = baseDate.AddMonths(-8),
                Notes = "Penugasan tahun ajaran lalu yang telah selesai secara administratif.",
                CreatedAt = now.AddDays(-200),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 10. Conclude historical assignment
        const string insertCompletionSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.assignment_completions WHERE id = @Id OR teaching_assignment_id = @AssignmentId)
            BEGIN
                INSERT INTO teaching.assignment_completions (
                    id, teaching_assignment_id, completion_date, academic_confirmation_received,
                    academic_confirmation_source, handover_confirmed, unresolved_items_status,
                    unresolved_items_description, capacity_released_date, completed_by, status,
                    notes, created_at, created_by
                ) VALUES (
                    @Id, @AssignmentId, @CompletionDate, 1,
                    'Dean of Academics - Penyerahan Nilai & Rapor Lengkap', 1, 'none',
                    NULL, @ReleasedDate, @CompletedBy, 'completed',
                    'Seluruh target kurikulum dan pelaporan nilai tuntas; kapasitas beban ajar resmi dilepaskan kembali.',
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertCompletionSql,
            new
            {
                Id = _acmpHistoricalId,
                AssignmentId = _assignmentHistoricalConcludedId,
                CompletionDate = baseDate.AddMonths(-2),
                ReleasedDate = baseDate.AddMonths(-2),
                CompletedBy = _coordinator,
                CreatedAt = now.AddDays(-60),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 11. Teaching History Entries for Budi
        await RecordHistoryEntryAsync(connection, budiId, "AssignmentApproved", "teaching_assignments", _assignmentBudiMathId, startTerm, "KepalaSekolah", _kepalaSekolah, "Penugasan mengajar Matematika SD Kelas 4A (ASN-2026-0001) disetujui", now.AddDays(-30), cancellationToken);
        await RecordHistoryEntryAsync(connection, budiId, "AssignmentApproved", "teaching_assignments", _assignmentBudiHomeroomId, startTerm, "KepalaSekolah", _kepalaSekolah, "Penugasan wali kelas 4A (ASN-2026-0002) disetujui", now.AddDays(-30), cancellationToken);
        await RecordHistoryEntryAsync(connection, budiId, "AssignmentCompleted", "teaching_assignments", _assignmentHistoricalConcludedId, baseDate.AddMonths(-2), "Coordinator", _coordinator, "Penugasan semester lalu ASN-2025-0099 telah selesai dan kapasitas dilepaskan", now.AddDays(-60), cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Persona 2: Siti Nurhaliza Stage 2 Seeding
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedSitiNurhalizaStage2Async(
        System.Data.Common.DbConnection connection,
        DateTime baseDate,
        DateTimeOffset now,
        Guid activePeriodId,
        Guid engUnitId,
        CancellationToken cancellationToken)
    {
        var sitiId = DemoDataConstants.SitiNurhalizaTeacherId;
        var startTerm = baseDate.AddMonths(-1);
        var endTerm = baseDate.AddMonths(5);

        // 1. Demand 3: English SMP Co-Teacher (4 hrs)
        const string insertDemandSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_demands WHERE id = @Id OR demand_code = @DemandCode)
            BEGIN
                INSERT INTO teaching.teaching_demands (
                    id, demand_code, source_domain, source_work_reference, source_work_label,
                    academic_period_id, required_capability_code, required_assignment_role_id,
                    expected_workload_hours, workload_unit, requires_assessment_authority,
                    requested_start_date, requested_end_date, campus_scope, required_teachers_count,
                    status, requested_by, requested_at, created_at, created_by
                ) VALUES (
                    @Id, @DemandCode, 'Academics', @SourceWorkReference, 'Bahasa Inggris SMP Kelas 7',
                    @AcademicPeriodId, 'ENG_JUNIOR', @RequiredAssignmentRoleId,
                    4.00, 'Teaching Hours', 0,
                    @StartDate, @EndDate, @CampusScope, 1,
                    'staffed', @RequestedBy, @RequestedAt, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertDemandSql,
            new
            {
                Id = _demandSitiEngId,
                DemandCode = "DEM-2026-0003",
                SourceWorkReference = engUnitId,
                AcademicPeriodId = activePeriodId,
                RequiredAssignmentRoleId = _roleGuruPendampingId,
                StartDate = startTerm,
                EndDate = endTerm,
                CampusScope = _campusMain,
                RequestedBy = _coordinator,
                RequestedAt = now.AddDays(-35),
                CreatedAt = now.AddDays(-35),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 2. Assignment 3: Siti English SMP Co-Teacher (4 hrs, approved)
        const string insertAsnSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_assignments WHERE id = @Id OR assignment_number = @AssignmentNumber)
            BEGIN
                INSERT INTO teaching.teaching_assignments (
                    id, assignment_number, teaching_demand_id, teacher_profile_id, assignment_role_id,
                    assignment_scope, scope_reference_id, scope_label, academic_period_id,
                    effective_start_date, effective_end_date, workload_commitment_value, workload_unit,
                    status, approved_by, approved_date, teacher_acknowledged, teacher_acknowledged_date,
                    notes, created_at, created_by
                ) VALUES (
                    @Id, @AssignmentNumber, @TeachingDemandId, @TeacherProfileId, @AssignmentRoleId,
                    'learning_unit', @ScopeReferenceId, 'Bahasa Inggris SMP Kelas 7 (Co-Teaching)', @AcademicPeriodId,
                    @EffectiveStartDate, @EffectiveEndDate, 4.00, 'Teaching Hours',
                    'approved', @ApprovedBy, @ApprovedDate, 1, @ApprovedDate,
                    'Penugasan guru pendamping co-teaching di bawah supervisi mentor.', @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAsnSql,
            new
            {
                Id = _assignmentSitiEngId,
                AssignmentNumber = "ASN-2026-0003",
                TeachingDemandId = _demandSitiEngId,
                TeacherProfileId = sitiId,
                AssignmentRoleId = _roleGuruPendampingId,
                ScopeReferenceId = engUnitId,
                AcademicPeriodId = activePeriodId,
                EffectiveStartDate = startTerm,
                EffectiveEndDate = endTerm,
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = startTerm,
                CreatedAt = now.AddDays(-30),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 3. Emergency Substitution for Budi Hartono (5 hrs coverage during his medical leave)
        const string insertSubSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_substitutions WHERE id = @Id OR substitution_number = @SubstitutionNumber)
            BEGIN
                INSERT INTO teaching.teaching_substitutions (
                    id, substitution_number, original_assignment_id, original_teacher_id,
                    substitute_teacher_id, coverage_start_date, coverage_end_date,
                    coverage_scope_type, partial_scope_details, reason, workload_impact_hours,
                    status, handover_notes, approved_by, approved_date, created_at, created_by
                ) VALUES (
                    @Id, @SubstitutionNumber, @OriginalAssignmentId, @OriginalTeacherId,
                    @SubstituteTeacherId, @CoverageStartDate, @CoverageEndDate,
                    'full', NULL, 'medical_emergency', 5.00,
                    'approved', @HandoverNotes, @ApprovedBy, @ApprovedDate, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertSubSql,
            new
            {
                Id = _subBudiMedicalId,
                SubstitutionNumber = "SUB-2026-0001",
                OriginalAssignmentId = _assignmentBudiMathId,
                OriginalTeacherId = DemoDataConstants.BudiHartonoTeacherId,
                SubstituteTeacherId = sitiId,
                CoverageStartDate = baseDate.AddDays(7),
                CoverageEndDate = baseDate.AddDays(14),
                HandoverNotes = "Penggantian sementara instruksi Matematika SD Kelas 4A selama masa rawat inap guru Budi Hartono.",
                ApprovedBy = _coordinator,
                ApprovedDate = baseDate.AddDays(6),
                CreatedAt = now.AddDays(-1),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 4. Weekly Availability for Siti
        const string insertAvailSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teacher_availabilities WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId AND status = 'active')
            BEGIN
                INSERT INTO teaching.teacher_availabilities (
                    id, teacher_profile_id, academic_period_id, max_available_hours, campus_scope,
                    available_delivery_modes, recurring_available_schedule, effective_start_date,
                    effective_end_date, status, notes, created_at, created_by
                ) VALUES (
                    NEWID(), @TeacherId, @PeriodId, 20.00, @CampusScope,
                    'face_to_face', '{"Monday":["08:00-14:00"],"Tuesday":["08:00-14:00"],"Wednesday":["08:00-14:00"],"Thursday":["08:00-14:00"]}',
                    @StartDate, @EndDate, 'active', 'Jadwal ketersediaan guru kontrak di bawah supervisi.',
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAvailSql,
            new
            {
                TeacherId = sitiId,
                PeriodId = activePeriodId,
                CampusScope = _campusMain,
                StartDate = startTerm,
                EndDate = endTerm,
                CreatedAt = now.AddDays(-30),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 5. Workload Commitment for Siti (4 hrs committed)
        const string insertCommitmentSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.workload_commitments WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId)
            BEGIN
                INSERT INTO teaching.workload_commitments (
                    id, teacher_profile_id, academic_period_id, workload_policy_id,
                    planned_workload_hours, committed_workload_hours, max_capacity_limit_hours,
                    remaining_capacity_hours, status, active_assignment_count, approved_exception_count,
                    as_of_timestamp, created_at, created_by
                ) VALUES (
                    NEWID(), @TeacherId, @PeriodId, @PolicyId,
                    0.00, 4.00, 24.00,
                    20.00, 'normal', 1, 0,
                    SYSUTCDATETIME(), @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertCommitmentSql,
            new
            {
                TeacherId = sitiId,
                PeriodId = activePeriodId,
                PolicyId = _workloadPolicyId,
                CreatedAt = now.AddDays(-30),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 6. History Entries
        await RecordHistoryEntryAsync(connection, sitiId, "AssignmentApproved", "teaching_assignments", _assignmentSitiEngId, startTerm, "KepalaSekolah", _kepalaSekolah, "Penugasan co-teaching Bahasa Inggris SMP (ASN-2026-0003) disetujui", now.AddDays(-30), cancellationToken);
        await RecordHistoryEntryAsync(connection, sitiId, "EmergencySubstitutionCommitted", "teaching_substitutions", _subBudiMedicalId, baseDate.AddDays(7), "Coordinator", _coordinator, "Persetujuan delegasi pengganti darurat (SUB-2026-0001) untuk Matematika SD Kelas 4A", now.AddDays(-1), cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Persona 3: John Miller Stage 2 Seeding (Visiting Lecturer with Workload Exception)
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedJohnMillerStage2Async(
        System.Data.Common.DbConnection connection,
        DateTime baseDate,
        DateTimeOffset now,
        Guid activePeriodId,
        Guid engUnitId,
        CancellationToken cancellationToken)
    {
        var johnId = DemoDataConstants.JohnMillerTeacherId;
        var startTerm = baseDate.AddMonths(-1);
        var endTerm = baseDate.AddMonths(3);

        // 1. Demand 4: Intensive English Immersion (30 hrs workload requested)
        const string insertDemandSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_demands WHERE id = @Id OR demand_code = @DemandCode)
            BEGIN
                INSERT INTO teaching.teaching_demands (
                    id, demand_code, source_domain, source_work_reference, source_work_label,
                    academic_period_id, required_capability_code, required_assignment_role_id,
                    expected_workload_hours, workload_unit, requires_assessment_authority,
                    requested_start_date, requested_end_date, campus_scope, required_teachers_count,
                    status, requested_by, requested_at, created_at, created_by
                ) VALUES (
                    @Id, @DemandCode, 'Academics', @SourceWorkReference, 'English Immersion Intensive Workshop',
                    @AcademicPeriodId, 'ENG_IMMERSION', @RequiredAssignmentRoleId,
                    30.00, 'Teaching Hours', 1,
                    @StartDate, @EndDate, @CampusScope, 1,
                    'staffed', @RequestedBy, @RequestedAt, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertDemandSql,
            new
            {
                Id = _demandJohnImmersionId,
                DemandCode = "DEM-2026-0004",
                SourceWorkReference = engUnitId,
                AcademicPeriodId = activePeriodId,
                RequiredAssignmentRoleId = _roleDosenTamuId,
                StartDate = startTerm,
                EndDate = endTerm,
                CampusScope = _campusMain,
                RequestedBy = _coordinator,
                RequestedAt = now.AddDays(-30),
                CreatedAt = now.AddDays(-30),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 2. Assignment 4: John Miller English Immersion (30 hrs, approved)
        const string insertAsnSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_assignments WHERE id = @Id OR assignment_number = @AssignmentNumber)
            BEGIN
                INSERT INTO teaching.teaching_assignments (
                    id, assignment_number, teaching_demand_id, teacher_profile_id, assignment_role_id,
                    assignment_scope, scope_reference_id, scope_label, academic_period_id,
                    effective_start_date, effective_end_date, workload_commitment_value, workload_unit,
                    status, approved_by, approved_date, teacher_acknowledged, teacher_acknowledged_date,
                    notes, created_at, created_by
                ) VALUES (
                    @Id, @AssignmentNumber, @TeachingDemandId, @TeacherProfileId, @AssignmentRoleId,
                    'learning_unit', @ScopeReferenceId, 'International English Immersion Program', @AcademicPeriodId,
                    @EffectiveStartDate, @EffectiveEndDate, 30.00, 'Teaching Hours',
                    'approved', @ApprovedBy, @ApprovedDate, 1, @ApprovedDate,
                    'Penugasan dosen tamu program imersi bahasa internasional dengan izin beban lebih (exception).',
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAsnSql,
            new
            {
                Id = _assignmentJohnImmersionId,
                AssignmentNumber = "ASN-2026-0004",
                TeachingDemandId = _demandJohnImmersionId,
                TeacherProfileId = johnId,
                AssignmentRoleId = _roleDosenTamuId,
                ScopeReferenceId = engUnitId,
                AcademicPeriodId = activePeriodId,
                EffectiveStartDate = startTerm,
                EffectiveEndDate = endTerm,
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = startTerm,
                CreatedAt = now.AddDays(-28),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 3. Workload Exception: +3 hrs above prohibited ceiling 28 hrs
        const string insertWexSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.workload_exceptions WHERE id = @Id OR exception_number = @ExceptionNumber)
            BEGIN
                INSERT INTO teaching.workload_exceptions (
                    id, exception_number, teaching_assignment_id, teacher_profile_id, workload_policy_id,
                    exceeded_hours, justification, recommended_by, recommended_date, approved_by,
                    approved_date, effective_start_date, effective_end_date, status, decision_remarks,
                    created_at, created_by
                ) VALUES (
                    @Id, @ExceptionNumber, @AssignmentId, @TeacherId, @PolicyId,
                    @ExceededHours, @Justification, @RecommendedBy, @RecommendedDate, @ApprovedBy,
                    @ApprovedDate, @StartDate, @EndDate, 'approved', @Remarks,
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertWexSql,
            new
            {
                Id = _wexJohnMillerId,
                ExceptionNumber = "WEX-2026-0001",
                AssignmentId = _assignmentJohnImmersionId,
                TeacherId = johnId,
                PolicyId = _workloadPolicyId,
                ExceededHours = 3.00m,
                Justification = "Dosen tamu internasional berkualifikasi Cambridge CELTA menyelenggarakan program intensif terjadwal; disetujui melebihi batas prohibited threshold 28 jam.",
                RecommendedBy = _coordinator,
                RecommendedDate = startTerm.AddDays(-2),
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = startTerm,
                StartDate = startTerm,
                EndDate = endTerm,
                Remarks = "Disetujui oleh Kepala Sekolah berdasarkan MOU kerja sama bahasa asing.",
                CreatedAt = now.AddDays(-28),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 4. Waived Capacity Conflict Record
        const string insertConflictSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.capacity_conflicts WHERE teaching_assignment_id = @AssignmentId AND teacher_profile_id = @TeacherId)
            BEGIN
                INSERT INTO teaching.capacity_conflicts (
                    id, teaching_assignment_id, teaching_demand_id, teacher_profile_id,
                    conflict_type, conflict_classification, severity, description,
                    detected_date, status, resolution_action, resolved_by, resolved_date,
                    resolution_notes, created_at, created_by
                ) VALUES (
                    NEWID(), @AssignmentId, @DemandId, @TeacherId,
                    'workload_overload', 'capacity_exceeded', 'blocking',
                    'Total beban kerja 30.00 jam melebihi batas prohibited threshold 28.00 jam.',
                    @DetectedDate, 'waived_with_exception', 'exception_granted', @ResolvedBy,
                    @ResolvedDate, 'Pengecualian beban kerja WEX-2026-0001 telah disetujui Kepala Sekolah.',
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertConflictSql,
            new
            {
                AssignmentId = _assignmentJohnImmersionId,
                DemandId = _demandJohnImmersionId,
                TeacherId = johnId,
                DetectedDate = startTerm.AddDays(-2),
                ResolvedBy = _kepalaSekolah,
                ResolvedDate = startTerm,
                CreatedAt = now.AddDays(-28),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 5. Workload Commitment for John Miller (30 hrs committed, status: overloaded_with_exception)
        const string insertCommitmentSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.workload_commitments WHERE teacher_profile_id = @TeacherId AND academic_period_id = @PeriodId)
            BEGIN
                INSERT INTO teaching.workload_commitments (
                    id, teacher_profile_id, academic_period_id, workload_policy_id,
                    planned_workload_hours, committed_workload_hours, max_capacity_limit_hours,
                    remaining_capacity_hours, status, active_assignment_count, approved_exception_count,
                    as_of_timestamp, created_at, created_by
                ) VALUES (
                    NEWID(), @TeacherId, @PeriodId, @PolicyId,
                    0.00, 30.00, 24.00,
                    -6.00, 'overloaded_with_exception', 1, 1,
                    SYSUTCDATETIME(), @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertCommitmentSql,
            new
            {
                TeacherId = johnId,
                PeriodId = activePeriodId,
                PolicyId = _workloadPolicyId,
                CreatedAt = now.AddDays(-28),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 6. History Entry
        await RecordHistoryEntryAsync(connection, johnId, "WorkloadExceptionApproved", "workload_exceptions", _wexJohnMillerId, startTerm, "KepalaSekolah", _kepalaSekolah, "Pengecualian beban kerja (+3.00 jam) WEX-2026-0001 disetujui", now.AddDays(-28), cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Persona 4: Hendra Setiawan Stage 2 Seeding (Ineligible Candidate Demo)
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedHendraSetiawanStage2Async(
        System.Data.Common.DbConnection connection,
        DateTime baseDate,
        DateTimeOffset now,
        Guid activePeriodId,
        Guid mathUnitId,
        CancellationToken cancellationToken)
    {
        var hendraId = DemoDataConstants.HendraSetiawanTeacherId;
        var startTerm = baseDate.AddMonths(-1);
        var endTerm = baseDate.AddMonths(5);

        // 1. Demand 5: Open Demand for Math SD Kelas 5
        const string insertDemandSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_demands WHERE id = @Id OR demand_code = @DemandCode)
            BEGIN
                INSERT INTO teaching.teaching_demands (
                    id, demand_code, source_domain, source_work_reference, source_work_label,
                    academic_period_id, required_capability_code, required_assignment_role_id,
                    expected_workload_hours, workload_unit, requires_assessment_authority,
                    requested_start_date, requested_end_date, campus_scope, required_teachers_count,
                    status, requested_by, requested_at, created_at, created_by
                ) VALUES (
                    @Id, @DemandCode, 'Academics', @SourceWorkReference, 'Matematika SD Kelas 5',
                    @AcademicPeriodId, 'MATH_PRIMARY', @RequiredAssignmentRoleId,
                    5.00, 'Teaching Hours', 1,
                    @StartDate, @EndDate, @CampusScope, 1,
                    'accepted_for_staffing', @RequestedBy, @RequestedAt, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertDemandSql,
            new
            {
                Id = _demandHendraPendingId,
                DemandCode = "DEM-2026-0005",
                SourceWorkReference = mathUnitId,
                AcademicPeriodId = activePeriodId,
                RequiredAssignmentRoleId = _roleGuruUtamaId,
                StartDate = startTerm,
                EndDate = endTerm,
                CampusScope = _campusMain,
                RequestedBy = _coordinator,
                RequestedAt = now.AddDays(-10),
                CreatedAt = now.AddDays(-10),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 2. Evaluation Record: Ineligible Outcome Demonstration
        const string insertEvalSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teacher_eligibility_evaluations WHERE teaching_demand_id = @DemandId AND teacher_profile_id = @TeacherId)
            BEGIN
                INSERT INTO teaching.teacher_eligibility_evaluations (
                    id, teaching_demand_id, teacher_profile_id, evaluation_date, evaluated_by,
                    is_eligible, eligibility_status, profile_active_check, authorization_check_passed,
                    qualification_check_passed, availability_check_passed, capacity_check_passed,
                    explanation, notes, created_at, created_by
                ) VALUES (
                    NEWID(), @DemandId, @TeacherId, @EvaluationDate, @EvaluatedBy,
                    0, 'ineligible_unqualified', 0, 0,
                    0, 1, 1,
                    @Explanation, @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertEvalSql,
            new
            {
                DemandId = _demandHendraPendingId,
                TeacherId = hendraId,
                EvaluationDate = baseDate.AddDays(-5),
                EvaluatedBy = _coordinator,
                Explanation = "Kandidat Hendra Setiawan tidak memenuhi kriteria penugasan: Status profil masih 'pending_approval' (belum aktif), belum memiliki otorisasi aktif untuk kapabilitas MATH_PRIMARY, dan bukti kualifikasi belum diverifikasi.",
                Notes = "Hasil evaluasi otomatis kepatuhan penugasan pengajar.",
                CreatedAt = now.AddDays(-5),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Step 7: Permanent Reassignment & Handover Example (Budi to Siti for Science SD)
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedPermanentReassignmentExampleAsync(
        System.Data.Common.DbConnection connection,
        DateTime baseDate,
        DateTimeOffset now,
        Guid activePeriodId,
        Guid sciUnitId,
        CancellationToken cancellationToken)
    {
        var budiId = DemoDataConstants.BudiHartonoTeacherId;
        var sitiId = DemoDataConstants.SitiNurhalizaTeacherId;
        var effDate = baseDate.AddMonths(-2);

        // Predecessor Assignment
        const string insertAsnSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_assignments WHERE id = @Id)
            BEGIN
                INSERT INTO teaching.teaching_assignments (
                    id, assignment_number, teaching_demand_id, teacher_profile_id, assignment_role_id,
                    assignment_scope, scope_reference_id, scope_label, academic_period_id,
                    effective_start_date, effective_end_date, workload_commitment_value, workload_unit,
                    status, approved_by, approved_date, teacher_acknowledged, teacher_acknowledged_date,
                    notes, created_at, created_by
                ) VALUES (
                    @Id, 'ASN-2026-0005', @DemandId, @TeacherId, @RoleId,
                    'learning_unit', @ScopeId, 'IPA Eksperimental SD', @PeriodId,
                    @StartDate, @EndDate, 3.00, 'Teaching Hours',
                    'completed', @ApprovedBy, @ApprovedDate, 1, @ApprovedDate,
                    'Penugasan terdahulu yang telah dialihkan secara permanen (reassigned).',
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAsnSql,
            new
            {
                Id = _assignmentBudiHandoverReplacedId,
                DemandId = _demandBudiMathId,
                TeacherId = budiId,
                RoleId = _roleGuruUtamaId,
                ScopeId = sciUnitId,
                PeriodId = activePeriodId,
                StartDate = baseDate.AddMonths(-6),
                EndDate = effDate,
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = baseDate.AddMonths(-6),
                CreatedAt = now.AddDays(-90),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // Reassignment record
        const string insertReaSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_reassignments WHERE id = @Id OR reassignment_number = @ReassignmentNumber)
            BEGIN
                INSERT INTO teaching.teaching_reassignments (
                    id, reassignment_number, original_assignment_id, predecessor_teacher_id,
                    new_teacher_id, replacement_assignment_id, effective_date, reassignment_type,
                    reason, requires_handover, handover_status, handover_completed_date,
                    handover_notes, status, approved_by, approved_date, created_at, created_by
                ) VALUES (
                    @Id, @ReassignmentNumber, @OriginalAssignmentId, @PredecessorId,
                    @NewTeacherId, NULL, @EffectiveDate, 'curriculum_reallocation',
                    @Reason, 1, 'completed', @EffectiveDate,
                    @HandoverNotes, 'approved', @ApprovedBy, @ApprovedDate, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertReaSql,
            new
            {
                Id = _reaBudiToSitiId,
                ReassignmentNumber = "REA-2026-0001",
                OriginalAssignmentId = _assignmentBudiHandoverReplacedId,
                PredecessorId = budiId,
                NewTeacherId = sitiId,
                EffectiveDate = effDate,
                Reason = "Penataan ulang alokasi pengajaran IPA SD ke guru pendamping dalam rangka mentoring terstruktur.",
                HandoverNotes = "Serah terima silabus IPA, kit eksperimen laboratorium, dan catatan observasi siswa telah diselesaikan penuh.",
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = effDate,
                CreatedAt = now.AddDays(-60),
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        await RecordHistoryEntryAsync(connection, budiId, "ReassignmentApproved", "teaching_reassignments", _reaBudiToSitiId, effDate, "KepalaSekolah", _kepalaSekolah, "Pengalihan permanen tanggung jawab ajar (REA-2026-0001) kepada Siti Nurhaliza disetujui", now.AddDays(-60), cancellationToken);
        await RecordHistoryEntryAsync(connection, sitiId, "ReassignmentApproved", "teaching_reassignments", _reaBudiToSitiId, effDate, "KepalaSekolah", _kepalaSekolah, "Penerimaan pengalihan tanggung jawab ajar (REA-2026-0001) dari Budi Hartono disetujui", now.AddDays(-60), cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: Immutable History Entry Insertion
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task RecordHistoryEntryAsync(
        System.Data.Common.DbConnection connection,
        Guid teacherProfileId,
        string eventType,
        string entityType,
        Guid entityId,
        DateTime effectiveDate,
        string actorRole,
        string actorId,
        string eventSummary,
        DateTimeOffset eventTimestamp,
        CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_history_entries WHERE teacher_profile_id = @TeacherId AND entity_id = @EntityId AND event_type = @EventType)
            BEGIN
                INSERT INTO teaching.teaching_history_entries (
                    id, teacher_profile_id, event_type, entity_type, entity_id,
                    effective_date, event_timestamp, actor_role, actor_id, event_summary,
                    change_payload, notes, created_at, created_by
                ) VALUES (
                    NEWID(), @TeacherId, @EventType, @EntityType, @EntityId,
                    @EffectiveDate, @EventTimestamp, @ActorRole, @ActorId, @EventSummary,
                    NULL, NULL, @EventTimestamp, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                TeacherId = teacherProfileId,
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                EffectiveDate = effectiveDate,
                EventTimestamp = eventTimestamp,
                ActorRole = actorRole,
                ActorId = actorId,
                EventSummary = eventSummary,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));
    }
}
