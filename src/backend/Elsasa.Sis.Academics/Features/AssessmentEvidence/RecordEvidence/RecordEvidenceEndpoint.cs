namespace Elsasa.Sis.Academics.Features.AssessmentEvidence.RecordEvidence;

using Elsasa.Sis.Academics.Features.AssessmentEvidence;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for recording raw assessment evidence (ACD-WF-08, ACD-WF-20, UC-ACD-07, GAP-07, P3-S06).
/// </summary>
public sealed class RecordEvidenceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assessments/{id:guid}/evidence", HandleAsync)
            .WithName("RecordAssessmentEvidence")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Evidence.Record)
            .WithValidation<RecordEvidenceRequest>()
            .Produces<AssessmentEvidenceDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        RecordEvidenceRequest request,
        RecordEvidenceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/assessments/{id}/evidence/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
