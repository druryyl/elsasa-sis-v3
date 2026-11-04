namespace Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for reallocating or reversing payment allocations (TD-05, OQ-003).
/// </summary>
public sealed class ReallocatePaymentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/finance/allocations/reallocate", HandleAsync)
            .WithName("ReallocatePayment")
            .WithTags("FinanceAllocations")
            .RequireAuthorization(Permissions.Allocations.Reallocate)
            .WithValidation<ReallocatePaymentRequest>()
            .Produces<ReallocatePaymentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        ReallocatePaymentRequest request,
        ReallocatePaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
