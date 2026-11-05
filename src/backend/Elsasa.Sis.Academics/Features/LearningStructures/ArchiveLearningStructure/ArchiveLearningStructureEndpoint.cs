namespace Elsasa.Sis.Academics.Features.LearningStructures.ArchiveLearningStructure;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint archiving a superseded or inactive learning structure (P3-S07, TD-06).
/// </summary>
public sealed class ArchiveLearningStructureEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-structures/{id:guid}/archive", HandleAsync)
            .WithName("ArchiveLearningStructure")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.Archive)
            .Produces<ArchiveLearningStructureResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ArchiveLearningStructureHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
