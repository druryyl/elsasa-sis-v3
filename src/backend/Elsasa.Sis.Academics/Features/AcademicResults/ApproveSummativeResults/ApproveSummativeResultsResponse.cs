namespace Elsasa.Sis.Academics.Features.AcademicResults.ApproveSummativeResults;

/// <summary>
/// Response payload after approving summative assessment results (UC-ACD-16, P4-S09, TD-04).
/// </summary>
/// <param name="ApprovedCount">The number of academic results certified to approved status.</param>
public sealed record ApproveSummativeResultsResponse(
    int ApprovedCount);
