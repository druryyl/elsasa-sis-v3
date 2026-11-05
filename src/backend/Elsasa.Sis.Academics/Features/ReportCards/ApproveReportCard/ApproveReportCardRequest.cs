namespace Elsasa.Sis.Academics.Features.ReportCards.ApproveReportCard;

/// <summary>
/// Request contract for approving and certifying a student report card (GAP-006, TD-05, UC-ACD-13).
/// </summary>
/// <param name="NarrativeComments">Optional updated homeroom teacher narrative remarks or certification summary.</param>
public sealed record ApproveReportCardRequest(string? NarrativeComments = null);
