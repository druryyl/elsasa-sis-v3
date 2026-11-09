namespace Elsasa.Sis.Academics.Features.AcademicResults.PublishAcademicResults;

/// <summary>
/// Response payload after publishing approved academic results (UC-ACD-16, P4-S09, TD-04).
/// </summary>
/// <param name="PublishedCount">The number of academic results released to published status.</param>
public sealed record PublishAcademicResultsResponse(
    int PublishedCount);
