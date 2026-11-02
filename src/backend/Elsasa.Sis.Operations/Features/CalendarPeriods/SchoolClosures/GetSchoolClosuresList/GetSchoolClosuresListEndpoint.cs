namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.GetSchoolClosuresList;

using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for listing school closures by academic year (ADR-005, UC-OPS-12).
/// </summary>
public sealed class GetSchoolClosuresListEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/operations/academic-years/{academicYearId:guid}/closures", HandleAsync)
            .WithName("GetSchoolClosuresList")
            .WithTags("SchoolClosures")
            .RequireAuthorization(Permissions.Operations.Calendar.View)
            .Produces<IReadOnlyList<SchoolClosureResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid academicYearId,
        GetSchoolClosuresListHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicYearId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
