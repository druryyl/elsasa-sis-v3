namespace Elsasa.Sis.Academics.Features.Assessments.ConductAssessment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for marking a planned assessment as conducted (UC-ACD-05, P2-S04).
/// </summary>
public sealed class ConductAssessmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assessments/{id:guid}/conduct", HandleAsync)
            .WithName("ConductAssessment")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assessments.Conduct)
            .Produces<ConductAssessmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ConductAssessmentRequest? request,
        ConductAssessmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
