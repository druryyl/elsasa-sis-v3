namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using System.Net;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// End-to-end integration tests for <see cref="IAcademicsModule"/> cross-domain inquiry service (IS008, P4-S13, ADR-001, TD-01, TD-09).
/// Verifies synchronous query methods consumed by downstream bounded contexts (Students, Scheduling, Finance)
/// against real SQL Server data.
/// </summary>
public sealed class AcademicsModuleCrossDomainTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _structuresUri = "/api/v1/academics/learning-structures";

    private IAcademicsModule CreateAcademicsModule()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IAcademicsModule>();
    }

    [SkippableFact]
    public async Task GetActiveLearningStructureForPeriodAsync_Should_ReturnActiveStructure_When_Exists()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "CROSS-ACTIVE");

        var academicsModule = CreateAcademicsModule();

        // 1. Initial check: period has no active structure -> returns null
        var initialResult = await academicsModule.GetActiveLearningStructureForPeriodAsync(periodId);
        initialResult.IsSuccess.Should().BeTrue();
        initialResult.Value.Should().BeNull();

        // 2. Create, populate, submit, and approve structure
        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Primary Curriculum Framework",
            version: "1.0",
            effectiveDate: new DateOnly(2026, 9, 1));

        await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "PRIM101",
            name: "Primary Literacy");

        await AcademicsIntegrationTestHelpers.CreateRequirementAsync(
            Client,
            structure.Id,
            name: "Literacy Assessment",
            scope: "program",
            type: "assessment_policy",
            description: "Assess reading speed and comprehension.");

        // Submit & Approve
        await Client.PostAsync($"{_structuresUri}/{structure.Id}/submit", null);
        var approveResponse = await Client.PostAsync($"{_structuresUri}/{structure.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Query via cross-domain contract
        var queryResult = await academicsModule.GetActiveLearningStructureForPeriodAsync(periodId);

        // Assert
        queryResult.IsSuccess.Should().BeTrue();
        queryResult.Value.Should().NotBeNull();

        var activeDto = queryResult.Value!;
        activeDto.Id.Should().Be(structure.Id);
        activeDto.Name.Should().Be("Primary Curriculum Framework");
        activeDto.AcademicPeriodId.Should().Be(periodId);
        activeDto.Version.Should().Be("1.0");
        activeDto.EffectiveDate.Should().Be(new DateOnly(2026, 9, 1));
        activeDto.Status.Should().Be("active");
        activeDto.ApprovedBy.Should().NotBeNullOrWhiteSpace();
        activeDto.ApprovedDate.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task GetLearningUnitsByStructureAsync_Should_ReturnOrderedUnits_ForStructure()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "CROSS-UNITS");

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Units Query Test Framework",
            version: "1.0");

        var rootUnit = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "ENG100",
            name: "English Literature",
            creditWeight: 4.0m);

        var childUnit = await AcademicsIntegrationTestHelpers.CreateChildUnitAsync(
            Client,
            structure.Id,
            parentUnitId: rootUnit.Id,
            code: "ENG100-POETRY",
            name: "Poetry Analysis",
            creditWeight: 2.0m);

        var academicsModule = CreateAcademicsModule();

        // Act 1: Query units by structure
        var unitsResult = await academicsModule.GetLearningUnitsByStructureAsync(structure.Id);

        // Assert 1
        unitsResult.IsSuccess.Should().BeTrue();
        unitsResult.Value.Should().NotBeNull();
        unitsResult.Value.Should().HaveCount(2);

        var rootDto = unitsResult.Value.Single(u => u.Id == rootUnit.Id);
        rootDto.Code.Should().Be("ENG100");
        rootDto.ParentUnitId.Should().BeNull();
        rootDto.CreditWeight.Should().Be(4.0m);

        var childDto = unitsResult.Value.Single(u => u.Id == childUnit.Id);
        childDto.Code.Should().Be("ENG100-POETRY");
        childDto.ParentUnitId.Should().Be(rootUnit.Id);
        childDto.CreditWeight.Should().Be(2.0m);

        // Act 2: Query single unit by ID
        var singleUnitResult = await academicsModule.GetLearningUnitByIdAsync(childUnit.Id);
        singleUnitResult.IsSuccess.Should().BeTrue();
        singleUnitResult.Value.Should().NotBeNull();
        singleUnitResult.Value!.Id.Should().Be(childUnit.Id);
        singleUnitResult.Value.ParentUnitId.Should().Be(rootUnit.Id);

        // Act 3: Query nonexistent unit
        var nonExistentResult = await academicsModule.GetLearningUnitByIdAsync(Guid.NewGuid());
        nonExistentResult.IsSuccess.Should().BeTrue();
        nonExistentResult.Value.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetAcademicRequirementsByStructureAsync_Should_ReturnRequirements_ForStructure()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "CROSS-REQS");

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Requirements Query Framework",
            version: "1.0");

        var unit = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "SCI100",
            name: "General Science");

        var req1 = await AcademicsIntegrationTestHelpers.CreateRequirementAsync(
            Client,
            structure.Id,
            name: "Requirement A - Program",
            scope: "program",
            type: "graduation",
            description: "Must fulfill program credits");

        var req2 = await AcademicsIntegrationTestHelpers.CreateRequirementAsync(
            Client,
            structure.Id,
            name: "Requirement B - Unit",
            scope: "learning_unit",
            type: "prerequisite",
            description: "Must pass prerequisite unit",
            learningUnitId: unit.Id);

        var academicsModule = CreateAcademicsModule();

        // Act
        var reqsResult = await academicsModule.GetAcademicRequirementsByStructureAsync(structure.Id);

        // Assert
        reqsResult.IsSuccess.Should().BeTrue();
        reqsResult.Value.Should().NotBeNull();
        reqsResult.Value.Should().HaveCount(2);

        reqsResult.Value.Should().Contain(r => r.Id == req1.Id && r.RequirementScope == "program" && r.RequirementType == "graduation");
        reqsResult.Value.Should().Contain(r => r.Id == req2.Id && r.RequirementScope == "learning_unit" && r.LearningUnitId == unit.Id);
    }
}
