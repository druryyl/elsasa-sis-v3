namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.GetAssignmentRoles;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying assignment roles catalog (TD-01, P3-S06).
/// </summary>
public sealed class GetAssignmentRolesHandler(ISqlConnectionFactory connectionFactory)
{
    static GetAssignmentRolesHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssignmentRolesSql = """
        SELECT
            id AS Id,
            code AS Code,
            name AS Name,
            description AS Description,
            is_lead_role AS IsLeadRole,
            counts_toward_workload AS CountsTowardWorkload,
            is_active AS IsActive,
            created_at AS CreatedAt
        FROM teaching.assignment_roles
        WHERE (@ActiveOnly = 0 OR is_active = 1)
        ORDER BY name ASC;
        """;

    public async Task<Result<IReadOnlyList<AssignmentRoleDto>>> HandleAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var roles = await connection.QueryAsync<AssignmentRoleDto>(new CommandDefinition(
            _selectAssignmentRolesSql,
            new { ActiveOnly = activeOnly ? 1 : 0 },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<AssignmentRoleDto>>(roles.ToList());
    }
}
