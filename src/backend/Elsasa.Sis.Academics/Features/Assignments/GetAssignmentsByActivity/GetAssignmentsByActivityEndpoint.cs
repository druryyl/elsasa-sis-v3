namespace Elsasa.Sis.Academics.Features.Assignments.GetAssignmentsByActivity;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying assignments for a specific learning activity (P5-S10).
/// </summary>
public sealed class GetAssignmentsByActivityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-activities/{activityId:guid}/assignments", HandleAsync)
            .WithName("GetAssignmentsByActivity")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assignments.View)
            .Produces<IReadOnlyList<ActivityAssignmentItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid activityId,
        GetAssignmentsByActivityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(activityId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
