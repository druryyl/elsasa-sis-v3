namespace Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnits;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying learning units of a structure (ADR-004, ADR-005, P3-S08).
/// </summary>
public sealed class GetLearningUnitsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-structures/{structureId:guid}/units", HandleAsync)
            .WithName("GetLearningUnits")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.View)
            .Produces<IReadOnlyList<LearningUnitItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid structureId,
        GetLearningUnitsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(structureId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
