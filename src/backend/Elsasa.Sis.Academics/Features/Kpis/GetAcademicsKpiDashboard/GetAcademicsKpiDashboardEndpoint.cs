namespace Elsasa.Sis.Academics.Features.Kpis.GetAcademicsKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint serving the Level 2 Academics KPI dashboard (<c>GET /api/academics/kpis/dashboard</c>).
/// </summary>
public sealed class GetAcademicsKpiDashboardEndpoint : IEndpoint
{
    private static readonly string[] _allowedRoles =
    [
        "Admin",
        "SuperAdmin",
        "AcademicAdmin",
        "AcademicCoordinator",
        "Academic Coordinator",
        "AcademicAuthority",
        "Academic Authority",
        "Teacher"
    ];

    private static readonly string[] _allowedPermissions =
    [
        "*",
        Permissions.Academics.Participations.View,
        "Permissions.Academics.Coursework.View"
    ];

    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/academics/kpis/dashboard", HandleAsync)
            .WithName("GetAcademicsKpiDashboard")
            .WithTags("Academics")
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
        [FromServices] GetAcademicsKpiDashboardHandler handler,
        CancellationToken cancellationToken)
    {
        if (!HasAccess(currentUser))
        {
            return Results.Forbid();
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

    private static bool HasAccess(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated)
        {
            return false;
        }

        if (currentUser.Permissions.Any(p =>
                _allowedPermissions.Contains(p, StringComparer.OrdinalIgnoreCase)))
        {
            return true;
        }

        return currentUser.Roles.Any(r =>
            _allowedRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }
}
