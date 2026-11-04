namespace Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for drafting a new financial policy (ADR-004, ADR-009).
/// </summary>
public sealed class CreateFinancialPolicyEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/finance/policies", HandleAsync)
            .WithName("CreateFinancialPolicy")
            .WithTags("FinancialPolicies")
            .RequireAuthorization(Permissions.Finance.Policies.Manage)
            .WithValidation<CreateFinancialPolicyRequest>()
            .Produces<CreateFinancialPolicyResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        CreateFinancialPolicyRequest request,
        CreateFinancialPolicyHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/finance/policies/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
