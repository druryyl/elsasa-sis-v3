namespace Elsasa.Sis.Academics.Features.AcademicStandings.BatchDetermineCohortStanding;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for batch determining academic standing across a cohort (P3-S07, UC-ACD-17, TD-03).
/// </summary>
public sealed class BatchDetermineCohortStandingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/academic-standings/batch", HandleAsync)
            .WithName("BatchDetermineCohortStanding")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Standings.BatchDetermine)
            .WithValidation<BatchDetermineCohortStandingCommand>()
            .Produces<BatchDetermineCohortStandingResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        BatchDetermineCohortStandingCommand command,
        BatchDetermineCohortStandingHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
