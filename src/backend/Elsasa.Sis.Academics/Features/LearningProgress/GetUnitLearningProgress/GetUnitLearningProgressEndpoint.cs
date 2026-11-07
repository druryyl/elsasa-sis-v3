namespace Elsasa.Sis.Academics.Features.LearningProgress.GetUnitLearningProgress;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying unit learning progress roster (P2-S05, UC-ACD-12, SCR-ACD-07).
/// </summary>
public sealed class GetUnitLearningProgressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-units/{unitId:guid}/progress", HandleAsync)
            .WithName("GetUnitLearningProgress")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Progress.View)
            .Produces<IReadOnlyList<UnitLearningProgressItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid unitId,
        Guid? academicPeriodId,
        string? status,
        GetUnitLearningProgressHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            unitId,
            academicPeriodId,
            status,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
