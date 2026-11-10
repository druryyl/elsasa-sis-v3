namespace Elsasa.Sis.Teaching.Features.RecordCorrections.ApproveRecordCorrection;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for Teaching Authority approval of requested record corrections (TD-06, P5-S14).
/// </summary>
public sealed class ApproveRecordCorrectionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/teaching/corrections/{id:guid}/approve", HandleAsync)
            .WithName("ApproveRecordCorrection")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.Authorize)
            .Produces<TeachingRecordCorrectionDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApproveRecordCorrectionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
