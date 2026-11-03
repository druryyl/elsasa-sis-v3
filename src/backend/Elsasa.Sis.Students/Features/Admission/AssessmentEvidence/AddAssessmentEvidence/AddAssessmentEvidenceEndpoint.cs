namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.AddAssessmentEvidence;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for adding entrance assessment evidence to an admission application (ADR-004, ADR-005).
/// </summary>
public sealed class AddAssessmentEvidenceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/students/admission/applications/{id:guid}/assessment-evidence", HandleAsync)
            .WithName("AddAssessmentEvidence")
            .WithTags("AdmissionAssessmentEvidence")
            .RequireAuthorization(Permissions.Students.Admission.Review)
            .WithValidation<AddAssessmentEvidenceRequest>()
            .Produces<AddAssessmentEvidenceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        AddAssessmentEvidenceRequest request,
        AddAssessmentEvidenceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/students/admission/applications/{id}/assessment-evidence/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
