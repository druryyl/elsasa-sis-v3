namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Immutable structured JSON content snapshot DTO representing a certified student report card (GAP-006, TD-05).
/// </summary>
/// <param name="ReportCardId">The unique identifier of the report card.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="StudentName">The full name of the student at certification time.</param>
/// <param name="StudentNisn">The national student identification number (NISN), if assigned.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="PeriodLabel">The label of the academic period (e.g., "Term 1 2026/2027").</param>
/// <param name="ReportType">The report card type ('mid_term' or 'end_of_term').</param>
/// <param name="TemplateName">The name of the report template applied (e.g., "Buku Rapor Merdeka").</param>
/// <param name="Status">The lifecycle status at snapshot time ('approved' or 'published').</param>
/// <param name="ApprovedBy">Username of the certifying academic authority.</param>
/// <param name="ApprovedDate">The date and time when the report card was formally approved.</param>
/// <param name="NarrativeComments">General homeroom or institution summary remarks.</param>
/// <param name="SubjectEntries">The itemized subject entries and scores.</param>
/// <param name="AttendanceSummary">Aggregated attendance tallies for the reporting period.</param>
public sealed record ReportCardSnapshotDto(
    Guid ReportCardId,
    Guid StudentId,
    string StudentName,
    string? StudentNisn,
    Guid AcademicPeriodId,
    string PeriodLabel,
    string ReportType,
    string TemplateName,
    string Status,
    string? ApprovedBy,
    DateTime? ApprovedDate,
    string? NarrativeComments,
    IReadOnlyList<ReportCardSubjectEntryDto> SubjectEntries,
    ReportCardAttendanceSummaryDto? AttendanceSummary);
