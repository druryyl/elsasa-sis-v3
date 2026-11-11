namespace Elsasa.Sis.Teaching.Features.Substitutions.ArrangeEmergencySubstitution;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for arranging an emergency substitution (TD-05, P4-S11).
/// </summary>
public sealed class ArrangeEmergencySubstitutionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/substitutions", HandleAsync)
            .WithName("ArrangeEmergencySubstitution")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Substitutions.Arrange)
            .WithValidation<ArrangeEmergencySubstitutionRequest>()
            .Produces<ArrangeEmergencySubstitutionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        ArrangeEmergencySubstitutionRequest request,
        ArrangeEmergencySubstitutionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/substitutions/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
