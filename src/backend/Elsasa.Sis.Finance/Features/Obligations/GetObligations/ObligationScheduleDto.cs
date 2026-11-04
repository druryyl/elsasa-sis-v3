namespace Elsasa.Sis.Finance.Features.Obligations.GetObligations;

/// <summary>
/// Summary projection representing an installment due schedule attached to an obligation.
/// </summary>
public sealed record ObligationScheduleDto(
    Guid Id,
    Guid ObligationId,
    int InstallmentSequence,
    DateOnly DueDate,
    long ScheduledAmount,
    string CurrencyCode,
    string Status,
    string? Notes);
