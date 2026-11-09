namespace Elsasa.Sis.Academics.Features.Transcripts;

using Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Detailed DTO representing a student academic transcript with identity details and full period breakdowns (UC-ACD-13, P5-S13).
/// </summary>
/// <param name="Id">The unique identifier of the transcript record.</param>
/// <param name="StudentId">The unique identifier of the target student.</param>
/// <param name="StudentName">Full name of the student.</param>
/// <param name="StudentNisn">National student identification number (NISN) if available.</param>
/// <param name="TranscriptType">The transcript type ('official' or 'unofficial').</param>
/// <param name="PeriodStart">Starting date boundary of covered history.</param>
/// <param name="PeriodEnd">Ending date boundary of covered history.</param>
/// <param name="Status">The lifecycle status ('draft', 'approved', or 'issued').</param>
/// <param name="Purpose">Declared issuance purpose.</param>
/// <param name="ApprovedBy">Username of certifying Academic Authority.</param>
/// <param name="ApprovedDate">Date when certification was granted.</param>
/// <param name="IssuedDate">Date when the transcript was formally issued.</param>
/// <param name="Snapshot">Frozen immutable JSON snapshot if issued.</param>
/// <param name="CompletedPeriods">Compiled or frozen period sections with course entries.</param>
/// <param name="Notes">Optional administrative notes.</param>
/// <param name="CreatedAt">Audit creation timestamp in UTC.</param>
/// <param name="CreatedBy">User who created this record.</param>
public sealed record TranscriptDetailDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string? StudentNisn,
    string TranscriptType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Status,
    string? Purpose,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    DateOnly? IssuedDate,
    TranscriptSnapshotDto? Snapshot,
    IReadOnlyList<TranscriptPeriodSummaryDto>? CompletedPeriods,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy);
