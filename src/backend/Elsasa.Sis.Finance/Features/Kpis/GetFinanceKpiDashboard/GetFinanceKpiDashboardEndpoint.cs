namespace Elsasa.Sis.Finance.Features.Kpis.GetFinanceKpiDashboard;

using Elsasa.Sis.Contracts.Kpis;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint serving the Level 2 Finance KPI Dashboard (<c>GET /api/finance/kpis/dashboard</c>).
/// </summary>
public sealed class GetFinanceKpiDashboardEndpoint : IEndpoint
{
    internal const string AuthorizationPolicyName = "Finance.Kpis.Dashboard.View";

    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/kpis/dashboard", HandleAsync)
            .WithName("GetFinanceKpiDashboard")
            .WithTags("FinanceKpis")
            .RequireAuthorization(AuthorizationPolicyName)
            .Produces<DomainKpiDashboardResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicYearId,
        [FromQuery] Guid? academicPeriodId,
        [FromQuery] string? campus,
        [FromQuery] string? schoolLevel,
        [FromServices] GetFinanceKpiDashboardHandler handler,
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
