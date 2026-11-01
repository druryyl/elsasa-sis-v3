namespace Elsasa.Sis.IntegrationTests.UnitTests.Fixtures;

using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

public sealed class TestCurrentUserTests
{
    [Fact]
    public void Constructor_Should_InitializeDefaultAdminUser_When_Instantiated()
    {
        // Arrange & Act
        var user = new TestCurrentUser();

        // Assert
        user.IsAuthenticated.Should().BeTrue();
        user.Id.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        user.UserId.Should().Be(user.Id);
        user.Username.Should().Be("test-integration-user");
        user.Email.Should().Be("test-integration-user@elsasa.edu");
        user.Roles.Should().ContainSingle().Which.Should().Be("Admin");
        user.Permissions.Should().ContainSingle().Which.Should().Be("*");
        user.CampusId.Should().BeNull();
    }

    [Fact]
    public void SetUnauthenticated_Should_ClearAllClaimsAndSetFalse_When_Invoked()
    {
        // Arrange
        var user = new TestCurrentUser();

        // Act
        user.SetUnauthenticated();

        // Assert
        user.IsAuthenticated.Should().BeFalse();
        user.Id.Should().BeNull();
        user.UserId.Should().BeNull();
        user.Username.Should().BeNull();
        user.Email.Should().BeNull();
        user.Roles.Should().BeEmpty();
        user.Permissions.Should().BeEmpty();
        user.CampusId.Should().BeNull();
    }

    [Fact]
    public void SetUser_Should_PopulateCustomClaims_When_Invoked()
    {
        // Arrange
        var user = new TestCurrentUser();
        var customId = Guid.NewGuid();
        var campusId = Guid.NewGuid();

        // Act
        user.SetUser(
            id: customId,
            username: "sarah.admin",
            email: "sarah@elsasa.edu",
            roles: ["Dean", "Registrar"],
            permissions: ["Students.Read", "Students.Write"],
            campusId: campusId);

        // Assert
        user.IsAuthenticated.Should().BeTrue();
        user.Id.Should().Be(customId);
        user.Username.Should().Be("sarah.admin");
        user.Email.Should().Be("sarah@elsasa.edu");
        user.Roles.Should().BeEquivalentTo(["Dean", "Registrar"]);
        user.Permissions.Should().BeEquivalentTo(["Students.Read", "Students.Write"]);
        user.CampusId.Should().Be(campusId);
    }

    [Fact]
    public void Reset_Should_RestoreDefaultAdminClaims_After_Customization()
    {
        // Arrange
        var user = new TestCurrentUser();
        user.SetUnauthenticated();
        user.IsAuthenticated.Should().BeFalse();

        // Act
        user.Reset();

        // Assert
        user.IsAuthenticated.Should().BeTrue();
        user.Id.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        user.Username.Should().Be("test-integration-user");
        user.Roles.Should().Contain("Admin");
        user.Permissions.Should().Contain("*");
    }
}
