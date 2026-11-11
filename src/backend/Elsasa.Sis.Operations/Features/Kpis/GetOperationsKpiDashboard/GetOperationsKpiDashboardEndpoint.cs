namespace Elsasa.Sis.Operations.Features.Kpis.GetOperationsKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint serving the Level 2 Operations KPI dashboard (<c>GET /api/operations/kpis/dashboard</c>).
/// </summary>
public sealed class GetOperationsKpiDashboardEndpoint : IEndpoint
{
    private static readonly string[] _allowedRoles =
    [
        "Admin",
        "SuperAdmin",
        "OperationsAuthority",
        "Operations Authority",
        "AcademicCoordinator",
        "Academic Coordinator",
        "AcademicAdmin"
    ];

    private static readonly string[] _allowedPermissions =
    [
        "*",
        Permissions.Operations.Calendar.View,
        Permissions.Operations.Calendar.Manage
    ];

    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/operations/kpis/dashboard", HandleAsync)
            .WithName("GetOperationsKpiDashboard")
            .WithTags("OperationsKpis")
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
        [FromServices] GetOperationsKpiDashboardHandler handler,
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
