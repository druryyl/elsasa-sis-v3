namespace Elsasa.Sis.Academics.Features.ReportCards.BatchCompileCohortReportCards;

using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for batch compiling cohort report cards via set-based SQL (TD-07, UC-ACD-13, P5-S11).
/// </summary>
public sealed class BatchCompileCohortReportCardsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/report-cards/batch-compile", HandleAsync)
            .WithName("BatchCompileCohortReportCards")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.ReportCards.Compile)
            .WithValidation<BatchCompileCohortReportCardsRequest>()
            .Produces<BatchCompilationSummaryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        BatchCompileCohortReportCardsRequest request,
        BatchCompileCohortReportCardsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
