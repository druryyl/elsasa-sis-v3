namespace Elsasa.Sis.Academics.Features.AcademicResults.PublishAcademicResults;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Academic Authority to release approved academic results to published status for portal visibility (UC-ACD-16, P4-S09, TD-04).
/// </summary>
public sealed class PublishAcademicResultsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assessments/{id:guid}/results/publish", HandleAsync)
            .WithName("PublishAcademicResults")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Results.Publish)
            .Produces<PublishAcademicResultsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        PublishAcademicResultsRequest? request,
        PublishAcademicResultsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
