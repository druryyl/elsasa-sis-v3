namespace Elsasa.Sis.Academics.Features.AssessmentEvidence.GetEvidenceByAssessment;

using Elsasa.Sis.Academics.Features.AssessmentEvidence;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying assessment evidence roster (ACD-WF-08, UC-ACD-07, P3-S06).
/// </summary>
public sealed class GetEvidenceByAssessmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/assessments/{id:guid}/evidence", HandleAsync)
            .WithName("GetEvidenceByAssessment")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Evidence.View)
            .Produces<IReadOnlyList<AssessmentEvidenceDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid? studentId,
        Guid? applicantId,
        GetEvidenceByAssessmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, studentId, applicantId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
