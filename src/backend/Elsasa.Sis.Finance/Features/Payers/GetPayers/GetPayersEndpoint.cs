namespace Elsasa.Sis.Finance.Features.Payers.GetPayers;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying payers with search filtering and pagination (SCR-FIN-02, TD-03).
/// </summary>
public sealed class GetPayersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/payers", HandleAsync)
            .WithName("GetPayers")
            .WithTags("FinancePayers")
            .RequireAuthorization(Permissions.Finance.Payers.View)
            .Produces<GetPayersResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] GetPayersRequest request,
        GetPayersHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
