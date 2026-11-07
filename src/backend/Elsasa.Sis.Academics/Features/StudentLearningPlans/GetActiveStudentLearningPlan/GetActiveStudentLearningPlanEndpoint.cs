namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.GetActiveStudentLearningPlan;

using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying the active student learning plan for a student in an academic period (SCR-ACD-04, SCR-ACD-05, P2-S06).
/// </summary>
public sealed class GetActiveStudentLearningPlanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/students/{studentId:guid}/learning-plans/active", HandleAsync)
            .WithName("GetActiveStudentLearningPlan")
            .WithTags("StudentLearningPlans")
            .RequireAuthorization(Permissions.Academics.LearningPlans.View)
            .Produces<ActiveStudentLearningPlanDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        Guid academicPeriodId,
        GetActiveStudentLearningPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(studentId, academicPeriodId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
