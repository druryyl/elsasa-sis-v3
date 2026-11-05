namespace Elsasa.Sis.Academics.Features.Assignments.UploadSubmissionFile;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for uploading coursework submission files (GAP-006, OQ-004, TD-07, P5-S11).
/// </summary>
public sealed class UploadSubmissionFileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/submissions/upload", HandleAsync)
            .WithName("UploadSubmissionFile")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Submissions.Submit)
            .DisableAntiforgery()
            .Produces<UploadSubmissionFileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest httpRequest,
        UploadSubmissionFileHandler handler,
        CancellationToken cancellationToken)
    {
        if (!httpRequest.HasFormContentType)
        {
            return DomainError.Validation("Academic.InvalidContentType", "Expected multipart/form-data content type.")
                .ToProblemDetails();
        }

        var form = await httpRequest.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);

        Guid? studentId = null;
        if (form.TryGetValue("studentId", out var studentIdVal) &&
            Guid.TryParse(studentIdVal, out var parsedStudentId))
        {
            studentId = parsedStudentId;
        }

        var result = await handler.HandleAsync(file, studentId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
