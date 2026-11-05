namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an individual subject entry in a report card snapshot.
/// </summary>
/// <param name="LearningUnitId">The unique identifier of the learning unit.</param>
/// <param name="SubjectCode">The curricular course code (e.g., "MAT-101").</param>
/// <param name="SubjectTitle">The curricular course title (e.g., "Mathematics Grade 4").</param>
/// <param name="ScoreNumeric">The numeric grade achieved, or null if non-numeric.</param>
/// <param name="GradeLetter">The letter grade achieved, or null.</param>
/// <param name="GradeLabel">Descriptive grade label or mastery descriptor.</param>
/// <param name="TeacherComment">Subject-specific teacher remarks and competencies demonstrated.</param>
/// <param name="DisplayOrder">Sequential display order on report card.</param>
public sealed record ReportCardSubjectEntryDto(
    Guid LearningUnitId,
    string SubjectCode,
    string SubjectTitle,
    decimal? ScoreNumeric,
    string? GradeLetter,
    string? GradeLabel,
    string? TeacherComment,
    int DisplayOrder);
