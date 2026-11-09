namespace Elsasa.Sis.Academics.Features.Assessments.UpdateAssessment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for updating a planned assessment (UC-ACD-05, P2-S04).
/// </summary>
public sealed class UpdateAssessmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPut("/api/v1/academics/assessments/{id:guid}", HandleAsync)
            .WithName("UpdateAssessment")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assessments.Manage)
            .WithValidation<UpdateAssessmentRequest>()
            .Produces<UpdateAssessmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateAssessmentRequest request,
        UpdateAssessmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
