namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.GetTeacherProfileById;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying a complete teacher profile dossier by ID (TD-02, TD-07, P2-S07).
/// </summary>
public sealed class GetTeacherProfileByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/teachers/{id:guid}", HandleAsync)
            .WithName("GetTeacherProfileById")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.View)
            .Produces<TeacherProfileDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromServices] GetTeacherProfileByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
