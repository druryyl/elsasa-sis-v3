namespace Elsasa.Sis.Contracts.DemoData;

using Elsasa.Sis.Contracts.Operations;

/// <summary>
/// Seeds the Operations domain calendar structure (academic years, periods,
/// school calendars, entries, closures) dynamically anchored around a base date.
/// </summary>
public interface IOperationsDemoDataSeeder
{
    Task<OperationsSeedResult> SeedOperationsAsync(
        DateTime baseDate,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Seeds the Academics domain curriculum structure: learning structure,
/// parent subject units, child PACE units, and grading requirements.
/// </summary>
public interface IAcademicsStructureDemoDataSeeder
{
    Task<AcademicsStructureSeedResult> SeedAcademicsStructureAsync(
        OperationsSeedResult operationsContext,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Seeds the Finance domain policies and charge rules (tuition, facility,
/// PACE material fees, scholarship waivers).
/// </summary>
public interface IFinancePolicyDemoDataSeeder
{
    Task<FinancePolicySeedResult> SeedFinancePoliciesAsync(
        OperationsSeedResult operationsContext,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Seeds the Students domain: staged applicants, enrolled students,
/// guardians, placements, and statuses.
/// </summary>
public interface IStudentsDemoDataSeeder
{
    Task<StudentsSeedResult> SeedStudentsAsync(
        OperationsSeedResult operationsContext,
        AcademicsStructureSeedResult academicsContext,
        FinancePolicySeedResult financeContext,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Seeds the Finance domain transactions: financial accounts, obligations,
/// billing documents, payments, allocations, and scholarship waivers.
/// </summary>
public interface IFinanceTransactionsDemoDataSeeder
{
    Task<FinanceTransactionsSeedResult> SeedFinanceTransactionsAsync(
        OperationsSeedResult operationsContext,
        FinancePolicySeedResult financePolicyContext,
        StudentsSeedResult studentsContext,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Seeds the Academics domain activities: participations, attendance,
/// coursework, submissions, results, report cards, and transcripts.
/// </summary>
public interface IAcademicsActivitiesDemoDataSeeder
{
    Task<AcademicsActivitiesSeedResult> SeedAcademicsActivitiesAsync(
        OperationsSeedResult operationsContext,
        AcademicsStructureSeedResult academicsContext,
        StudentsSeedResult studentsContext,
        CancellationToken cancellationToken = default);
}
