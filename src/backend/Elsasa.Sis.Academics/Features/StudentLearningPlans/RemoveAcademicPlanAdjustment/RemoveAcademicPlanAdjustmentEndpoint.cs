namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.RemoveAcademicPlanAdjustment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for removing an academic plan adjustment from a draft student learning plan (UC-ACD-09, P3-S07).
/// </summary>
public sealed class RemoveAcademicPlanAdjustmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDelete("/api/v1/academics/learning-plans/{id:guid}/adjustments/{adjustmentId:guid}", HandleAsync)
            .WithName("RemoveAcademicPlanAdjustment")
            .WithTags("StudentLearningPlans")
            .RequireAuthorization(Permissions.Academics.LearningPlans.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid adjustmentId,
        RemoveAcademicPlanAdjustmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, adjustmentId, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToProblemDetails();
    }
}
