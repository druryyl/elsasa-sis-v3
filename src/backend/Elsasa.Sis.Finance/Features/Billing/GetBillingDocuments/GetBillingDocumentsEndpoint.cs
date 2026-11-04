namespace Elsasa.Sis.Finance.Features.Billing.GetBillingDocuments;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying billing documents with filters and pagination (FIN-WF-04, SCR-FIN-03, GAP-005, TD-05).
/// </summary>
public sealed class GetBillingDocumentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/billing-documents", HandleAsync)
            .WithName("GetBillingDocuments")
            .WithTags("FinanceBilling")
            .RequireAuthorization(Permissions.Billing.View)
            .Produces<PaginatedList<BillingDocumentListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] Guid? studentId,
        [FromQuery] string? documentType,
        [FromQuery] string? deliveryStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] GetBillingDocumentsHandler? handler = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var result = await handler.HandleAsync(
            studentId,
            documentType,
            deliveryStatus,
            page,
            pageSize,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
