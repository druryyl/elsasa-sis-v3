namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.VerifyQualificationEvidence;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Qualification Verifiers to verify authenticity of submitted credentials (P3-S10).
/// </summary>
public sealed class VerifyQualificationEvidenceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/qualifications/{id:guid}/verify", HandleAsync)
            .WithName("VerifyQualificationEvidence")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Verify)
            .Produces<VerifyQualificationEvidenceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        VerifyQualificationEvidenceRequest? request,
        [Microsoft.AspNetCore.Mvc.FromServices] VerifyQualificationEvidenceHandler handler,
        [Microsoft.AspNetCore.Mvc.FromServices] IValidator<VerifyQualificationEvidenceRequest>? validator,
        CancellationToken cancellationToken)
    {
        request ??= new VerifyQualificationEvidenceRequest();

        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return DomainError.Validation(
                    ValidationFilter<VerifyQualificationEvidenceRequest>.DefaultValidationErrorCode,
                    ValidationFilter<VerifyQualificationEvidenceRequest>.DefaultValidationErrorMessage,
                    validationResult.ToDictionary()).ToProblemDetails();
            }
        }

        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
