namespace Elsasa.Sis.Academics.Features.LearningStructures.SupersedeLearningStructure;

using Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructureById;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint branching an active learning structure into a new draft version (P3-S07, TD-03).
/// </summary>
public sealed class SupersedeLearningStructureEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-structures/{id:guid}/supersede", HandleAsync)
            .WithName("SupersedeLearningStructure")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.Approve)
            .WithValidation<SupersedeStructureRequest>()
            .Produces<LearningStructureDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        SupersedeStructureRequest request,
        SupersedeLearningStructureHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/learning-structures/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
