namespace Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for recording an official admission decision (ADR-004, ADR-005).
/// </summary>
public sealed class RecordAdmissionDecisionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/students/admission/applications/{id:guid}/decisions", HandleAsync)
            .WithName("RecordAdmissionDecision")
            .WithTags("Admission")
            .RequireAuthorization(Permissions.Students.Admission.Decide)
            .WithValidation<RecordAdmissionDecisionRequest>()
            .Produces<RecordAdmissionDecisionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        RecordAdmissionDecisionRequest request,
        RecordAdmissionDecisionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
