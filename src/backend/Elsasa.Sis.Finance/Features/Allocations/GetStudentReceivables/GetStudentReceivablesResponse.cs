namespace Elsasa.Sis.Finance.Features.Allocations.GetStudentReceivables;

/// <summary>
/// Response payload for the student receivables query (TD-05, OQ-003).
/// Aggregates net obligations, paid totals, outstanding balances, overdue flags, elapsed overdue days, and itemized receivables.
/// </summary>
public sealed record GetStudentReceivablesResponse(
    Guid StudentId,
    long TotalNetObligation,
    long TotalPaid,
    long TotalOutstanding,
    int TotalOverdueCount,
    IReadOnlyList<StudentReceivableDto> Receivables,
    IReadOnlyList<StudentReceivableDto> OpenObligations);
