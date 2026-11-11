namespace Elsasa.Sis.Finance.Features.Obligations.GetObligations;

/// <summary>
/// Projections representing a financial obligation with its attached due schedules summary.
/// </summary>
public sealed record ObligationListItemDto(
    Guid Id,
    string ObligationNumber,
    Guid StudentAccountId,
    Guid StudentId,
    string StudentNumber,
    string StudentFullName,
    Guid PolicyId,
    string? PolicyCode,
    Guid? ChargeRuleId,
    string? ChargeRuleCode,
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
    DateTimeOffset CreatedAt,
    IReadOnlyList<ObligationScheduleDto> Schedules,
    long? OutstandingAmount = null,
    string? AgingBracket = null)
{
    public ObligationListItemDto WithSchedules(IReadOnlyList<ObligationScheduleDto> schedules) =>
        this with { Schedules = schedules };
}
