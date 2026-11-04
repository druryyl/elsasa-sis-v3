namespace Elsasa.Sis.Finance.Features.Payers.GetPayers;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing paginated and filtered payer search (SCR-FIN-02, TD-03).
/// </summary>
public sealed class GetPayersHandler(ISqlConnectionFactory connectionFactory)
{
    static GetPayersHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _countSql = """
        SELECT COUNT(1)
        FROM finance.payers p
        WHERE (@PayerType IS NULL OR p.payer_type = @PayerType)
          AND (@SearchTerm IS NULL OR (
              p.payer_name LIKE @SearchTermLike
              OR p.payer_number LIKE @SearchTermLike
              OR p.identification_number LIKE @SearchTermLike
              OR p.email LIKE @SearchTermLike
              OR p.phone_number LIKE @SearchTermLike
          ));
        """;

    private const string _selectSql = """
        SELECT
            p.id AS Id,
            p.payer_number AS PayerNumber,
            p.payer_name AS PayerName,
            p.payer_type AS PayerType,
            p.identification_number AS IdentificationNumber,
            p.email AS Email,
            p.phone_number AS PhoneNumber,
            p.created_at AS CreatedAt
        FROM finance.payers p
        WHERE (@PayerType IS NULL OR p.payer_type = @PayerType)
          AND (@SearchTerm IS NULL OR (
              p.payer_name LIKE @SearchTermLike
              OR p.payer_number LIKE @SearchTermLike
              OR p.identification_number LIKE @SearchTermLike
              OR p.email LIKE @SearchTermLike
              OR p.phone_number LIKE @SearchTermLike
          ))
        ORDER BY p.payer_name ASC, p.created_at DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    public async Task<Result<GetPayersResponse>> HandleAsync(
        GetPayersRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var safePage = request.Page < 1 ? 1 : request.Page;
        var safePageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);
        var offset = (safePage - 1) * safePageSize;

        var cleanType = string.IsNullOrWhiteSpace(request.PayerType) ? null : request.PayerType.Trim();
        var cleanSearch = string.IsNullOrWhiteSpace(request.SearchTerm) ? null : request.SearchTerm.Trim();
        var searchLike = cleanSearch is null ? null : $"%{cleanSearch}%";

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countSql,
            new
            {
                PayerType = cleanType,
                SearchTerm = cleanSearch,
                SearchTermLike = searchLike
            },
            cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<PayerListItemDto>(new CommandDefinition(
            _selectSql,
            new
            {
                PayerType = cleanType,
                SearchTerm = cleanSearch,
                SearchTermLike = searchLike,
                Offset = offset,
                PageSize = safePageSize
            },
            cancellationToken: cancellationToken))).ToList();

        var result = GetPayersResponse.Create(
            items,
            totalCount,
            safePage,
            safePageSize);

        return Result.Success(result);
    }
}
