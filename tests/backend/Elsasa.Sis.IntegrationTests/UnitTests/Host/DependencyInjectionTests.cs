namespace Elsasa.Sis.IntegrationTests.UnitTests.Host;

using Elsasa.Sis.Api.Configuration;
using Elsasa.Sis.Api.Services;
using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddHostComposition_Should_ThrowArgumentNullException_When_ArgumentsAreNull()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddHostComposition(configuration));
        Assert.Throws<ArgumentNullException>(() => services.AddHostComposition(null!));
    }

    [Fact]
    public void AddHostComposition_Should_RegisterISqlConnectionFactory_When_ConnectionStringProvided()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;Integrated Security=true;TrustServerCertificate=True;",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var services = new ServiceCollection();

        // Act
        services.AddHostComposition(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetService<ISqlConnectionFactory>();
        Assert.NotNull(factory);
        Assert.IsType<SqlConnectionFactory>(factory);
    }

    [Fact]
    public void AddHostComposition_Should_RegisterCurrentUserAndHttpContextAccessor()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddHostComposition(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var accessor = provider.GetService<IHttpContextAccessor>();
        Assert.NotNull(accessor);

        using var scope = provider.CreateScope();
        var currentUser = scope.ServiceProvider.GetService<ICurrentUser>();
        Assert.NotNull(currentUser);
        Assert.IsType<CurrentUser>(currentUser);
    }

    [Fact]
    public void AddHostComposition_Should_RegisterMediatR_AndResolveIMediator()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddHostComposition(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var mediator = provider.GetService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddHostComposition_Should_RegisterHealthChecksService()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddHostComposition(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = provider.GetService<HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }

    [Fact]
    public void AddHostComposition_Should_RegisterFinanceModule()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddHostComposition(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Assert
        var financeModule = scope.ServiceProvider.GetService<IFinanceModule>();
        Assert.NotNull(financeModule);
    }

    [Fact]
    public void AddHostComposition_Should_ContainNoUnresolvableServices()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;Integrated Security=true;TrustServerCertificate=True;",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var services = new ServiceCollection();

        // EndpointDataSource and IHostEnvironment are normally supplied by the web host's routing and hosting services.
        services.AddRouting();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new TestHostEnvironment());

        // Act
        services.AddHostComposition(configuration);
        Elsasa.Sis.Academics.DependencyInjection.AddAcademicsDomain(services, configuration);
        Elsasa.Sis.Teaching.DependencyInjection.AddTeachingModule(services, configuration);

        // Assert: ValidateOnBuild reproduces the host's startup service validation,
        // catching missing registrations such as IFinanceModule.
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    private sealed class TestHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Elsasa.Sis.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public void DomainAssemblies_Should_IncludeExpectedAssemblies()
    {
        // Assert
        Assert.Equal(7, DependencyInjection.DomainAssemblies.Length);
        Assert.Equal(3, DependencyInjection.Stage1Assemblies.Length);
        Assert.Equal(2, DependencyInjection.Stage2Assemblies.Length);
    }
}
