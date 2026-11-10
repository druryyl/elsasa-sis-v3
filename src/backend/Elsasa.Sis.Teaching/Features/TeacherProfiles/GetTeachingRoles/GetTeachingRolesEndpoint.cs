namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.GetTeachingRoles;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying recognized active teaching roles (TD-02, TD-07, P2-S07).
/// </summary>
public sealed class GetTeachingRolesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/roles", HandleAsync)
            .WithName("GetTeachingRoles")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.View)
            .Produces<IReadOnlyList<TeachingRoleDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromServices] GetTeachingRolesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
