namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.ResolveSupervisionRequirement;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Teaching Authority to conclude and resolve supervision requirements (TD-05, P4-S13).
/// </summary>
public sealed class ResolveSupervisionRequirementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/supervision/{id:guid}/resolve", HandleAsync)
            .WithName("ResolveSupervisionRequirement")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Authorize)
            .WithValidation<ResolveSupervisionRequirementRequest>()
            .Produces<ResolveSupervisionRequirementResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ResolveSupervisionRequirementRequest request,
        ResolveSupervisionRequirementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
