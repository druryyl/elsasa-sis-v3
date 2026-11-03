namespace Elsasa.Sis.Students.Features.Records.Documents.VerifyStudentDocument;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for verifying or rejecting an official student document (ADR-004, ADR-005, P6-S09).
/// </summary>
public sealed class VerifyStudentDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/students/{id:guid}/documents/{docId:guid}/verify", HandleAsync)
            .WithName("VerifyStudentDocument")
            .WithTags("StudentDocuments")
            .RequireAuthorization(Permissions.Students.Records.Edit)
            .WithValidation<VerifyStudentDocumentRequest>()
            .Produces<VerifyStudentDocumentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid docId,
        VerifyStudentDocumentRequest request,
        VerifyStudentDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, docId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
