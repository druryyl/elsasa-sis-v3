namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.GetSchoolCalendarsList;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for listing school calendars by academic year (ADR-005).
/// </summary>
public sealed class GetSchoolCalendarsListEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/operations/academic-years/{academicYearId:guid}/calendars", HandleAsync)
            .WithName("GetSchoolCalendarsList")
            .WithTags("SchoolCalendars")
            .RequireAuthorization(Permissions.Operations.Calendar.View)
            .Produces<IReadOnlyList<SchoolCalendarResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid academicYearId,
        GetSchoolCalendarsListHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicYearId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}