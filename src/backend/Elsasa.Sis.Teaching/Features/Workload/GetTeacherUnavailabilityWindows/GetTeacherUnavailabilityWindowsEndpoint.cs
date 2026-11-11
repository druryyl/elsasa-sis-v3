namespace Elsasa.Sis.Teaching.Features.Workload.GetTeacherUnavailabilityWindows;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying a teacher's leave and blackout windows (P3-S08).
/// </summary>
public sealed class GetTeacherUnavailabilityWindowsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/teachers/{teacherId:guid}/unavailability", HandleAsync)
            .WithName("GetTeacherUnavailabilityWindows")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.View)
            .Produces<IReadOnlyList<TeacherUnavailabilityWindowDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid teacherId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromServices] GetTeacherUnavailabilityWindowsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            teacherId,
            status,
            fromDate,
            toDate,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
