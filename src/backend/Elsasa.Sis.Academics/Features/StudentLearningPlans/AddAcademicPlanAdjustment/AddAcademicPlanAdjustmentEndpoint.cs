namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.AddAcademicPlanAdjustment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for adding an academic plan adjustment to a draft student learning plan (UC-ACD-09, P3-S07).
/// </summary>
public sealed class AddAcademicPlanAdjustmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-plans/{id:guid}/adjustments", HandleAsync)
            .WithName("AddAcademicPlanAdjustment")
            .WithTags("StudentLearningPlans")
            .RequireAuthorization(Permissions.Academics.LearningPlans.Manage)
            .WithValidation<AddAcademicPlanAdjustmentRequest>()
            .Produces<AddAcademicPlanAdjustmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        AddAcademicPlanAdjustmentRequest request,
        AddAcademicPlanAdjustmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/learning-plans/{id}/adjustments/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
