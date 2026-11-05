namespace Elsasa.Sis.Academics.Features.RecordCorrections.GetResultCorrectionHistory;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying the audit log of alterations for an academic result (UC-ACD-19, TD-06, P6-S14).
/// </summary>
public sealed class GetResultCorrectionHistoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/academic-results/{id:guid}/corrections", HandleAsync)
            .WithName("GetResultCorrectionHistory")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Corrections.ViewHistory)
            .Produces<IReadOnlyList<AcademicRecordCorrectionDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetResultCorrectionHistoryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
