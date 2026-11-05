namespace Elsasa.Sis.Academics.Features.ReportCards;

using Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Detailed DTO representing a student report card with identity metadata, itemized subject entries,
/// and frozen snapshot or live preview content (GAP-006, TD-05, UC-ACD-13).
/// </summary>
/// <param name="Id">The unique identifier of the report card.</param>
/// <param name="StudentId">The unique identifier of the target student.</param>
/// <param name="StudentName">Full name of the student.</param>
/// <param name="StudentNisn">National student identification number (NISN) if available.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="AcademicPeriodLabel">Human-readable label of the academic period.</param>
/// <param name="ReportType">Report type ('mid_term' or 'end_of_term').</param>
/// <param name="TemplateName">Name of the report card template applied (e.g., 'Buku Rapor Merdeka').</param>
/// <param name="Status">Report card lifecycle status ('draft', 'approved', 'published').</param>
/// <param name="ApprovedBy">Username of the certifying authority, if approved.</param>
/// <param name="ApprovedDate">Date when formally certified, if applicable.</param>
/// <param name="PublishedDate">Date when published to portals, if applicable.</param>
/// <param name="NarrativeComments">Homeroom summary remarks or general notes.</param>
/// <param name="ContentSnapshot">Immutable frozen JSON snapshot for approved/published report cards.</param>
/// <param name="Snapshot">Deserialized snapshot DTO contract if approved or published.</param>
/// <param name="Entries">Constituent subject-level report card entries.</param>
/// <param name="AttendanceSummary">Aggregated attendance tallies for the reporting period.</param>
/// <param name="CreatedAt">Audit creation timestamp in UTC.</param>
/// <param name="CreatedBy">Creation audit username.</param>
public sealed record ReportCardDetailDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string? StudentNisn,
    Guid AcademicPeriodId,
    string PeriodLabel,
    string ReportType,
    string TemplateName,
    string Status,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    DateOnly? PublishedDate,
    string? NarrativeComments,
    string? ContentSnapshot,
    ReportCardSnapshotDto? Snapshot,
    IReadOnlyList<ReportCardEntryDto> Entries,
    ReportCardAttendanceSummaryDto? AttendanceSummary,
    DateTimeOffset CreatedAt,
    string CreatedBy);
