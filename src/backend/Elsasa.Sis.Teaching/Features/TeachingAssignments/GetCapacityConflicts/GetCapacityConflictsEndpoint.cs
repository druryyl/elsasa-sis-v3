namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.GetCapacityConflicts;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying capacity conflicts worklist (TD-01, TD-05, P3-S07).
/// </summary>
public sealed class GetCapacityConflictsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/conflicts", HandleAsync)
            .WithName("GetCapacityConflicts")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.View)
            .Produces<PaginatedList<CapacityConflictDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicPeriodId,
        [FromQuery] string? severity,
        [FromQuery] string? status,
        [FromQuery] Guid? teacherProfileId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] GetCapacityConflictsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(
            academicPeriodId,
            severity,
            status,
            teacherProfileId,
            page,
            pageSize,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
