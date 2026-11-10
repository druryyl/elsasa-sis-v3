namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.GrantTeachingAuthorization;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Teaching Authority to grant teaching authorizations with scope definitions, evidence linking, and permissive warnings (TD-05, P4-S11).
/// </summary>
public sealed class GrantTeachingAuthorizationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/teachers/{id:guid}/authorizations", HandleAsync)
            .WithName("GrantTeachingAuthorization")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Authorize)
            .WithValidation<GrantTeachingAuthorizationRequest>()
            .Produces<GrantTeachingAuthorizationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GrantTeachingAuthorizationRequest request,
        GrantTeachingAuthorizationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/teachers/{id}/authorizations/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
