namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.GetTeacherProfiles;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying teacher profiles with work queue filters and pagination (TD-02, TD-07, P2-S07).
/// </summary>
public sealed class GetTeacherProfilesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/teachers", HandleAsync)
            .WithName("GetTeacherProfiles")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.View)
            .Produces<PaginatedList<TeacherSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] string? queueFilter,
        [FromQuery] string? standing,
        [FromQuery] string? campus,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] GetTeacherProfilesHandler? handler = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var result = await handler.HandleAsync(
            queueFilter,
            standing,
            campus,
            search,
            page,
            pageSize,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
