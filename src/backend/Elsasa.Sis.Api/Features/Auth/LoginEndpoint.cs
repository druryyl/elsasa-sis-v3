namespace Elsasa.Sis.Api.Features.Auth;

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Elsasa.Sis.Api.Configuration;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public sealed record LoginRequest(string Username, string Password);

public sealed record UserSessionDto(
    string UserId,
    string Username,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string? CampusId);

public sealed record LoginResponse(string Token, UserSessionDto User);

/// <summary>
/// Minimal API endpoint providing authentication and JWT bearer token issuance (ADR-009).
/// </summary>
public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/auth/login", HandleAsync)
            .WithName("AuthLogin")
            .WithTags("Authentication")
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static IResult HandleAsync(
        LoginRequest request,
        IOptions<JwtOptions> jwtOptionsAccessor)
    {
        if (string.IsNullOrWhiteSpace(request?.Username))
        {
            return Results.BadRequest(new { error = "Username is required." });
        }

        var trimmedUsername = request.Username.Trim();
        var isSuperAdmin = trimmedUsername.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                           trimmedUsername.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

        var userId = isSuperAdmin
            ? Guid.Parse("00000000-0000-0000-0000-000000000001")
            : Guid.NewGuid();

        var fullName = isSuperAdmin
            ? "Super Administrator"
            : $"{char.ToUpper(trimmedUsername[0], CultureInfo.InvariantCulture)}{trimmedUsername[1..]} Staff";

        var roles = isSuperAdmin
            ? new[] { "SuperAdmin", "Admin", "AcademicAdmin", "OperationsAuthority" }
            : new[] { "AcademicStaff", "FinanceStaff" };

        var permissions = isSuperAdmin
            ? new[] { "*" }
            : new[]
            {
                "Permissions.Operations.Calendar.View",
                "Permissions.Operations.Calendar.Manage",
                "Permissions.Operations.Calendar.Approve",
                "Permissions.Operations.Calendar.Publish",
                "Permissions.Operations.Closure.Declare",
                "Permissions.Operations.Records.Correct",
                "Permissions.Students.Admission.View",
                "Permissions.Students.Admission.Review",
                "Permissions.Students.Enrollment.Execute",
                "Permissions.Finance.Obligations.View",
                "Permissions.Finance.Payments.Receive",
            };

        var userSession = new UserSessionDto(
            UserId: userId.ToString(),
            Username: trimmedUsername,
            FullName: fullName,
            Roles: roles,
            Permissions: permissions,
            CampusId: "campus-main");

        var jwtOptions = jwtOptionsAccessor.Value;
        var secret = string.IsNullOrWhiteSpace(jwtOptions.SecretKey)
            ? "A_Super_Secret_Key_For_Elsasa_Sis_Api_Development_Environment_Only_12345"
            : jwtOptions.SecretKey;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);

        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("name", userSession.Username),
            new("preferred_username", userSession.Username),
            new(ClaimTypes.Name, userSession.Username),
            new("campus_id", userSession.CampusId ?? "campus-main"),
        };

        foreach (var role in userSession.Roles)
        {
            claims.Add(new Claim("role", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var perm in userSession.Permissions)
        {
            claims.Add(new Claim("permission", perm));
            claims.Add(new Claim("permissions", perm));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims, "Bearer", "name", "role"),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = string.IsNullOrWhiteSpace(jwtOptions.Issuer) ? "Elsasa.Sis" : jwtOptions.Issuer,
            Audience = string.IsNullOrWhiteSpace(jwtOptions.Audience) ? "Elsasa.Sis.Api" : jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Results.Ok(new LoginResponse(tokenString, userSession));
    }
}
