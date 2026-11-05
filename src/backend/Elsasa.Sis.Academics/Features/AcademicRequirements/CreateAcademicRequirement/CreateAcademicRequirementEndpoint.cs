namespace Elsasa.Sis.Academics.Features.AcademicRequirements.CreateAcademicRequirement;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for drafting an academic requirement under a learning structure (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed class CreateAcademicRequirementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/learning-structures/{structureId:guid}/requirements", HandleAsync)
            .WithName("CreateAcademicRequirement")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.Manage)
            .WithValidation<CreateAcademicRequirementRequest>()
            .Produces<CreateAcademicRequirementResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid structureId,
        CreateAcademicRequirementRequest request,
        CreateAcademicRequirementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(structureId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/academic-requirements/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
