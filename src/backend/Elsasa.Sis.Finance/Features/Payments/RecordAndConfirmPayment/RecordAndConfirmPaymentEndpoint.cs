namespace Elsasa.Sis.Finance.Features.Payments.RecordAndConfirmPayment;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for cashier counter payment recording and atomic confirmation (SCR-FIN-02, UC-FIN-04, TD-04, OQ-004).
/// </summary>
public sealed class RecordAndConfirmPaymentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/finance/payments", HandleAsync)
            .WithName("RecordAndConfirmPayment")
            .WithTags("FinancePayments")
            .RequireAuthorization(Permissions.Payments.Record)
            .WithValidation<RecordAndConfirmPaymentRequest>()
            .Produces<RecordAndConfirmPaymentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        RecordAndConfirmPaymentRequest request,
        RecordAndConfirmPaymentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/finance/payments/{result.Value.PaymentId}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
