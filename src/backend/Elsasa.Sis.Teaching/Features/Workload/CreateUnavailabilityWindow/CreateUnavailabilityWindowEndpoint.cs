namespace Elsasa.Sis.Teaching.Features.Workload.CreateUnavailabilityWindow;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for recording a temporary leave or blackout window (P3-S08).
/// </summary>
public sealed class CreateUnavailabilityWindowEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/teachers/{teacherId:guid}/unavailability", HandleAsync)
            .WithName("CreateUnavailabilityWindow")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Availability.Declare)
            .WithValidation<CreateUnavailabilityWindowRequest>()
            .Produces<CreateUnavailabilityWindowResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid teacherId,
        [FromBody] CreateUnavailabilityWindowRequest request,
        [FromServices] CreateUnavailabilityWindowHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(teacherId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/teachers/{teacherId}/unavailability", result.Value)
            : result.Error.ToProblemDetails();
    }
}
