namespace Elsasa.Sis.Academics.Features.Assignments.GetStudentCourseworkWorkspace;

/// <summary>
/// Coursework task item projected for the student self-service workspace (SCR-SELF-03, TD-06, GAP-007, OQ-005, P5-S12).
/// </summary>
public sealed record StudentCourseworkTaskDto(
    Guid AssignmentId,
    string Title,
    string? Description,
    DateOnly ReleaseDate,
    DateOnly? DueDate,
    string AssignmentStatus,
    Guid LearningActivityId,
    string LearningActivityTitle,
    Guid LearningUnitId,
    string LearningUnitCode,
    string LearningUnitName,
    string? SectionLabel,
    string DisplayStatus,
    int? CountdownDays,
    Guid? SubmissionId,
    DateTimeOffset? SubmittedDate,
    string? SubmissionReference,
    bool IsResubmission,
    string? SubmissionNotes);
