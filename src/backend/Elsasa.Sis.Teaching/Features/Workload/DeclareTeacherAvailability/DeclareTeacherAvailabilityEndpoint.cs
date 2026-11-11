namespace Elsasa.Sis.Teaching.Features.Workload.DeclareTeacherAvailability;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for declaring recurring weekly teacher availability (P3-S08).
/// </summary>
public sealed class DeclareTeacherAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/teachers/{teacherId:guid}/availability", HandleAsync)
            .WithName("DeclareTeacherAvailability")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Availability.Declare)
            .WithValidation<DeclareTeacherAvailabilityRequest>()
            .Produces<DeclareTeacherAvailabilityResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid teacherId,
        [FromBody] DeclareTeacherAvailabilityRequest request,
        [FromServices] DeclareTeacherAvailabilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(teacherId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/teachers/{teacherId}/availability", result.Value)
            : result.Error.ToProblemDetails();
    }
}
