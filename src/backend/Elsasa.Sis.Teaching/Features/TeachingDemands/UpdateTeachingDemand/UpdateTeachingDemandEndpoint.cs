namespace Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemand;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for updating mutable parameters of a teaching demand (TD-02, P2-S04).
/// </summary>
public sealed class UpdateTeachingDemandEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/v1/teaching/demands/{id:guid}", HandleAsync)
            .WithName("UpdateTeachingDemand")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.Manage)
            .WithValidation<UpdateTeachingDemandRequest>()
            .Produces<UpdateTeachingDemandResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateTeachingDemandRequest request,
        UpdateTeachingDemandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
