namespace Elsasa.Sis.Finance.Features.Policies.GetPolicyCategories;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying active financial policy categories ordered for UI consumption (ADR-004, IS017).
/// </summary>
public sealed class GetPolicyCategoriesHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectActiveCategoriesSql = """
        SELECT
            code AS Code,
            name AS Name,
            description AS Description,
            display_order AS DisplayOrder,
            is_active AS IsActive
        FROM finance.policy_categories
        WHERE is_active = 1
        ORDER BY display_order ASC, name ASC;
        """;

    public async Task<Result<IReadOnlyList<PolicyCategoryDto>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var categories = await connection.QueryAsync<PolicyCategoryDto>(new CommandDefinition(
            _selectActiveCategoriesSql,
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<PolicyCategoryDto>>(categories.AsList());
    }
}
