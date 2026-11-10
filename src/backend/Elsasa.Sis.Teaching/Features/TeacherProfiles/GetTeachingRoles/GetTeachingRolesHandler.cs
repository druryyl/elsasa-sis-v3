namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.GetTeachingRoles;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler retrieving all active teaching roles (TD-02, TD-07, P2-S07).
/// </summary>
public sealed class GetTeachingRolesHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeachingRolesHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectActiveRolesSql = """
        SELECT
            id AS Id,
            code AS Code,
            name AS Name,
            description AS Description,
            requires_qualification_check AS RequiresQualificationCheck,
            is_active AS IsActive
        FROM teaching.teaching_roles
        WHERE is_active = 1
        ORDER BY name ASC;
        """;

    /// <summary>
    /// Executes query returning list of active teaching roles.
    /// </summary>
    public async Task<Result<IReadOnlyList<TeachingRoleDto>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var roles = await connection.QueryAsync<TeachingRoleDto>(
            new CommandDefinition(_selectActiveRolesSql, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<TeachingRoleDto>>(roles.AsList());
    }
}
