namespace Elsasa.Sis.Finance.Features.Accounts.SearchStudentFinancialAccounts;

/// <summary>
/// Summary item projecting student financial account joined with student demographics and derived open balance (TD-02).
/// </summary>
public sealed record StudentFinancialAccountListItemDto(
    Guid AccountId,
    string AccountNumber,
    Guid StudentId,
    string StudentNumber,
    string StudentFullName,
    string Campus,
    string Grade,
    string CurrencyCode,
    string AccountStatus,
    DateOnly OpenedDate,
    long TotalAssessed,
    long TotalSettled,
    long CurrentBalance);
