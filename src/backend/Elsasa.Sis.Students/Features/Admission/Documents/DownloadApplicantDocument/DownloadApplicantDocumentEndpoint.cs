namespace Elsasa.Sis.Students.Features.Admission.Documents.DownloadApplicantDocument;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for safely streaming an applicant credential document (ADR-005).
/// </summary>
public sealed class DownloadApplicantDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/students/admission/applications/{id:guid}/documents/{docId:guid}/file", HandleAsync)
            .WithName("DownloadApplicantDocument")
            .WithTags("AdmissionDocuments")
            .RequireAuthorization(Permissions.Students.Admission.View)
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid docId,
        DownloadApplicantDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, docId, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblemDetails();
        }

        var streamResult = result.Value;
        return Results.Stream(
            streamResult.Stream,
            contentType: streamResult.ContentType,
            fileDownloadName: streamResult.FileName);
    }
}
