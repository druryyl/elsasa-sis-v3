namespace Elsasa.Sis.Academics.Features.Assignments.RecordSubmissionException;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for recording a coursework submission exception (TD-06, P5-S11).
/// </summary>
public sealed class RecordSubmissionExceptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assignments/{id:guid}/submissions/exception", HandleAsync)
            .WithName("RecordSubmissionException")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Submissions.Exception)
            .WithValidation<RecordSubmissionExceptionRequest>()
            .Produces<RecordSubmissionExceptionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        RecordSubmissionExceptionRequest request,
        RecordSubmissionExceptionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/assignments/{id}/submissions/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
