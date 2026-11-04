namespace Elsasa.Sis.Academics.Features.LearningStructures.GetLearningStructures;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying paginated learning structures (ADR-004, ADR-005, P3-S06).
/// </summary>
public sealed class GetLearningStructuresEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/v1/academics/learning-structures", HandleAsync)
            .WithName("GetLearningStructures")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Curriculum.View)
            .Produces<GetLearningStructuresResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        Guid? academicPeriodId,
        string? status,
        string? search,
        int? page,
        int? pageSize,
        GetLearningStructuresHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(academicPeriodId, status, search, page, pageSize, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
