namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.DeleteAssessmentEvidence;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for deleting entrance assessment evidence (ADR-004, ADR-005).
/// </summary>
public sealed class DeleteAssessmentEvidenceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapDelete("/api/students/admission/applications/{id:guid}/assessment-evidence/{evidenceId:guid}", HandleAsync)
            .WithName("DeleteAssessmentEvidence")
            .WithTags("AdmissionAssessmentEvidence")
            .RequireAuthorization(Permissions.Students.Admission.Review)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid evidenceId,
        DeleteAssessmentEvidenceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, evidenceId, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToProblemDetails();
    }
}
