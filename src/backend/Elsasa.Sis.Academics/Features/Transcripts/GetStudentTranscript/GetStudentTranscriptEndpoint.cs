namespace Elsasa.Sis.Academics.Features.Transcripts.GetStudentTranscript;

using Elsasa.Sis.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying a student's academic transcript (UC-ACD-13, P5-S13).
/// </summary>
public sealed class GetStudentTranscriptEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/students/{studentId:guid}/transcript", HandleAsync)
            .WithName("GetStudentTranscript")
            .WithTags("Transcripts")
            .RequireAuthorization(Permissions.Academics.Transcripts.View)
            .Produces<TranscriptDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        string? transcriptType,
        GetStudentTranscriptHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(studentId, transcriptType, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
