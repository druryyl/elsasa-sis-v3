namespace Elsasa.Sis.Academics.Features.RecordCorrections;

/// <summary>
/// Data transfer object representing an immutable academic record correction audit log entry (UC-ACD-19, TD-06, P6-S14).
/// </summary>
/// <param name="Id">The unique identifier of the correction audit record.</param>
/// <param name="RecordType">The category of academic record corrected (e.g. 'academic_result').</param>
/// <param name="RecordId">The unique identifier of the target academic record.</param>
/// <param name="OriginalValue">JSON serialized state of the values prior to alteration.</param>
/// <param name="CorrectedValue">JSON serialized state of the values after alteration.</param>
/// <param name="Reason">The mandatory justification for the correction (minimum 10 characters).</param>
/// <param name="Evidence">Optional supporting document reference or observation notes.</param>
/// <param name="CorrectedBy">Username of the authority who executed the correction.</param>
/// <param name="CorrectedDate">The date when the correction was executed.</param>
/// <param name="IsMaterial">Indicates whether the alteration was material (e.g. altering passing/failing status).</param>
/// <param name="Notes">Optional administrative remarks.</param>
/// <param name="CreatedAt">The UTC timestamp when the audit log record was created.</param>
/// <param name="CreatedBy">The user who initiated the log record.</param>
public sealed record AcademicRecordCorrectionDto(
    Guid Id,
    string RecordType,
    Guid RecordId,
    string OriginalValue,
    string CorrectedValue,
    string Reason,
    string? Evidence,
    string CorrectedBy,
    DateOnly CorrectedDate,
    bool IsMaterial,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
