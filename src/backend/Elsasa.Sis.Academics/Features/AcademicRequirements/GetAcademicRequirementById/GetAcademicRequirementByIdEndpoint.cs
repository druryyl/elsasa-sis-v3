namespace Elsasa.Sis.Academics.Features.AcademicRequirements.GetAcademicRequirementById;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for inspecting an individual academic requirement (ADR-004, ADR-005, P3-S09).
/// </summary>
public sealed class GetAcademicRequirementByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/academic-requirements/{id:guid}", HandleAsync)
            .WithName("GetAcademicRequirementById")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.View)
            .Produces<AcademicRequirementDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetAcademicRequirementByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
