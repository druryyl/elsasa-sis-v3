namespace Elsasa.Sis.Teaching.Features.Workload.RecalculatePeriodWorkload;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for on-demand period faculty capacity recalculation (TD-04, P3-S09).
/// </summary>
public sealed class RecalculatePeriodWorkloadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/workload/recalculate", HandleAsync)
            .WithName("RecalculatePeriodWorkload")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Workload.Recalculate)
            .WithValidation<RecalculatePeriodWorkloadRequest>()
            .Produces<RecalculatePeriodWorkloadResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] RecalculatePeriodWorkloadRequest request,
        [FromServices] RecalculatePeriodWorkloadHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
