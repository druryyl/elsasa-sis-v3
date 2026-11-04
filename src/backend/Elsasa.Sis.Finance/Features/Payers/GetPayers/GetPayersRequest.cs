namespace Elsasa.Sis.Finance.Features.Payers.GetPayers;

/// <summary>
/// Query parameters for paginated search and listing of payers (SCR-FIN-02, TD-03).
/// </summary>
/// <param name="SearchTerm">Optional search term matching name, number, identification, email, or phone.</param>
/// <param name="PayerType">Optional filter by payer classification (e.g. 'parent_guardian', 'sponsor', 'embassy').</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size between 1 and 100 (default: 20).</param>
public sealed record GetPayersRequest(
    string? SearchTerm = null,
    string? PayerType = null,
    int Page = 1,
    int PageSize = 20);
