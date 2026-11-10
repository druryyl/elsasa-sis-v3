namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.RejectQualificationEvidence;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Qualification Verifiers to record rejection of submitted credentials with rationale (P3-S10).
/// </summary>
public sealed class RejectQualificationEvidenceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/qualifications/{id:guid}/reject", HandleAsync)
            .WithName("RejectQualificationEvidence")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Verify)
            .WithValidation<RejectQualificationEvidenceRequest>()
            .Produces<RejectQualificationEvidenceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        RejectQualificationEvidenceRequest request,
        RejectQualificationEvidenceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
