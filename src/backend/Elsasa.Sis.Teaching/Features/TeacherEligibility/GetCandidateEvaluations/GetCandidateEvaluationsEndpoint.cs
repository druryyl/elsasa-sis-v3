namespace Elsasa.Sis.Teaching.Features.TeacherEligibility.GetCandidateEvaluations;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying candidate teacher eligibility evaluations (TD-03, P2-S05).
/// Filterable by teachingDemandId or teacherProfileId.
/// </summary>
public sealed class GetCandidateEvaluationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/eligibility/evaluations", HandleAsync)
            .WithName("GetCandidateEvaluations")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.View)
            .Produces<IReadOnlyList<CandidateEvaluationSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? teachingDemandId,
        [FromQuery] Guid? teacherProfileId,
        [FromServices] GetCandidateEvaluationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            teachingDemandId,
            teacherProfileId,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
