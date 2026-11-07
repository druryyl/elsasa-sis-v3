namespace Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortProgression;

using Elsasa.Sis.Academics.Features.CohortReview;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for evaluating cohort grade progression recommendation proposals dynamically (TD-03, UC-ACD-17, P3-S06).
/// </summary>
public sealed class EvaluateCohortProgressionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/cohort-review/evaluate-progression", HandleAsync)
            .WithName("EvaluateCohortProgression")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Progression.Evaluate)
            .WithValidation<EvaluateCohortProgressionRequest>()
            .Produces<IReadOnlyList<CohortProgressionProposalDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        EvaluateCohortProgressionRequest request,
        EvaluateCohortProgressionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
