namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.GetAcademicYearsList;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for listing academic years (ADR-005).
/// </summary>
public sealed class GetAcademicYearsListEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/operations/academic-years", HandleAsync)
            .WithName("GetAcademicYearsList")
            .WithTags("AcademicYears")
            .RequireAuthorization(Permissions.Operations.Calendar.View)
            .Produces<IReadOnlyList<AcademicYearListItemResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        string? status,
        GetAcademicYearsListHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(status, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
