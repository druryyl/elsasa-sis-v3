namespace Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicyById;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying full details and attached charge rules of a financial policy (ADR-004, ADR-009).
/// </summary>
public sealed class GetFinancialPolicyByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/policies/{id:guid}", HandleAsync)
            .WithName("GetFinancialPolicyById")
            .WithTags("FinancialPolicies")
            .RequireAuthorization(Permissions.Finance.Policies.View)
            .Produces<FinancialPolicyDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        GetFinancialPolicyByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
