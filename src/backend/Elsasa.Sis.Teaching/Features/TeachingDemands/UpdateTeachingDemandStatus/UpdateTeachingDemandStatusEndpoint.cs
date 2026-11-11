namespace Elsasa.Sis.Teaching.Features.TeachingDemands.UpdateTeachingDemandStatus;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for transitioning teaching demand lifecycle status (TD-02, P2-S04).
/// </summary>
public sealed class UpdateTeachingDemandStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/demands/{id:guid}/status", HandleAsync)
            .WithName("UpdateTeachingDemandStatus")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.Manage)
            .WithValidation<UpdateTeachingDemandStatusRequest>()
            .Produces<UpdateTeachingDemandStatusResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateTeachingDemandStatusRequest request,
        UpdateTeachingDemandStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
