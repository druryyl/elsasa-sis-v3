namespace Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructureById;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for retrieving a single learning structure framework by identifier (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed class GetLearningStructureByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-structures/{id:guid}", HandleAsync)
            .WithName("GetLearningStructureById")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.View)
            .Produces<LearningStructureDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetLearningStructureByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
