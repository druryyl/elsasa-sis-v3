namespace Elsasa.Sis.Academics.Features.ReportCards.ApproveReportCard;

using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for approving a student report card and freezing structured JSON snapshot (UC-ACD-13, GAP-006, TD-05, P5-S12).
/// </summary>
public sealed class ApproveReportCardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/report-cards/{id:guid}/approve", HandleAsync)
            .WithName("ApproveReportCard")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.ReportCards.Approve)
            .Produces<ReportCardDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] ApproveReportCardRequest? request,
        ApproveReportCardHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
