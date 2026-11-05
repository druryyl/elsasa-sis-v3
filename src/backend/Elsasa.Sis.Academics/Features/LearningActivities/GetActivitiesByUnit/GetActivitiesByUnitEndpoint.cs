namespace Elsasa.Sis.Academics.Features.LearningActivities.GetActivitiesByUnit;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying activities of a learning unit (ADR-004, ADR-005, P3-S07).
/// </summary>
public sealed class GetActivitiesByUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-units/{learningUnitId:guid}/activities", HandleAsync)
            .WithName("GetActivitiesByUnit")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Activities.View)
            .Produces<IReadOnlyList<LearningActivityItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid learningUnitId,
        DateOnly? startDate,
        DateOnly? endDate,
        string? sectionLabel,
        string? status,
        GetActivitiesByUnitHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            learningUnitId,
            startDate,
            endDate,
            sectionLabel,
            status,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
