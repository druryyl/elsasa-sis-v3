namespace Elsasa.Sis.Finance.Features.Billing.GenerateBillingDocument;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for generating/compiling a point-in-time billing document (bill/invoice) (FIN-WF-04, UC-FIN-03, GAP-005, TD-05).
/// </summary>
public sealed class GenerateBillingDocumentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/finance/billing-documents/generate", HandleAsync)
            .WithName("GenerateBillingDocument")
            .WithTags("FinanceBilling")
            .RequireAuthorization(Permissions.Billing.Generate)
            .WithValidation<GenerateBillingDocumentRequest>()
            .Produces<GenerateBillingDocumentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        GenerateBillingDocumentRequest request,
        GenerateBillingDocumentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/finance/billing-documents/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
