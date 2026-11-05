namespace Elsasa.Sis.Academics.Features.LearningActivities.CancelLearningActivity;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for cancelling a planned learning activity (ADR-004, ADR-005, P3-S07).
/// </summary>
public sealed class CancelLearningActivityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-activities/{id:guid}/cancel", HandleAsync)
            .WithName("CancelLearningActivity")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Activities.Manage)
            .WithValidation<CancelLearningActivityRequest>()
            .Produces<CancelLearningActivityResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        CancelLearningActivityRequest request,
        CancelLearningActivityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
