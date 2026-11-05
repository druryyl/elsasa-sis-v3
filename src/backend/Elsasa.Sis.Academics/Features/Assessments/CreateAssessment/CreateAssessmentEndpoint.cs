namespace Elsasa.Sis.Academics.Features.Assessments.CreateAssessment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for creating a planned assessment (UC-ACD-05, ACD-WF-07, P2-S04).
/// </summary>
public sealed class CreateAssessmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assessments", HandleAsync)
            .WithName("CreateAssessment")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Assessments.Manage)
            .WithValidation<CreateAssessmentRequest>()
            .Produces<CreateAssessmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        CreateAssessmentRequest request,
        CreateAssessmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/assessments/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
