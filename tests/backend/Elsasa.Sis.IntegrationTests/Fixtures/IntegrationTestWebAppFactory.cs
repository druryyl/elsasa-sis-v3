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
