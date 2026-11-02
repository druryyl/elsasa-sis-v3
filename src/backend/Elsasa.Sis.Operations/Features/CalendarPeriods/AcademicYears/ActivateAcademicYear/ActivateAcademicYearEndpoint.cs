namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ActivateAcademicYear;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for activating an academic year (ADR-005).
/// </summary>
public sealed class ActivateAcademicYearEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/academic-years/{id:guid}/activate", HandleAsync)
            .WithName("ActivateAcademicYear")
            .WithTags("AcademicYears")
            .RequireAuthorization(Permissions.Operations.Calendar.Approve)
            .Produces<ActivateAcademicYearResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ActivateAcademicYearHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
