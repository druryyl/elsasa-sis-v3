namespace Elsasa.Sis.IntegrationTests.Fixtures;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Custom authentication handler that creates an authenticated <see cref="ClaimsPrincipal"/>
/// directly from the active <see cref="TestCurrentUser"/> instance, bypassing JWT validation for tests.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TestCurrentUser currentUser)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "TestAuthenticationScheme";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!currentUser.IsAuthenticated || currentUser.Id is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, currentUser.Id.Value.ToString()),
            new("sub", currentUser.Id.Value.ToString()),
            new(ClaimTypes.Name, currentUser.Username ?? "test-user"),
        };

        if (!string.IsNullOrWhiteSpace(currentUser.Email))
        {
            claims.Add(new(ClaimTypes.Email, currentUser.Email));
        }

        foreach (var role in currentUser.Roles)
        {
            claims.Add(new(ClaimTypes.Role, role));
        }

        foreach (var permission in currentUser.Permissions)
        {
            claims.Add(new("permission", permission));
            claims.Add(new("permissions", permission));
        }

        if (currentUser.CampusId.HasValue)
        {
            claims.Add(new("campus_id", currentUser.CampusId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
