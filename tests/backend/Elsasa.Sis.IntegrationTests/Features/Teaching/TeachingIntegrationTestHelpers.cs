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
        string name = "Homeroom Teacher")
    {
        ArgumentNullException.ThrowIfNull(connection);

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_roles WHERE code = @Code)
            BEGIN
                INSERT INTO teaching.teaching_roles (
                    id, code, name, description, requires_qualification_check, is_active, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, 'Homeroom teacher role for test execution', 1, 1, SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM teaching.teaching_roles WHERE code = @Code;
            """;

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name
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
}
