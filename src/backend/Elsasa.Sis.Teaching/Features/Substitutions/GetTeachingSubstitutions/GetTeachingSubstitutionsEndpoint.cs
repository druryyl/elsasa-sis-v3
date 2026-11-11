namespace Elsasa.Sis.Teaching.Features.Substitutions.GetTeachingSubstitutions;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying teaching substitutions with filters and pagination (TD-05, P4-S11).
/// </summary>
public sealed class GetTeachingSubstitutionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/substitutions", HandleAsync)
            .WithName("GetTeachingSubstitutions")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.View)
            .Produces<PaginatedList<TeachingSubstitutionDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        GetTeachingSubstitutionsHandler handler,
        Guid? originalAssignmentId,
        Guid? substituteTeacherId,
        string? status,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(
            originalAssignmentId,
            substituteTeacherId,
            status,
            page,
            pageSize,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
