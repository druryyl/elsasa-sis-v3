namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.UpdateStudentLearningPlan;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for updating draft student learning plan notes (UC-ACD-09, P2-S04).
/// </summary>
public sealed class UpdateStudentLearningPlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/v1/academics/learning-plans/{id:guid}", HandleAsync)
            .WithName("UpdateStudentLearningPlan")
            .WithTags("StudentLearningPlans")
            .RequireAuthorization(Permissions.Academics.LearningPlans.Manage)
            .WithValidation<UpdateStudentLearningPlanRequest>()
            .Produces<UpdateStudentLearningPlanResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateStudentLearningPlanRequest request,
        UpdateStudentLearningPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
