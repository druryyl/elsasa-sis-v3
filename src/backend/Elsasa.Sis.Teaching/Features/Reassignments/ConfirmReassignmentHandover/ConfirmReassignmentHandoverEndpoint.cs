namespace Elsasa.Sis.Teaching.Features.Reassignments.ConfirmReassignmentHandover;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for confirming reassignment handover (TD-01, P4-S12).
/// </summary>
public sealed class ConfirmReassignmentHandoverEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/reassignments/{id:guid}/handover", HandleAsync)
            .WithName("ConfirmReassignmentHandover")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.Manage)
            .Produces<ConfirmReassignmentHandoverResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] ConfirmReassignmentHandoverRequest? request,
        [FromServices] ConfirmReassignmentHandoverHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request ?? new ConfirmReassignmentHandoverRequest(), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
