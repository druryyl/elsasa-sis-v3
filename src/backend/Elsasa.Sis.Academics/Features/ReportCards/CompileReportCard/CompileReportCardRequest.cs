namespace Elsasa.Sis.Academics.Features.ReportCards.CompileReportCard;

/// <summary>
/// Request payload for compiling an individual student report card (UC-ACD-13, ACD-WF-12, P5-S11).
/// </summary>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="AcademicPeriodLabel">Human-readable label for the academic period.</param>
/// <param name="ReportType">Report type ('mid_term' or 'end_of_term').</param>
/// <param name="TemplateName">Name of the layout template (e.g., 'Buku Rapor Merdeka').</param>
/// <param name="NarrativeComments">Optional homeroom teacher remarks or overall notes.</param>
public sealed record CompileReportCardRequest(
    Guid StudentId,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string ReportType,
    string TemplateName,
    string? NarrativeComments);
