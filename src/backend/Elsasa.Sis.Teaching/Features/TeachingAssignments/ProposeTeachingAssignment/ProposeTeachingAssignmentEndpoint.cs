namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ProposeTeachingAssignment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for proposing a new teaching assignment (TD-01, TD-04, P3-S06).
/// </summary>
public sealed class ProposeTeachingAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/assignments/propose", HandleAsync)
            .WithName("ProposeTeachingAssignment")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.Manage)
            .WithValidation<ProposeTeachingAssignmentRequest>()
            .Produces<ProposeTeachingAssignmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        ProposeTeachingAssignmentRequest request,
        ProposeTeachingAssignmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/assignments/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
