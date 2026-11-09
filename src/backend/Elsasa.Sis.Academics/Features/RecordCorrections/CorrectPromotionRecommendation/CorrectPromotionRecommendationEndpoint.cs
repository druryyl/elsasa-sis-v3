namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectPromotionRecommendation;

using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Academic Authorities to perform auditable in-place corrections to approved promotion recommendations (P5-S12, UC-ACD-15, TD-05).
/// </summary>
public sealed class CorrectPromotionRecommendationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/promotion-recommendations/{id:guid}/correct", HandleAsync)
            .WithName("CorrectPromotionRecommendation")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Corrections.CorrectProgression)
            .WithValidation<CorrectPromotionRecommendationRequest>()
            .Produces<PromotionRecommendationDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        CorrectPromotionRecommendationRequest request,
        CorrectPromotionRecommendationHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CorrectPromotionRecommendationCommand(
            id,
            request.Recommendation,
            request.FromLevel,
            request.RecommendedToLevel,
            request.Conditions,
            request.SupportingEvidence,
            request.IsException,
            request.ExceptionReason,
            request.CommitteeReference,
            request.Reason);

        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
