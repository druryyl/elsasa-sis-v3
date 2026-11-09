namespace Elsasa.Sis.Academics.Features.AcademicResults.EvaluateStudentLearning;

using Elsasa.Sis.Academics.Features.AcademicResults;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for recording a student learning evaluation and formative release (UC-ACD-06, P4-S08).
/// </summary>
public sealed class EvaluateStudentLearningEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/assessments/{id:guid}/results", HandleAsync)
            .WithName("EvaluateStudentLearning")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Results.Evaluate)
            .WithValidation<EvaluateStudentLearningRequest>()
            .Produces<AcademicResultDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        EvaluateStudentLearningRequest request,
        EvaluateStudentLearningHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/academics/assessments/{id}/results/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
