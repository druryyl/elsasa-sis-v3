namespace Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for auditable attendance corrections (ADR-004, ADR-005, P4-S09, TD-04, UC-ACD-15).
/// </summary>
public sealed class CorrectAttendanceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/attendance-records/{id:guid}/correct", HandleAsync)
            .WithName("CorrectAttendance")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Attendance.Correct)
            .WithValidation<CorrectAttendanceRequest>()
            .Produces<CorrectAttendanceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        CorrectAttendanceRequest request,
        CorrectAttendanceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
