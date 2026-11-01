namespace Elsasa.Sis.IntegrationTests.UnitTests.Host;

using System.Security.Claims;
using Elsasa.Sis.Api.Services;
using Elsasa.Sis.Shared.Common;
using Microsoft.AspNetCore.Http;

public sealed class CurrentUserClaimExtractionTests
{
    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_HttpContextAccessorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CurrentUser(null!));
    }

    [Fact]
    public void CurrentUser_Should_ReturnNullsAndEmptyLists_When_HttpContextIsNull()
    {
        // Arrange
        var accessor = new HttpContextAccessor { HttpContext = null };
        var currentUser = new CurrentUser(accessor);

        // Act & Assert
        Assert.Null(currentUser.Id);
        Assert.Null(currentUser.UserId);
        Assert.Null(currentUser.Username);
        Assert.Null(currentUser.Email);
        Assert.Empty(currentUser.Roles);
        Assert.Empty(currentUser.Permissions);
        Assert.Null(currentUser.CampusId);
        Assert.False(currentUser.IsAuthenticated);
    }

    [Fact]
    public void CurrentUser_Should_ReturnNullsAndEmptyLists_When_UserIsUnauthenticated()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()) // unauthenticated
        };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var currentUser = new CurrentUser(accessor);

        // Act & Assert
        Assert.Null(currentUser.Id);
        Assert.Null(currentUser.UserId);
        Assert.Null(currentUser.Username);
        Assert.Null(currentUser.Email);
        Assert.Empty(currentUser.Roles);
        Assert.Empty(currentUser.Permissions);
        Assert.Null(currentUser.CampusId);
        Assert.False(currentUser.IsAuthenticated);
    }

    [Fact]
    public void CurrentUser_Should_ExtractSubAsGuid_When_SubClaimIsPresent()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("sub", expectedGuid.ToString()),
            new Claim("name", "john.doe"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        // Act & Assert
        Assert.Equal(expectedGuid, currentUser.Id);
        Assert.Equal(expectedGuid, currentUser.UserId);
        Assert.True(currentUser.IsAuthenticated);
    }

    [Fact]
    public void CurrentUser_Should_ExtractNameIdentifierAsGuid_When_SubIsMissing()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedGuid.ToString()),
            new Claim("name", "jane.doe"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        // Act & Assert
        Assert.Equal(expectedGuid, currentUser.Id);
        Assert.Equal(expectedGuid, currentUser.UserId);
    }

    [Fact]
    public void CurrentUser_Should_ReturnNullId_When_SubIsNotValidGuid()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("sub", "not-a-valid-guid-format"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        // Act & Assert
        Assert.Null(currentUser.Id);
        Assert.Null(currentUser.UserId);
    }

    [Theory]
    [InlineData("name", "principal_name")]
    [InlineData("preferred_username", "preferred_user")]
    [InlineData(ClaimTypes.Name, "claimtypes_user")]
    public void CurrentUser_Should_ExtractUsername_FromSupportedClaimTypes(string claimType, string expectedUsername)
    {
        // Arrange
        var claims = new[] { new Claim(claimType, expectedUsername) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        // Act & Assert
        Assert.Equal(expectedUsername, currentUser.Username);
    }

    [Theory]
    [InlineData("email", "officer@elsasa.sch.id")]
    [InlineData(ClaimTypes.Email, "officer2@elsasa.sch.id")]
    public void CurrentUser_Should_ExtractEmail_FromSupportedClaimTypes(string claimType, string expectedEmail)
    {
        // Arrange
        var claims = new[] { new Claim(claimType, expectedEmail) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        // Act & Assert
        Assert.Equal(expectedEmail, currentUser.Email);
    }

    [Fact]
    public void CurrentUser_Should_ExtractDistinctRoles_FromRoleAndClaimTypesRole()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("role", "Admin"),
            new Claim(ClaimTypes.Role, "Registrar"),
            new Claim("role", "admin"), // Case-insensitive duplicate
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        // Act & Assert
        Assert.Equal(2, currentUser.Roles.Count);
        Assert.Contains("Admin", currentUser.Roles, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Registrar", currentUser.Roles, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void CurrentUser_Should_ExtractDistinctPermissions_FromPermissionAndPermissionsClaims()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("permission", "AcademicYears.Create"),
            new Claim("permissions", "AcademicYears.Read"),
            new Claim("permission", "AcademicYears.Create"), // Duplicate
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        // Act & Assert
        Assert.Equal(2, currentUser.Permissions.Count);
        Assert.Contains("AcademicYears.Create", currentUser.Permissions, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("AcademicYears.Read", currentUser.Permissions, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void CurrentUser_Should_ExtractCampusId_When_ValidGuidPresent()
    {
        // Arrange
        var campusId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("campus_id", campusId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        // Act & Assert
        Assert.Equal(campusId, currentUser.CampusId);
    }

    [Fact]
    public void CurrentUser_Should_ReturnNullCampusId_When_InvalidGuidOrMissing()
    {
        // Arrange
        var claims = new[]
        {
            new Claim("campus_id", "invalid-guid"),
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var currentUser = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        // Act & Assert
        Assert.Null(currentUser.CampusId);
    }
}
