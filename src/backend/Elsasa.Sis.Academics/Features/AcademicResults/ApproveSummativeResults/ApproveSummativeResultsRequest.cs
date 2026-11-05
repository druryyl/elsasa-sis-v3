namespace Elsasa.Sis.Academics.Features.AcademicResults.ApproveSummativeResults;

/// <summary>
/// Request payload for approving summative assessment results (UC-ACD-16, P4-S09, TD-04).
/// If <see cref="ResultIds"/> is null or empty, approves all provisional operative results for the assessment.
/// </summary>
/// <param name="ResultIds">Optional collection of specific academic result IDs to approve.</param>
public sealed record ApproveSummativeResultsRequest(
    IReadOnlyList<Guid>? ResultIds = null);
