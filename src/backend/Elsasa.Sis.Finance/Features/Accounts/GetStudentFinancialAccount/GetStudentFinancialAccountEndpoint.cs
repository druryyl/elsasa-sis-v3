namespace Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for retrieving student financial account details (ADR-004, ADR-005, TD-02).
/// </summary>
public sealed class GetStudentFinancialAccountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/accounts/{studentId:guid}", HandleAsync)
            .WithName("GetStudentFinancialAccount")
            .WithTags("FinanceAccounts")
            .RequireAuthorization(Permissions.Accounts.View)
            .Produces<GetStudentFinancialAccountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId,
        GetStudentFinancialAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(studentId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
