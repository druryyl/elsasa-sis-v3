namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Academics.Features.LearningUnits.CreateLearningUnit;
using Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnits;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

/// <summary>
/// End-to-end integration tests for 2-Level Unit Composition & Invariant Rules (IS008, P4-S13, GAP-004, OQ-002, TD-04).
/// Verifies root unit addition, child unit addition, hierarchy depth capping, cross-structure parent rejection,
/// duplicate unit code constraint, and parent deletion guard.
/// </summary>
public sealed class TwoLevelUnitCompositionTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _structuresUri = "/api/v1/academics/learning-structures";
    private const string _unitsUri = "/api/v1/academics/learning-units";

    [SkippableFact]
    public async Task TwoLevelComposition_Should_AllowRootAndChild_And_QueryFlatListForTreeProjection()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "COMP-VALID");

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Composition Valid Framework",
            version: "1.0");

        // 1. Add Root Unit (ParentUnitId = null)
        var rootUnit = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "ENG100",
            name: "English Literature",
            unitType: "subject",
            creditWeight: 5.0m);

        rootUnit.ParentUnitId.Should().BeNull();
        rootUnit.Code.Should().Be("ENG100");

        // 2. Add Child Unit under Root Unit
        var childUnit = await AcademicsIntegrationTestHelpers.CreateChildUnitAsync(
            Client,
            structure.Id,
            parentUnitId: rootUnit.Id,
            code: "ENG101-POETRY",
            name: "Romantic Poetry Module",
            unitType: "module",
            creditWeight: 2.5m);

        childUnit.ParentUnitId.Should().Be(rootUnit.Id);
        childUnit.Code.Should().Be("ENG101-POETRY");

        // 3. Query all units for structure via GET /api/v1/academics/learning-structures/{structureId}/units
        var getUnitsResponse = await Client.GetAsync($"{_structuresUri}/{structure.Id}/units");
        getUnitsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var units = await getUnitsResponse.Content.ReadFromJsonAsync<IReadOnlyList<LearningUnitItemDto>>();
        units.Should().NotBeNull();
        units!.Should().HaveCount(2);

        var queriedRoot = units!.Single(u => u.Id == rootUnit.Id);
        var queriedChild = units!.Single(u => u.Id == childUnit.Id);

        queriedRoot.ParentUnitId.Should().BeNull();
        queriedChild.ParentUnitId.Should().Be(rootUnit.Id);
    }

    [SkippableFact]
    public async Task AddChildUnit_Should_FailWithHierarchyDepthExceeded_When_ParentIsAlreadyAChild()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "DEPTH-EXCEEDED");

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Hierarchy Cap Framework",
            version: "1.0");

        // Level 0: Root Unit
        var root = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "SCI-ROOT",
            name: "Science Core",
            unitType: "course");

        // Level 1: Child Unit
        var child = await AcademicsIntegrationTestHelpers.CreateChildUnitAsync(
            Client,
            structure.Id,
            parentUnitId: root.Id,
            code: "SCI-CHILD",
            name: "Chemistry Module",
            unitType: "module");

        // Level 2 Attempt: Try to create sub-child under Level 1 child
        var subChildRequest = new CreateLearningUnitRequest(
            Code: "SCI-SUBCHILD",
            Name: "Organic Chemistry Subsection",
            UnitType: "competency",
            ParentUnitId: child.Id);

        // Act
        var response = await Client.PostAsJsonAsync($"{_structuresUri}/{structure.Id}/units", subChildRequest);

        // Assert: 422 UnprocessableEntity with Academic.HierarchyDepthExceeded
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be("Academic.HierarchyDepthExceeded");
    }

    [SkippableFact]
    public async Task AddChildUnit_Should_FailWithCrossStructureParent_When_ParentBelongsToDifferentStructure()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "CROSS-STRUCT");

        var structure1 = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Structure 1",
            version: "1.0");

        var structure2 = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Structure 2",
            version: "1.0");

        // Root unit belonging to Structure 1
        var structure1Root = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure1.Id,
            code: "STR1-ROOT",
            name: "Structure 1 Root Course");

        // Attempt to create a unit in Structure 2 pointing to parent in Structure 1
        var crossRequest = new CreateLearningUnitRequest(
            Code: "STR2-CHILD",
            Name: "Structure 2 Attempted Child",
            UnitType: "module",
            ParentUnitId: structure1Root.Id);

        // Act
        var response = await Client.PostAsJsonAsync($"{_structuresUri}/{structure2.Id}/units", crossRequest);

        // Assert: 422 UnprocessableEntity with Academic.CrossStructureParent
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be("Academic.CrossStructureParent");
    }

    [SkippableFact]
    public async Task AddUnit_Should_FailWithConflict_When_CodeIsDuplicateWithinSameStructure()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "DUP-CODE");

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Duplicate Code Test Framework",
            version: "1.0");

        await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "UNIQUE-CODE",
            name: "Initial Unit");

        // Duplicate code request
        var duplicateRequest = new CreateLearningUnitRequest(
            Code: "UNIQUE-CODE",
            Name: "Duplicate Code Unit",
            UnitType: "subject");

        // Act
        var response = await Client.PostAsJsonAsync($"{_structuresUri}/{structure.Id}/units", duplicateRequest);

        // Assert: 409 Conflict with Academic.DuplicateUnitCode
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be("Academic.DuplicateUnitCode");
    }

    [SkippableFact]
    public async Task DeleteUnit_Should_FailWithConflict_When_UnitHasChildren()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "DEL-PARENT");

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Delete Parent Framework",
            version: "1.0");

        var parent = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "PARENT-DEL",
            name: "Parent Unit");

        await AcademicsIntegrationTestHelpers.CreateChildUnitAsync(
            Client,
            structure.Id,
            parentUnitId: parent.Id,
            code: "CHILD-DEL",
            name: "Child Unit");

        // Act: Attempt to delete parent unit while child exists
        var deleteResponse = await Client.DeleteAsync($"{_unitsUri}/{parent.Id}");

        // Assert: 409 Conflict with Academic.UnitHasChildren
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await deleteResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be("Academic.UnitHasChildren");
    }
}
