namespace Elsasa.Sis.Finance.Features.Allocations.GetStudentReceivables;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint for querying student receivables, aging brackets, and outstanding balances (TD-05, OQ-003).
/// </summary>
public sealed class GetStudentReceivablesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/accounts/{studentId:guid}/receivables", HandleAsync)
            .WithName("GetStudentReceivables")
            .WithTags("FinanceAccounts", "FinanceAllocations")
            .RequireAuthorization(policy => policy.RequireAssertion(ctx =>
                ctx.User.Identity?.IsAuthenticated == true && (
                    ctx.User.HasClaim(c => (c.Type == "permission" || c.Type == "permissions") &&
                        (c.Value == Permissions.Accounts.View || c.Value == Permissions.Allocations.View || c.Value == "*"))
                    || ctx.User.IsInRole("Admin")
                    || ctx.User.IsInRole("FinanceAuthority") || ctx.User.IsInRole("Finance Authority")
                    || ctx.User.IsInRole("FinanceOfficer") || ctx.User.IsInRole("Finance Officer")
                    || ctx.User.IsInRole("SchoolLeadership") || ctx.User.IsInRole("School Leadership")
                )))
            .Produces<GetStudentReceivablesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        [FromQuery] bool? openOnly,
        [FromServices] GetStudentReceivablesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(studentId, openOnly, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
