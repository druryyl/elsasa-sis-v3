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
using Elsasa.Sis.Academics.Services;
using Elsasa.Sis.Academics.Services.Reassessment;
using Elsasa.Sis.Api.Configuration;
using Elsasa.Sis.Contracts;
using Elsasa.Sis.Contracts.Academics;
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

    [Fact]
    public void AddAcademicsDomain_Should_RegisterAllSubmodule3ServicesAndHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISqlConnectionFactory>(new FaultyConnectionFactory());
        services.AddScoped<ICurrentUser, TestCurrentUser>();
        services.AddSingleton<IStudentsModule>(new TestStudentsModule());
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

        // Verify representative endpoints are mapped
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

            // Verify Academics paths in Swagger doc
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/assessments"));
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/report-cards"));
            swaggerDoc.Paths.Keys.Should().Contain(k => k.Contains("/api/v1/academics/transcripts"));
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
