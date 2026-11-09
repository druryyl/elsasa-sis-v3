namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.CreateStudentLearningPlan;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for formulating an initial draft student learning plan (UC-ACD-09, P2-S04).
/// </summary>
public sealed class CreateStudentLearningPlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-plans", HandleAsync)
            .WithName("CreateStudentLearningPlan")
            .WithTags("StudentLearningPlans")
            .RequireAuthorization(Permissions.Academics.LearningPlans.Manage)
            .WithValidation<CreateStudentLearningPlanRequest>()
            .Produces<CreateStudentLearningPlanResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        CreateStudentLearningPlanRequest request,
        CreateStudentLearningPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/learning-plans/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
