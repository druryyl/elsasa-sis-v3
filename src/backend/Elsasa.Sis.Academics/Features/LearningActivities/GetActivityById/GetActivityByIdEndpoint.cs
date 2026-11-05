namespace Elsasa.Sis.Academics.Features.LearningActivities.GetActivityById;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying a learning activity by ID (ADR-004, ADR-005, P3-S07).
/// </summary>
public sealed class GetActivityByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-activities/{id:guid}", HandleAsync)
            .WithName("GetActivityById")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Activities.View)
            .Produces<GetActivityByIdResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetActivityByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
