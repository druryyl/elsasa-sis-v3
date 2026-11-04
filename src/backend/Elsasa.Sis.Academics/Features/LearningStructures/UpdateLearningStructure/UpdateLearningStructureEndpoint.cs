namespace Elsasa.Sis.Academics.Features.LearningStructures.UpdateLearningStructure;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for updating metadata of a learning structure framework (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed class UpdateLearningStructureEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/v1/academics/learning-structures/{id:guid}", HandleAsync)
            .WithName("UpdateLearningStructure")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.Manage)
            .WithValidation<UpdateLearningStructureRequest>()
            .Produces<UpdateLearningStructureResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateLearningStructureRequest request,
        UpdateLearningStructureHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
