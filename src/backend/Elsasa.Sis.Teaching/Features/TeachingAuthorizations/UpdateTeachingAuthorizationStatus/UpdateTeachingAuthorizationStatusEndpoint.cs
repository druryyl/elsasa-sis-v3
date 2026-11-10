namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.UpdateTeachingAuthorizationStatus;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Teaching Authority to transition authorization lifecycle status (TD-05, P4-S12).
/// </summary>
public sealed class UpdateTeachingAuthorizationStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/authorizations/{id:guid}/status", HandleAsync)
            .WithName("UpdateTeachingAuthorizationStatus")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Authorize)
            .WithValidation<UpdateTeachingAuthorizationStatusRequest>()
            .Produces<UpdateTeachingAuthorizationStatusResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateTeachingAuthorizationStatusRequest request,
        UpdateTeachingAuthorizationStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
