namespace Elsasa.Sis.Teaching.Features.AssignmentCompletions.ConcludeTeachingAssignment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for concluding an approved teaching assignment and releasing workload capacity (TD-01, TD-04, P4-S13).
/// </summary>
public sealed class ConcludeTeachingAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/assignments/{id:guid}/conclude", HandleAsync)
            .WithName("ConcludeTeachingAssignment")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.Approve)
            .Produces<ConcludeTeachingAssignmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ConcludeTeachingAssignmentRequest request,
        ConcludeTeachingAssignmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
