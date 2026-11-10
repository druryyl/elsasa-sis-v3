namespace Elsasa.Sis.Teaching.Features.RecordCorrections.ApplyRecordCorrection;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for submitting or applying auditable record corrections on finalized faculty records (TD-06, P5-S14).
/// </summary>
public sealed class ApplyRecordCorrectionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/corrections", HandleAsync)
            .WithName("ApplyRecordCorrection")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Correct)
            .Produces<TeachingRecordCorrectionDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        ApplyRecordCorrectionRequest request,
        [Microsoft.AspNetCore.Mvc.FromServices] ApplyRecordCorrectionHandler handler,
        [Microsoft.AspNetCore.Mvc.FromServices] IValidator<ApplyRecordCorrectionRequest>? validator,
        CancellationToken cancellationToken)
    {
        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return DomainError.Validation(
                    ValidationFilter<ApplyRecordCorrectionRequest>.DefaultValidationErrorCode,
                    ValidationFilter<ApplyRecordCorrectionRequest>.DefaultValidationErrorMessage,
                    validationResult.ToDictionary()).ToProblemDetails();
            }
        }

        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/teaching/corrections/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
