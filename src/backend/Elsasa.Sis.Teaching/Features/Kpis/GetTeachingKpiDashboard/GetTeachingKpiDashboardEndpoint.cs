namespace Elsasa.Sis.Teaching.Features.Kpis.GetTeachingKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint serving the Level 2 Teaching KPI Dashboard (<c>GET /api/teaching/kpis/dashboard</c>).
/// </summary>
public sealed class GetTeachingKpiDashboardEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/teaching/kpis/dashboard", HandleAsync)
            .WithName("GetTeachingKpiDashboard")
            .WithTags("Teaching")
            .RequireAuthorization(TeachingKpiPolicies.DashboardView)
            .Produces<DomainKpiDashboardResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicYearId,
        [FromQuery] Guid? academicPeriodId,
        [FromQuery] string? campus,
        [FromQuery] string? schoolLevel,
        [FromServices] GetTeachingKpiDashboardHandler handler,
        CancellationToken cancellationToken)
    {
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
}
