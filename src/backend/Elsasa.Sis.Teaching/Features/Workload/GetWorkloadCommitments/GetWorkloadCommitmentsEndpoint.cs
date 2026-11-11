namespace Elsasa.Sis.Teaching.Features.Workload.GetWorkloadCommitments;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying faculty capacity monitoring snapshots (TD-04, P3-S09).
/// </summary>
public sealed class GetWorkloadCommitmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/workload/commitments", HandleAsync)
            .WithName("GetWorkloadCommitments")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Workload.View)
            .Produces<IReadOnlyList<TeacherWorkloadCommitmentDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicPeriodId,
        [FromQuery] Guid? teacherProfileId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromServices] GetWorkloadCommitmentsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            academicPeriodId,
            teacherProfileId,
            status,
            search,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
