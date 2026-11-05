namespace Elsasa.Sis.Academics.Features.LearningActivities.PlanLearningActivity;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for planning an instructional session or homeroom activity (ADR-004, ADR-005, P3-S07).
/// </summary>
public sealed class PlanLearningActivityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-activities", HandleAsync)
            .WithName("PlanLearningActivity")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Activities.Manage)
            .WithValidation<PlanLearningActivityRequest>()
            .Produces<PlanLearningActivityResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        PlanLearningActivityRequest request,
        PlanLearningActivityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/learning-activities/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
