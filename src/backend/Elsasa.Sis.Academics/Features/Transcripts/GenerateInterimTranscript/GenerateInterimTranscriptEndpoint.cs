namespace Elsasa.Sis.Academics.Features.Transcripts.GenerateInterimTranscript;

using Elsasa.Sis.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for generating a draft cumulative interim transcript (UC-ACD-13, P5-S13).
/// </summary>
public sealed class GenerateInterimTranscriptEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/transcripts/generate", HandleAsync)
            .WithName("GenerateInterimTranscript")
            .WithTags("Transcripts")
            .RequireAuthorization(Permissions.Academics.Transcripts.Generate)
            .WithValidation<GenerateInterimTranscriptRequest>()
            .Produces<TranscriptDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        GenerateInterimTranscriptRequest request,
        GenerateInterimTranscriptHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, ct);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/transcripts/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
