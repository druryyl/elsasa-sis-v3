namespace Elsasa.Sis.Finance.Features.DemoData;

using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous purger for the Finance domain schema (ADR-002, ADR-004).
/// Deletes all demo data from finance.* tables in strict reverse dependency order.
/// </summary>
public sealed class FinanceDemoDataPurger(
    ISqlConnectionFactory connectionFactory) : IFinanceDemoDataPurger
{
    private const string _purgeSql = """
        DELETE FROM finance.payment_allocations;
        DELETE FROM finance.payments;
        DELETE FROM finance.payers;
        DELETE FROM finance.billing_document_items;
        DELETE FROM finance.billing_documents;
        DELETE FROM finance.obligation_due_schedules;
        DELETE FROM finance.financial_obligations;
        DELETE FROM finance.student_financial_accounts;
        DELETE FROM finance.charge_rules;
        DELETE FROM finance.financial_policies;
        """;

    public async Task PurgeDomainDataAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(_purgeSql, cancellationToken: cancellationToken));
    }
}
