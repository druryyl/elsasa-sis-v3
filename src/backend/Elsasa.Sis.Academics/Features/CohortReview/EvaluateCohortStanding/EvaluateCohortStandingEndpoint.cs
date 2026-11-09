namespace Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortStanding;

using Elsasa.Sis.Academics.Features.CohortReview;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for evaluating cohort academic standing proposals dynamically (TD-03, UC-ACD-17, P3-S06).
/// </summary>
public sealed class EvaluateCohortStandingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/cohort-review/evaluate-standing", HandleAsync)
            .WithName("EvaluateCohortStanding")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Standings.Evaluate)
            .WithValidation<EvaluateCohortStandingRequest>()
            .Produces<IReadOnlyList<CohortStandingProposalDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        EvaluateCohortStandingRequest request,
        EvaluateCohortStandingHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
