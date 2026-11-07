namespace Elsasa.Sis.Academics.Features.AcademicInterventions.GetAtRiskLearners;

using Elsasa.Sis.Academics.Features.AcademicInterventions;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying dynamically evaluated at-risk learners (UC-ACD-11, P4-S10, TD-04).
/// </summary>
public sealed class GetAtRiskLearnersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/interventions/at-risk", HandleAsync)
            .WithName("GetAtRiskLearners")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Interventions.View)
            .Produces<IReadOnlyList<AtRiskLearnerDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid academicPeriodId,
        [FromQuery] string? gradeLevel,
        GetAtRiskLearnersHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAtRiskLearnersQuery(academicPeriodId, gradeLevel), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
