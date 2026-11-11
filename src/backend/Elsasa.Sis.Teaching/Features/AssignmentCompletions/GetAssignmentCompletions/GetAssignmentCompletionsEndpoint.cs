namespace Elsasa.Sis.Teaching.Features.AssignmentCompletions.GetAssignmentCompletions;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying concluded assignments and released capacity (TD-01, TD-04, P4-S13).
/// </summary>
public sealed class GetAssignmentCompletionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/completions", HandleAsync)
            .WithName("GetAssignmentCompletions")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.View)
            .Produces<IReadOnlyList<AssignmentCompletionDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? academicPeriodId,
        [FromQuery] Guid? teacherProfileId,
        [FromQuery] string? search,
        [FromServices] GetAssignmentCompletionsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicPeriodId, teacherProfileId, search, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
