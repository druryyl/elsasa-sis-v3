namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Text.Json;
using Elsasa.Sis.Academics;
using Elsasa.Sis.Academics.Features.AcademicResults.ApproveSummativeResults;
using Elsasa.Sis.Academics.Features.AcademicResults.EvaluateStudentLearning;
using Elsasa.Sis.Academics.Features.AcademicResults.GetResultsByAssessment;
using Elsasa.Sis.Academics.Features.AcademicResults.GetStudentResultsWorkspace;
using Elsasa.Sis.Academics.Features.AcademicResults.PublishAcademicResults;
using Elsasa.Sis.Academics.Features.AssessmentEvidence.GetEvidenceByAssessment;
using Elsasa.Sis.Academics.Features.AssessmentEvidence.RecordEvidence;
using Elsasa.Sis.Academics.Features.Assessments.CancelAssessment;
using Elsasa.Sis.Academics.Features.Assessments.ConductAssessment;
using Elsasa.Sis.Academics.Features.Assessments.CreateAssessment;
using Elsasa.Sis.Academics.Features.Assessments.GetAssessmentById;
using Elsasa.Sis.Academics.Features.Assessments.GetAssessmentsByUnit;
using Elsasa.Sis.Academics.Features.Assessments.UpdateAssessment;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;
using Elsasa.Sis.Academics.Features.RecordCorrections.GetResultCorrectionHistory;
using Elsasa.Sis.Academics.Features.ReportCards.ApproveReportCard;
using Elsasa.Sis.Academics.Features.ReportCards.BatchCompileCohortReportCards;
using Elsasa.Sis.Academics.Features.ReportCards.CompileReportCard;
using Elsasa.Sis.Academics.Features.ReportCards.GetReportCardById;
using Elsasa.Sis.Academics.Features.ReportCards.GetStudentReportCards;
using Elsasa.Sis.Academics.Features.ReportCards.PublishReportCard;
using Elsasa.Sis.Academics.Features.Transcripts.GenerateInterimTranscript;
using Elsasa.Sis.Academics.Features.Transcripts.GetStudentTranscript;
using Elsasa.Sis.Academics.Features.Transcripts.GetTranscriptById;
using Elsasa.Sis.Academics.Features.Transcripts.IssueTranscript;
using Elsasa.Sis.Academics.Features.AcademicAchievements.ConferAcademicAchievement;
using Elsasa.Sis.Academics.Features.AcademicAchievements.GetPeriodAchievements;
using Elsasa.Sis.Academics.Features.AcademicAchievements.GetStudentAchievements;
using Elsasa.Sis.Academics.Features.AcademicInterventions.GetActiveInterventions;
using Elsasa.Sis.Academics.Features.AcademicInterventions.GetAtRiskLearners;
using Elsasa.Sis.Academics.Features.AcademicInterventions.GetInterventionById;
using Elsasa.Sis.Academics.Features.AcademicInterventions.InitiateAcademicIntervention;
using Elsasa.Sis.Academics.Features.AcademicInterventions.UpdateInterventionStatus;
using Elsasa.Sis.Academics.Features.AcademicStandings.BatchDetermineCohortStanding;
using Elsasa.Sis.Academics.Features.AcademicStandings.DetermineAcademicStanding;
using Elsasa.Sis.Academics.Features.AcademicStandings.GetCohortStandings;
using Elsasa.Sis.Academics.Features.AcademicStandings.GetStudentStandings;
using Elsasa.Sis.Academics.Features.CohortReview;
using Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortProgression;
using Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortStanding;
using Elsasa.Sis.Academics.Features.PromotionRecommendations.GetCohortPromotionRecommendations;
using Elsasa.Sis.Academics.Features.PromotionRecommendations.GetStudentPromotionRecommendations;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectAcademicStanding;
using Elsasa.Sis.Academics.Features.RecordCorrections.CorrectPromotionRecommendation;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.AddAcademicPlanAdjustment;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.AddSlpLearningUnit;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ApproveStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ArchiveStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.CreateStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.GetActiveStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlanById;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlans;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.RemoveAcademicPlanAdjustment;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.RemoveSlpLearningUnit;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ReviseStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.SubmitStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.SyncSlpParticipations;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.UpdateStudentLearningPlan;
using Elsasa.Sis.Academics.Services;
using Elsasa.Sis.Academics.Services.Reassessment;
using Elsasa.Sis.Api.Configuration;
using Elsasa.Sis.Contracts;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class AcademicsModuleHostIntegrationTests
{
    private sealed class FaultyConnectionFactory : ISqlConnectionFactory
    {
        public ValueTask<System.Data.Common.DbConnection> CreateConnectionAsync(CancellationToken ct = default)
        {
            throw new InvalidOperationException("Simulated connection failure.");
        }
    }

    private sealed class TestOperationsCalendarQueryService : IOperationsCalendarQueryService
    {
        public Task<Result<ActiveAcademicYearDto?>> GetActiveAcademicYearAsync(CancellationToken ct = default) =>
            Task.FromResult(Result.Success<ActiveAcademicYearDto?>(null));

        public Task<Result<OperationalPeriodDto?>> GetOperationalPeriodByIdAsync(Guid periodId, CancellationToken ct = default) =>
            Task.FromResult(Result.Success<OperationalPeriodDto?>(new OperationalPeriodDto(
                periodId,
                Guid.NewGuid(),
                "SEM-1-2029-2030",
                "Semester 1 2029/2030",
                "academic_semester",
                new DateOnly(2029, 7, 15),
                new DateOnly(2029, 12, 20),
                null,
                "active")));

        public Task<Result<IReadOnlyList<OperationalPeriodDto>>> GetActiveAdmissionWindowsAsync(DateOnly date, CancellationToken ct = default) =>
            Task.FromResult(Result.Success<IReadOnlyList<OperationalPeriodDto>>(Array.Empty<OperationalPeriodDto>()));

        public Task<Result<bool>> IsInstructionalDayAsync(DateOnly date, string campus = "All", CancellationToken ct = default) =>
            Task.FromResult(Result.Success(true));
    }

    [Fact]
    public void AddAcademicsDomain_Should_RegisterAllSubmodule3ServicesAndHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISqlConnectionFactory>(new FaultyConnectionFactory());
        services.AddScoped<ICurrentUser, TestCurrentUser>();
        services.AddSingleton<IStudentsModule>(new TestStudentsModule());
        services.AddSingleton<IOperationsCalendarQueryService>(new TestOperationsCalendarQueryService());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(AcademicsAssemblyMarker).Assembly,
            typeof(ContractsAssemblyMarker).Assembly));

        // Act
        services.AddAcademicsDomain();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        // Assert - Module & Domain Services
        sp.GetService<IAcademicsModule>().Should().NotBeNull().And.BeOfType<AcademicsModule>();
        sp.GetService<IReassessmentPolicyEvaluator>().Should().NotBeNull().And.BeOfType<ReassessmentPolicyEvaluator>();

        // Assert - Assessments Feature Handlers
        sp.GetService<CreateAssessmentHandler>().Should().NotBeNull();
        sp.GetService<UpdateAssessmentHandler>().Should().NotBeNull();
        sp.GetService<ConductAssessmentHandler>().Should().NotBeNull();
        sp.GetService<CancelAssessmentHandler>().Should().NotBeNull();
        sp.GetService<GetAssessmentsByUnitHandler>().Should().NotBeNull();
        sp.GetService<GetAssessmentByIdHandler>().Should().NotBeNull();

        // Assert - Evidence Handlers
        sp.GetService<RecordEvidenceHandler>().Should().NotBeNull();
        sp.GetService<GetEvidenceByAssessmentHandler>().Should().NotBeNull();

        // Assert - Academic Results Handlers
        sp.GetService<EvaluateStudentLearningHandler>().Should().NotBeNull();
        sp.GetService<ApproveSummativeResultsHandler>().Should().NotBeNull();
        sp.GetService<PublishAcademicResultsHandler>().Should().NotBeNull();
        sp.GetService<GetResultsByAssessmentHandler>().Should().NotBeNull();
        sp.GetService<GetStudentResultsWorkspaceHandler>().Should().NotBeNull();

        // Assert - Record Corrections Handlers
        sp.GetService<CorrectApprovedResultHandler>().Should().NotBeNull();
        sp.GetService<GetResultCorrectionHistoryHandler>().Should().NotBeNull();

        // Assert - Report Cards Handlers
        sp.GetService<CompileReportCardHandler>().Should().NotBeNull();
        sp.GetService<BatchCompileCohortReportCardsHandler>().Should().NotBeNull();
        sp.GetService<ApproveReportCardHandler>().Should().NotBeNull();
        sp.GetService<PublishReportCardHandler>().Should().NotBeNull();
        sp.GetService<GetReportCardByIdHandler>().Should().NotBeNull();
        sp.GetService<GetStudentReportCardsHandler>().Should().NotBeNull();

        // Assert - Transcripts Handlers
        sp.GetService<GenerateInterimTranscriptHandler>().Should().NotBeNull();
        sp.GetService<IssueTranscriptHandler>().Should().NotBeNull();
        sp.GetService<GetStudentTranscriptHandler>().Should().NotBeNull();
        sp.GetService<GetTranscriptByIdHandler>().Should().NotBeNull();

        // Assert - Submodule 4: Learning Progress Handlers (P2-S04, P2-S05)
        sp.GetService<Elsasa.Sis.Academics.Features.LearningProgress.RecordLearningProgress.RecordLearningProgressHandler>().Should().NotBeNull();
        sp.GetService<Elsasa.Sis.Academics.Features.LearningProgress.ConcludeParticipation.ConcludeParticipationHandler>().Should().NotBeNull();
        sp.GetService<Elsasa.Sis.Academics.Features.LearningProgress.GetStudentLearningProgress.GetStudentLearningProgressHandler>().Should().NotBeNull();
        sp.GetService<Elsasa.Sis.Academics.Features.LearningProgress.GetUnitLearningProgress.GetUnitLearningProgressHandler>().Should().NotBeNull();

        // Assert - Submodule 4: Cohort Review & Standings Handlers (P3-S06, P3-S07, P3-S09)
        sp.GetService<ThresholdRuleEvaluator>().Should().NotBeNull();
        sp.GetService<EvaluateCohortStandingHandler>().Should().NotBeNull();
        sp.GetService<EvaluateCohortProgressionHandler>().Should().NotBeNull();
        sp.GetService<DetermineAcademicStandingHandler>().Should().NotBeNull();
        sp.GetService<BatchDetermineCohortStandingHandler>().Should().NotBeNull();
        sp.GetService<GetStudentStandingsHandler>().Should().NotBeNull();
        sp.GetService<GetCohortStandingsHandler>().Should().NotBeNull();

        // Assert - Submodule 4: Promotion Recommendations Handlers (P3-S08, P3-S09)
        sp.GetService<Elsasa.Sis.Academics.Features.PromotionRecommendations.IssuePromotionRecommendation.IssuePromotionRecommendationHandler>().Should().NotBeNull();
        sp.GetService<Elsasa.Sis.Academics.Features.PromotionRecommendations.BatchIssuePromotionRecommendations.BatchIssuePromotionRecommendationsHandler>().Should().NotBeNull();
        sp.GetService<GetStudentPromotionRecommendationsHandler>().Should().NotBeNull();
        sp.GetService<GetCohortPromotionRecommendationsHandler>().Should().NotBeNull();

        // Assert - Submodule 4: Academic Interventions Handlers (P4-S10)
        sp.GetService<GetAtRiskLearnersHandler>().Should().NotBeNull();
        sp.GetService<InitiateAcademicInterventionHandler>().Should().NotBeNull();
        sp.GetService<UpdateInterventionStatusHandler>().Should().NotBeNull();
        sp.GetService<GetActiveInterventionsHandler>().Should().NotBeNull();
        sp.GetService<GetInterventionByIdHandler>().Should().NotBeNull();

        // Assert - Submodule 4: Academic Achievements Handlers (P4-S11)
        sp.GetService<ConferAcademicAchievementHandler>().Should().NotBeNull();
        sp.GetService<GetStudentAchievementsHandler>().Should().NotBeNull();
        sp.GetService<GetPeriodAchievementsHandler>().Should().NotBeNull();

        // Assert - Submodule 4: Non-Grade Corrections Handlers (P5-S12)
        sp.GetService<CorrectAcademicStandingHandler>().Should().NotBeNull();
        sp.GetService<CorrectPromotionRecommendationHandler>().Should().NotBeNull();

        // Assert - Submodule 5: Student Learning Plans Handlers (P2-S04, P2-S05, P2-S06, P3-S07, P3-S08, P3-S09, P4-S10, P4-S11)
        sp.GetService<CreateStudentLearningPlanHandler>().Should().NotBeNull();
        sp.GetService<UpdateStudentLearningPlanHandler>().Should().NotBeNull();
        sp.GetService<ArchiveStudentLearningPlanHandler>().Should().NotBeNull();
        sp.GetService<AddSlpLearningUnitHandler>().Should().NotBeNull();
        sp.GetService<RemoveSlpLearningUnitHandler>().Should().NotBeNull();
        sp.GetService<AddAcademicPlanAdjustmentHandler>().Should().NotBeNull();
        sp.GetService<RemoveAcademicPlanAdjustmentHandler>().Should().NotBeNull();
        sp.GetService<SubmitStudentLearningPlanHandler>().Should().NotBeNull();
        sp.GetService<ApproveStudentLearningPlanHandler>().Should().NotBeNull();
        sp.GetService<ReviseStudentLearningPlanHandler>().Should().NotBeNull();
        sp.GetService<SyncSlpParticipationsHandler>().Should().NotBeNull();
        sp.GetService<GetStudentLearningPlanByIdHandler>().Should().NotBeNull();
        sp.GetService<GetStudentLearningPlansHandler>().Should().NotBeNull();
        sp.GetService<GetActiveStudentLearningPlanHandler>().Should().NotBeNull();
    }

    [Fact]
    public void MapAcademicsEndpoints_Should_RegisterEndpointsWithoutErrors()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddLogging();
        builder.Services.AddAcademicsDomain();
        var app = builder.Build();

        // Act & Assert
        var act = () => app.MapAcademicsEndpoints();
        act.Should().NotThrow();

        var endpointSources = ((IEndpointRouteBuilder)app).DataSources;
        endpointSources.Should().NotBeEmpty();

        var endpoints = endpointSources.SelectMany(ds => ds.Endpoints).ToList();
        endpoints.Should().NotBeEmpty();

        // Verify representative endpoints are mapped across all submodules
        var routePatterns = endpoints
            .Select(e => (e as RouteEndpoint)?.RoutePattern.RawText)
            .Where(r => r is not null)
            .ToList();

        routePatterns.Should().Contain(p => p!.Contains("academics/assessments"));
        routePatterns.Should().Contain(p => p!.Contains("evidence"));
        routePatterns.Should().Contain(p => p!.Contains("results"));
        routePatterns.Should().Contain(p => p!.Contains("academic-results") && p!.Contains("correct"));
        routePatterns.Should().Contain(p => p!.Contains("report-cards/compile"));
        routePatterns.Should().Contain(p => p!.Contains("transcripts/generate"));
        routePatterns.Should().Contain(p => p!.Contains("portal/learning/results"));

        // Submodule 4 route patterns
        routePatterns.Should().Contain(p => p!.Contains("learning-progress"));
        routePatterns.Should().Contain(p => p!.Contains("academic-standings"));
        routePatterns.Should().Contain(p => p!.Contains("promotion-recommendations"));
        routePatterns.Should().Contain(p => p!.Contains("interventions"));
        routePatterns.Should().Contain(p => p!.Contains("achievements"));
        routePatterns.Should().Contain(p => p!.Contains("cohort-review"));

        // Submodule 5 route patterns (P4-S11)
        routePatterns.Should().Contain(p => p!.Contains("learning-plans"));
    }

    [Fact]
    public async Task WebApiHost_Should_GenerateValidSwaggerSchema_IncludingAcademicsEndpoints()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddLogging();
        builder.Services.AddSwaggerDocumentation();
        builder.Services.AddAcademicsDomain();

        var app = builder.Build();
        app.MapAcademicsEndpoints();
        await app.StartAsync();

        try
        {
            // Act
            var swaggerGenerator = app.Services.GetRequiredService<ISwaggerProvider>();
            var swaggerDoc = swaggerGenerator.GetSwagger("v1");

            // Assert
            swaggerDoc.Should().NotBeNull();
            swaggerDoc.Paths.Should().NotBeEmpty();

            // Verify Academics paths in Swagger doc across submodules
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/assessments"));
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/report-cards"));
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/transcripts"));

            // Submodule 4 routes in Swagger
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/learning-progress"));
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/academic-standings"));
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/promotion-recommendations"));
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/interventions"));
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/achievements"));
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/cohort-review"));

            // Submodule 5 routes in Swagger (P4-S11)
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/learning-plans"));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AcademicsModule_GetStudentApprovedResultsAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new AcademicsModule(new FaultyConnectionFactory());

        // Act
        var result = await module.GetStudentApprovedResultsAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.StudentApprovedResultsQueryFailed");
    }

    [Fact]
    public async Task AcademicsModule_GetApplicantAssessmentScoreAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new AcademicsModule(new FaultyConnectionFactory());

        // Act
        var result = await module.GetApplicantAssessmentScoreAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.ApplicantAssessmentScoreQueryFailed");
    }

    [Fact]
    public async Task AcademicsModule_GetStudentReportCardSnapshotAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new AcademicsModule(new FaultyConnectionFactory());

        // Act
        var result = await module.GetStudentReportCardSnapshotAsync(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.ReportCardSnapshotQueryFailed");
    }

    [Fact]
    public async Task AcademicsModule_GetStudentAcademicStandingAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new AcademicsModule(new FaultyConnectionFactory());

        // Act
        var result = await module.GetStudentAcademicStandingAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.StudentAcademicStandingQueryFailed");
    }

    [Fact]
    public async Task AcademicsModule_GetPromotionRecommendationAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new AcademicsModule(new FaultyConnectionFactory());

        // Act
        var result = await module.GetPromotionRecommendationAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.PromotionRecommendationQueryFailed");
    }

    [Fact]
    public async Task AcademicsModule_GetStudentAcademicAchievementsAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new AcademicsModule(new FaultyConnectionFactory());

        // Act
        var result = await module.GetStudentAcademicAchievementsAsync(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.StudentAcademicAchievementsQueryFailed");
    }

    [Fact]
    public async Task AcademicsModule_GetStudentLearningProgressSummaryAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new AcademicsModule(new FaultyConnectionFactory());

        // Act
        var result = await module.GetStudentLearningProgressSummaryAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.StudentLearningProgressSummaryQueryFailed");
    }

    [Fact]
    public async Task AcademicsModule_GetActiveStudentLearningPlanAsync_Should_ReturnFailure_When_DatabaseThrows()
    {
        // Arrange
        var module = new AcademicsModule(new FaultyConnectionFactory());

        // Act
        var result = await module.GetActiveStudentLearningPlanAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Academics.ActiveStudentLearningPlanQueryFailed");
    }

    [Fact]
    public async Task AcademicsModule_GetActiveStudentLearningPlanAsync_Should_ReturnSuccessNull_When_IdsAreEmpty()
    {
        // Arrange
        var module = new AcademicsModule(new FaultyConnectionFactory());

        // Act
        var result = await module.GetActiveStudentLearningPlanAsync(Guid.Empty, Guid.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void ReportCardSnapshotDto_RoundTripSerialization_Should_PreserveAllFields()
    {
        // Arrange
        var snapshot = new ReportCardSnapshotDto(
            ReportCardId: Guid.NewGuid(),
            StudentId: Guid.NewGuid(),
            StudentName: "Ahmad Dahlan",
            StudentNisn: "0012345678",
            AcademicPeriodId: Guid.NewGuid(),
            PeriodLabel: "Semester 1 2026/2027",
            ReportType: "end_of_term",
            TemplateName: "Buku Rapor Merdeka",
            Status: "approved",
            ApprovedBy: "coordinator@school.id",
            ApprovedDate: DateTime.UtcNow,
            NarrativeComments: "Sangat berprestasi dan aktif.",
            SubjectEntries:
            [
                new ReportCardSubjectEntryDto(
                    LearningUnitId: Guid.NewGuid(),
                    SubjectCode: "MATH-01",
                    SubjectTitle: "Matematika",
                    ScoreNumeric: 88.50m,
                    GradeLetter: "A",
                    GradeLabel: "Sangat Mahir",
                    TeacherComment: "Pemahaman konsep sangat baik.",
                    DisplayOrder: 1)
            ],
            AttendanceSummary: new ReportCardAttendanceSummaryDto(
                PresentDays: 90,
                AbsentDays: 0,
                LateDays: 2,
                ExcusedDays: 1));

        var json = JsonSerializer.Serialize(snapshot, _jsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<ReportCardSnapshotDto>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ReportCardId.Should().Be(snapshot.ReportCardId);
        deserialized.StudentName.Should().Be("Ahmad Dahlan");
        deserialized.SubjectEntries.Should().HaveCount(1);
        deserialized.SubjectEntries[0].SubjectCode.Should().Be("MATH-01");
        deserialized.AttendanceSummary.Should().NotBeNull();
        deserialized.AttendanceSummary!.PresentDays.Should().Be(90);
    }
}
