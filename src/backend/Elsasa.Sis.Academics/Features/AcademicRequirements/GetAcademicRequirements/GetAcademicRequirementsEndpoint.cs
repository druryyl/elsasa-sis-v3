namespace Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirements;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying academic requirements of a learning structure (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed class GetAcademicRequirementsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-structures/{structureId:guid}/requirements", HandleAsync)
            .WithName("GetAcademicRequirements")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.View)
            .Produces<GetAcademicRequirementsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid structureId,
        [FromQuery] string? scope,
        [FromQuery] string? type,
        GetAcademicRequirementsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(structureId, scope, type, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
