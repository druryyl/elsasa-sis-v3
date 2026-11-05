namespace Elsasa.Sis.Academics.Features.AcademicResults.GetResultsByAssessment;

using Elsasa.Sis.Academics.Features.AcademicResults;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying assessment results roster with attempt lineage and scoring (SCR-ACD-02, P4-S10).
/// </summary>
public sealed class GetResultsByAssessmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/assessments/{id:guid}/results", HandleAsync)
            .WithName("GetResultsByAssessment")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Results.View)
            .Produces<IReadOnlyList<AcademicResultDetailDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromQuery] string? status,
        [FromQuery] bool? onlyFinal,
        GetResultsByAssessmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            assessmentId: id,
            status: status,
            onlyFinal: onlyFinal ?? true,
            cancellationToken: cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
