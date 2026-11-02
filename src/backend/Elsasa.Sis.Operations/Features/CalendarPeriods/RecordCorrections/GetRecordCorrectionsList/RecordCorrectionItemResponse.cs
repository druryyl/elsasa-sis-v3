namespace Elsasa.Sis.Operations.Features.CalendarPeriods.RecordCorrections.GetRecordCorrectionsList;

/// <summary>
/// Response item representing an auditable record correction history entry (UC-OPS-13).
/// </summary>
public sealed record RecordCorrectionItemResponse(
    Guid Id,
    Guid AcademicYearId,
    string TargetTableName,
    Guid TargetRecordId,
    string FieldName,
    string? OldValue,
    string? NewValue,
    string JustificationReason,
    string AuthorizedBy,
    DateTimeOffset AuthorizedAt,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
