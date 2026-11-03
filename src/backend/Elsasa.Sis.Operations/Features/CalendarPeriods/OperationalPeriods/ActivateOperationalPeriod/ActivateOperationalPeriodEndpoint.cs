namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.ActivateOperationalPeriod;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for activating an operational period (ADR-005).
/// </summary>
public sealed class ActivateOperationalPeriodEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/operations/periods/{id:guid}/activate", HandleAsync)
            .WithName("ActivateOperationalPeriod")
            .WithTags("OperationalPeriods")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .Produces<ActivateOperationalPeriodResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ActivateOperationalPeriodHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
