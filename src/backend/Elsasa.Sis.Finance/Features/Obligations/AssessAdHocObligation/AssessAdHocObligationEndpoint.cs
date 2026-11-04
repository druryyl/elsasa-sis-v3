namespace Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for assessing an ad-hoc financial obligation for a single student (FIN-WF-02, UC-FIN-02, GAP-003, TD-04).
/// </summary>
public sealed class AssessAdHocObligationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/finance/obligations", HandleAsync)
            .WithName("AssessAdHocObligation")
            .WithTags("FinanceObligations")
            .RequireAuthorization(Permissions.Obligations.Assess)
            .WithValidation<AssessAdHocObligationRequest>()
            .Produces<AssessAdHocObligationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        AssessAdHocObligationRequest request,
        AssessAdHocObligationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/finance/obligations/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
