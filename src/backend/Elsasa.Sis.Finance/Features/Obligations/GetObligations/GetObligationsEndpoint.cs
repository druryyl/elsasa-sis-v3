namespace Elsasa.Sis.Finance.Features.Obligations.GetObligations;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying financial obligations with filters (FIN-WF-02, SCR-FIN-03, GAP-004, TD-04).
/// </summary>
public sealed class GetObligationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/obligations", HandleAsync)
            .WithName("GetObligations")
            .WithTags("FinanceObligations")
            .RequireAuthorization(Permissions.Obligations.View)
            .Produces<GetObligationsResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        Guid? studentId,
        string? status,
        Guid? academicPeriodId,
        string? chargeType,
        string? aging,
        string? search,
        int? pageNumber,
        int? page,
        int? pageSize,
        GetObligationsHandler handler,
        CancellationToken cancellationToken)
    {
        var resolvedPageNumber = pageNumber ?? page ?? 1;
        var resolvedPageSize = pageSize ?? 20;

        var result = await handler.HandleAsync(
            studentId,
            status,
            academicPeriodId,
            chargeType,
            aging,
            search,
            resolvedPageNumber,
            resolvedPageSize,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
