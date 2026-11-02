namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarAmendments.GetCalendarAmendmentsList;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for retrieving the amendment history of a school calendar (ADR-005, TD-05).
/// </summary>
public sealed class GetCalendarAmendmentsListEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/operations/calendars/{calendarId:guid}/amendments", HandleAsync)
            .WithName("GetCalendarAmendmentsList")
            .WithTags("CalendarAmendments")
            .RequireAuthorization(Permissions.Operations.Calendar.View)
            .Produces<IReadOnlyList<CalendarAmendmentResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid calendarId,
        GetCalendarAmendmentsListHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(calendarId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
