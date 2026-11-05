namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Academics.Features.AcademicRequirements.UpdateAcademicRequirement;
using Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructureById;
using Elsasa.Sis.Academics.Features.LearningStructures.UpdateLearningStructure;
using Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnitById;
using Elsasa.Sis.Academics.Features.LearningUnits.UpdateLearningUnit;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

/// <summary>
/// End-to-end integration tests for Controlled In-Place Edit & Immutability Guards (IS008, P4-S13, GAP-006, OQ-005, TD-06).
/// Verifies metadata updates succeed on active frameworks and units, while structural alterations or deletions
/// are strictly rejected with 409 Conflict.
/// </summary>
public sealed class ImmutabilityGuardTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _structuresUri = "/api/v1/academics/learning-structures";
    private const string _unitsUri = "/api/v1/academics/learning-units";
    private const string _requirementsUri = "/api/v1/academics/academic-requirements";

    [SkippableFact]
    public async Task MetadataUpdates_Should_SucceedOnActiveStructureAndUnit()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "IMMUT-META");

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Active Metadata Framework",
            version: "1.0");

        var rootUnit = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "HIST101",
            name: "World History",
            unitType: "course",
            creditWeight: 3.0m);

        var req = await AcademicsIntegrationTestHelpers.CreateRequirementAsync(
            Client,
            structure.Id,
            name: "History Core",
            scope: "program",
            type: "completion",
            description: "Complete World History");

        // Activate the structure
        await Client.PostAsync($"{_structuresUri}/{structure.Id}/submit", null);
        await Client.PostAsync($"{_structuresUri}/{structure.Id}/approve", null);

        // 1. Update structure metadata (name, description, notes) -> 200 OK
        var updateStructureRequest = new UpdateLearningStructureRequest(
            Name: "World History & Civilization Updated",
            Description: "Updated description for active framework",
            Notes: "Updated operational notes");

        var updateStructureResponse = await Client.PutAsJsonAsync($"{_structuresUri}/{structure.Id}", updateStructureRequest);
        updateStructureResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var structureDetail = await (await Client.GetAsync($"{_structuresUri}/{structure.Id}")).Content.ReadFromJsonAsync<LearningStructureDetailResponse>();
        structureDetail.Should().NotBeNull();
        structureDetail!.Name.Should().Be("World History & Civilization Updated");
        structureDetail.Description.Should().Be("Updated description for active framework");

        // 2. Update unit metadata (name, description, notes, credit_weight, level) keeping structural fields intact -> 200 OK
        var updateUnitRequest = new UpdateLearningUnitRequest(
            Code: "HIST101", // unchanged structural field
            Name: "Global History & Heritage", // updated metadata
            UnitType: "course", // unchanged structural field
            ParentUnitId: null, // unchanged structural field
            Level: "Grade 11", // updated metadata
            Description: "Updated global history description",
            CreditWeight: 3.5m, // updated metadata
            Notes: "Updated syllabus notes");

        var updateUnitResponse = await Client.PutAsJsonAsync($"{_unitsUri}/{rootUnit.Id}", updateUnitRequest);
        updateUnitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var unitDetail = await (await Client.GetAsync($"{_unitsUri}/{rootUnit.Id}")).Content.ReadFromJsonAsync<LearningUnitDetailResponse>();
        unitDetail.Should().NotBeNull();
        unitDetail!.Name.Should().Be("Global History & Heritage");
        unitDetail.CreditWeight.Should().Be(3.5m);
        unitDetail.Level.Should().Be("Grade 11");

        // 3. Update requirement metadata (name, description, notes) keeping scope/type intact -> 200 OK
        var updateReqRequest = new UpdateAcademicRequirementRequest(
            Name: "History Core Updated",
            RequirementScope: "program", // unchanged structural field
            RequirementType: "completion", // unchanged structural field
            Description: "Updated requirement text",
            LearningUnitId: null,
            Level: null,
            Notes: "Updated requirement notes");

        var updateReqResponse = await Client.PutAsJsonAsync($"{_requirementsUri}/{req.Id}", updateReqRequest);
        updateReqResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task StructuralUpdatesAndDeletions_Should_FailWithConflict_OnActiveFramework()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "IMMUT-STRUCT");

        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Active Lock Framework",
            version: "1.0");

        var parentUnit = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "ART100",
            name: "Studio Art",
            unitType: "course");

        var childUnit = await AcademicsIntegrationTestHelpers.CreateChildUnitAsync(
            Client,
            structure.Id,
            parentUnitId: parentUnit.Id,
            code: "ART101",
            name: "Drawing & Painting",
            unitType: "module");

        var req = await AcademicsIntegrationTestHelpers.CreateRequirementAsync(
            Client,
            structure.Id,
            name: "Art Requirement",
            scope: "program",
            type: "credit",
            description: "Earn art credits");

        // Activate framework
        await Client.PostAsync($"{_structuresUri}/{structure.Id}/submit", null);
        await Client.PostAsync($"{_structuresUri}/{structure.Id}/approve", null);

        // 1. Attempt to mutate unit code on active structure -> 409 Conflict
        var mutateCodeRequest = new UpdateLearningUnitRequest(
            Code: "ART100-MUTATED", // mutated structural code
            Name: parentUnit.Name,
            UnitType: "course",
            ParentUnitId: null);

        var mutateCodeResponse = await Client.PutAsJsonAsync($"{_unitsUri}/{parentUnit.Id}", mutateCodeRequest);
        mutateCodeResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var mutateCodeProblem = await mutateCodeResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        mutateCodeProblem.Should().NotBeNull();
        mutateCodeProblem!.Extensions.Should().ContainKey("errorCode");
        mutateCodeProblem.Extensions["errorCode"]!.ToString().Should().Be("Academic.ImmutableStructuralData");

        // 2. Attempt to mutate unit_type on active structure -> 409 Conflict
        var mutateTypeRequest = new UpdateLearningUnitRequest(
            Code: "ART100",
            Name: parentUnit.Name,
            UnitType: "module", // mutated unit_type
            ParentUnitId: null);

        var mutateTypeResponse = await Client.PutAsJsonAsync($"{_unitsUri}/{parentUnit.Id}", mutateTypeRequest);
        mutateTypeResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var mutateTypeProblem = await mutateTypeResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        mutateTypeProblem!.Extensions["errorCode"]!.ToString().Should().Be("Academic.ImmutableStructuralData");

        // 3. Attempt to mutate parent_unit_id on active structure -> 409 Conflict
        var mutateParentRequest = new UpdateLearningUnitRequest(
            Code: "ART101",
            Name: childUnit.Name,
            UnitType: "module",
            ParentUnitId: null); // mutated parent linkage

        var mutateParentResponse = await Client.PutAsJsonAsync($"{_unitsUri}/{childUnit.Id}", mutateParentRequest);
        mutateParentResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var mutateParentProblem = await mutateParentResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        mutateParentProblem!.Extensions["errorCode"]!.ToString().Should().Be("Academic.ImmutableStructuralData");

        // 4. Attempt to mutate requirement scope on active structure -> 409 Conflict
        var mutateReqScopeRequest = new UpdateAcademicRequirementRequest(
            Name: "Art Requirement",
            RequirementScope: "level", // mutated scope
            RequirementType: "credit",
            Description: "Earn art credits",
            Level: "Grade 10");

        var mutateReqScopeResponse = await Client.PutAsJsonAsync($"{_requirementsUri}/{req.Id}", mutateReqScopeRequest);
        mutateReqScopeResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var mutateReqScopeProblem = await mutateReqScopeResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        mutateReqScopeProblem!.Extensions["errorCode"]!.ToString().Should().Be("Academics.ImmutableStructuralData");

        // 5. Attempt to delete unit on active structure -> 409 Conflict
        var deleteUnitResponse = await Client.DeleteAsync($"{_unitsUri}/{childUnit.Id}");
        deleteUnitResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var deleteUnitProblem = await deleteUnitResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        deleteUnitProblem!.Extensions["errorCode"]!.ToString().Should().Be("Academic.ImmutableStructuralData");

        // 6. Attempt to delete requirement on active structure -> 409 Conflict
        var deleteReqResponse = await Client.DeleteAsync($"{_requirementsUri}/{req.Id}");
        deleteReqResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var deleteReqProblem = await deleteReqResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        deleteReqProblem!.Extensions["errorCode"]!.ToString().Should().Be("Academics.ImmutableAcademicRequirement");
    }
}
