namespace Elsasa.Sis.Operations.Features.CalendarPeriods.RecordCorrections.GetRecordCorrectionsList;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for retrieving record corrections for an academic year (ADR-005, UC-OPS-13).
/// </summary>
public sealed class GetRecordCorrectionsListEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/operations/academic-years/{academicYearId:guid}/corrections", HandleAsync)
            .WithName("GetRecordCorrectionsList")
            .WithTags("RecordCorrections")
            .RequireAuthorization(Permissions.Operations.Calendar.View)
            .Produces<IReadOnlyList<RecordCorrectionItemResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid academicYearId,
        GetRecordCorrectionsListHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicYearId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
