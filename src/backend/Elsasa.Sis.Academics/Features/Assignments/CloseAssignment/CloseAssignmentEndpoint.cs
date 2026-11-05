namespace Elsasa.Sis.Academics.Features.Assignments.CloseAssignment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for closing a coursework assignment (TD-05, UC-ACD-04, P5-S10).
/// </summary>
public sealed class CloseAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assignments/{id:guid}/close", HandleAsync)
            .WithName("CloseAssignment")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assignments.Close)
            .Produces<CloseAssignmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        CloseAssignmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
