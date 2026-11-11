namespace Elsasa.Sis.Students.Features.Kpis.GetStudentsKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint serving the Level 2 Students KPI Dashboard (<c>GET /api/students/kpis/dashboard</c>).
/// </summary>
public sealed class GetStudentsKpiDashboardEndpoint : IEndpoint
{
    private static readonly string[] _allowedRoles =
    [
        "Admin",
        "SuperAdmin",
        "AdmissionOfficer",
        "Admission Officer",
        "StudentAdminOfficer",
        "Student Administration Officer",
        "SchoolLeadership",
        "School Leadership"
    ];

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/students/kpis/dashboard", HandleAsync)
            .WithName("GetStudentsKpiDashboard")
            .WithTags("Students")
            .RequireAuthorization()
            .Produces<DomainKpiDashboardResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicYearId,
        [FromQuery] Guid? academicPeriodId,
        [FromQuery] string? campus,
        [FromQuery] string? schoolLevel,
        [FromServices] ICurrentUser currentUser,
        [FromServices] GetStudentsKpiDashboardHandler handler,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return DomainError.Unauthorized(
                "Auth.Unauthenticated",
                "Authentication is required to access the Students KPI dashboard.")
                .ToProblemDetails();
        }

        if (!HasStudentsKpiAccess(currentUser))
        {
            return DomainError.Forbidden(
                "Students.KpiAccessDenied",
                "You do not have permission to access the Students KPI dashboard.")
                .ToProblemDetails();
        }

        var filter = new GlobalKpiFilterDto(
            AcademicYearId: academicYearId,
            AcademicPeriodId: academicPeriodId,
            Campus: campus,
            SchoolLevel: schoolLevel);

        var result = await handler.HandleAsync(filter, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }

    private static bool HasStudentsKpiAccess(ICurrentUser currentUser)
    {
        if (currentUser.Permissions.Any(p =>
            string.Equals(p, "*", StringComparison.OrdinalIgnoreCase)
            || string.Equals(p, Permissions.Students.Admission.View, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p, Permissions.Students.Records.View, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return currentUser.Roles.Any(r =>
            _allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }
}
