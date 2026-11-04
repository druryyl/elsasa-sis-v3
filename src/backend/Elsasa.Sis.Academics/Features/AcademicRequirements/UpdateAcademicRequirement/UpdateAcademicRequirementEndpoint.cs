namespace Elsasa.Sis.Academics.Features.AcademicRequirements.UpdateAcademicRequirement;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for modifying an academic requirement (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed class UpdateAcademicRequirementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/v1/academics/academic-requirements/{id:guid}", HandleAsync)
            .WithName("UpdateAcademicRequirement")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.Manage)
            .WithValidation<UpdateAcademicRequirementRequest>()
            .Produces<UpdateAcademicRequirementResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateAcademicRequirementRequest request,
        UpdateAcademicRequirementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
