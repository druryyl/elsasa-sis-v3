namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Security.Claims;
using Elsasa.Sis.Academics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class AcademicsAssessmentPermissionsTests
{
    [Fact]
    public void Permissions_Academics_Constants_Should_HaveExpectedValues()
    {
        // Assessments
        Assert.Equal("academics.assessments.view", Permissions.Academics.Assessments.View);
        Assert.Equal("academics.assessments.manage", Permissions.Academics.Assessments.Manage);
        Assert.Equal("academics.assessments.conduct", Permissions.Academics.Assessments.Conduct);

        // Evidence
        Assert.Equal("academics.evidence.record", Permissions.Academics.Evidence.Record);
        Assert.Equal("academics.evidence.view", Permissions.Academics.Evidence.View);

        // Results
        Assert.Equal("academics.results.evaluate", Permissions.Academics.Results.Evaluate);
        Assert.Equal("academics.results.approve", Permissions.Academics.Results.Approve);
        Assert.Equal("academics.results.publish", Permissions.Academics.Results.Publish);
        Assert.Equal("academics.results.view", Permissions.Academics.Results.View);

        // Report Cards
        Assert.Equal("academics.reportcards.compile", Permissions.Academics.ReportCards.Compile);
        Assert.Equal("academics.reportcards.approve", Permissions.Academics.ReportCards.Approve);
        Assert.Equal("academics.reportcards.publish", Permissions.Academics.ReportCards.Publish);
        Assert.Equal("academics.reportcards.view", Permissions.Academics.ReportCards.View);

        // Transcripts
        Assert.Equal("academics.transcripts.generate", Permissions.Academics.Transcripts.Generate);
        Assert.Equal("academics.transcripts.issue", Permissions.Academics.Transcripts.Issue);
        Assert.Equal("academics.transcripts.view", Permissions.Academics.Transcripts.View);

        // Corrections
        Assert.Equal("academics.corrections.correct", Permissions.Academics.Corrections.Correct);
        Assert.Equal("academics.corrections.viewhistory", Permissions.Academics.Corrections.ViewHistory);
    }

    [Fact]
    public void AddAcademicsDomain_Should_RegisterAllSubmodule3AuthorizationPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAcademicsDomain();
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Assessments.View));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Assessments.Manage));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Assessments.Conduct));

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Evidence.Record));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Evidence.View));

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Results.Evaluate));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Results.Approve));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Results.Publish));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Results.View));

        Assert.NotNull(options.GetPolicy(Permissions.Academics.ReportCards.Compile));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.ReportCards.Approve));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.ReportCards.Publish));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.ReportCards.View));

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Transcripts.Generate));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Transcripts.Issue));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Transcripts.View));

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Corrections.Correct));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Corrections.ViewHistory));
    }

    [Theory]
    [InlineData("Teacher", true)]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Student", true)]
    [InlineData("Parent", true)]
    [InlineData("AdmissionOfficer", true)]
    [InlineData("Guest", false)]
    public async Task AssessmentsViewPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Assessments.View, role, expectedSuccess);
    }

    [Theory]
    [InlineData("Teacher", true)]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Student", false)]
    [InlineData("Parent", false)]
    public async Task AssessmentsManagePolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Assessments.Manage, role, expectedSuccess);
    }

    [Theory]
    [InlineData("Teacher", true)]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Student", false)]
    public async Task ResultsEvaluatePolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Results.Evaluate, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task ResultsApprovePolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Results.Approve, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task ResultsPublishPolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Results.Publish, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task ReportCardsCompilePolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.ReportCards.Compile, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    public async Task ReportCardsApprovePolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.ReportCards.Approve, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    public async Task CorrectionsCorrectPolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Corrections.Correct, role, expectedSuccess);
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
