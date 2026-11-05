namespace Elsasa.Sis.Academics.Features.Attendance.RecordSessionAttendance;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for recording live session roll-call attendance with 24-hour grace window (ADR-004, ADR-005, P4-S08, TD-04, UC-ACD-03).
/// </summary>
public sealed class RecordSessionAttendanceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-activities/{id:guid}/attendance", HandleAsync)
            .WithName("RecordSessionAttendance")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Attendance.Record)
            .WithValidation<RecordSessionAttendanceRequest>()
            .Produces<RecordSessionAttendanceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        RecordSessionAttendanceRequest request,
        RecordSessionAttendanceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
