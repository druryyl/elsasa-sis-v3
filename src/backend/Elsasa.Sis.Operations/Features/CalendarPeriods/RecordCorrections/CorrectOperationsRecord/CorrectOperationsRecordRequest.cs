namespace Elsasa.Sis.Operations.Features.CalendarPeriods.RecordCorrections.CorrectOperationsRecord;

/// <summary>
/// Request payload for submitting an auditable record correction (UC-OPS-13).
/// </summary>
public sealed record CorrectOperationsRecordRequest(
    Guid? AcademicYearId,
    string TargetTableName,
    Guid TargetRecordId,
    string FieldName,
    string? NewValue,
    string JustificationReason,
    string? AuthorizedBy);
