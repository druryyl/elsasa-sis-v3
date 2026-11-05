namespace Elsasa.Sis.Academics.Features.Assignments.GetAssignmentSubmissions;

/// <summary>
/// Coursework submission roster item projecting student participation and on-the-fly submission status (SCR-ACD-02, TD-06, GAP-007, P5-S12).
/// </summary>
public sealed record AssignmentSubmissionRosterItemDto(
    Guid StudentId,
    string? StudentNumber,
    string? StudentName,
    string? SectionLabel,
    string DisplayStatus,
    Guid? SubmissionId,
    DateTimeOffset? SubmittedDate,
    string? SubmissionReference,
    bool IsResubmission,
    string? Notes,
    DateTimeOffset? CreatedAt,
    string? CreatedBy);
