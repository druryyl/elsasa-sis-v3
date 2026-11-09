namespace Elsasa.Sis.Academics.Features.AcademicAchievements.ConferAcademicAchievement;

using Elsasa.Sis.Academics;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for conferring an academic achievement or distinction (UC-ACD-21, P4-S11).
/// </summary>
public sealed class ConferAcademicAchievementEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/achievements", HandleAsync)
            .WithName("ConferAcademicAchievement")
            .WithTags("AcademicAchievements")
            .RequireAuthorization(Permissions.Academics.Achievements.Confer)
            .WithValidation<ConferAcademicAchievementCommand>()
            .Produces<AcademicAchievementDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        ConferAcademicAchievementCommand command,
        ConferAcademicAchievementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/achievements/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
