namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.IssuePromotionRecommendation;

using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for issuing a single student's academic progression recommendation (P3-S08, UC-ACD-17).
/// </summary>
public sealed class IssuePromotionRecommendationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/promotion-recommendations", HandleAsync)
            .WithName("IssuePromotionRecommendation")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Progression.Issue)
            .WithValidation<IssuePromotionRecommendationCommand>()
            .Produces<PromotionRecommendationDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        IssuePromotionRecommendationCommand command,
        IssuePromotionRecommendationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
