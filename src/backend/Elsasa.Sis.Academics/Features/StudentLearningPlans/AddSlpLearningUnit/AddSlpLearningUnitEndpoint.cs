namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.AddSlpLearningUnit;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for adding a constituent learning unit to a draft student learning plan (UC-ACD-09, P2-S05).
/// </summary>
public sealed class AddSlpLearningUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-plans/{id:guid}/units", HandleAsync)
            .WithName("AddSlpLearningUnit")
            .WithTags("StudentLearningPlans")
            .RequireAuthorization(Permissions.Academics.LearningPlans.Manage)
            .WithValidation<AddSlpLearningUnitRequest>()
            .Produces<AddSlpLearningUnitResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] AddSlpLearningUnitRequest request,
        AddSlpLearningUnitHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/learning-plans/{id}/units/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
