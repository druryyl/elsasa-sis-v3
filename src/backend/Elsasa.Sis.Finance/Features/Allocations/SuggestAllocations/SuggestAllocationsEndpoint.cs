namespace Elsasa.Sis.Finance.Features.Allocations.SuggestAllocations;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying smart payment allocation suggestions (TD-05, OQ-003).
/// </summary>
public sealed class SuggestAllocationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/allocations/suggest", HandleAsync)
            .WithName("SuggestAllocations")
            .WithTags("FinanceAllocations")
            .RequireAuthorization(Permissions.Allocations.View)
            .Produces<SuggestAllocationsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? studentId,
        [FromQuery] Guid? applicantId,
        [FromQuery] long paymentAmount,
        [FromServices] SuggestAllocationsHandler handler,
        CancellationToken cancellationToken)
    {
        var request = new SuggestAllocationsRequest(studentId, applicantId, paymentAmount);
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
