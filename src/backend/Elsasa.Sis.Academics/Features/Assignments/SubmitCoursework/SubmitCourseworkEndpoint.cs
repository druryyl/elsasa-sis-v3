namespace Elsasa.Sis.Academics.Features.Assignments.SubmitCoursework;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for student coursework submission (TD-07, UC-ACD-01, P5-S11).
/// </summary>
public sealed class SubmitCourseworkEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assignments/{id:guid}/submissions", HandleAsync)
            .WithName("SubmitCoursework")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Submissions.Submit)
            .WithValidation<SubmitCourseworkRequest>()
            .Produces<SubmitCourseworkResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        SubmitCourseworkRequest request,
        SubmitCourseworkHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/assignments/{id}/submissions/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
