namespace Elsasa.Sis.Teaching.Features.Reassignments.GetTeachingReassignments;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying teaching reassignments worklist (TD-01, P4-S12).
/// </summary>
public sealed class GetTeachingReassignmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/reassignments", HandleAsync)
            .WithName("GetTeachingReassignments")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.View)
            .Produces<PaginatedList<TeachingReassignmentDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? originalAssignmentId,
        [FromQuery] Guid? predecessorTeacherId,
        [FromQuery] Guid? replacementTeacherId,
        [FromQuery] Guid? newTeacherId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] GetTeachingReassignmentsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(
            originalAssignmentId,
            predecessorTeacherId,
            replacementTeacherId,
            newTeacherId,
            status,
            page,
            pageSize,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
