namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.GetOperationalPeriodsByAcademicYear;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for retrieving operational periods by academic year (ADR-005).
/// </summary>
public sealed class GetOperationalPeriodsByAcademicYearEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/operations/academic-years/{academicYearId:guid}/periods", HandleAsync)
            .WithName("GetOperationalPeriodsByAcademicYear")
            .WithTags("OperationalPeriods")
            .RequireAuthorization(Permissions.Operations.Calendar.View)
            .Produces<GetOperationalPeriodsByAcademicYearResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid academicYearId,
        GetOperationalPeriodsByAcademicYearHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicYearId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
