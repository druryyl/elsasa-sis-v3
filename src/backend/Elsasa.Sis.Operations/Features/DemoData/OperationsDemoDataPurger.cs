namespace Elsasa.Sis.Operations.Features.DemoData;

using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous purger for the Operations domain schema (ADR-002, ADR-004).
/// Deletes all demo data from operations.* tables in strict reverse dependency order.
/// </summary>
public sealed class OperationsDemoDataPurger(
    ISqlConnectionFactory connectionFactory) : IOperationsDemoDataPurger
{
    private const string _purgeSql = """
        DELETE FROM operations.operations_record_corrections;
        DELETE FROM operations.calendar_amendments;
        DELETE FROM operations.calendar_entries;
        DELETE FROM operations.school_closures;
        DELETE FROM operations.school_calendars;
        DELETE FROM operations.operational_periods;
        DELETE FROM operations.academic_years;
        """;

    public async Task PurgeDomainDataAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(_purgeSql, cancellationToken: cancellationToken));
    }
}
