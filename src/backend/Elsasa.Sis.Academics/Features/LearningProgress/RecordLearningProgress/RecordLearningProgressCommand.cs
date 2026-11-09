namespace Elsasa.Sis.Academics.Features.LearningProgress.RecordLearningProgress;

/// <summary>
/// Command payload for recording student progress toward learning expectations (P2-S04, UC-ACD-12).
/// Supports explicit completion date stamping for out-of-period completion and recognized transfer learning.
/// </summary>
/// <param name="StudentId">Target student identifier.</param>
/// <param name="LearningUnitId">Target learning unit identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="Status">Categorical progress status ('not_started', 'in_progress', 'completed', 'needs_remediation', 'exempted').</param>
/// <param name="Basis">Operational basis ('assessment_evidence', 'learning_work', 'authorized_decision').</param>
/// <param name="CompletionDate">Optional explicit completion date, defaulted if status is completed, null otherwise.</param>
/// <param name="Notes">Optional explanatory notes, mandatory when basis is 'authorized_decision'.</param>
public sealed record RecordLearningProgressCommand(
    Guid StudentId,
    Guid LearningUnitId,
    Guid AcademicPeriodId,
    string Status,
    string Basis,
    DateOnly? CompletionDate = null,
    string? Notes = null);
