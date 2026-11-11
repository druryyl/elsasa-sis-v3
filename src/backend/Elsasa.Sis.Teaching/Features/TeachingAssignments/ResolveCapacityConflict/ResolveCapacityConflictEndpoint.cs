namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ResolveCapacityConflict;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for resolving or waiving a capacity conflict (TD-01, P3-S07).
/// </summary>
public sealed class ResolveCapacityConflictEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/conflicts/{id:guid}/resolve", HandleAsync)
            .WithName("ResolveCapacityConflict")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.Manage)
            .WithValidation<ResolveCapacityConflictRequest>()
            .Produces<ResolveCapacityConflictResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ResolveCapacityConflictRequest request,
        ResolveCapacityConflictHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
