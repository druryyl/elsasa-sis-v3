namespace Elsasa.Sis.Academics.Features.Transcripts.IssueTranscript;

using Elsasa.Sis.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for certifying and issuing an academic transcript (UC-ACD-13, P5-S13).
/// </summary>
public sealed class IssueTranscriptEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/transcripts/{id:guid}/issue", HandleAsync)
            .WithName("IssueTranscript")
            .WithTags("Transcripts")
            .RequireAuthorization(Permissions.Academics.Transcripts.Issue)
            .Produces<IssueTranscriptResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IssueTranscriptHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
