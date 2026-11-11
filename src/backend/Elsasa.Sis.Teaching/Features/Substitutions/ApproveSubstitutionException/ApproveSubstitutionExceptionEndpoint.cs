namespace Elsasa.Sis.Teaching.Features.Substitutions.ApproveSubstitutionException;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for approving an escalated emergency substitution exception (TD-05, P4-S11).
/// </summary>
public sealed class ApproveSubstitutionExceptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/substitutions/{id:guid}/approve", HandleAsync)
            .WithName("ApproveSubstitutionException")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Substitutions.ApproveException)
            .Produces<ApproveSubstitutionExceptionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApproveSubstitutionExceptionRequest? request,
        ApproveSubstitutionExceptionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
