namespace Elsasa.Sis.Teaching.Features.Workload.GetWorkloadExceptions;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying workload exceptions (TD-04, P3-S10).
/// </summary>
public sealed class GetWorkloadExceptionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/workload/exceptions", HandleAsync)
            .WithName("GetWorkloadExceptions")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Workload.View)
            .Produces<IReadOnlyList<WorkloadExceptionDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicPeriodId,
        [FromQuery] Guid? teacherProfileId,
        [FromQuery] string? status,
        [FromServices] GetWorkloadExceptionsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicPeriodId, teacherProfileId, status, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
