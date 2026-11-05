namespace Elsasa.Sis.Academics.Features.LearningUnits.CreateLearningUnit;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for creating a learning unit in a learning structure (ADR-004, ADR-005, P3-S08).
/// </summary>
public sealed class CreateLearningUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-structures/{structureId:guid}/units", HandleAsync)
            .WithName("CreateLearningUnit")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.Manage)
            .WithValidation<CreateLearningUnitRequest>()
            .Produces<CreateLearningUnitResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid structureId,
        CreateLearningUnitRequest request,
        CreateLearningUnitHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(structureId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/learning-units/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
