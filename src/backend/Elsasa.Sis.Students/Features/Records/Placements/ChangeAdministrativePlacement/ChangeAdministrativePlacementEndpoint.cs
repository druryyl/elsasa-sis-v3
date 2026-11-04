namespace Elsasa.Sis.Students.Features.Records.Placements.ChangeAdministrativePlacement;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for reassigning a student's administrative placement (ADR-004, ADR-005, UC-STD-06).
/// </summary>
public sealed class ChangeAdministrativePlacementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/students/{id:guid}/placements", HandleAsync)
            .WithName("ChangeAdministrativePlacement")
            .WithTags("StudentPlacements")
            .RequireAuthorization(Permissions.Students.Records.Edit)
            .WithValidation<ChangeAdministrativePlacementRequest>()
            .Produces<ChangeAdministrativePlacementResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ChangeAdministrativePlacementRequest request,
        ChangeAdministrativePlacementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
