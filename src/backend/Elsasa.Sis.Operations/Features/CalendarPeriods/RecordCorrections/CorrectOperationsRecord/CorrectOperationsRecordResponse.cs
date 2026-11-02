namespace Elsasa.Sis.Operations.Features.CalendarPeriods.RecordCorrections.CorrectOperationsRecord;

/// <summary>
/// Response payload returned after successfully executing an auditable record correction (UC-OPS-13).
/// </summary>
public sealed record CorrectOperationsRecordResponse(
    Guid Id,
    Guid AcademicYearId,
    string TargetTableName,
    Guid TargetRecordId,
    string FieldName,
    string? OldValue,
    string? NewValue,
    string JustificationReason,
    string AuthorizedBy,
    DateTimeOffset AuthorizedAt);
