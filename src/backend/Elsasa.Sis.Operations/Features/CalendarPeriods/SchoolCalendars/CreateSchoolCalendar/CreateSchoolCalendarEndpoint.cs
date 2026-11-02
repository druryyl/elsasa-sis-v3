namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.CreateSchoolCalendar;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for creating a school calendar (ADR-005).
/// </summary>
public sealed class CreateSchoolCalendarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/academic-years/{academicYearId:guid}/calendars", HandleAsync)
            .WithName("CreateSchoolCalendar")
            .WithTags("SchoolCalendars")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .WithValidation<CreateSchoolCalendarRequest>()
            .Produces<CreateSchoolCalendarResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid academicYearId,
        CreateSchoolCalendarRequest request,
        CreateSchoolCalendarHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicYearId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/operations/calendars/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
