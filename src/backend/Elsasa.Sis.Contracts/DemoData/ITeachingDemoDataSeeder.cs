namespace Elsasa.Sis.Contracts.DemoData;

/// <summary>
/// Seeds the Teaching domain reference data (teaching roles, capability types, qualification types),
/// realistic Hagios personas, potential capabilities, qualification evidence, and authorizations.
/// </summary>
public interface ITeachingDemoDataSeeder
{
    /// <summary>
    /// Seeds the Teaching domain using operations context for campus and date alignment.
    /// </summary>
    /// <param name="operationsContext">The operations seed context containing academic year and period identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result summary of seeded teaching entities and persona identifiers.</returns>
    Task<TeachingSeedResult> SeedTeachingAsync(
        OperationsSeedResult operationsContext,
        CancellationToken cancellationToken = default);
}
