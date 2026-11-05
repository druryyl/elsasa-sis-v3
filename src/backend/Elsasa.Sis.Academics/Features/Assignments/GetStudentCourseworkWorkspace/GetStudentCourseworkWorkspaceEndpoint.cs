namespace Elsasa.Sis.Academics.Features.Assignments.GetStudentCourseworkWorkspace;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying student coursework self-service workspace tasks (SCR-SELF-03, TD-06, GAP-007, OQ-005, P5-S12).
/// </summary>
public sealed class GetStudentCourseworkWorkspaceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/portal/learning/coursework", HandleAsync)
            .WithName("GetStudentCourseworkWorkspace")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assignments.View)
            .Produces<IReadOnlyList<StudentCourseworkTaskDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] string? statusFilter,
        [FromQuery] Guid? studentId,
        GetStudentCourseworkWorkspaceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(statusFilter, studentId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
