namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using System.Security.Claims;
using Elsasa.Sis.Contracts.Finance;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Shared.Storage;
using Elsasa.Sis.Students;
using Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;
using Elsasa.Sis.Students.Features.Enrollment.Services;
using Elsasa.Sis.Students.Features.Records.Documents.DownloadStudentDocument;
using Elsasa.Sis.Students.Features.Records.Documents.GetStudentDocuments;
using Elsasa.Sis.Students.Features.Records.Documents.UploadStudentDocument;
using Elsasa.Sis.Students.Features.Records.Documents.VerifyStudentDocument;
using Elsasa.Sis.Students.Features.Records.GetStudentById;
using Elsasa.Sis.Students.Features.Records.Guardians.AddGuardianRelationship;
using Elsasa.Sis.Students.Features.Records.Guardians.UpdateGuardianRelationship;
using Elsasa.Sis.Students.Features.Records.SearchStudents;
using Elsasa.Sis.Students.Features.Records.UpdateStudentIdentity;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class StudentsEnrollmentDomainCompositionTests
{
    [Fact]
    public void Permissions_Students_Enrollment_And_Records_Constants_Should_HaveExpectedValues()
    {
        // Assert Enrollment permissions
        Permissions.Students.Enrollment.Execute.Should().Be("Permissions.Students.Enrollment.Execute");

        // Assert Records permissions
        Permissions.Students.Records.View.Should().Be("Permissions.Students.Records.View");
        Permissions.Students.Records.Edit.Should().Be("Permissions.Students.Records.Edit");
    }

    [Fact]
    public void AddStudentsDomain_Should_RegisterEnrollmentAndRecordsAuthorizationPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        options.GetPolicy(Permissions.Students.Enrollment.Execute).Should().NotBeNull();
        options.GetPolicy(Permissions.Students.Records.View).Should().NotBeNull();
        options.GetPolicy(Permissions.Students.Records.Edit).Should().NotBeNull();
    }

    #region Policy Requirement Assertions for Various Role Claims

    [Theory]
    [InlineData("StudentAdminOfficer", true)]
    [InlineData("Student Administration Officer", true)]
    [InlineData("AdmissionOfficer", true)]
    [InlineData("Admission Officer", true)]
    [InlineData("Admin", true)]
    [InlineData("SchoolLeadership", false)]
    [InlineData("School Leadership", false)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task EnrollmentExecutePolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.Role, role),
        ], "TestAuth"));

        // Act
        var result = await authService.AuthorizeAsync(user, Permissions.Students.Enrollment.Execute);

        // Assert
        result.Succeeded.Should().Be(expectedSuccess);
    }

    [Theory]
    [InlineData("StudentAdminOfficer", true)]
    [InlineData("Student Administration Officer", true)]
    [InlineData("AdmissionOfficer", true)]
    [InlineData("Admission Officer", true)]
    [InlineData("SchoolLeadership", true)]
    [InlineData("School Leadership", true)]
    [InlineData("Admin", true)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task RecordsViewPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.Role, role),
        ], "TestAuth"));

        // Act
        var result = await authService.AuthorizeAsync(user, Permissions.Students.Records.View);

        // Assert
        result.Succeeded.Should().Be(expectedSuccess);
    }

    [Theory]
    [InlineData("StudentAdminOfficer", true)]
    [InlineData("Student Administration Officer", true)]
    [InlineData("Admin", true)]
    [InlineData("AdmissionOfficer", false)]
    [InlineData("Admission Officer", false)]
    [InlineData("SchoolLeadership", false)]
    [InlineData("School Leadership", false)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task RecordsEditPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.Role, role),
        ], "TestAuth"));

        // Act
        var result = await authService.AuthorizeAsync(user, Permissions.Students.Records.Edit);

        // Assert
        result.Succeeded.Should().Be(expectedSuccess);
    }

    [Theory]
    [InlineData(Permissions.Students.Enrollment.Execute)]
    [InlineData(Permissions.Students.Records.View)]
    [InlineData(Permissions.Students.Records.Edit)]
    public async Task EnrollmentAndRecordsPolicies_Should_Authorize_ExactPermissionClaim(string policyName)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "exact-claim-user"),
            new Claim("permission", policyName),
        ], "TestAuth"));

        // Act
        var result = await authService.AuthorizeAsync(user, policyName);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(Permissions.Students.Enrollment.Execute)]
    [InlineData(Permissions.Students.Records.View)]
    [InlineData(Permissions.Students.Records.Edit)]
    public async Task EnrollmentAndRecordsPolicies_Should_Authorize_WildcardPermissionClaim(string policyName)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "super-user"),
            new Claim("permissions", "*"),
        ], "TestAuth"));

        // Act
        var result = await authService.AuthorizeAsync(user, policyName);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(Permissions.Students.Enrollment.Execute)]
    [InlineData(Permissions.Students.Records.View)]
    [InlineData(Permissions.Students.Records.Edit)]
    public async Task EnrollmentAndRecordsPolicies_Should_Reject_UnauthenticatedUser(string policyName)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await authService.AuthorizeAsync(anonymousUser, policyName);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    #endregion

    #region Service Descriptor and DI Resolution Assertions

    [Fact]
    public void AddStudentsDomain_Should_RegisterAllRequiredServiceDescriptors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStudentsDomain();

        // Assert generator
        services.Should().Contain(d => d.ServiceType == typeof(IStudentNumberGenerator) &&
                                       d.ImplementationType == typeof(SqlStudentNumberGenerator) &&
                                       d.Lifetime == ServiceLifetime.Scoped);

        // Assert enrollment execution handler
        services.Should().Contain(d => d.ServiceType == typeof(EnrollAcceptedApplicantHandler) &&
                                       d.Lifetime == ServiceLifetime.Scoped);

        // Assert records query handlers
        services.Should().Contain(d => d.ServiceType == typeof(SearchStudentsHandler) &&
                                       d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(GetStudentByIdHandler) &&
                                       d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(GetStudentDocumentsHandler) &&
                                       d.Lifetime == ServiceLifetime.Scoped);

        // Assert records command handlers
        services.Should().Contain(d => d.ServiceType == typeof(UpdateStudentIdentityHandler) &&
                                       d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(AddGuardianRelationshipHandler) &&
                                       d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(UpdateGuardianRelationshipHandler) &&
                                       d.Lifetime == ServiceLifetime.Scoped);

        // Assert document lifecycle handlers
        services.Should().Contain(d => d.ServiceType == typeof(UploadStudentDocumentHandler) &&
                                       d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(DownloadStudentDocumentHandler) &&
                                       d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(VerifyStudentDocumentHandler) &&
                                       d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddStudentsDomain_Should_ResolveAllNewHandlersAndGenerator_FromServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // External dependencies stubbed for resolution test
        services.AddSingleton<ISqlConnectionFactory>(new DummySqlConnectionFactory());
        services.AddSingleton<IFinanceModule>(new DummyFinanceModule());
        services.AddSingleton<IPublisher>(new DummyPublisher());
        services.AddSingleton<ICurrentUser>(new DummyCurrentUser());
        services.AddSingleton<IFileStorageService>(new DummyFileStorageService());

        // Act
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Assert
        scope.ServiceProvider.GetRequiredService<IStudentNumberGenerator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<EnrollAcceptedApplicantHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<SearchStudentsHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<GetStudentByIdHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<GetStudentDocumentsHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<UpdateStudentIdentityHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<AddGuardianRelationshipHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<UpdateGuardianRelationshipHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<UploadStudentDocumentHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<DownloadStudentDocumentHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<VerifyStudentDocumentHandler>().Should().NotBeNull();
    }

    #endregion

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
