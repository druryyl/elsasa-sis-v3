namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.PublishSchoolCalendar;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for publishing a school calendar (ADR-005).
/// </summary>
public sealed class PublishSchoolCalendarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/calendars/{id:guid}/publish", HandleAsync)
            .WithName("PublishSchoolCalendar")
            .WithTags("SchoolCalendars")
            .RequireAuthorization(Permissions.Operations.Calendar.Publish)
            .Produces<PublishSchoolCalendarResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        PublishSchoolCalendarHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
