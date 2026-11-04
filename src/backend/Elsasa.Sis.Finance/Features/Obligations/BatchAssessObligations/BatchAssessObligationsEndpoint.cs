namespace Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for batch obligation assessment (POST /api/finance/obligations/batch-assess) (FIN-WF-02, UC-FIN-02, SCR-FIN-01, ADR-004, ADR-009).
/// </summary>
public sealed class BatchAssessObligationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/finance/obligations/batch-assess", HandleAsync)
            .WithName("BatchAssessObligations")
            .WithTags("Obligations")
            .RequireAuthorization(Permissions.Finance.Obligations.Assess)
            .WithValidation<BatchAssessObligationsRequest>()
            .Produces<BatchAssessObligationsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        BatchAssessObligationsRequest request,
        BatchAssessObligationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
