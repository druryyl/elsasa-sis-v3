namespace Elsasa.Sis.Teaching.Features.Reassignments.ApprovePermanentReassignment;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for approving a permanent teaching responsibility reassignment (TD-01, TD-04, P4-S12).
/// </summary>
public sealed class ApprovePermanentReassignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/reassignments/{id:guid}/approve", HandleAsync)
            .WithName("ApprovePermanentReassignment")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.Approve)
            .Produces<ApprovePermanentReassignmentResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] ApprovePermanentReassignmentRequest? request,
        [FromServices] ApprovePermanentReassignmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request ?? new ApprovePermanentReassignmentRequest(), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
