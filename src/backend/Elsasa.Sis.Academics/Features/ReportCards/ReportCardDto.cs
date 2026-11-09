namespace Elsasa.Sis.Academics.Features.ReportCards;

/// <summary>
/// Operational report card DTO returned upon compilation or inspection (UC-ACD-13, ACD-WF-12).
/// </summary>
/// <param name="Id">The unique identifier of the report card.</param>
/// <param name="StudentId">The student identifier.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="AcademicPeriodLabel">Human-readable label of the academic period.</param>
/// <param name="ReportType">Report type ('mid_term' or 'end_of_term').</param>
/// <param name="TemplateName">Name of the report card template applied.</param>
/// <param name="Status">Report card lifecycle status ('draft', 'approved', 'published').</param>
/// <param name="ApprovedBy">Username of the certifying authority, if approved.</param>
/// <param name="ApprovedDate">Date when formally approved, if applicable.</param>
/// <param name="PublishedDate">Date when published to portals, if applicable.</param>
/// <param name="NarrativeComments">Homeroom summary remarks or general notes.</param>
/// <param name="ContentSnapshot">Immutable frozen JSON snapshot for approved/published report cards.</param>
/// <param name="Entries">Constituent subject-level report card entries.</param>
/// <param name="CreatedAt">Creation audit timestamp.</param>
/// <param name="CreatedBy">Creation audit username.</param>
public sealed record ReportCardDto(
    Guid Id,
    Guid StudentId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string ReportType,
    string TemplateName,
    string Status,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    DateOnly? PublishedDate,
    string? NarrativeComments,
    string? ContentSnapshot,
    IReadOnlyList<ReportCardEntryDto> Entries,
    DateTimeOffset CreatedAt,
    string CreatedBy);
