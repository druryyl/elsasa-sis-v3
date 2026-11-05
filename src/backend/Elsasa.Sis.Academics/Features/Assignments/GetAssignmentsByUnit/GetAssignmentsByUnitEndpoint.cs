namespace Elsasa.Sis.Academics.Features.Assignments.GetAssignmentsByUnit;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying assignments across a learning unit (P5-S10).
/// </summary>
public sealed class GetAssignmentsByUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-units/{unitId:guid}/assignments", HandleAsync)
            .WithName("GetAssignmentsByUnit")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assignments.View)
            .Produces<IReadOnlyList<UnitAssignmentItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid unitId,
        string? status,
        GetAssignmentsByUnitHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(unitId, status, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
