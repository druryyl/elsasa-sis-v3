namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarAmendments.GetCalendarAmendmentsList;

/// <summary>
/// Response payload representing a calendar amendment audit record (TD-05).
/// </summary>
public sealed record CalendarAmendmentResponse(
    Guid Id,
    Guid CalendarId,
    string AmendmentNumber,
    string FromVersion,
    string ToVersion,
    string AmendmentType,
    Guid? AffectedEntryId,
    string ChangeSummary,
    string? PreviousValue,
    string? NewValue,
    string Reason,
    DateOnly EffectiveDate,
    string AuthorizedBy,
    DateOnly AuthorizedDate,
    bool DownstreamImpactReviewed,
    DateTimeOffset CreatedAt);
