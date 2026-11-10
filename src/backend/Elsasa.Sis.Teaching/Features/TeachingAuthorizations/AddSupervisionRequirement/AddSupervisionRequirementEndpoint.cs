namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.AddSupervisionRequirement;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Teaching Authority to attach supervision requirements to a teaching authorization (TD-05, P4-S13).
/// </summary>
public sealed class AddSupervisionRequirementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/authorizations/{id:guid}/supervision", HandleAsync)
            .WithName("AddSupervisionRequirement")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Authorize)
            .WithValidation<AddSupervisionRequirementRequest>()
            .Produces<AddSupervisionRequirementResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        AddSupervisionRequirementRequest request,
        AddSupervisionRequirementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/authorizations/{id}/supervision/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
