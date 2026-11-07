namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Security.Claims;
using Elsasa.Sis.Academics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class AcademicsLearningPlansPermissionsTests
{
    [Fact]
    public void Permissions_Academics_LearningPlans_Constants_Should_HaveExpectedValues()
    {
        Assert.Equal("academics:learning-plans:view", Permissions.Academics.LearningPlans.View);
        Assert.Equal("academics:learning-plans:manage", Permissions.Academics.LearningPlans.Manage);
        Assert.Equal("academics:learning-plans:approve", Permissions.Academics.LearningPlans.Approve);
        Assert.Equal("academics:learning-plans:revise", Permissions.Academics.LearningPlans.Revise);
    }

    [Fact]
    public void AddAcademicsDomain_Should_RegisterAllLearningPlansAuthorizationPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAcademicsDomain();
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.GetPolicy(Permissions.Academics.LearningPlans.View));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.LearningPlans.Manage));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.LearningPlans.Approve));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.LearningPlans.Revise));
    }

    [Theory]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("Academic Coordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Academic Authority", true)]
    [InlineData("Teacher", true)]
    [InlineData("StudentDevelopmentOfficer", true)]
    [InlineData("Student Development Officer", true)]
    [InlineData("Admin", true)]
    [InlineData("Student", false)]
    [InlineData("Parent", false)]
    [InlineData("FinanceOfficer", false)]
    [InlineData("Guest", false)]
    public async Task LearningPlansViewPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.LearningPlans.View, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("Academic Coordinator", true)]
    [InlineData("Admin", true)]
    [InlineData("AcademicAuthority", false)]
    [InlineData("Academic Authority", false)]
    [InlineData("Teacher", false)]
    [InlineData("StudentDevelopmentOfficer", false)]
    [InlineData("Student", false)]
    [InlineData("Parent", false)]
    [InlineData("Guest", false)]
    public async Task LearningPlansManagePolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.LearningPlans.Manage, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Academic Authority", true)]
    [InlineData("CurriculumLead", true)]
    [InlineData("Curriculum Lead", true)]
    [InlineData("AcademicHead", true)]
    [InlineData("Academic Head", true)]
    [InlineData("VPAcademics", true)]
    [InlineData("VP Academics", true)]
    [InlineData("Admin", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Academic Coordinator", false)]
    [InlineData("Teacher", false)]
    [InlineData("StudentDevelopmentOfficer", false)]
    [InlineData("Student", false)]
    [InlineData("Parent", false)]
    [InlineData("Guest", false)]
    public async Task LearningPlansApprovePolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.LearningPlans.Approve, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("Academic Coordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Academic Authority", true)]
    [InlineData("Admin", true)]
    [InlineData("Teacher", false)]
    [InlineData("StudentDevelopmentOfficer", false)]
    [InlineData("Student", false)]
    [InlineData("Parent", false)]
    [InlineData("Guest", false)]
    public async Task LearningPlansRevisePolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.LearningPlans.Revise, role, expectedSuccess);
    }

    [Fact]
    public async Task LearningPlansPolicy_Should_Authorize_WhenPermissionClaimPresent()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAcademicsDomain();
        using var provider = services.BuildServiceProvider();

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "api-caller"),
            new Claim("permission", Permissions.Academics.LearningPlans.Manage)
        ], "TestAuth"));

        var result = await authService.AuthorizeAsync(user, Permissions.Academics.LearningPlans.Manage);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task LearningPlansPolicy_Should_Authorize_WhenWildcardPermissionClaimPresent()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAcademicsDomain();
        using var provider = services.BuildServiceProvider();

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "super-user"),
            new Claim("permissions", "*")
        ], "TestAuth"));

        var result = await authService.AuthorizeAsync(user, Permissions.Academics.LearningPlans.Approve);
        Assert.True(result.Succeeded);
    }

    private static async Task AssertPolicyAccess(string policyName, string role, bool expectedSuccess)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAcademicsDomain();
        using var provider = services.BuildServiceProvider();

        var authService = provider.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, role)
        ], "TestAuth"));

        var result = await authService.AuthorizeAsync(user, policyName);
        Assert.Equal(expectedSuccess, result.Succeeded);
    }
}
