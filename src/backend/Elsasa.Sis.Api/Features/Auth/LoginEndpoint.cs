namespace Elsasa.Sis.Api.Features.Auth;

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Elsasa.Sis.Api.Configuration;
using Elsasa.Sis.Contracts.DemoData;
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
/// Minimal API endpoint providing authentication and JWT bearer token issuance (ADR-009, GAP-003, OQ-007, TD-07).
/// </summary>
public sealed class LoginEndpoint : IEndpoint
{
    private sealed record DemoAccountDefinition(
        string Password,
        string UserId,
        string FullName,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions,
        IReadOnlyList<Claim>? AdditionalClaims = null);

    // Pre-configured demo accounts with their roles, permissions, and persona claims (TD-07)
    private static readonly Dictionary<string, DemoAccountDefinition> _demoAccounts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin"] = new(
            Password: DemoDataConstants.DefaultDemoPassword,
            UserId: "00000000-0000-0000-0000-000000000001",
            FullName: "Super Administrator",
            Roles: ["SuperAdmin", "Admin", "AcademicAdmin", "OperationsAuthority"],
            Permissions: ["*"]),

        ["superadmin"] = new(
            Password: DemoDataConstants.DefaultDemoPassword,
            UserId: "00000000-0000-0000-0000-000000000001",
            FullName: "Super Administrator",
            Roles: ["SuperAdmin", "Admin", "AcademicAdmin", "OperationsAuthority"],
            Permissions: ["*"]),

        ["admission"] = new(
            Password: DemoDataConstants.DefaultDemoPassword,
            UserId: "00000000-0000-0000-0000-000000000002",
            FullName: "Sarah Jenkins",
            Roles: ["AdmissionOfficer", "Staff"],
            Permissions:
            [
                "Permissions.Students.Admission.View",
                "Permissions.Students.Admission.Register",
                "Permissions.Students.Admission.Review",
                "Permissions.Students.Admission.VerifyDocument",
                "Permissions.Students.Admission.Decide",
                "Permissions.Students.Enrollment.Execute",
                "Permissions.Students.Records.View",
                "Permissions.Students.Records.Edit"
            ]),

        ["finance"] = new(
            Password: DemoDataConstants.DefaultDemoPassword,
            UserId: "00000000-0000-0000-0000-000000000003",
            FullName: "Hendro Susanto",
            Roles: ["FinanceOfficer", "FinanceStaff"],
            Permissions:
            [
                "Permissions.Finance.Policies.View",
                "Permissions.Finance.Policies.Manage",
                "Permissions.Finance.Policies.Approve",
                "Permissions.Finance.Obligations.View",
                "Permissions.Finance.Obligations.Assess",
                "Permissions.Finance.Billing.View",
                "Permissions.Finance.Billing.Generate",
                "Permissions.Finance.Accounts.View",
                "Permissions.Finance.Payments.View",
                "Permissions.Finance.Payments.Record",
                "Permissions.Finance.Payments.Confirm",
                "Permissions.Finance.Payments.Reject",
                "Permissions.Finance.Allocations.View",
                "Permissions.Finance.Allocations.Execute",
                "Permissions.Finance.Allocations.Reallocate",
                "Permissions.Finance.Payers.View",
                "Permissions.Finance.Payers.Manage"
            ]),

        ["supervisor"] = new(
            Password: DemoDataConstants.DefaultDemoPassword,
            UserId: "00000000-0000-0000-0000-000000000004",
            FullName: "Maria Magdalena",
            Roles: ["Teacher", "AcademicStaff"],
            Permissions:
            [
                "academics.participations.view",
                "academics.participations.manage",
                "academics.attendance.view",
                "academics.attendance.record",
                "academics.attendance.correct",
                "academics.activities.view",
                "academics.activities.manage",
                "academics.activities.conduct",
                "academics.assignments.view",
                "academics.assignments.manage",
                "academics.assignments.release",
                "academics.assignments.close",
                "academics.submissions.view",
                "academics.submissions.submit",
                "academics.submissions.exception",
                "academics.assessments.view",
                "academics.assessments.conduct",
                "academics.evidence.view",
                "academics.evidence.record",
                "academics.results.view",
                "academics.results.evaluate",
                "Permissions.Academics.Coursework.View",
                "Permissions.Academics.Coursework.Manage",
                "Permissions.Academics.Assessments.Evaluate",
                "Permissions.Academics.Attendance.View",
                "Permissions.Academics.Attendance.Record"
            ]),

        ["coordinator"] = new(
            Password: DemoDataConstants.DefaultDemoPassword,
            UserId: "00000000-0000-0000-0000-000000000005",
            FullName: "Dr. Andreas Hartono",
            Roles: ["AcademicCoordinator", "AcademicAuthority"],
            Permissions:
            [
                "academics.curriculum.view",
                "academics.curriculum.manage",
                "academics.curriculum.submit",
                "academics.curriculum.approve",
                "academics.curriculum.archive",
                "academics.participations.view",
                "academics.participations.manage",
                "academics.assessments.view",
                "academics.assessments.manage",
                "academics.results.view",
                "academics.results.approve",
                "academics.results.publish",
                "academics.reportcards.view",
                "academics.reportcards.compile",
                "academics.reportcards.approve",
                "academics.reportcards.publish",
                "academics.transcripts.view",
                "academics.transcripts.generate",
                "academics.transcripts.issue",
                "academics.corrections.correct",
                "academics.corrections.viewhistory",
                "Permissions.Academics.Assessments.Approve",
                "Permissions.Academics.ReportCards.View",
                "Permissions.Academics.ReportCards.Compile",
                "Permissions.Academics.ReportCards.Approve",
                "Permissions.Academics.ReportCards.Publish",
                "Permissions.Academics.Transcripts.View",
                "Permissions.Academics.Transcripts.Generate",
                "Permissions.Academics.Transcripts.Issue"
            ]),

        ["student.jonathan"] = new(
            Password: DemoDataConstants.DefaultDemoPassword,
            UserId: DemoDataConstants.JonathanStudentId.ToString(),
            FullName: "Jonathan Pratama",
            Roles: ["Student"],
            Permissions:
            [
                "academics.participations.view",
                "academics.activities.view",
                "academics.attendance.view",
                "academics.assignments.view",
                "academics.submissions.view",
                "academics.submissions.submit",
                "academics.results.view",
                "academics.reportcards.view",
                "academics.transcripts.view",
                "Permissions.SelfService.Student"
            ],
            AdditionalClaims:
            [
                new Claim("student_id", DemoDataConstants.JonathanStudentId.ToString())
            ]),

        ["parent.jonathan"] = new(
            Password: DemoDataConstants.DefaultDemoPassword,
            UserId: DemoDataConstants.HendraParentId.ToString(),
            FullName: "Hendra Pratama",
            Roles: ["Parent"],
            Permissions:
            [
                "academics.participations.view",
                "academics.activities.view",
                "academics.attendance.view",
                "academics.results.view",
                "academics.reportcards.view",
                "academics.transcripts.view",
                "Permissions.Finance.Obligations.View",
                "Permissions.Finance.Billing.View",
                "Permissions.Finance.Payments.View",
                "Permissions.SelfService.Parent"
            ],
            AdditionalClaims:
            [
                new Claim("student_id", DemoDataConstants.JonathanStudentId.ToString()),
                new Claim("parent_id", DemoDataConstants.HendraParentId.ToString())
            ])
    };

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
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Username and password are required." });
        }

        var trimmedUsername = request.Username.Trim();

        // Check pre-configured demo accounts
        if (_demoAccounts.TryGetValue(trimmedUsername, out var demoAccount))
        {
            if (request.Password != demoAccount.Password)
            {
                return Results.BadRequest(new { error = "Invalid username or password." });
            }

            return IssueToken(
                trimmedUsername,
                demoAccount.UserId,
                demoAccount.FullName,
                demoAccount.Roles.ToArray(),
                demoAccount.Permissions.ToArray(),
                jwtOptionsAccessor,
                demoAccount.AdditionalClaims);
        }

        // Fallback for legacy admin handling
        var isSuperAdmin = trimmedUsername.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                           trimmedUsername.Equals("superadmin", StringComparison.OrdinalIgnoreCase);

        if (!isSuperAdmin)
        {
            return Results.BadRequest(new { error = "Invalid username or password." });
        }

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

        return IssueToken(trimmedUsername, userId.ToString(), fullName, roles, permissions, jwtOptionsAccessor);
    }

    private static IResult IssueToken(
        string username,
        string userId,
        string fullName,
        string[] roles,
        string[] permissions,
        IOptions<JwtOptions> jwtOptionsAccessor,
        IEnumerable<Claim>? additionalClaims = null)
    {
        var jwtOptions = jwtOptionsAccessor.Value;
        var secret = string.IsNullOrWhiteSpace(jwtOptions.SecretKey)
            ? "A_Super_Secret_Key_For_Elsasa_Sis_Api_Development_Environment_Only_12345"
            : jwtOptions.SecretKey;

        var userSession = new UserSessionDto(
            UserId: userId,
            Username: username,
            FullName: fullName,
            Roles: roles,
            Permissions: permissions,
            CampusId: "campus-main");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);

        var claims = new List<Claim>
        {
            new("sub", userId),
            new("name", username),
            new("preferred_username", username),
            new(ClaimTypes.Name, username),
            new("campus_id", "campus-main"),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var perm in permissions)
        {
            claims.Add(new Claim("permission", perm));
            claims.Add(new Claim("permissions", perm));
        }

        if (additionalClaims is not null)
        {
            claims.AddRange(additionalClaims);
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
