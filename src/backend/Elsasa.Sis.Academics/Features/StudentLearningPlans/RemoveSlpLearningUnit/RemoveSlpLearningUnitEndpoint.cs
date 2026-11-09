namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.RemoveSlpLearningUnit;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for removing a constituent learning unit from a draft student learning plan (UC-ACD-09, P2-S05).
/// </summary>
public sealed class RemoveSlpLearningUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDelete("/api/v1/academics/learning-plans/{id:guid}/units/{unitId:guid}", HandleAsync)
            .WithName("RemoveSlpLearningUnit")
            .WithTags("StudentLearningPlans")
            .RequireAuthorization(Permissions.Academics.LearningPlans.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid unitId,
        RemoveSlpLearningUnitHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, unitId, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToProblemDetails();
    }
}
