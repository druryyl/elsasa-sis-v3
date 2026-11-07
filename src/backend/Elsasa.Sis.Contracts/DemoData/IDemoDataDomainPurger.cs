namespace Elsasa.Sis.Contracts.DemoData;

/// <summary>
/// Marker interface implemented by every domain purger (ADR-001, ADR-002).
/// Each domain assembly owns and implements this contract to wipe its
/// own schema in reverse dependency order. The host orchestrator calls
/// this interface only; no cross-domain assembly references are required.
/// </summary>
public interface IDemoDataDomainPurger
{
    /// <summary>
    /// Purges all demo data from the owning domain schema.
    /// </summary>
    Task PurgeDomainDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Domain purger interface for the Academics bounded context schema (academics.*).
/// </summary>
public interface IAcademicsDemoDataPurger : IDemoDataDomainPurger
{
}

/// <summary>
/// Domain purger interface for the Finance bounded context schema (finance.*).
/// </summary>
public interface IFinanceDemoDataPurger : IDemoDataDomainPurger
{
}

/// <summary>
/// Domain purger interface for the Students bounded context schema (students.*).
/// </summary>
public interface IStudentsDemoDataPurger : IDemoDataDomainPurger
{
}

/// <summary>
/// Domain purger interface for the Operations bounded context schema (operations.*).
/// </summary>
public interface IOperationsDemoDataPurger : IDemoDataDomainPurger
{
}

