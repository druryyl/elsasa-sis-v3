namespace Elsasa.Sis.Academics.Features.AcademicStandings.DetermineAcademicStanding;

using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for certifying a single student's academic standing determination (P3-S07, UC-ACD-17).
/// </summary>
public sealed class DetermineAcademicStandingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/academics/academic-standings", HandleAsync)
            .WithName("DetermineAcademicStanding")
            .WithTags("Academics")
            .RequireAuthorization(Permissions.Academics.Standings.Determine)
            .WithValidation<DetermineAcademicStandingCommand>()
            .Produces<StudentAcademicStandingDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        DetermineAcademicStandingCommand command,
        DetermineAcademicStandingHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
