namespace Elsasa.Sis.Academics.Features.LearningUnits.GetLearningUnitById;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for retrieving a single learning unit by ID (ADR-004, ADR-005, P3-S08).
/// </summary>
public sealed class GetLearningUnitByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-units/{id:guid}", HandleAsync)
            .WithName("GetLearningUnitById")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.View)
            .Produces<LearningUnitDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetLearningUnitByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
