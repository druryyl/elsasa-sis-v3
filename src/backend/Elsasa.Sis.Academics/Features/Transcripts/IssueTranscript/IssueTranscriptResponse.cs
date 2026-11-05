namespace Elsasa.Sis.Academics.Features.Transcripts.IssueTranscript;

using Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Response payload returned after an academic transcript is officially certified and issued (UC-ACD-13, P5-S13).
/// </summary>
/// <param name="Id">The unique identifier of the issued transcript record.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="TranscriptType">The transcript type ('official' or 'unofficial').</param>
/// <param name="Status">The lifecycle status (strictly 'issued').</param>
/// <param name="IssuedDate">Date when the transcript was formally issued.</param>
/// <param name="ApprovedBy">Username of the certifying Academic Authority.</param>
/// <param name="Snapshot">The frozen immutable structured JSON snapshot contract.</param>
public sealed record IssueTranscriptResponse(
    Guid Id,
    Guid StudentId,
    string TranscriptType,
    string Status,
    DateOnly IssuedDate,
    string ApprovedBy,
    TranscriptSnapshotDto Snapshot);
