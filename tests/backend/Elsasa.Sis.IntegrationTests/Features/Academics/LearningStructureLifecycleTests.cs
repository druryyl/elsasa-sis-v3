namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Academics.Features.LearningStructures.ApproveLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.ArchiveLearningStructure;
using Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructureById;
using Elsasa.Sis.Academics.Features.LearningStructures.SubmitLearningStructure;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

/// <summary>
/// End-to-end integration tests for Learning Structure Governance Lifecycles (IS008, P4-S13, ACD-WF-01, TD-06, TD-09).
/// Verifies draft creation -> unit & requirement addition -> submit -> approve lifecycle,
/// Stage 2 Criterion 1 empty framework validation, and domain event publication.
/// </summary>
public sealed class LearningStructureLifecycleTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _structuresUri = "/api/v1/academics/learning-structures";

    [SkippableFact]
    public async Task LearningStructure_Should_TransitionThrough_DraftSubmittedApprovedLifecycle()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "LIFECYCLE-1");

        // 1. Create Draft Learning Structure
        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Science Curriculum Framework",
            version: "1.0",
            effectiveDate: new DateOnly(2026, 9, 1));

        structure.Status.Should().Be("draft");

        // 2. Add 1 Learning Unit and 1 Academic Requirement to satisfy Stage 2 Criterion 1
        var unit = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "PHYS101",
            name: "Introductory Physics",
            unitType: "course");

        unit.Status.Should().Be("draft");

        var requirement = await AcademicsIntegrationTestHelpers.CreateRequirementAsync(
            Client,
            structure.Id,
            name: "Core Physics Requirement",
            scope: "program",
            type: "completion",
            description: "Must complete Introductory Physics with grade C or above.");

        requirement.Status.Should().Be("draft");

        // 3. Submit Structure for review
        var submitResponse = await Client.PostAsync($"{_structuresUri}/{structure.Id}/submit", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitResult = await submitResponse.Content.ReadFromJsonAsync<SubmitLearningStructureResponse>();
        submitResult.Should().NotBeNull();
        submitResult!.Id.Should().Be(structure.Id);
        submitResult.Status.Should().Be("submitted");
        submitResult.UpdatedBy.Should().NotBeNullOrWhiteSpace();

        // 4. Approve Structure
        var approveResponse = await Client.PostAsync($"{_structuresUri}/{structure.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approveResult = await approveResponse.Content.ReadFromJsonAsync<ApproveLearningStructureResponse>();
        approveResult.Should().NotBeNull();
        approveResult!.Id.Should().Be(structure.Id);
        approveResult.Status.Should().Be("active");
        approveResult.ApprovedBy.Should().NotBeNullOrWhiteSpace();
        approveResult.ApprovedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

        // 5. Verify database states across structure, child units, and requirements
        var getByIdResponse = await Client.GetAsync($"{_structuresUri}/{structure.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await getByIdResponse.Content.ReadFromJsonAsync<LearningStructureDetailResponse>();
        detail.Should().NotBeNull();
        detail!.Status.Should().Be("active");
        detail.ApprovedBy.Should().NotBeNullOrWhiteSpace();
        detail.ApprovedDate.Should().NotBeNull();
        detail.UnitCount.Should().Be(1);
        detail.RequirementCount.Should().Be(1);

        const string selectUnitStatusSql = "SELECT status FROM academics.learning_units WHERE id = @Id;";
        var unitStatus = await ExecuteScalarAsync<string>(selectUnitStatusSql, new { Id = unit.Id });
        unitStatus.Should().Be("active");

        const string selectReqStatusSql = "SELECT status FROM academics.academic_requirements WHERE id = @Id;";
        var reqStatus = await ExecuteScalarAsync<string>(selectReqStatusSql, new { Id = requirement.Id });
        reqStatus.Should().Be("active");

        // 6. Verify LearningStructureActivatedEvent was published post-commit
        Factory.EventCollector.LearningStructureActivatedEvents.Should().ContainSingle(evt =>
            evt.LearningStructureId == structure.Id &&
            evt.AcademicPeriodId == periodId &&
            evt.Version == "1.0" &&
            evt.EffectiveDate == new DateOnly(2026, 9, 1));
    }

    [SkippableFact]
    public async Task SubmitLearningStructure_Should_ReturnValidationError_When_FrameworkIsEmpty_Criterion1()
    {
        // Arrange: Framework with 0 units and 0 requirements
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "CRIT1-EMPTY");

        var emptyStructure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Empty Structure Test",
            version: "1.0");

        // Act 1: Submit with 0 units & 0 requirements
        var response1 = await Client.PostAsync($"{_structuresUri}/{emptyStructure.Id}/submit", null);

        // Assert 1: Validation error 422
        response1.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem1 = await response1.Content.ReadFromJsonAsync<ProblemDetails>();
        problem1.Should().NotBeNull();
        problem1!.Extensions.Should().ContainKey("errorCode");
        problem1.Extensions["errorCode"]!.ToString().Should().Be("Academics.EmptyLearningStructure");

        // Act 2: Add 1 unit only (0 requirements)
        await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            emptyStructure.Id,
            code: "UNIT-ONLY",
            name: "Unit Only");

        var response2 = await Client.PostAsync($"{_structuresUri}/{emptyStructure.Id}/submit", null);

        // Assert 2: Still fails Criterion 1 because requirements count is 0
        response2.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem2 = await response2.Content.ReadFromJsonAsync<ProblemDetails>();
        problem2.Should().NotBeNull();
        problem2!.Extensions.Should().ContainKey("errorCode");
        problem2.Extensions["errorCode"]!.ToString().Should().Be("Academics.EmptyLearningStructure");

        // Act 3: Add requirement -> submission should now succeed
        await AcademicsIntegrationTestHelpers.CreateRequirementAsync(
            Client,
            emptyStructure.Id,
            name: "Requirement Now Added",
            scope: "program",
            type: "graduation",
            description: "Graduation rule");

        var response3 = await Client.PostAsync($"{_structuresUri}/{emptyStructure.Id}/submit", null);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task ArchiveLearningStructure_Should_ArchiveFrameworkAndChildren_When_Eligible()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        await using var connection = await OpenConnectionAsync();
        var (_, periodId) = await AcademicsIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, "ARCHIVE-1");

        // 1. Create, populate, submit, and approve a structure whose effective end date is in the past
        var structure = await AcademicsIntegrationTestHelpers.CreateDraftStructureAsync(
            Client,
            periodId,
            name: "Structure to Archive",
            version: "0.9",
            effectiveDate: new DateOnly(2020, 1, 1));

        var unit = await AcademicsIntegrationTestHelpers.CreateRootUnitAsync(
            Client,
            structure.Id,
            code: "OLD-UNIT",
            name: "Old Unit");

        var req = await AcademicsIntegrationTestHelpers.CreateRequirementAsync(
            Client,
            structure.Id,
            name: "Old Requirement",
            scope: "program",
            type: "credit",
            description: "Old rule");

        // Set structure end_date in past via direct DB update to allow archive of active structure
        const string setEndDateSql = """
            UPDATE academics.learning_structures
            SET end_date = '2020-12-31'
            WHERE id = @Id;
            """;
        await ExecuteAsync(setEndDateSql, new { Id = structure.Id });

        await Client.PostAsync($"{_structuresUri}/{structure.Id}/submit", null);
        await Client.PostAsync($"{_structuresUri}/{structure.Id}/approve", null);

        // Act: Archive
        var archiveResponse = await Client.PostAsync($"{_structuresUri}/{structure.Id}/archive", null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var archiveResult = await archiveResponse.Content.ReadFromJsonAsync<ArchiveLearningStructureResponse>();
        archiveResult.Should().NotBeNull();
        archiveResult!.Status.Should().Be("archived");

        // Assert DB status
        const string selectStatusSql = "SELECT status FROM academics.learning_structures WHERE id = @Id;";
        var status = await ExecuteScalarAsync<string>(selectStatusSql, new { Id = structure.Id });
        status.Should().Be("archived");

        const string selectUnitStatusSql = "SELECT status FROM academics.learning_units WHERE id = @Id;";
        var unitStatus = await ExecuteScalarAsync<string>(selectUnitStatusSql, new { Id = unit.Id });
        unitStatus.Should().Be("archived");

        const string selectReqStatusSql = "SELECT status FROM academics.academic_requirements WHERE id = @Id;";
        var reqStatus = await ExecuteScalarAsync<string>(selectReqStatusSql, new { Id = req.Id });
        reqStatus.Should().Be("archived");
    }
}
