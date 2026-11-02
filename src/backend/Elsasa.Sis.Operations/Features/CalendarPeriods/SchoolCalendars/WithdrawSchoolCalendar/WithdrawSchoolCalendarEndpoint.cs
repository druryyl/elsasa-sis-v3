namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.WithdrawSchoolCalendar;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for withdrawing a school calendar (ADR-005).
/// </summary>
public sealed class WithdrawSchoolCalendarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/calendars/{id:guid}/withdraw", HandleAsync)
            .WithName("WithdrawSchoolCalendar")
            .WithTags("SchoolCalendars")
            .RequireAuthorization(Permissions.Operations.Calendar.Publish)
            .Produces<WithdrawSchoolCalendarResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        WithdrawSchoolCalendarHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
