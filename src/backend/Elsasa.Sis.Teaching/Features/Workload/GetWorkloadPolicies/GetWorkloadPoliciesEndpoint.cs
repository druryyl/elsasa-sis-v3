namespace Elsasa.Sis.Teaching.Features.Workload.GetWorkloadPolicies;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying active institutional workload policies (TD-04, P3-S09).
/// </summary>
public sealed class GetWorkloadPoliciesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/workload/policies", HandleAsync)
            .WithName("GetWorkloadPolicies")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Workload.View)
            .Produces<IReadOnlyList<WorkloadPolicyDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicYearId,
        [FromQuery] string? status,
        [FromServices] GetWorkloadPoliciesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            academicYearId,
            status,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
