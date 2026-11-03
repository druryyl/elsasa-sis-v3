namespace Elsasa.Sis.Students.Features.Records.Documents.UploadStudentDocument;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for uploading an official student document (ADR-004, ADR-005, P6-S09).
/// </summary>
public sealed class UploadStudentDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/students/{id:guid}/documents", HandleAsync)
            .WithName("UploadStudentDocument")
            .WithTags("StudentDocuments")
            .RequireAuthorization(Permissions.Students.Records.Edit)
            .DisableAntiforgery()
            .Produces<UploadStudentDocumentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        HttpRequest request,
        UploadStudentDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid content type",
                Detail = "Request must be multipart/form-data.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
        var documentType = form["documentType"].ToString();

        if (string.IsNullOrWhiteSpace(documentType) && request.Query.TryGetValue("documentType", out var queryDocType))
        {
            documentType = queryDocType.ToString();
        }

        var result = await handler.HandleAsync(id, documentType, file, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/students/{id}/documents/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
