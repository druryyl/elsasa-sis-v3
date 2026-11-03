namespace Elsasa.Sis.Students.Features.Admission.Documents.VerifyApplicantDocument;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for verifying or rejecting an applicant document (ADR-005).
/// </summary>
public sealed class VerifyApplicantDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/students/admission/applications/{id:guid}/documents/{docId:guid}/verify", HandleAsync)
            .WithName("VerifyApplicantDocument")
            .WithTags("AdmissionDocuments")
            .RequireAuthorization(Permissions.Students.Admission.VerifyDocument)
            .WithValidation<VerifyApplicantDocumentRequest>()
            .Produces<VerifyApplicantDocumentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid docId,
        VerifyApplicantDocumentRequest request,
        VerifyApplicantDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, docId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
