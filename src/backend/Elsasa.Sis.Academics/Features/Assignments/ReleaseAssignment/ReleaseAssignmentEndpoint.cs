namespace Elsasa.Sis.Academics.Features.Assignments.ReleaseAssignment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for releasing a coursework assignment (TD-05, UC-ACD-04, P5-S10).
/// </summary>
public sealed class ReleaseAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assignments/{id:guid}/release", HandleAsync)
            .WithName("ReleaseAssignment")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assignments.Release)
            .Produces<ReleaseAssignmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ReleaseAssignmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
