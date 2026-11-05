namespace Elsasa.Sis.Academics.Features.LearningActivities.GetTeacherDailyActivities;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying daily scheduled or conducted activities for the authenticated teacher (ADR-004, ADR-005, P3-S07, SCR-ACD-01).
/// </summary>
public sealed class GetTeacherDailyActivitiesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/teacher/activities", HandleAsync)
            .WithName("GetTeacherDailyActivities")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Activities.View)
            .Produces<IReadOnlyList<TeacherDailyActivityItemDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        DateOnly? date,
        string? sectionLabel,
        string? teacherName,
        GetTeacherDailyActivitiesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(date, sectionLabel, teacherName, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
