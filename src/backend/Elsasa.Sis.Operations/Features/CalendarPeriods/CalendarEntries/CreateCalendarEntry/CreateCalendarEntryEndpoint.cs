namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.CreateCalendarEntry;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for creating a calendar entry (ADR-005).
/// </summary>
public sealed class CreateCalendarEntryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/calendars/{calendarId:guid}/entries", HandleAsync)
            .WithName("CreateCalendarEntry")
            .WithTags("CalendarEntries")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .WithValidation<CreateCalendarEntryRequest>()
            .Produces<CreateCalendarEntryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid calendarId,
        CreateCalendarEntryRequest request,
        CreateCalendarEntryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(calendarId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/operations/calendars/{calendarId}/entries/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
