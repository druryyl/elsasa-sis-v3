namespace Elsasa.Sis.Finance.Features.Payments.SubmitPayment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for self-service payment intake (SCR-SELF-02, UC-FIN-01, TD-04, OQ-004).
/// </summary>
public sealed class SubmitPaymentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/finance/payments/submit", HandleAsync)
            .WithName("SubmitPayment")
            .WithTags("FinancePayments")
            .RequireAuthorization()
            .WithValidation<SubmitPaymentRequest>()
            .Produces<SubmitPaymentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        SubmitPaymentRequest request,
        SubmitPaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/finance/payments/{result.Value.PaymentId}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
