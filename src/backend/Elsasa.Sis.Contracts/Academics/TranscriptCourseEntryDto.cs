namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an individual completed course in a transcript snapshot.
/// </summary>
/// <param name="LearningUnitId">The unique identifier of the learning unit.</param>
/// <param name="SubjectCode">The curricular course code.</param>
/// <param name="SubjectTitle">The curricular course title.</param>
/// <param name="Credits">Credit weight allocated to this course.</param>
/// <param name="ScoreNumeric">The numeric grade achieved, or null.</param>
/// <param name="GradeLetter">The letter grade achieved, or null.</param>
/// <param name="IsTransferred">Indicates whether credits were transferred from an external institution [T].</param>
public sealed record TranscriptCourseEntryDto(
    Guid LearningUnitId,
    string SubjectCode,
    string SubjectTitle,
    decimal Credits,
    decimal? ScoreNumeric,
    string? GradeLetter,
    bool IsTransferred);
