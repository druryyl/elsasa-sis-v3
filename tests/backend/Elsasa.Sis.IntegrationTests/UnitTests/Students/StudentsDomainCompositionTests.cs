namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using System.Security.Claims;
using Elsasa.Sis.Api.Configuration;
using Elsasa.Sis.Shared.Storage;
using Elsasa.Sis.Students;
using Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationById;
using Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationsList;
using Elsasa.Sis.Students.Features.Admission.GetApplicationStatusByNumber;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Xunit;

[Trait("Category", "UnitTest")]
public sealed class StudentsDomainCompositionTests
{
    [Fact]
    public void Permissions_Students_Admission_Constants_Should_HaveExpectedValues()
    {
        Assert.Equal("Permissions.Students.Admission.View", Permissions.Students.Admission.View);
        Assert.Equal("Permissions.Students.Admission.Register", Permissions.Students.Admission.Register);
        Assert.Equal("Permissions.Students.Admission.Review", Permissions.Students.Admission.Review);
        Assert.Equal("Permissions.Students.Admission.VerifyDocument", Permissions.Students.Admission.VerifyDocument);
        Assert.Equal("Permissions.Students.Admission.Decide", Permissions.Students.Admission.Decide);
    }

    [Fact]
    public void AddStudentsDomain_Should_ThrowArgumentNullException_When_ServicesNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddStudentsDomain());
    }

    [Fact]
    public void AddStudentsDomain_Should_RegisterAuthorizationPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        Assert.NotNull(options.GetPolicy(Permissions.Students.Admission.View));
        Assert.NotNull(options.GetPolicy(Permissions.Students.Admission.Register));
        Assert.NotNull(options.GetPolicy(Permissions.Students.Admission.Review));
        Assert.NotNull(options.GetPolicy(Permissions.Students.Admission.VerifyDocument));
        Assert.NotNull(options.GetPolicy(Permissions.Students.Admission.Decide));
    }

    [Fact]
    public void AddStudentsDomain_Should_RegisterAdmissionQueryHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStudentsDomain();

        // Assert
        Assert.Contains(services, d => d.ServiceType == typeof(GetAdmissionApplicationsListHandler) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(GetAdmissionApplicationByIdHandler) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(GetApplicationStatusByNumberHandler) && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddStudentsDomain_Should_RegisterDocumentManagementHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStudentsDomain();

        // Assert
        Assert.Contains(services, d => d.ServiceType == typeof(Elsasa.Sis.Students.Features.Admission.Documents.UploadApplicantDocument.UploadApplicantDocumentHandler) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(Elsasa.Sis.Students.Features.Admission.Documents.DownloadApplicantDocument.DownloadApplicantDocumentHandler) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, d => d.ServiceType == typeof(Elsasa.Sis.Students.Features.Admission.Documents.VerifyApplicantDocument.VerifyApplicantDocumentHandler) && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Theory]
    [InlineData("AdmissionOfficer", true)]
    [InlineData("Admission Officer", true)]
    [InlineData("StudentAdminOfficer", true)]
    [InlineData("Student Administration Officer", true)]
    [InlineData("SchoolLeadership", true)]
    [InlineData("Admin", true)]
    [InlineData("Teacher", false)]
    public async Task ViewPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
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
        var result = await authService.AuthorizeAsync(user, Permissions.Students.Admission.View);

        // Assert
        Assert.Equal(expectedSuccess, result.Succeeded);
    }

    [Fact]
    public async Task Policy_Should_Authorize_When_UserHasWildcardOrExactPermissionClaim()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var userWithExactPermission = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("permission", Permissions.Students.Admission.Decide),
        ], "TestAuth"));
        var userWithWildcard = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("permissions", "*"),
        ], "TestAuth"));

        // Act
        var exactResult = await authService.AuthorizeAsync(userWithExactPermission, Permissions.Students.Admission.Decide);
        var wildcardResult = await authService.AuthorizeAsync(userWithWildcard, Permissions.Students.Admission.Decide);

        // Assert
        Assert.True(exactResult.Succeeded);
        Assert.True(wildcardResult.Succeeded);
    }

    [Fact]
    public void AddHostComposition_Should_RegisterStudentsDomainAndSharedStorage()
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

        // Assert: Shared storage is registered
        var fileStorage = provider.GetService<IFileStorageService>();
        Assert.NotNull(fileStorage);
        Assert.IsType<PhysicalFileStorageService>(fileStorage);

        // Assert: Authorization policies for Students are registered
        var authOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        Assert.NotNull(authOptions.GetPolicy(Permissions.Students.Admission.View));
        Assert.NotNull(authOptions.GetPolicy(Permissions.Students.Admission.Decide));
    }
}
