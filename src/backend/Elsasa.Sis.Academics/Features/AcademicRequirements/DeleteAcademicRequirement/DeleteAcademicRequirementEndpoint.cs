namespace Elsasa.Sis.Academics.Features.AcademicRequirements.DeleteAcademicRequirement;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for deleting an academic requirement (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed class DeleteAcademicRequirementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDelete("/api/v1/academics/academic-requirements/{id:guid}", HandleAsync)
            .WithName("DeleteAcademicRequirement")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        DeleteAcademicRequirementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToProblemDetails();
    }
}
