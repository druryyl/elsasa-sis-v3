namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using System.Net;
using System.Net.Http.Json;
using Dapper;
using Elsasa.Sis.Academics.Features.AcademicRequirements.CreateAcademicRequirement;
using Elsasa.Sis.Academics.Features.LearningStructures.CreateLearningStructure;
using Elsasa.Sis.Academics.Features.LearningUnits.CreateLearningUnit;
using FluentAssertions;
using Microsoft.Data.SqlClient;

/// <summary>
/// Reusable test data seeding and fixture helpers for Academics integration test suites (ADR-008, IS008).
/// </summary>
public static class AcademicsIntegrationTestHelpers
{
    private const string _structuresUri = "/api/v1/academics/learning-structures";

    /// <summary>
    /// Seeds an active Academic Year and active Operational Period directly in operations tables for cross-domain integrity.
    /// </summary>
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
            Code = $"AY-ACD-{suffix}",
            Name = $"Academic Year Academics {suffix}"
        });

        const string insertPeriodSql = """
            IF NOT EXISTS (SELECT 1 FROM operations.operational_periods WHERE period_code = @PeriodCode)
            BEGIN
                INSERT INTO operations.operational_periods (
                    id, academic_year_id, period_code, name, period_type, start_date, end_date, campus_context, status, created_at, created_by
                ) VALUES (
                    @Id, @AcademicYearId, @PeriodCode, @Name, 'semester', '2026-08-01', '2026-12-31', 'All', 'active', SYSUTCDATETIME(), 'TEST_SETUP'
                );
            END;
            SELECT TOP (1) id FROM operations.operational_periods WHERE period_code = @PeriodCode;
            """;

        var actualPeriodId = await connection.ExecuteScalarAsync<Guid>(insertPeriodSql, new
        {
            Id = periodId,
            AcademicYearId = actualAyId,
            PeriodCode = $"SEM-ACD-{suffix}",
            Name = $"Semester Academics {suffix}"
        });

        return (actualAyId, actualPeriodId);
    }

    /// <summary>
    /// Creates a valid draft learning structure via HTTP endpoint.
    /// </summary>
    public static async Task<CreateLearningStructureResponse> CreateDraftStructureAsync(
        HttpClient client,
        Guid periodId,
        string name = "High School Diploma Framework",
        string version = "1.0",
        DateOnly? effectiveDate = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        var request = new CreateLearningStructureRequest(
            Name: name,
            AcademicPeriodId: periodId,
            Version: version,
            EffectiveDate: effectiveDate ?? new DateOnly(2026, 8, 1),
            EndDate: null,
            Description: "Curriculum framework for testing",
            Notes: "Initial draft creation");

        var response = await client.PostAsJsonAsync(_structuresUri, request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreateLearningStructureResponse>();
        created.Should().NotBeNull();
        return created!;
    }

    /// <summary>
    /// Adds a root learning unit (ParentUnitId is null) to the specified learning structure.
    /// </summary>
    public static async Task<CreateLearningUnitResponse> CreateRootUnitAsync(
        HttpClient client,
        Guid structureId,
        string code = "MATH101",
        string name = "Foundational Mathematics",
        string unitType = "course",
        decimal? creditWeight = 4.0m)
    {
        ArgumentNullException.ThrowIfNull(client);

        var request = new CreateLearningUnitRequest(
            Code: code,
            Name: name,
            UnitType: unitType,
            ParentUnitId: null,
            Level: "Grade 10",
            Description: "Root mathematics course",
            CreditWeight: creditWeight,
            Notes: "Root unit");

        var response = await client.PostAsJsonAsync($"{_structuresUri}/{structureId}/units", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreateLearningUnitResponse>();
        created.Should().NotBeNull();
        return created!;
    }

    /// <summary>
    /// Adds a child learning unit under the specified parent unit.
    /// </summary>
    public static async Task<CreateLearningUnitResponse> CreateChildUnitAsync(
        HttpClient client,
        Guid structureId,
        Guid parentUnitId,
        string code = "MATH101-MOD1",
        string name = "Algebra Essentials",
        string unitType = "module",
        decimal? creditWeight = 2.0m)
    {
        ArgumentNullException.ThrowIfNull(client);

        var request = new CreateLearningUnitRequest(
            Code: code,
            Name: name,
            UnitType: unitType,
            ParentUnitId: parentUnitId,
            Level: "Grade 10",
            Description: "Child module of mathematics",
            CreditWeight: creditWeight,
            Notes: "Child unit");

        var response = await client.PostAsJsonAsync($"{_structuresUri}/{structureId}/units", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreateLearningUnitResponse>();
        created.Should().NotBeNull();
        return created!;
    }

    /// <summary>
    /// Adds an academic requirement to the specified learning structure.
    /// </summary>
    public static async Task<CreateAcademicRequirementResponse> CreateRequirementAsync(
        HttpClient client,
        Guid structureId,
        string name = "Minimum Math Credits",
        string scope = "program",
        string type = "credit",
        string description = "Student must complete at least 4 math credits.",
        Guid? learningUnitId = null,
        string? level = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        var request = new CreateAcademicRequirementRequest(
            Name: name,
            RequirementScope: scope,
            LearningUnitId: learningUnitId,
            Level: level,
            RequirementType: type,
            Description: description,
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1),
            Notes: "Requirement rule for testing");

        var response = await client.PostAsJsonAsync($"{_structuresUri}/{structureId}/requirements", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreateAcademicRequirementResponse>();
        created.Should().NotBeNull();
        return created!;
    }
}
