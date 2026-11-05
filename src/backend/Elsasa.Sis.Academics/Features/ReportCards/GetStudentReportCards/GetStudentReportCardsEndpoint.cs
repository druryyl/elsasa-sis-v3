namespace Elsasa.Sis.Academics.Features.ReportCards.GetStudentReportCards;

using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying published report cards in the self-service student/family learning portal (SCR-SELF-03, P5-S12).
/// </summary>
public sealed class GetStudentReportCardsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/portal/learning/report-cards", HandleAsync)
            .WithName("GetStudentReportCards")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.ReportCards.View)
            .Produces<IReadOnlyList<ReportCardSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? studentId,
        [FromQuery] Guid? academicPeriodId,
        GetStudentReportCardsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            studentIdOverride: studentId,
            academicPeriodId: academicPeriodId,
            cancellationToken: cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
