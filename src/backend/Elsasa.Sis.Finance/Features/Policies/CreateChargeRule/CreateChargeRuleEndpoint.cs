namespace Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for adding an operational charge rule to a governing policy (ADR-004, ADR-009).
/// </summary>
public sealed class CreateChargeRuleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/finance/policies/{id:guid}/rules", HandleAsync)
            .WithName("CreateChargeRule")
            .WithTags("FinancialPolicies")
            .RequireAuthorization(Permissions.Finance.Policies.Manage)
            .WithValidation<CreateChargeRuleRequest>()
            .Produces<CreateChargeRuleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        CreateChargeRuleRequest request,
        CreateChargeRuleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/finance/policies/{id}/rules/{result.Value.Id}", result.Value)
            : result.Error.ToProblemDetails();
    }
}
