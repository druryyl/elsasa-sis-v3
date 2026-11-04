namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Api.Configuration;
using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Storage;
using Elsasa.Sis.Students;
using Elsasa.Sis.Students.Features.Records.History.GetStudentAdministrativeHistory;
using Elsasa.Sis.Students.Features.Records.Placements.ChangeAdministrativePlacement;
using Elsasa.Sis.Students.Features.Records.Status.ChangeStudentStatus;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class StudentsPlacementDomainCompositionTests
{
    [Fact]
    public void AddStudentsDomain_Should_RegisterPlacementAndStatusHandlers_WithScopedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStudentsDomain();

        // Assert
        services.Should().Contain(d => d.ServiceType == typeof(GetStudentAdministrativeHistoryHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(ChangeAdministrativePlacementHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(ChangeStudentStatusHandler) && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddStudentsDomain_Should_ResolvePlacementAndStatusHandlers_FromServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Register external stubs required by Students domain dependencies
        services.AddSingleton<ISqlConnectionFactory>(new DummySqlConnectionFactory());
        services.AddSingleton<IFinanceModule>(new DummyFinanceModule());
        services.AddSingleton<IPublisher>(new DummyPublisher());
        services.AddSingleton<ICurrentUser>(new DummyCurrentUser());
        services.AddSingleton<IFileStorageService>(new DummyFileStorageService());

        // Act
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Assert - required by P4-S09 criteria
        scope.ServiceProvider.GetRequiredService<GetStudentAdministrativeHistoryHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ChangeAdministrativePlacementHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ChangeStudentStatusHandler>().Should().NotBeNull();
    }

    [Fact]
    public void AddHostComposition_Should_RegisterAndResolvePlacementAndStatusValidators()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Storage:LocalRootPath"] = "App_Data/test_storage",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var services = new ServiceCollection();

        // Act
        services.AddHostComposition(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Assert: FluentValidation validators scanned and resolved from Host composition
        scope.ServiceProvider.GetRequiredService<IValidator<ChangeAdministrativePlacementRequest>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IValidator<ChangeStudentStatusRequest>>().Should().NotBeNull();
    }

    #region Dummy Stubs for DI Resolution

    private sealed class DummySqlConnectionFactory : ISqlConnectionFactory
    {
        public ValueTask<System.Data.Common.DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class DummyFinanceModule : IFinanceModule
    {
        public Task<Result<bool>> IsEnrollmentFeePaidAsync(Guid applicantId, Guid academicYearId, CancellationToken ct = default) =>
            Task.FromResult(Result.Success(true));
    }

    private sealed class DummyPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private sealed class DummyCurrentUser : ICurrentUser
    {
        public Guid? Id => Guid.Empty;
        public Guid? UserId => Guid.Empty;
        public string? Username => "test-admin";
        public string? Email => "admin@example.com";
        public bool IsAuthenticated => true;
        public IReadOnlyList<string> Roles => ["Admin"];
        public IReadOnlyList<string> Permissions => ["*"];
        public Guid? CampusId => null;
    }

    private sealed class DummyFileStorageService : IFileStorageService
    {
        public Task<Result<StoredFileResult>> SaveFileAsync(Stream stream, string fileName, string contentType, string category, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<FileStreamResult>> OpenReadStreamAsync(string fileReference, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> DeleteFileAsync(string fileReference, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    #endregion
}
