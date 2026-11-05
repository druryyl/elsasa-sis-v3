namespace Elsasa.Sis.Academics.Features.Assignments.GetAssignmentSubmissions;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying assignment submissions with dynamic on-the-fly missing projection (SCR-ACD-02, TD-06, GAP-007, OQ-005, P5-S12).
/// </summary>
public sealed class GetAssignmentSubmissionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/assignments/{id:guid}/submissions", HandleAsync)
            .WithName("GetAssignmentSubmissions")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Submissions.View)
            .Produces<IReadOnlyList<AssignmentSubmissionRosterItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetAssignmentSubmissionsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
