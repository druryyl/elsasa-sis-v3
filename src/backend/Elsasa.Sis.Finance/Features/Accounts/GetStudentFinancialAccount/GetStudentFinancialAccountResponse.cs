namespace Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;

/// <summary>
/// Complete 360-degree student financial account dossier response with dynamically derived balance (TD-02).
/// </summary>
public sealed record GetStudentFinancialAccountResponse(
    Guid AccountId,
    string AccountNumber,
    Guid StudentId,
    string? StudentNumber,
    string? StudentFullName,
    string CurrencyCode,
    string AccountStatus,
    DateOnly OpenedDate,
    DateOnly? ClosedDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    long TotalAssessed,
    long TotalSettled,
    long CurrentBalance,
    IReadOnlyList<OpenObligationDto> OpenObligations,
    IReadOnlyList<RecentBillingDocumentDto> RecentBillingDocuments);
