namespace Elsasa.Sis.Teaching.Features.TeachingCapabilities.GetCapabilityTypes;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler retrieving all active teaching capability taxonomy types (P2-S08).
/// </summary>
public sealed class GetCapabilityTypesHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectActiveCapabilityTypesSql = """
        SELECT
            id AS Id,
            code AS Code,
            name AS Name,
            description AS Description,
            is_active AS IsActive
        FROM teaching.capability_types
        WHERE is_active = 1
        ORDER BY name ASC;
        """;

    public async Task<Result<IReadOnlyList<CapabilityTypeDto>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var types = await connection.QueryAsync<CapabilityTypeDto>(
            new CommandDefinition(_selectActiveCapabilityTypesSql, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<CapabilityTypeDto>>(types.AsList());
    }
}
