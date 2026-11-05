namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an academic period section in a transcript snapshot.
/// </summary>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="PeriodLabel">The label of the academic period.</param>
/// <param name="Courses">The courses completed or credited during this period.</param>
/// <param name="Gpa">The grade point average calculated for this period, or null.</param>
public sealed record TranscriptPeriodSummaryDto(
    Guid AcademicPeriodId,
    string PeriodLabel,
    IReadOnlyList<TranscriptCourseEntryDto> Courses,
    decimal? Gpa);
