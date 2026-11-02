namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.DeleteCalendarEntry;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for deleting a calendar entry (ADR-005).
/// </summary>
public sealed class DeleteCalendarEntryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDelete("/api/operations/calendars/{calendarId:guid}/entries/{entryId:guid}", HandleAsync)
            .WithName("DeleteCalendarEntry")
            .WithTags("CalendarEntries")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid calendarId,
        Guid entryId,
        DeleteCalendarEntryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(calendarId, entryId, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToProblemDetails();
    }
}
