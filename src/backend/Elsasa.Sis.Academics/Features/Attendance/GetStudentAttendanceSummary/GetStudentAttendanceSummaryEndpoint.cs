namespace Elsasa.Sis.Academics.Features.Attendance.GetStudentAttendanceSummary;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying aggregated student attendance summary (ADR-004, ADR-005, P4-S09, TD-04).
/// </summary>
public sealed class GetStudentAttendanceSummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/students/{studentId:guid}/attendance-summary", HandleAsync)
            .WithName("GetStudentAttendanceSummary")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Attendance.View)
            .Produces<GetStudentAttendanceSummaryResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        [FromQuery] Guid? academicPeriodId,
        GetStudentAttendanceSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(studentId, academicPeriodId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
