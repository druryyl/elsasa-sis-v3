namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;

using Elsasa.Sis.Academics.Features.AcademicResults;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Academic Authority to perform auditable in-place alterations of approved academic results (UC-ACD-19, TD-06, P6-S14).
/// </summary>
public sealed class CorrectApprovedResultEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/academic-results/{id:guid}/correct", HandleAsync)
            .WithName("CorrectApprovedResult")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Corrections.Correct)
            .Produces<AcademicResultDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        CorrectApprovedResultRequest request,
        CorrectApprovedResultHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
