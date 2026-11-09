namespace Elsasa.Sis.Academics.Features.PromotionRecommendations.GetCohortPromotionRecommendations;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying committed academic progression recommendations for a cohort in an academic period and grade level (TD-03, UC-ACD-17, P3-S09, SCR-ACD-09).
/// </summary>
public sealed class GetCohortPromotionRecommendationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/cohort-review/promotion-recommendations", HandleAsync)
            .WithName("GetCohortPromotionRecommendations")
            .WithTags("PromotionRecommendations")
            .RequireAuthorization(Permissions.Academics.Progression.View)
            .Produces<IReadOnlyList<CohortPromotionRecommendationItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid academicPeriodId,
        [FromQuery] string? gradeLevel,
        GetCohortPromotionRecommendationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicPeriodId, gradeLevel, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
