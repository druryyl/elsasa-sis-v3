namespace Elsasa.Sis.Academics.Features.ReportCards;

/// <summary>
/// Summary DTO representing a certified student report card for portal listings (GAP-006, SCR-SELF-03).
/// </summary>
/// <param name="Id">The unique identifier of the report card.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="PeriodLabel">Human-readable label of the academic period.</param>
/// <param name="ReportType">Report type ('mid_term' or 'end_of_term').</param>
/// <param name="TemplateName">Name of the report card template applied.</param>
/// <param name="Status">Report card lifecycle status ('published').</param>
/// <param name="ApprovedBy">Username of certifying authority.</param>
/// <param name="ApprovedDate">Date when formally approved.</param>
/// <param name="PublishedDate">Date when published to portals.</param>
/// <param name="NarrativeComments">Homeroom summary remarks.</param>
/// <param name="CreatedAt">Audit creation timestamp in UTC.</param>
public sealed record ReportCardSummaryDto(
    Guid Id,
    Guid StudentId,
    Guid AcademicPeriodId,
    string PeriodLabel,
    string ReportType,
    string TemplateName,
    string Status,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    DateOnly? PublishedDate,
    string? NarrativeComments,
    DateTimeOffset CreatedAt);
