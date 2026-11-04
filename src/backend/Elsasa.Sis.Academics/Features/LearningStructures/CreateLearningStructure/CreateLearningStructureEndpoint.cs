namespace Elsasa.Sis.Academics.Features.LearningStructures.CreateLearningStructure;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for drafting a new learning structure framework (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed class CreateLearningStructureEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-structures", HandleAsync)
            .WithName("CreateLearningStructure")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.Manage)
            .WithValidation<CreateLearningStructureRequest>()
            .Produces<CreateLearningStructureResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        CreateLearningStructureRequest request,
        CreateLearningStructureHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/learning-structures/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
