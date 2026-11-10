namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.SubmitQualificationEvidence;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for submitting qualification documentary evidence for a teacher profile (TD-05, P3-S09).
/// </summary>
public sealed class SubmitQualificationEvidenceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/teachers/{id:guid}/qualifications", HandleAsync)
            .WithName("SubmitQualificationEvidence")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Manage)
            .WithValidation<SubmitQualificationEvidenceRequest>()
            .Produces<SubmitQualificationEvidenceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        SubmitQualificationEvidenceRequest request,
        SubmitQualificationEvidenceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/teachers/{id}/qualifications/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
