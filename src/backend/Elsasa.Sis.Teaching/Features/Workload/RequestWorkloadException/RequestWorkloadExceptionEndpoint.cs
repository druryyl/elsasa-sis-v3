namespace Elsasa.Sis.Teaching.Features.Workload.RequestWorkloadException;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for requesting a workload exception (TD-04, P3-S10).
/// </summary>
public sealed class RequestWorkloadExceptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/workload/exceptions", HandleAsync)
            .WithName("RequestWorkloadException")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Workload.RequestException)
            .WithValidation<RequestWorkloadExceptionRequest>()
            .Produces<RequestWorkloadExceptionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] RequestWorkloadExceptionRequest request,
        [FromServices] RequestWorkloadExceptionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/workload/exceptions/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
