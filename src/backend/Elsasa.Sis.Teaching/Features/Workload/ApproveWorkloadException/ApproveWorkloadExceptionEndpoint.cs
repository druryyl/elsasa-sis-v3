namespace Elsasa.Sis.Teaching.Features.Workload.ApproveWorkloadException;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for approving a workload exception (TD-04, P3-S10).
/// </summary>
public sealed class ApproveWorkloadExceptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/workload/exceptions/{id:guid}/approve", HandleAsync)
            .WithName("ApproveWorkloadException")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Workload.ApproveException)
            .Produces<ApproveWorkloadExceptionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] ApproveWorkloadExceptionRequest? request,
        [FromServices] ApproveWorkloadExceptionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request ?? new ApproveWorkloadExceptionRequest(), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
