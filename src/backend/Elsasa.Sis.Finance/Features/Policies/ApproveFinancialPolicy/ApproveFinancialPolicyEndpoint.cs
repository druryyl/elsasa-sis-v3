namespace Elsasa.Sis.Finance.Features.Policies.ApproveFinancialPolicy;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for approving a financial policy draft (ADR-004, ADR-009).
/// </summary>
public sealed class ApproveFinancialPolicyEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/finance/policies/{id:guid}/approve", HandleAsync)
            .WithName("ApproveFinancialPolicy")
            .WithTags("FinancialPolicies")
            .RequireAuthorization(Permissions.Finance.Policies.Approve)
            .Produces<ApproveFinancialPolicyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ApproveFinancialPolicyRequest? request,
        ApproveFinancialPolicyHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
