namespace Elsasa.Sis.Finance.Features.Payments.GetPayments;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying payments and verification queue worklists (SCR-FIN-02, TD-04).
/// </summary>
public sealed class GetPaymentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/payments", HandleAsync)
            .WithName("GetPayments")
            .WithTags("FinancePayments")
            .RequireAuthorization(Permissions.Payments.View)
            .Produces<GetPaymentsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] string? status,
        [FromQuery] Guid? studentId,
        [FromQuery] Guid? applicantId,
        [FromQuery] Guid? payerId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? searchTerm,
        [FromQuery] int? page,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromServices] GetPaymentsHandler? handler = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var resolvedPage = pageNumber ?? page ?? 1;
        var resolvedPageSize = pageSize ?? 20;

        var result = await handler.HandleAsync(
            status,
            studentId,
            applicantId,
            payerId,
            startDate,
            endDate,
            searchTerm,
            resolvedPage,
            resolvedPageSize,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
