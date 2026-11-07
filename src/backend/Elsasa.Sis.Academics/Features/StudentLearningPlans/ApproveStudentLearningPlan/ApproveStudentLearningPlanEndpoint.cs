namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ApproveStudentLearningPlan;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Academic Authority approval of a student learning plan (UC-ACD-09, P3-S08).
/// </summary>
public sealed class ApproveStudentLearningPlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-plans/{id:guid}/approve", HandleAsync)
            .WithName("ApproveStudentLearningPlan")
            .WithTags("StudentLearningPlans")
            .RequireAuthorization(Permissions.Academics.LearningPlans.Approve)
            .WithValidation<ApproveStudentLearningPlanRequest>()
            .Produces<ApproveStudentLearningPlanResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] ApproveStudentLearningPlanRequest request,
        ApproveStudentLearningPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
