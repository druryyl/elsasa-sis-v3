namespace Elsasa.Sis.IntegrationTests.Features.Teaching;

using Dapper;
using Microsoft.Data.SqlClient;

/// <summary>
/// Reusable test data seeding and fixture helpers for Teaching integration test suites (ADR-008, IS014).
/// </summary>
public static class TeachingIntegrationTestHelpers
{
    /// <summary>
    /// Ensures an active teaching role exists in <c>teaching.teaching_roles</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureTeachingRoleAsync(
        SqlConnection connection,
        string code = "HOMEROOM_TEACHER",
        string name = "Homeroom Teacher",
        bool requiresQualificationCheck = true)
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_roles WHERE code = @Code)
            BEGIN
                INSERT INTO teaching.teaching_roles (
                    id, code, name, description, requires_qualification_check, is_active, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, 'Homeroom teacher role for test execution', @RequiresQualificationCheck, 1, SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM teaching.teaching_roles WHERE code = @Code;
            """;

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            RequiresQualificationCheck = requiresQualificationCheck
        });
    }

    /// <summary>
    /// Ensures an active capability type exists in <c>teaching.capability_types</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureCapabilityTypeAsync(
        SqlConnection connection,
        string code = "SUBJECT_INSTRUCTION",
        string name = "Subject Instruction")
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.capability_types WHERE code = @Code)
            BEGIN
                INSERT INTO teaching.capability_types (
                    id, code, name, description, is_active, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, 'Pedagogical instruction capability', 1, SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM teaching.capability_types WHERE code = @Code;
            """;

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name
        });
    }

    /// <summary>
    /// Ensures an active qualification type exists in <c>teaching.qualification_types</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureQualificationTypeAsync(
        SqlConnection connection,
        string code = "DEGREE_BACHELOR",
        string name = "Bachelor of Education",
        bool requiresRenewal = false,
        int? defaultValidityMonths = null)
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.qualification_types WHERE code = @Code)
            BEGIN
                INSERT INTO teaching.qualification_types (
                    id, code, name, description, requires_renewal, default_validity_months, is_active, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, 'Undergraduate education degree', @RequiresRenewal, @DefaultValidityMonths, 1, SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM teaching.qualification_types WHERE code = @Code;
            """;

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            RequiresRenewal = requiresRenewal,
            DefaultValidityMonths = defaultValidityMonths
        });
    }

    /// <summary>
    /// Ensures an active teacher profile exists in <c>teaching.teacher_profiles</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureTeacherProfileAsync(
        SqlConnection connection,
        string standing = "active",
        Guid? teachingRoleId = null)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var roleId = teachingRoleId ?? await EnsureTeachingRoleAsync(connection);
        var id = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var nationalId = $"3171{Random.Shared.Next(10000000, 99999999)}0001";

        const string sql = """
            INSERT INTO teaching.teacher_profiles (
                id, teacher_number, full_name, display_name, employee_ref, national_id_or_external_ref,
                email, phone, teaching_role_id, campus_affiliation, department, standing,
                effective_from, approved_by, approved_date, created_at, created_by
            ) VALUES (
                @Id, @TeacherNumber, @FullName, @DisplayName, @EmployeeRef, @NationalId,
                @Email, '+6281234567890', @RoleId, 'Kampus Utama', 'Math & Science', @Standing,
                '2026-08-01', 'TEST_APPROVER', '2026-08-01', SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            TeacherNumber = $"TCH-HELP-{suffix}",
            FullName = $"Test Teacher {suffix}",
            DisplayName = $"Teacher {suffix}",
            EmployeeRef = $"EMP-{suffix}",
            NationalId = nationalId,
            Email = $"teacher.{suffix}@elsasa.test",
            RoleId = roleId,
            Standing = standing
        });

        return id;
    }

    /// <summary>
    /// Ensures an active assignment role exists in <c>teaching.assignment_roles</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureAssignmentRoleAsync(
        SqlConnection connection,
        string code = "GURU_UTAMA",
        string name = "Guru Utama",
        bool isLeadRole = true,
        bool countsTowardWorkload = true)
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.assignment_roles WHERE code = @Code)
            BEGIN
                INSERT INTO teaching.assignment_roles (
                    id, code, name, description, is_lead_role, counts_toward_workload, is_active, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, 'Primary subject teacher role', @IsLeadRole, @CountsTowardWorkload, 1, SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM teaching.assignment_roles WHERE code = @Code;
            """;

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            IsLeadRole = isLeadRole,
            CountsTowardWorkload = countsTowardWorkload
        });
    }

    /// <summary>
    /// Ensures an active workload policy exists in <c>teaching.workload_policies</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureWorkloadPolicyAsync(
        SqlConnection connection,
        string code = "WPOL-DEFAULT",
        string name = "Standard Academic Workload Policy")
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.workload_policies WHERE code = @Code)
            BEGIN
                INSERT INTO teaching.workload_policies (
                    id, code, name, academic_year_id, version, workload_unit,
                    max_standard_capacity, warning_threshold, prohibited_threshold,
                    includes_assessment_effort, includes_advising_effort, includes_preparation_time,
                    calculation_rules_summary, effective_start_date, status,
                    approved_by, approved_date, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, NEWID(), '1.0', 'Teaching Hours',
                    24.00, 20.00, 28.00,
                    1, 1, 0,
                    'Standard policy: 24 max standard hours, 20 warning threshold, 28 prohibited threshold.',
                    '2026-08-01', 'active',
                    'Dean of Academics', '2026-08-01', SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM teaching.workload_policies WHERE code = @Code;
            """;

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name
        });
    }

    /// <summary>
    /// Ensures a teaching demand exists in <c>teaching.teaching_demands</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureTeachingDemandAsync(
        SqlConnection connection,
        Guid assignmentRoleId,
        Guid? academicPeriodId = null,
        string? demandCode = null,
        string status = "received")
    {
        ArgumentNullException.ThrowIfNull(connection);

        var id = Guid.NewGuid();
        var code = demandCode ?? $"DEM-TEST-{Guid.NewGuid():N}"[..20];
        var periodId = academicPeriodId ?? Guid.NewGuid();

        const string sql = """
            INSERT INTO teaching.teaching_demands (
                id, demand_code, source_domain, source_work_reference, source_work_label,
                academic_period_id, required_capability_code, required_assignment_role_id,
                expected_workload_hours, workload_unit, requires_assessment_authority,
                requested_start_date, requested_end_date, campus_scope, required_teachers_count,
                status, requested_by, requested_at, created_at, created_by
            ) VALUES (
                @Id, @DemandCode, 'Academics', NEWID(), 'Matematika Kelas 4',
                @AcademicPeriodId, 'MATH_PRIMARY', @RequiredAssignmentRoleId,
                4.00, 'Teaching Hours', 1,
                '2026-08-01', '2026-12-15', 'Kampus Utama', 1,
                @Status, 'test-coordinator', SYSUTCDATETIME(), SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            DemandCode = code,
            AcademicPeriodId = periodId,
            RequiredAssignmentRoleId = assignmentRoleId,
            Status = status
        });

        return id;
    }

    /// <summary>
    /// Ensures a teaching assignment exists in <c>teaching.teaching_assignments</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureTeachingAssignmentAsync(
        SqlConnection connection,
        Guid demandId,
        Guid teacherId,
        Guid assignmentRoleId,
        Guid? academicPeriodId = null,
        string? assignmentNumber = null,
        string status = "proposed")
    {
        ArgumentNullException.ThrowIfNull(connection);

        var id = Guid.NewGuid();
        var number = assignmentNumber ?? $"ASN-TEST-{Guid.NewGuid():N}"[..20];
        var periodId = academicPeriodId ?? Guid.NewGuid();

        const string sql = """
            INSERT INTO teaching.teaching_assignments (
                id, assignment_number, teaching_demand_id, teacher_profile_id,
                assignment_role_id, assignment_scope, scope_reference_id, scope_label,
                academic_period_id, effective_start_date, effective_end_date,
                workload_commitment_value, workload_unit, status,
                teacher_acknowledged, created_at, created_by
            ) VALUES (
                @Id, @AssignmentNumber, @DemandId, @TeacherId,
                @AssignmentRoleId, 'learning_unit', NEWID(), 'Matematika Kelas 4 SD',
                @AcademicPeriodId, '2026-08-01', '2026-12-15',
                4.00, 'Teaching Hours', @Status,
                0, SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            AssignmentNumber = number,
            DemandId = demandId,
            TeacherId = teacherId,
            AssignmentRoleId = assignmentRoleId,
            AcademicPeriodId = periodId,
            Status = status
        });

        return id;
    }

    /// <summary>
    /// Ensures a teaching capability exists in <c>teaching.teaching_capabilities</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureTeachingCapabilityAsync(
        SqlConnection connection,
        string capabilityCode = "MATH_PRIMARY",
        string name = "Primary Mathematics Instruction")
    {
        ArgumentNullException.ThrowIfNull(connection);

        var capTypeId = await EnsureCapabilityTypeAsync(connection);

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_capabilities WHERE capability_code = @Code)
            BEGIN
                INSERT INTO teaching.teaching_capabilities (
                    id, capability_type_id, capability_code, name, description, is_active, created_at, created_by
                ) VALUES (
                    @Id, @CapTypeId, @Code, @Name, 'Test capability instruction', 1, SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM teaching.teaching_capabilities WHERE capability_code = @Code;
            """;

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = Guid.NewGuid(),
            CapTypeId = capTypeId,
            Code = capabilityCode,
            Name = name
        });
    }

    /// <summary>
    /// Ensures an active teaching authorization exists in <c>teaching.teaching_authorizations</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureTeachingAuthorizationAsync(
        SqlConnection connection,
        Guid teacherProfileId,
        Guid? capabilityId = null,
        string scopeDescription = "Mathematics Grades 1-6",
        string campusScope = "Kampus Utama",
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string status = "active")
    {
        ArgumentNullException.ThrowIfNull(connection);

        var id = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var sDate = startDate ?? new DateOnly(2026, 1, 1);
        var eDate = endDate ?? new DateOnly(2027, 12, 31);

        const string sql = """
            INSERT INTO teaching.teaching_authorizations (
                id, authorization_number, teacher_profile_id, authorized_capability_id,
                scope_description, campus_scope, effective_start_date, effective_end_date,
                status, unverified_credential_warning, created_at, created_by
            ) VALUES (
                @Id, @AuthNumber, @TeacherProfileId, @CapabilityId,
                @ScopeDescription, @CampusScope, @StartDate, @EndDate,
                @Status, 0, SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            AuthNumber = $"ATH-TEST-{suffix}",
            TeacherProfileId = teacherProfileId,
            CapabilityId = capabilityId,
            ScopeDescription = scopeDescription,
            CampusScope = campusScope,
            StartDate = sDate,
            EndDate = (object?)eDate ?? DBNull.Value,
            Status = status
        });

        return id;
    }

    /// <summary>
    /// Ensures an unavailability window exists in <c>teaching.teacher_unavailability_windows</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureUnavailabilityWindowAsync(
        SqlConnection connection,
        Guid teacherProfileId,
        DateOnly startDate,
        DateOnly endDate,
        string unavailabilityType = "medical_leave",
        string reason = "Medical recovery window",
        string status = "active")
    {
        ArgumentNullException.ThrowIfNull(connection);

        var id = Guid.NewGuid();

        const string sql = """
            INSERT INTO teaching.teacher_unavailability_windows (
                id, teacher_profile_id, unavailability_type, start_date, end_date,
                reason, emergency_coverage_required, status, created_at, created_by
            ) VALUES (
                @Id, @TeacherProfileId, @UnavailabilityType, @StartDate, @EndDate,
                @Reason, 1, @Status, SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            TeacherProfileId = teacherProfileId,
            UnavailabilityType = unavailabilityType,
            StartDate = startDate,
            EndDate = endDate,
            Reason = reason,
            Status = status
        });

        return id;
    }

    /// <summary>
    /// Ensures a workload commitment exists in <c>teaching.workload_commitments</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureWorkloadCommitmentAsync(
        SqlConnection connection,
        Guid teacherProfileId,
        Guid academicPeriodId,
        decimal plannedHours = 0.00m,
        decimal committedHours = 0.00m,
        decimal maxCapacityHours = 24.00m,
        int activeCount = 0,
        string commitmentStatus = "normal")
    {
        ArgumentNullException.ThrowIfNull(connection);

        var id = Guid.NewGuid();
        var remainingHours = maxCapacityHours - committedHours;

        const string sql = """
            IF NOT EXISTS (
                SELECT 1 FROM teaching.workload_commitments
                WHERE teacher_profile_id = @TeacherProfileId AND academic_period_id = @AcademicPeriodId
            )
            BEGIN
                INSERT INTO teaching.workload_commitments (
                    id, teacher_profile_id, academic_period_id, planned_workload_hours, committed_workload_hours,
                    max_capacity_limit_hours, remaining_capacity_hours, active_assignment_count,
                    commitment_status, approved_exception_count, last_calculated_at, created_at, created_by
                ) VALUES (
                    @Id, @TeacherProfileId, @AcademicPeriodId, @PlannedHours, @CommittedHours,
                    @MaxCapacityHours, @RemainingHours, @ActiveCount,
                    @CommitmentStatus, 0, SYSUTCDATETIME(), SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM teaching.workload_commitments
            WHERE teacher_profile_id = @TeacherProfileId AND academic_period_id = @AcademicPeriodId;
            """;

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = id,
            TeacherProfileId = teacherProfileId,
            AcademicPeriodId = academicPeriodId,
            PlannedHours = plannedHours,
            CommittedHours = committedHours,
            MaxCapacityHours = maxCapacityHours,
            RemainingHours = remainingHours,
            ActiveCount = activeCount,
            CommitmentStatus = commitmentStatus
        });
    }

    /// <summary>
    /// Ensures a capacity conflict exists in <c>teaching.capacity_conflicts</c> and returns its ID.
    /// </summary>
    public static async Task<Guid> EnsureCapacityConflictAsync(
        SqlConnection connection,
        Guid academicPeriodId,
        Guid? teacherProfileId = null,
        Guid? assignmentId = null,
        Guid? demandId = null,
        string conflictType = "schedule_overlap",
        string severity = "blocking",
        string status = "unresolved",
        string description = "Potential schedule conflict detected")
    {
        ArgumentNullException.ThrowIfNull(connection);

        var id = Guid.NewGuid();

        const string sql = """
            INSERT INTO teaching.capacity_conflicts (
                id, conflict_type, severity, teacher_profile_id, teaching_assignment_id, teaching_demand_id,
                academic_period_id, description, status, identified_at, created_at, created_by
            ) VALUES (
                @Id, @ConflictType, @Severity, @TeacherProfileId, @AssignmentId, @DemandId,
                @AcademicPeriodId, @Description, @Status, SYSUTCDATETIME(), SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            ConflictType = conflictType,
            Severity = severity,
            TeacherProfileId = teacherProfileId,
            AssignmentId = assignmentId,
            DemandId = demandId,
            AcademicPeriodId = academicPeriodId,
            Description = description,
            Status = status
        });

        return id;
    }
}
