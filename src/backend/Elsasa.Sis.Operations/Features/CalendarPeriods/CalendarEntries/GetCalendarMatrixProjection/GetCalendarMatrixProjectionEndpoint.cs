namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.GetCalendarMatrixProjection;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for retrieving the synthesized calendar matrix projection (ADR-005, TD-04).
/// </summary>
public sealed class GetCalendarMatrixProjectionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/operations/calendars/{calendarId:guid}/matrix", HandleAsync)
            .WithName("GetCalendarMatrixProjection")
            .WithTags("CalendarEntries")
            .RequireAuthorization(Permissions.Operations.Calendar.View)
            .Produces<CalendarMatrixProjectionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid calendarId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        GetCalendarMatrixProjectionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(calendarId, startDate, endDate, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
