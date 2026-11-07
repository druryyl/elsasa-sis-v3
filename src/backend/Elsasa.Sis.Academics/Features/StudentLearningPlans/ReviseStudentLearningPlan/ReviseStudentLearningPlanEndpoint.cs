namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ReviseStudentLearningPlan;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for cloning an active student learning plan into a new draft revision (UC-ACD-09, P3-S09, TD-02).
/// </summary>
public sealed class ReviseStudentLearningPlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-plans/{id:guid}/revise", HandleAsync)
            .WithName("ReviseStudentLearningPlan")
            .WithTags("StudentLearningPlans")
            .RequireAuthorization(Permissions.Academics.LearningPlans.Revise)
            .WithValidation<ReviseStudentLearningPlanRequest>()
            .Produces<ReviseStudentLearningPlanResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] ReviseStudentLearningPlanRequest request,
        ReviseStudentLearningPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/learning-plans/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
