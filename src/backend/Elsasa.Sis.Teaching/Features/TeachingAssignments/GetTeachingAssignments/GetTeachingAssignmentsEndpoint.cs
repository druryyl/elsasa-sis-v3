namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.GetTeachingAssignments;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying teaching assignments worklist (TD-01, P3-S06).
/// </summary>
public sealed class GetTeachingAssignmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/assignments", HandleAsync)
            .WithName("GetTeachingAssignments")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.View)
            .Produces<PaginatedList<TeachingAssignmentDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicPeriodId,
        [FromQuery] string? status,
        [FromQuery] Guid? teacherProfileId,
        [FromQuery] string? scopeType,
        [FromQuery] Guid? assignmentRoleId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] GetTeachingAssignmentsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(
            academicPeriodId,
            status,
            teacherProfileId,
            scopeType,
            assignmentRoleId,
            search,
            page,
            pageSize,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
