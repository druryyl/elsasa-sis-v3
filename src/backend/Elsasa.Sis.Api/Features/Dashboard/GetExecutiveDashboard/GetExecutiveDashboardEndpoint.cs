namespace Elsasa.Sis.Api.Features.Dashboard.GetExecutiveDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint serving the Level 1 Executive Overview dashboard (<c>GET /api/dashboard/executive</c>).
/// </summary>
public sealed class GetExecutiveDashboardEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/dashboard/executive", HandleAsync)
            .WithName("GetExecutiveDashboard")
            .WithTags("Dashboard")
            .RequireAuthorization()
            .Produces<ExecutiveDashboardResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicYearId,
        [FromQuery] Guid? academicPeriodId,
        [FromQuery] string? campus,
        [FromQuery] string? schoolLevel,
        [FromServices] GetExecutiveDashboardHandler handler,
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
