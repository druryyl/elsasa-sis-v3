namespace Elsasa.Sis.Academics.Features.LearningStructures.SubmitLearningStructure;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint submitting a draft learning structure for authority review (P3-S07, TD-06).
/// </summary>
public sealed class SubmitLearningStructureEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-structures/{id:guid}/submit", HandleAsync)
            .WithName("SubmitLearningStructure")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.Submit)
            .Produces<SubmitLearningStructureResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        SubmitLearningStructureHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
