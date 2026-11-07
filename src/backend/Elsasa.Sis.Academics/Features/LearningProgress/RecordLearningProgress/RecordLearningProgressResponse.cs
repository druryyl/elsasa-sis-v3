namespace Elsasa.Sis.Academics.Features.LearningProgress.RecordLearningProgress;

/// <summary>
/// Response payload for learning progress recording (P2-S04, UC-ACD-12).
/// </summary>
/// <param name="Id">The unique identifier of the progress record.</param>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="LearningUnitId">Target learning unit identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="Status">Categorical progress status ('not_started', 'in_progress', 'completed', 'needs_remediation', 'exempted').</param>
/// <param name="Basis">Operational basis ('assessment_evidence', 'learning_work', 'authorized_decision').</param>
/// <param name="CompletionDate">Recorded completion date, or null if incomplete.</param>
/// <param name="Notes">Explanatory or pedagogical notes.</param>
/// <param name="CreatedAt">Creation audit timestamp.</param>
/// <param name="CreatedBy">Creation user identity.</param>
/// <param name="UpdatedAt">Last update audit timestamp.</param>
/// <param name="UpdatedBy">Last update user identity.</param>
public sealed record RecordLearningProgressResponse(
    Guid Id,
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string Status,
    string Basis,
    DateOnly? CompletionDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
