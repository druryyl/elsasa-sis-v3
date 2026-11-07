namespace Elsasa.Sis.Academics.Features.AcademicAchievements.GetPeriodAchievements;

using Elsasa.Sis.Academics;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying academic achievements conferred across all students for a period (UC-ACD-21, P4-S11).
/// </summary>
public sealed class GetPeriodAchievementsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/cohort-review/achievements", HandleAsync)
            .WithName("GetPeriodAchievements")
            .WithTags("AcademicAchievements")
            .RequireAuthorization(Permissions.Academics.Achievements.View)
            .Produces<IReadOnlyList<AcademicAchievementDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid academicPeriodId,
        GetPeriodAchievementsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicPeriodId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
