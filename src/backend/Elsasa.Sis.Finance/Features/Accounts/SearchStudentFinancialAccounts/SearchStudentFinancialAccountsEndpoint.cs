namespace Elsasa.Sis.Finance.Features.Accounts.SearchStudentFinancialAccounts;

using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Minimal API endpoint mapping for searching student financial accounts (ADR-004, ADR-005, TD-02).
/// </summary>
public sealed class SearchStudentFinancialAccountsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/finance/accounts", HandleAsync)
            .WithName("SearchStudentFinancialAccounts")
            .WithTags("FinanceAccounts")
            .RequireAuthorization(Permissions.Accounts.View)
            .Produces<SearchStudentFinancialAccountsResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        string? searchTerm,
        string? campus,
        string? grade,
        string? status,
        int? pageNumber,
        int? pageSize,
        SearchStudentFinancialAccountsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            searchTerm,
            campus,
            grade,
            status,
            pageNumber ?? 1,
            pageSize ?? 20,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblemDetails();
    }
}
