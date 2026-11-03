namespace Elsasa.Sis.Students.Features.Records.Documents.DownloadStudentDocument;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for safely streaming an official student document (ADR-004, ADR-005, P6-S09).
/// </summary>
public sealed class DownloadStudentDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/students/{id:guid}/documents/{docId:guid}/file", HandleAsync)
            .WithName("DownloadStudentDocument")
            .WithTags("StudentDocuments")
            .RequireAuthorization(Permissions.Students.Records.View)
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid docId,
        DownloadStudentDocumentHandler handler,
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
