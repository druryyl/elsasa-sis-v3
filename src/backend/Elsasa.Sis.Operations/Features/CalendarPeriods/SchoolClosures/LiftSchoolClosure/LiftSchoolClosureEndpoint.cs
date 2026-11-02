namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.LiftSchoolClosure;

using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for lifting a school closure (ADR-005, UC-OPS-12).
/// </summary>
public sealed class LiftSchoolClosureEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/closures/{id:guid}/lift", HandleAsync)
            .WithName("LiftSchoolClosure")
            .WithTags("SchoolClosures")
            .RequireAuthorization(Permissions.Operations.Closure.Declare)
            .Produces<SchoolClosureResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        LiftSchoolClosureHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
