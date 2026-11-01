namespace Elsasa.Sis.IntegrationTests.UnitTests.Common;

using Elsasa.Sis.Shared.Common;

public sealed class CurrentUserTests
{
    [Fact]
    public void CurrentUser_Should_ExposeExpectedProperties_When_Authenticated()
    {
        var userId = Guid.NewGuid();
        var roles = new[] { "Admin", "Registrar" };

        ICurrentUser user = new TestUser(
            Id: userId,
            Username: "admin_user",
            Email: "admin@elsasa.sch.id",
            Roles: roles,
            IsAuthenticated: true);

        Assert.Equal(userId, user.Id);
        Assert.Equal(userId, user.UserId);
        Assert.Equal("admin_user", user.Username);
        Assert.Equal("admin@elsasa.sch.id", user.Email);
        Assert.Equal(roles, user.Roles);
        Assert.True(user.IsAuthenticated);
        Assert.Empty(user.Permissions);
        Assert.Null(user.CampusId);
    }

    [Fact]
    public void CurrentUser_Should_ExposeNullsAndEmptyRoles_When_Unauthenticated()
    {
        ICurrentUser user = new TestUser(
            Id: null,
            Username: null,
            Email: null,
            Roles: [],
            IsAuthenticated: false);

        Assert.Null(user.Id);
        Assert.Null(user.UserId);
        Assert.Null(user.Username);
        Assert.Null(user.Email);
        Assert.Empty(user.Roles);
        Assert.False(user.IsAuthenticated);
        Assert.Empty(user.Permissions);
        Assert.Null(user.CampusId);
    }

    [Fact]
    public void CurrentUser_Should_AllowOverridingPermissionsAndCampusId()
    {
        var campusId = Guid.NewGuid();
        var permissions = new[] { "Students.Read", "Students.Write" };

        ICurrentUser user = new ExtendedTestUser(
            Id: Guid.NewGuid(),
            Username: "campus_admin",
            Email: "campus@elsasa.sch.id",
            Roles: ["Staff"],
            IsAuthenticated: true,
            Permissions: permissions,
            CampusId: campusId);

        Assert.Equal(campusId, user.CampusId);
        Assert.Equal(permissions, user.Permissions);
    }

    private sealed record TestUser(
        Guid? Id,
        string? Username,
        string? Email,
        IReadOnlyList<string> Roles,
        bool IsAuthenticated) : ICurrentUser;

    private sealed record ExtendedTestUser(
        Guid? Id,
        string? Username,
        string? Email,
        IReadOnlyList<string> Roles,
        bool IsAuthenticated,
        IReadOnlyList<string> Permissions,
        Guid? CampusId) : ICurrentUser;
}
