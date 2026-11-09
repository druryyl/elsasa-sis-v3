namespace Elsasa.Sis.Academics.Features.Assessments.GetAssessmentsByUnit;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying assessments of a learning unit (P2-S05).
/// </summary>
public sealed class GetAssessmentsByUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-units/{unitId:guid}/assessments", HandleAsync)
            .WithName("GetAssessmentsByUnit")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assessments.View)
            .Produces<IReadOnlyList<AssessmentSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid unitId,
        string? status,
        string? assessmentType,
        DateOnly? fromDate,
        DateOnly? toDate,
        GetAssessmentsByUnitHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            unitId,
            status,
            assessmentType,
            fromDate,
            toDate,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
