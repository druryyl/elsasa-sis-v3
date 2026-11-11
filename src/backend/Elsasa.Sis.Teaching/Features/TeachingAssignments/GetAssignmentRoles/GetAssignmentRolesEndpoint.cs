namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.GetAssignmentRoles;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying assignment roles catalog (TD-01, P3-S06).
/// </summary>
public sealed class GetAssignmentRolesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/assignment-roles", HandleAsync)
            .WithName("GetAssignmentRoles")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.View)
            .Produces<IReadOnlyList<AssignmentRoleDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] bool? activeOnly,
        [FromServices] GetAssignmentRolesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(activeOnly ?? true, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
