namespace Elsasa.Sis.Finance.Features.Policies.GetPolicyCategories;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying active financial policy categories (ADR-004, ADR-009, IS017).
/// </summary>
public sealed class GetPolicyCategoriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/policy-categories", HandleAsync)
            .WithName("GetPolicyCategories")
            .WithTags("FinancialPolicies")
            .RequireAuthorization(Permissions.Finance.Policies.View)
            .Produces<IReadOnlyList<PolicyCategoryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        GetPolicyCategoriesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
