namespace Elsasa.Sis.Api.Services;

using System.Security.Claims;
using Elsasa.Sis.Shared.Common;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Provides access to the currently authenticated user's identity and claims
/// extracted from <see cref="IHttpContextAccessor.HttpContext"/> (ADR-009).
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? Id
    {
        get
        {
            var user = Principal;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var sub = user.FindFirstValue("sub")
                      ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(sub, out var guid) ? guid : null;
        }
    }

    /// <inheritdoc />
    public Guid? UserId => Id;

    /// <inheritdoc />
    public string? Username
    {
        get
        {
            var user = Principal;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.FindFirstValue("name")
                   ?? user.FindFirstValue("preferred_username")
                   ?? user.FindFirstValue(ClaimTypes.Name);
        }
    }

    /// <inheritdoc />
    public string? Email
    {
        get
        {
            var user = Principal;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.FindFirstValue("email")
                   ?? user.FindFirstValue(ClaimTypes.Email);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Roles
    {
        get
        {
            var user = Principal;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return [];
            }

            return user.FindAll(c => c.Type == "role" || c.Type == ClaimTypes.Role)
                       .Select(c => c.Value)
                       .Where(v => !string.IsNullOrWhiteSpace(v))
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Permissions
    {
        get
        {
            var user = Principal;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return [];
            }

            return user.FindAll(c => c.Type == "permission" || c.Type == "permissions")
                       .Select(c => c.Value)
                       .Where(v => !string.IsNullOrWhiteSpace(v))
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList();
        }
    }

    /// <inheritdoc />
    public Guid? CampusId
    {
        get
        {
            var user = Principal;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var campus = user.FindFirstValue("campus_id")
                         ?? user.FindFirstValue("campusId");

            return Guid.TryParse(campus, out var guid) ? guid : null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
}
