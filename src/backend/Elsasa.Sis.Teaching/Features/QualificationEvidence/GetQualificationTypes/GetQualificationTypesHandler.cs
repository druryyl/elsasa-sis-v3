namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.GetQualificationTypes;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler retrieving all active qualification types (TD-02, P3-S09).
/// </summary>
public sealed class GetQualificationTypesHandler(ISqlConnectionFactory connectionFactory)
{
    static GetQualificationTypesHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectActiveQualificationTypesSql = """
        SELECT
            id AS Id,
            code AS Code,
            name AS Name,
            description AS Description,
            requires_renewal AS RequiresRenewal,
            default_validity_months AS DefaultValidityMonths,
            is_active AS IsActive
        FROM teaching.qualification_types
        WHERE is_active = 1
        ORDER BY name ASC;
        """;

    /// <summary>
    /// Executes query returning list of active qualification types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active qualification types.</returns>
    public async Task<Result<IReadOnlyList<QualificationTypeResponse>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var types = await connection.QueryAsync<QualificationTypeResponse>(
            new CommandDefinition(_selectActiveQualificationTypesSql, cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<QualificationTypeResponse>>(types.AsList());
    }
}
