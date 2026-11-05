namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Academics.Features.AcademicRequirements.CreateAcademicRequirement;
using Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirements;
using Elsasa.Sis.Academics.Features.LearningStructures.ApproveLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructureById;
using Elsasa.Sis.Academics.Features.LearningStructures.SupersedeLearningStructure;
using Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnits;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

/// <summary>
/// End-to-end integration tests for Deep Clone-on-Supersede Curriculum Versioning (IS008, P4-S13, GAP-003, OQ-001, TD-03).
/// Verifies atomic deep clone into draft framework, GUID remapping of 2-level unit hierarchy and unit-scoped requirements,
/// predecessor non-disruption, and transactional predecessor retirement upon successor approval.
/// </summary>
public sealed class DeepCloneVersioningTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _structuresUri = "/api/v1/academics/learning-structures";

    [SkippableFact]
    public async Task SupersedeLearningStructure_Should_ExecuteDeepClone_RemapPointers_And_RetirePredecessorOnApproval()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "CLONE-FULL");

        // 1. Establish Predecessor V1.0
        var v1 = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Science Curriculum V1",
            version: "1.0",
            effectiveDate: new DateOnly(2026, 8, 1));

        var rootUnitV1 = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            v1.Id,
            code: "BIO101",
            name: "General Biology",
            unitType: "course",
            creditWeight: 4.0m);

        var childUnitV1 = await AcademicsIntegrationTestHelpers.CreateChildUnitAsync(
            Client,
            v1.Id,
            parentUnitId: rootUnitV1.Id,
            code: "BIO101-LAB",
            name: "Cellular Biology Lab",
            unitType: "module",
            creditWeight: 1.5m);

        var reqProgramV1 = await AcademicsIntegrationTestHelpers.CreateRequirementAsync(
            Client,
            v1.Id,
            name: "Core Biology Completion",
            scope: "program",
            type: "completion",
            description: "Must complete General Biology");

        // Unit-scoped requirement explicitly linked to childUnitV1
        var reqUnitV1Request = new CreateAcademicRequirementRequest(
            Name: "Lab Safety Certification",
            RequirementScope: "learning_unit",
            LearningUnitId: childUnitV1.Id,
            Level: null,
            RequirementType: "prerequisite",
            Description: "Must pass lab safety quiz prior to wet lab participation.",
            Version: "1.0",
            EffectiveDate: new DateOnly(2026, 7, 1));

        var reqUnitResponse = await Client.PostAsJsonAsync($"{_structuresUri}/{v1.Id}/requirements", reqUnitV1Request);
        reqUnitResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var reqUnitV1 = await reqUnitResponse.Content.ReadFromJsonAsync<CreateAcademicRequirementResponse>();
        reqUnitV1.Should().NotBeNull();

        // Submit & Approve V1 -> transitions to active
        await Client.PostAsync($"{_structuresUri}/{v1.Id}/submit", null);
        var approveV1 = await Client.PostAsync($"{_structuresUri}/{v1.Id}/approve", null);
        approveV1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Supersede V1 to V2
        var newEffectiveDate = new DateOnly(2027, 8, 1);
        var supersedeRequest = new SupersedeStructureRequest(
            NewVersion: "2.0",
            EffectiveDate: newEffectiveDate,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "Semester Academics 2026");

        var supersedeResponse = await Client.PostAsJsonAsync($"{_structuresUri}/{v1.Id}/supersede", supersedeRequest);
        supersedeResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var v2 = await supersedeResponse.Content.ReadFromJsonAsync<LearningStructureDetailResponse>();
        v2.Should().NotBeNull();
        v2!.Id.Should().NotBe(v1.Id);
        v2.Version.Should().Be("2.0");
        v2.Status.Should().Be("draft");
        v2.SupersedesId.Should().Be(v1.Id);
        v2.PredecessorVersion.Should().Be("1.0");
        v2.EffectiveDate.Should().Be(newEffectiveDate);
        v2.UnitCount.Should().Be(2);
        v2.RequirementCount.Should().Be(2);

        // 3. Verify V2 cloned units & remapped hierarchy pointers
        var getUnitsV2Response = await Client.GetAsync($"{_structuresUri}/{v2.Id}/units");
        getUnitsV2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var unitsV2 = await getUnitsV2Response.Content.ReadFromJsonAsync<IReadOnlyList<LearningUnitItemDto>>();
        unitsV2.Should().NotBeNull();
        unitsV2!.Should().HaveCount(2);

        var rootUnitV2 = unitsV2!.Single(u => u.Code == "BIO101");
        var childUnitV2 = unitsV2!.Single(u => u.Code == "BIO101-LAB");

        rootUnitV2.Id.Should().NotBe(rootUnitV1.Id);
        rootUnitV2.ParentUnitId.Should().BeNull();
        rootUnitV2.Status.Should().Be("draft");

        childUnitV2.Id.Should().NotBe(childUnitV1.Id);
        childUnitV2.ParentUnitId.Should().Be(rootUnitV2.Id, "child unit parent_unit_id must be remapped to the cloned root unit ID");
        childUnitV2.Status.Should().Be("draft");

        // 4. Verify V2 cloned requirements & remapped unit pointer
        var getReqsV2Response = await Client.GetAsync($"{_structuresUri}/{v2.Id}/requirements");
        getReqsV2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reqsV2 = await getReqsV2Response.Content.ReadFromJsonAsync<GetAcademicRequirementsResponse>();
        reqsV2.Should().NotBeNull();
        reqsV2!.Requirements.Should().HaveCount(2);

        var clonedUnitReq = reqsV2.Requirements.Single(r => r.Name == "Lab Safety Certification");
        clonedUnitReq.Id.Should().NotBe(reqUnitV1!.Id);
        clonedUnitReq.LearningUnitId.Should().Be(childUnitV2.Id, "unit-scoped requirement must be remapped to the cloned unit ID");
        clonedUnitReq.SupersedesId.Should().Be(reqUnitV1.Id);
        clonedUnitReq.Status.Should().Be("draft");

        // 5. Verify predecessor V1 remains ACTIVE prior to V2 approval (zero downtime)
        const string selectStatusSql = "SELECT status, end_date FROM academics.learning_structures WHERE id = @Id;";
        var v1BeforeApproval = await QuerySingleOrDefaultAsync<dynamic>(selectStatusSql, new { Id = v1.Id });
        ((string)v1BeforeApproval!.status).Should().Be("active");
        ((object?)v1BeforeApproval.end_date).Should().BeNull();

        // 6. Approve V2 -> atomically retires predecessor V1
        var approveV2Response = await Client.PostAsync($"{_structuresUri}/{v2.Id}/approve", null);
        approveV2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Predecessor V1 is now superseded and end_date matches V2 effective date
        var v1AfterApproval = await QuerySingleOrDefaultAsync<dynamic>(selectStatusSql, new { Id = v1.Id });
        ((string)v1AfterApproval!.status).Should().Be("superseded");
        ((DateTime)v1AfterApproval.end_date).Date.Should().Be(new DateTime(2027, 8, 1));

        // Predecessor child units archived
        const string selectUnitStatusSql = "SELECT status FROM academics.learning_units WHERE id = @Id;";
        var v1RootUnitStatus = await ExecuteScalarAsync<string>(selectUnitStatusSql, new { Id = rootUnitV1.Id });
        var v1ChildUnitStatus = await ExecuteScalarAsync<string>(selectUnitStatusSql, new { Id = childUnitV1.Id });
        v1RootUnitStatus.Should().Be("archived");
        v1ChildUnitStatus.Should().Be("archived");

        // Predecessor requirements superseded
        const string selectReqStatusSql = "SELECT status FROM academics.academic_requirements WHERE id = @Id;";
        var v1ReqStatus = await ExecuteScalarAsync<string>(selectReqStatusSql, new { Id = reqUnitV1.Id });
        v1ReqStatus.Should().Be("superseded");
    }

    [SkippableFact]
    public async Task SupersedeLearningStructure_Should_FailWithConflict_When_PredecessorIsNotActive()
    {
        // Arrange: Predecessor is still in draft
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "CLONE-NOTACTIVE");

        var draftStructure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Draft Structure Cannot Supersede",
            version: "1.0");

        var request = new SupersedeStructureRequest(
            NewVersion: "1.1",
            EffectiveDate: new DateOnly(2027, 1, 1));

        // Act
        var response = await Client.PostAsJsonAsync($"{_structuresUri}/{draftStructure.Id}/supersede", request);

        // Assert: 409 Conflict with Academic.InvalidSupersedeStatus
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions.Should().ContainKey("errorCode");
        problem.Extensions["errorCode"]!.ToString().Should().Be("Academic.InvalidSupersedeStatus");
    }
}
