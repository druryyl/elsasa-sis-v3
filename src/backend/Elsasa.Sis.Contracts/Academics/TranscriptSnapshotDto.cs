namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Immutable structured JSON content snapshot DTO representing an issued student transcript (UC-ACD-13, TD-05).
/// </summary>
/// <param name="TranscriptId">The unique identifier of the transcript record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="StudentName">The full name of the student at issuance time.</param>
/// <param name="StudentNisn">The national student identification number (NISN), if assigned.</param>
/// <param name="TranscriptType">The transcript type ('official' or 'unofficial').</param>
/// <param name="PeriodStart">The starting boundary date of the covered academic history.</param>
/// <param name="PeriodEnd">The ending boundary date of the covered academic history.</param>
/// <param name="Status">The lifecycle status ('draft', 'approved', or 'issued').</param>
/// <param name="Purpose">Optional declared issuance purpose (e.g., "University Application").</param>
/// <param name="CompletedPeriods">List of completed academic periods and courses.</param>
public sealed record TranscriptSnapshotDto(
    Guid TranscriptId,
    Guid StudentId,
    string StudentName,
    string? StudentNisn,
    string TranscriptType,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string Status,
    string? Purpose,
    IReadOnlyList<TranscriptPeriodSummaryDto> CompletedPeriods);
