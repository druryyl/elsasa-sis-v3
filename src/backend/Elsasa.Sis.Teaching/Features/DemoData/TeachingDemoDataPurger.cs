namespace Elsasa.Sis.Teaching.Features.DemoData;

using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous purger for the Teaching domain schema (ADR-002, ADR-004, TD-09, P5-S16).
/// Deletes all demo data from teaching.* tables in strict reverse-dependency order.
/// </summary>
public sealed class TeachingDemoDataPurger(
    ISqlConnectionFactory connectionFactory) : ITeachingDemoDataPurger
{
    private const string _purgeSql = """
        DELETE FROM teaching.teaching_record_corrections;
        DELETE FROM teaching.supervision_requirements;
        DELETE FROM teaching.authorization_evidence;
        DELETE FROM teaching.teaching_authorizations;
        DELETE FROM teaching.qualification_evidence;
        DELETE FROM teaching.teaching_capabilities;
        DELETE FROM teaching.teacher_profiles;
        DELETE FROM teaching.qualification_types;
        DELETE FROM teaching.capability_types;
        DELETE FROM teaching.teaching_roles;
        """;

    /// <inheritdoc/>
    public async Task PurgeDomainDataAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(_purgeSql, cancellationToken: cancellationToken));
    }
}
