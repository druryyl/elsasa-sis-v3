namespace Elsasa.Sis.Academics.Features.AcademicAchievements.GetStudentAchievements;

using Elsasa.Sis.Academics;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying academic achievements for a specific student (UC-ACD-21, P4-S11).
/// </summary>
public sealed class GetStudentAchievementsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/students/{studentId:guid}/achievements", HandleAsync)
            .WithName("GetStudentAchievements")
            .WithTags("AcademicAchievements")
            .RequireAuthorization(Permissions.Academics.Achievements.View)
            .Produces<IReadOnlyList<AcademicAchievementDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        [FromQuery] Guid? academicPeriodId,
        GetStudentAchievementsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(studentId, academicPeriodId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
