namespace Elsasa.Sis.Teaching.Features.TeachingHistory.GetTeacherHistory;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying a teacher's immutable history audit log (TD-07, P4-S14).
/// </summary>
public sealed class GetTeacherHistoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/teachers/{teacherId:guid}/history", HandleAsync)
            .WithName("GetTeacherHistory")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.View)
            .Produces<IReadOnlyList<TeachingHistoryEntryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid teacherId,
        [FromQuery] string? eventType,
        [FromServices] GetTeacherHistoryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(teacherId, eventType, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
