namespace Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for enrolling an accepted applicant as an official student (ADR-004, ADR-005).
/// </summary>
public sealed class EnrollAcceptedApplicantEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/students/enrollments", HandleAsync)
            .WithName("EnrollAcceptedApplicant")
            .WithTags("Enrollment")
            .RequireAuthorization(Permissions.Students.Enrollment.Execute)
            .WithValidation<EnrollAcceptedApplicantRequest>()
            .Produces<EnrollAcceptedApplicantResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        EnrollAcceptedApplicantRequest request,
        EnrollAcceptedApplicantHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/students/{result.Value.StudentId}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
