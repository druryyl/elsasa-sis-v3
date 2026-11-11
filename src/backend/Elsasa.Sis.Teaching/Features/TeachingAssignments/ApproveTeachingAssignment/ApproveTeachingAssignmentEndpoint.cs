namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ApproveTeachingAssignment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for approving a proposed teaching assignment (TD-01, TD-04, P3-S06).
/// </summary>
public sealed class ApproveTeachingAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/assignments/{id:guid}/approve", HandleAsync)
            .WithName("ApproveTeachingAssignment")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.Approve)
            .Produces<ApproveTeachingAssignmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApproveTeachingAssignmentRequest? request,
        ApproveTeachingAssignmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
