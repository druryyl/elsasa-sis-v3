namespace Elsasa.Sis.Academics.Features.Transcripts;

using Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Data transfer object representing an academic transcript header and its compiled completed periods (UC-ACD-13, P5-S13).
/// </summary>
/// <param name="Id">The unique identifier of the transcript record.</param>
/// <param name="StudentId">The unique identifier of the target student.</param>
/// <param name="TranscriptType">The transcript type ('official' or 'unofficial').</param>
/// <param name="PeriodStart">The starting boundary date of the covered academic history.</param>
/// <param name="PeriodEnd">The ending boundary date of the covered academic history.</param>
/// <param name="Status">The lifecycle status ('draft', 'approved', or 'issued').</param>
/// <param name="Purpose">Optional declared issuance purpose (e.g. 'Advising', 'Interim Transfer').</param>
/// <param name="ApprovedBy">Username of the certifying Academic Authority.</param>
/// <param name="ApprovedDate">Date when certification was granted.</param>
/// <param name="IssuedDate">Date when the transcript was formally issued.</param>
/// <param name="ContentSnapshot">Frozen JSON snapshot string if certified/issued.</param>
/// <param name="Notes">Optional administrative notes.</param>
/// <param name="CreatedAt">Audit creation timestamp in UTC.</param>
/// <param name="CreatedBy">User who created this transcript record.</param>
/// <param name="CompletedPeriods">Optional compiled completed academic periods and courses.</param>
public sealed record TranscriptDto(
    Guid Id,
    Guid StudentId,
    string TranscriptType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Status,
    string? Purpose,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    DateOnly? IssuedDate,
    string? ContentSnapshot,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    IReadOnlyList<TranscriptPeriodSummaryDto>? CompletedPeriods = null);
