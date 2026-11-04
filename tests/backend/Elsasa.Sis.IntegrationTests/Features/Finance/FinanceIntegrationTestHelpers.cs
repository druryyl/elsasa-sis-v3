namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using Dapper;
using Microsoft.Data.SqlClient;

/// <summary>
/// Reusable test data seeding helpers for Finance integration test suites (ADR-008).
/// </summary>
public static class FinanceIntegrationTestHelpers
{
    public static async Task<(Guid AcademicYearId, Guid OperationalPeriodId)> EnsureAcademicYearAndPeriodAsync(
        SqlConnection connection,
        string suffix = "2026")
    {
        ArgumentNullException.ThrowIfNull(connection);

        var ayId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        const string insertAySql = """
            IF NOT EXISTS (SELECT 1 FROM operations.academic_years WHERE code = @Code)
            BEGIN
                INSERT INTO operations.academic_years (
                    id, code, name, start_date, end_date, status, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, '2026-08-01', '2027-07-31', 'active', SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM operations.academic_years WHERE code = @Code;
            """;

        var actualAyId = await connection.ExecuteScalarAsync<Guid>(insertAySql, new
        {
            Id = ayId,
            Code = $"AY-{suffix}",
            Name = $"Academic Year {suffix}"
        });

        const string insertPeriodSql = """
            IF NOT EXISTS (SELECT 1 FROM operations.operational_periods WHERE period_code = @PeriodCode)
            BEGIN
                INSERT INTO operations.operational_periods (
                    id, academic_year_id, period_code, name, period_type, start_date, end_date, campus_context, status, created_at, created_by
                ) VALUES (
                    @Id, @AcademicYearId, @PeriodCode, @Name, 'term', '2026-08-01', '2026-12-31', 'All', 'active', SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM operations.operational_periods WHERE period_code = @PeriodCode;
            """;

        var actualPeriodId = await connection.ExecuteScalarAsync<Guid>(insertPeriodSql, new
        {
            Id = periodId,
            AcademicYearId = actualAyId,
            PeriodCode = $"SEM1-{suffix}",
            Name = $"Semester 1 {suffix}"
        });

        return (actualAyId, actualPeriodId);
    }

    public static async Task<Guid> SeedStudentAsync(
        SqlConnection connection,
        string suffix,
        string status = "enrolled")
    {
        ArgumentNullException.ThrowIfNull(connection);

        var applicantId = Guid.NewGuid();
        const string insertApplicantSql = """
            INSERT INTO students.applicants (
                id, first_name, last_name, full_name, date_of_birth, gender, nationality, created_at
            ) VALUES (
                @Id, @FirstName, @LastName, @FullName, '2016-04-10', 'Male', 'Indonesian', SYSUTCDATETIME()
            );
            """;

        await connection.ExecuteAsync(insertApplicantSql, new
        {
            Id = applicantId,
            FirstName = $"StuFirst{suffix}",
            LastName = $"StuLast{suffix}",
            FullName = $"Student {suffix}"
        });

        var studentId = Guid.NewGuid();
        const string insertStudentSql = """
            INSERT INTO students.students (
                id, student_number, applicant_id, full_name, date_of_birth, gender,
                nationality, current_status, enrollment_date, created_at
            ) VALUES (
                @Id, @StudentNumber, @ApplicantId, @FullName, '2016-04-10', 'Male',
                'Indonesian', @Status, '2026-08-01', SYSUTCDATETIME()
            );
            """;

        await connection.ExecuteAsync(insertStudentSql, new
        {
            Id = studentId,
            StudentNumber = $"STU-{suffix}",
            ApplicantId = applicantId,
            FullName = $"Student {suffix}",
            Status = status
        });

        return studentId;
    }

    public static async Task SeedPlacementAsync(
        SqlConnection connection,
        Guid studentId,
        string campus = "Main Campus",
        string grade = "Grade 1",
        string program = "Reguler",
        string academicYear = "2026/2027")
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string insertPlacementSql = """
            INSERT INTO students.administrative_placements (
                id, student_id, campus, school_level, grade, academic_year,
                effective_date, program, is_current, created_at, created_by
            ) VALUES (
                NEWID(), @StudentId, @Campus, 'Primary', @Grade, @AcademicYear,
                '2026-08-01', @Program, 1, SYSUTCDATETIME(), 'TEST_SETUP'
            );
            """;

        await connection.ExecuteAsync(insertPlacementSql, new
        {
            StudentId = studentId,
            Campus = campus,
            Grade = grade,
            Program = program,
            AcademicYear = academicYear
        });
    }

    public static async Task<Guid> SeedGuardianAsync(
        SqlConnection connection,
        Guid studentId,
        string fullName,
        string relationshipType = "Father",
        bool isFinancialContact = true,
        bool isPrimaryContact = true,
        string phone = "+6281234567890",
        string email = "guardian@example.com")
    {
        ArgumentNullException.ThrowIfNull(connection);

        var guardianId = Guid.NewGuid();
        const string insertGuardianSql = """
            INSERT INTO students.guardians (
                id, full_name, government_id_type, government_id_number, phone, email, address, created_at
            ) VALUES (
                @Id, @FullName, 'NIK', @GovId, @Phone, @Email, 'Jakarta', SYSUTCDATETIME()
            );
            """;

        var govId = $"3171{Random.Shared.Next(10000000, 99999999)}0001";
        await connection.ExecuteAsync(insertGuardianSql, new
        {
            Id = guardianId,
            FullName = fullName,
            GovId = govId,
            Phone = phone,
            Email = email
        });

        const string insertRelSql = """
            INSERT INTO students.student_guardian_relationships (
                id, student_id, guardian_id, relationship_type, is_primary_contact,
                is_emergency_contact, is_financial_contact, is_authorized_pickup,
                is_active, created_at
            ) VALUES (
                NEWID(), @StudentId, @GuardianId, @RelationshipType, @IsPrimaryContact,
                1, @IsFinancialContact, 1, 1, SYSUTCDATETIME()
            );
            """;

        await connection.ExecuteAsync(insertRelSql, new
        {
            StudentId = studentId,
            GuardianId = guardianId,
            RelationshipType = relationshipType,
            IsPrimaryContact = isPrimaryContact ? 1 : 0,
            IsFinancialContact = isFinancialContact ? 1 : 0
        });

        return guardianId;
    }

    public static readonly Guid DefaultSeedPolicyId = Guid.Parse("00000000-0000-0000-0002-000000000001");

    public static async Task<Guid> EnsureActivePolicyAsync(
        SqlConnection connection,
        string policyCode = "FP-2026-001")
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM finance.financial_policies WHERE policy_code = @PolicyCode)
            BEGIN
                INSERT INTO finance.financial_policies (
                    id, policy_code, name, category, business_purpose, applicable_context,
                    effective_date, end_date, policy_version, status, approving_authority,
                    approved_date, supersedes_policy_id, notes, created_at, created_by
                ) VALUES (
                    @Id, @PolicyCode, @Name, 'tuition', 'Test Policy Purpose', 'Academic Year 2026/2027',
                    '2026-01-15', NULL, '1.0', 'active', 'Direktur Keuangan',
                    '2026-01-15', NULL, 'Integration test active policy', SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM finance.financial_policies WHERE policy_code = @PolicyCode;
            """;

        var id = (policyCode == "FP-2026-001") ? DefaultSeedPolicyId : Guid.NewGuid();

        var policyId = await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = id,
            PolicyCode = policyCode,
            Name = $"Kebijakan Biaya Pendidikan {policyCode}"
        });

        return policyId;
    }
}
