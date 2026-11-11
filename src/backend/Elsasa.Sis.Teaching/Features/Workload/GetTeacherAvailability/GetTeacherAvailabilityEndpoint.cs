namespace Elsasa.Sis.Teaching.Features.Workload.GetTeacherAvailability;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying a teacher's availability record (P3-S08).
/// </summary>
public sealed class GetTeacherAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/teachers/{teacherId:guid}/availability", HandleAsync)
            .WithName("GetTeacherAvailability")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Assignments.View)
            .Produces<TeacherAvailabilityDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] Guid teacherId,
        [FromQuery] Guid? academicPeriodId,
        [FromServices] GetTeacherAvailabilityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(teacherId, academicPeriodId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblemDetails();
        }

        return result.Value is not null
            ? Results.Ok(result.Value)
            : Results.NotFound(new { Message = $"No active availability found for teacher ID '{teacherId}'." });
    }
}
