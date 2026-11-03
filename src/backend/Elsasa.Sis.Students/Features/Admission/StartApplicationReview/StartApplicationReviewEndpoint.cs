namespace Elsasa.Sis.Students.Features.Admission.StartApplicationReview;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for starting an admission application review (ADR-005).
/// </summary>
public sealed class StartApplicationReviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/students/admission/applications/{id:guid}/start-review", HandleAsync)
            .WithName("StartApplicationReview")
            .WithTags("AdmissionApplications")
            .RequireAuthorization(Permissions.Students.Admission.Review)
            .Produces<StartApplicationReviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        StartApplicationReviewHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
