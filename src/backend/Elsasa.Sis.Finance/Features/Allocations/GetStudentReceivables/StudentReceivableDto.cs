namespace Elsasa.Sis.Finance.Features.Allocations.GetStudentReceivables;

/// <summary>
/// Data transfer object representing an open or historical student receivable item (TD-05, OQ-003).
/// Projected dynamically from <c>finance.v_student_receivables</c>.
/// </summary>
public sealed record StudentReceivableDto(
    Guid ObligationId,
    string ObligationNumber,
    Guid StudentAccountId,
    Guid? StudentId,
    Guid? ApplicantId,
    string ChargeType,
    string Description,
    bool IsMandatory,
    DateOnly DueDate,
    long OriginalAmount,
    string CurrencyCode,
    Guid? AcademicPeriodId,
    string? AcademicPeriodLabel,
    string ObligationStatus,
    long NetObligation,
    long TotalPaid,
    long OutstandingAmount,
    bool IsOverdue,
    int DaysOverdue,
    string AgingBracket,
    string ReceivableStatus);
