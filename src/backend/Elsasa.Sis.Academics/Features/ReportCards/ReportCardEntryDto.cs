namespace Elsasa.Sis.Academics.Features.ReportCards;

/// <summary>
/// Subject-level entry in a compiled report card (UC-ACD-13, ACD-WF-12).
/// </summary>
/// <param name="Id">The unique identifier of the report card entry.</param>
/// <param name="ReportCardId">The unique identifier of the parent report card.</param>
/// <param name="LearningUnitId">The unique identifier of the evaluated learning unit.</param>
/// <param name="SubjectCode">The curricular code of the learning unit.</param>
/// <param name="SubjectTitle">The title of the learning unit.</param>
/// <param name="AcademicResultId">The approved operative academic result ID, if available.</param>
/// <param name="ScoreNumeric">The numeric score achieved, if applicable.</param>
/// <param name="GradeLetter">The letter grade achieved, if applicable.</param>
/// <param name="GradeLabel">The competency or mastery label, if applicable.</param>
/// <param name="DisplayOrder">Sequential display order in report card.</param>
/// <param name="TeacherComment">Subject teacher feedback or evaluation comments.</param>
public sealed record ReportCardEntryDto(
    Guid Id,
    Guid ReportCardId,
    Guid LearningUnitId,
    string? SubjectCode,
    string? SubjectTitle,
    Guid? AcademicResultId,
    decimal? ScoreNumeric,
    string? GradeLetter,
    string? GradeLabel,
    int DisplayOrder,
    string? TeacherComment);
