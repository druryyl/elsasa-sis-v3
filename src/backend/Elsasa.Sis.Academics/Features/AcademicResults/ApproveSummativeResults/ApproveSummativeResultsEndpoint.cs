namespace Elsasa.Sis.Academics.Features.AcademicResults.ApproveSummativeResults;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Academic Authority / Coordinator to certify provisional summative results to approved status (UC-ACD-16, P4-S09, TD-04).
/// </summary>
public sealed class ApproveSummativeResultsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assessments/{id:guid}/results/approve", HandleAsync)
            .WithName("ApproveSummativeResults")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Results.Approve)
            .Produces<ApproveSummativeResultsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApproveSummativeResultsRequest? request,
        ApproveSummativeResultsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
