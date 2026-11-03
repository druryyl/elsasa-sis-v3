namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.UpdateAssessmentEvidence;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for updating entrance assessment evidence (ADR-004, ADR-005).
/// </summary>
public sealed class UpdateAssessmentEvidenceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/students/admission/applications/{id:guid}/assessment-evidence/{evidenceId:guid}", HandleAsync)
            .WithName("UpdateAssessmentEvidence")
            .WithTags("AdmissionAssessmentEvidence")
            .RequireAuthorization(Permissions.Students.Admission.Review)
            .WithValidation<UpdateAssessmentEvidenceRequest>()
            .Produces<UpdateAssessmentEvidenceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid evidenceId,
        UpdateAssessmentEvidenceRequest request,
        UpdateAssessmentEvidenceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, evidenceId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
