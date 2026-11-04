namespace Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation;

/// <summary>
/// Summary representation of an ad-hoc assessed obligation and its initial sequence-1 schedule.
/// </summary>
public sealed record AssessAdHocObligationResponse(
    Guid Id,
    string ObligationNumber,
    Guid StudentAccountId,
    Guid StudentId,
    Guid PolicyId,
    Guid? ChargeRuleId,
    string Description,
    string ChargeType,
    bool IsMandatory,
    long OriginalAmount,
    string CurrencyCode,
    DateOnly IssueDate,
    DateOnly DueDate,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string Status,
    string AuthorizedBy,
    string? Notes,
    Guid InitialScheduleId,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
