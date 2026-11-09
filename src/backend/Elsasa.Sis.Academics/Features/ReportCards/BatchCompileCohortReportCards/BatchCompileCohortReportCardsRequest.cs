namespace Elsasa.Sis.Academics.Features.ReportCards.BatchCompileCohortReportCards;

/// <summary>
/// Request payload for set-based cohort report card compilation (TD-07, UC-ACD-13, P5-S11).
/// </summary>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="AcademicPeriodLabel">Human-readable label for the academic period.</param>
/// <param name="ReportType">Report type ('mid_term' or 'end_of_term').</param>
/// <param name="TemplateName">Name of the layout template (e.g., 'Buku Rapor Merdeka').</param>
/// <param name="SectionLabel">Optional cohort section filter (e.g., '4A'). If null or empty, compiles for all active students in period.</param>
public sealed record BatchCompileCohortReportCardsRequest(
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string ReportType,
    string TemplateName,
    string? SectionLabel);
