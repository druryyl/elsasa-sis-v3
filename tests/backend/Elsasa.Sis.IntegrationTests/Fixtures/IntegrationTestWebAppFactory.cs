namespace Elsasa.Sis.IntegrationTests.Fixtures;

using Elsasa.Sis.Api.Configuration;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> tailored for integration testing (ADR-008).
/// Replaces SQL Server connection string with the container instance and injects test user authentication bypass.
/// </summary>
public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public TestCurrentUser CurrentUser { get; } = new();

    public TestEventCollector EventCollector { get; } = new();

    public TestFinanceModule FinanceModule { get; } = new();

    public TestStudentsModule StudentsModule { get; } = new();

    public SqlServerContainerFixture ContainerFixture { get; }

    public IntegrationTestWebAppFactory()
        : this(new SqlServerContainerFixture())
    {
    }

    internal IntegrationTestWebAppFactory(SqlServerContainerFixture containerFixture)
    {
        ArgumentNullException.ThrowIfNull(containerFixture);
        ContainerFixture = containerFixture;
    }

    public async Task InitializeAsync()
    {
        await ContainerFixture.InitializeAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await ContainerFixture.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        // 1. Replaces DefaultConnection configuration with container connection string
        builder.ConfigureAppConfiguration((_, config) =>
        {
            if (ContainerFixture.IsAvailable && !string.IsNullOrWhiteSpace(ContainerFixture.ConnectionString))
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{DependencyInjection.DefaultConnectionKey}"] = ContainerFixture.ConnectionString,
                });
            }
        });

        // 2. Configure mock/bypass services
        builder.ConfigureTestServices(services =>
        {
            if (ContainerFixture.IsAvailable && !string.IsNullOrWhiteSpace(ContainerFixture.ConnectionString))
            {
                services.RemoveAll<ISqlConnectionFactory>();
                services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(ContainerFixture.ConnectionString));
            }

            // Replace ICurrentUser with test-controlled mutable instance
            services.RemoveAll<ICurrentUser>();
            services.AddSingleton(CurrentUser);
            services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<TestCurrentUser>());

            // Register TestEventCollector for capturing published MediatR domain events
            services.AddSingleton(EventCollector);
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Students.Events.ApplicantAcceptedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Students.Events.StudentEnrolledEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Students.Events.PlacementChangedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Finance.Events.ObligationAssessedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Finance.Events.PaymentConfirmedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.LearningStructureActivatedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AcademicParticipationRegisteredEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.ParticipationConcludedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.LearningActivityConductedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AttendanceRecordedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AttendanceCorrectedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AssignmentReleasedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.CourseworkSubmittedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AcademicResultApprovedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AcademicResultPublishedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AcademicResultCorrectedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.ReportCardPublishedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.TranscriptIssuedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.LearningProgressUpdatedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AcademicStandingDeterminedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.PromotionRecommendationIssuedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AcademicInterventionInitiatedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AcademicInterventionConcludedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AcademicAchievementConferredEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.AcademicStandingCorrectedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.PromotionRecommendationCorrectedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.StudentLearningPlanCreatedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.StudentLearningPlanApprovedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.StudentLearningPlanRevisedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Academics.Events.StudentLearningPlanParticipationsSyncedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeacherProfileCreatedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeacherProfileApprovedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeacherStandingChangedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.QualificationEvidenceVerifiedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeachingAuthorizationGrantedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeachingAuthorizationStatusChangedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeachingRecordCorrectedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeachingDemandReceivedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeachingAssignmentProposedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeachingAssignmentApprovedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeachingAssignmentCancelledEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeachingAssignmentCompletedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.TeachingSubstitutionAssignedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());
            services.AddSingleton<MediatR.INotificationHandler<Elsasa.Sis.Contracts.Teaching.Events.WorkloadExceptionApprovedEvent>>(sp => sp.GetRequiredService<TestEventCollector>());

            // Register TestFinanceModule for simulating IFinanceModule verification
            services.RemoveAll<Elsasa.Sis.Contracts.Finance.IFinanceModule>();
            services.AddSingleton<Elsasa.Sis.Contracts.Finance.IFinanceModule>(FinanceModule);

            // Register TestStudentsModule for simulating IStudentsModule verification
            services.RemoveAll<Elsasa.Sis.Contracts.Students.IStudentsModule>();
            services.AddSingleton<Elsasa.Sis.Contracts.Students.IStudentsModule>(StudentsModule);

            // Configure TestAuthHandler for bypass/customizable claims
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, _ => { });
        });
    }
}
