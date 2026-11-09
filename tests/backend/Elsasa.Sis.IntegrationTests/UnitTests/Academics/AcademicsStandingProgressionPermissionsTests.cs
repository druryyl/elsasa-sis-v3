namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Security.Claims;
using Elsasa.Sis.Academics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class AcademicsStandingProgressionPermissionsTests
{
    [Fact]
    public void Permissions_Academics_Constants_Should_HaveExpectedValues()
    {
        // Progress
        Assert.Equal("academics.progress.view", Permissions.Academics.Progress.View);
        Assert.Equal("academics.progress.record", Permissions.Academics.Progress.Record);
        Assert.Equal("academics.progress.conclude", Permissions.Academics.Progress.Conclude);

        // Standings
        Assert.Equal("academics.standings.view", Permissions.Academics.Standings.View);
        Assert.Equal("academics.standings.evaluate", Permissions.Academics.Standings.Evaluate);
        Assert.Equal("academics.standings.determine", Permissions.Academics.Standings.Determine);
        Assert.Equal("academics.standings.batch_determine", Permissions.Academics.Standings.BatchDetermine);

        // Progression
        Assert.Equal("academics.progression.view", Permissions.Academics.Progression.View);
        Assert.Equal("academics.progression.evaluate", Permissions.Academics.Progression.Evaluate);
        Assert.Equal("academics.progression.issue", Permissions.Academics.Progression.Issue);
        Assert.Equal("academics.progression.batch_issue", Permissions.Academics.Progression.BatchIssue);

        // Interventions
        Assert.Equal("academics.interventions.view", Permissions.Academics.Interventions.View);
        Assert.Equal("academics.interventions.initiate", Permissions.Academics.Interventions.Initiate);
        Assert.Equal("academics.interventions.update_status", Permissions.Academics.Interventions.UpdateStatus);

        // Achievements
        Assert.Equal("academics.achievements.view", Permissions.Academics.Achievements.View);
        Assert.Equal("academics.achievements.confer", Permissions.Academics.Achievements.Confer);

        // Corrections
        Assert.Equal("academics.corrections.standing", Permissions.Academics.Corrections.CorrectStanding);
        Assert.Equal("academics.corrections.progression", Permissions.Academics.Corrections.CorrectProgression);
    }

    [Fact]
    public void AddAcademicsDomain_Should_RegisterAllSubmodule4AuthorizationPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAcademicsDomain();
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Progress.View));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Progress.Record));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Progress.Conclude));

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Standings.View));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Standings.Evaluate));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Standings.Determine));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Standings.BatchDetermine));

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Progression.View));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Progression.Evaluate));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Progression.Issue));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Progression.BatchIssue));

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Interventions.View));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Interventions.Initiate));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Interventions.UpdateStatus));

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Achievements.View));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Achievements.Confer));

        Assert.NotNull(options.GetPolicy(Permissions.Academics.Corrections.CorrectStanding));
        Assert.NotNull(options.GetPolicy(Permissions.Academics.Corrections.CorrectProgression));
    }

    [Theory]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Teacher", true)]
    [InlineData("Student", true)]
    [InlineData("Guest", false)]
    public async Task ProgressViewPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Progress.View, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task ProgressRecordPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Progress.Record, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Teacher", false)]
    public async Task ProgressConcludePolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Progress.Conclude, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("Teacher", true)]
    [InlineData("StudentAdminOfficer", true)]
    [InlineData("StudentAdministrationOfficer", true)]
    [InlineData("Guest", false)]
    public async Task StandingsViewPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Standings.View, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task StandingsEvaluatePolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Standings.Evaluate, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task StandingsDeterminePolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Standings.Determine, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    public async Task StandingsBatchDeterminePolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Standings.BatchDetermine, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("Teacher", true)]
    [InlineData("StudentAdminOfficer", true)]
    [InlineData("StudentAdministrationOfficer", true)]
    [InlineData("Guest", false)]
    public async Task ProgressionViewPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Progression.View, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    [InlineData("StudentAdminOfficer", false)]
    public async Task ProgressionIssuePolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Progression.Issue, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    public async Task ProgressionBatchIssuePolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Progression.BatchIssue, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Teacher", true)]
    [InlineData("Student", false)]
    public async Task InterventionsViewPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Interventions.View, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Teacher", true)]
    [InlineData("Student", false)]
    public async Task InterventionsInitiatePolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Interventions.Initiate, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("AcademicAuthority", true)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task InterventionsUpdateStatusPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Interventions.UpdateStatus, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", true)]
    [InlineData("Teacher", true)]
    [InlineData("Student", true)]
    [InlineData("Parent", true)]
    [InlineData("AdmissionOfficer", true)]
    [InlineData("Guest", false)]
    public async Task AchievementsViewPolicy_Should_Authorize_ExpectedRoles(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Achievements.View, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    [InlineData("Student", false)]
    public async Task AchievementsConferPolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Achievements.Confer, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    public async Task CorrectionsStandingPolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Corrections.CorrectStanding, role, expectedSuccess);
    }

    [Theory]
    [InlineData("AcademicAuthority", true)]
    [InlineData("AcademicCoordinator", false)]
    [InlineData("Teacher", false)]
    public async Task CorrectionsProgressionPolicy_Should_StrictlyEnforce_AuthorityRole(string role, bool expectedSuccess)
    {
        await AssertPolicyAccess(Permissions.Academics.Corrections.CorrectProgression, role, expectedSuccess);
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
