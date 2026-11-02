namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CloseOperationalPeriod;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for closing an operational period (ADR-005).
/// </summary>
public sealed class CloseOperationalPeriodEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/periods/{id:guid}/close", HandleAsync)
            .WithName("CloseOperationalPeriod")
            .WithTags("OperationalPeriods")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .Produces<CloseOperationalPeriodResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        CloseOperationalPeriodHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
