namespace Elsasa.Sis.Teaching.Features.RecordCorrections.GetRecordCorrections;

using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying chronological audit trail of faculty record corrections (TD-06, P5-S14).
/// </summary>
public sealed class GetRecordCorrectionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/teaching/corrections", HandleAsync)
            .WithName("GetRecordCorrections")
            .WithTags("Teaching")
            .RequireAuthorization(Permissions.Teaching.Faculty.View)
            .Produces<IReadOnlyList<TeachingRecordCorrectionDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? teacherProfileId,
        [FromQuery] string? targetTableName,
        [FromQuery] string? status,
        [FromServices] GetRecordCorrectionsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            teacherProfileId,
            targetTableName,
            status,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
