namespace Elsasa.Sis.Academics.Features.ReportCards.CompileReportCard;

using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for compiling an individual student draft report card (UC-ACD-13, ACD-WF-12, P5-S11).
/// </summary>
public sealed class CompileReportCardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/report-cards/compile", HandleAsync)
            .WithName("CompileReportCard")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.ReportCards.Compile)
            .WithValidation<CompileReportCardRequest>()
            .Produces<ReportCardDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        CompileReportCardRequest request,
        CompileReportCardHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/report-cards/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
