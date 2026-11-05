namespace Elsasa.Sis.Academics.Features.AcademicResults.PublishAcademicResults;

/// <summary>
/// Request payload for publishing approved academic results for student/parent portal visibility (UC-ACD-16, P4-S09, TD-04).
/// If <see cref="ResultIds"/> is null or empty, publishes all approved operative results for the assessment.
/// </summary>
/// <param name="ResultIds">Optional collection of specific academic result IDs to publish.</param>
public sealed record PublishAcademicResultsRequest(
    IReadOnlyList<Guid>? ResultIds = null);
