namespace Elsasa.Sis.Students.Features.Records.History.GetStudentAdministrativeHistory;

/// <summary>
/// Projection DTO representing an immutable administrative history audit log entry (GAP-006, TD-06).
/// </summary>
public sealed record AdministrativeHistoryEntryDto(
    Guid Id,
    string EventType,
    DateOnly EventDate,
    string Detail,
    string? AuthorizedBy,
    string? DecisionReference,
    bool IsCorrection,
    Guid? CorrectsEntryId,
    DateTimeOffset CreatedAt,
    string CreatedBy);
