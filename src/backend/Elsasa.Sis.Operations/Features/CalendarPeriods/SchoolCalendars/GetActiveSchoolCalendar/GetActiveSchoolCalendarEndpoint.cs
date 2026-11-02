namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.GetActiveSchoolCalendar;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for retrieving the active school calendar of an academic year (ADR-005).
/// </summary>
public sealed class GetActiveSchoolCalendarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/operations/academic-years/{academicYearId:guid}/calendars/active", HandleAsync)
            .WithName("GetActiveSchoolCalendar")
            .WithTags("SchoolCalendars")
            .RequireAuthorization(Permissions.Operations.Calendar.View)
            .Produces<ActiveSchoolCalendarResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid academicYearId,
        GetActiveSchoolCalendarHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicYearId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
