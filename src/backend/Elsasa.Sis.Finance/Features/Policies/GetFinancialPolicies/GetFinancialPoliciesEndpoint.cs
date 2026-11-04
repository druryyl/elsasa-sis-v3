namespace Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicies;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying financial policies with their active charge rules (ADR-004, ADR-009).
/// </summary>
public sealed class GetFinancialPoliciesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/policies", HandleAsync)
            .WithName("GetFinancialPolicies")
            .WithTags("FinancialPolicies")
            .RequireAuthorization(Permissions.Finance.Policies.View)
            .Produces<IReadOnlyList<FinancialPolicyListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        string? status,
        string? category,
        GetFinancialPoliciesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(status, category, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
