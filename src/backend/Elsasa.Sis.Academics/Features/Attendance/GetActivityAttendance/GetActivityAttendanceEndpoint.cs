namespace Elsasa.Sis.Academics.Features.Attendance.GetActivityAttendance;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying session student roster attendance with historical correction logs (ADR-004, ADR-005, P4-S09, TD-04).
/// </summary>
public sealed class GetActivityAttendanceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-activities/{id:guid}/attendance", HandleAsync)
            .WithName("GetActivityAttendance")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Attendance.View)
            .Produces<IReadOnlyList<ActivityAttendanceRecordDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetActivityAttendanceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
