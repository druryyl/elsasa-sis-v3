namespace Elsasa.Sis.Academics.Features.Assessments.CancelAssessment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for cancelling a planned assessment (UC-ACD-05, P2-S04).
/// </summary>
public sealed class CancelAssessmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assessments/{id:guid}/cancel", HandleAsync)
            .WithName("CancelAssessment")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assessments.Manage)
            .WithValidation<CancelAssessmentRequest>()
            .Produces<CancelAssessmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        CancelAssessmentRequest request,
        CancelAssessmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
