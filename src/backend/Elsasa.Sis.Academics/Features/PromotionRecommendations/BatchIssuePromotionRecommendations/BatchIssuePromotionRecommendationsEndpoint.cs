namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.BatchIssuePromotionRecommendations;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for batch issuing cohort academic progression recommendations (P3-S08, UC-ACD-17, TD-03).
/// </summary>
public sealed class BatchIssuePromotionRecommendationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/promotion-recommendations/batch", HandleAsync)
            .WithName("BatchIssuePromotionRecommendations")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Progression.BatchIssue)
            .WithValidation<BatchIssuePromotionRecommendationsCommand>()
            .Produces<BatchIssuePromotionRecommendationsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        BatchIssuePromotionRecommendationsCommand command,
        BatchIssuePromotionRecommendationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
