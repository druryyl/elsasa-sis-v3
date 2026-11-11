namespace Elsasa.Sis.Teaching.Features.TeacherEligibility.EvaluateCandidateEligibility;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for candidate teacher eligibility evaluation (TD-03, P2-S05).
/// Evaluates the 5 policy criteria in memory and persists the explainable determination.
/// </summary>
public sealed class EvaluateCandidateEligibilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/eligibility/evaluate", HandleAsync)
            .WithName("EvaluateCandidateEligibility")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.Manage)
            .WithValidation<EvaluateCandidateEligibilityRequest>()
            .Produces<EvaluateCandidateEligibilityResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        EvaluateCandidateEligibilityRequest request,
        EvaluateCandidateEligibilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
