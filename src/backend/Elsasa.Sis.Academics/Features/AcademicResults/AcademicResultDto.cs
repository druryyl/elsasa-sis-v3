namespace Elsasa.Sis.Academics.Features.AcademicResults;

/// <summary>
/// Data transfer object representing an authoritative academic evaluation result (P4-S08, TD-03, TD-04).
/// </summary>
/// <param name="Id">The unique identifier of the academic result.</param>
/// <param name="AssessmentId">The unique identifier of the parent assessment.</param>
/// <param name="StudentId">The student unique identifier, if evaluated for an enrolled student.</param>
/// <param name="ApplicantId">The applicant unique identifier, if evaluated for an admissions entrance candidate.</param>
/// <param name="AttemptNumber">The attempt sequence number (1 for initial attempt, 2+ for retakes).</param>
/// <param name="ReplacesResultId">The ID of the superceded attempt result, if applicable.</param>
/// <param name="IsFinal">Indicates whether this attempt is currently adjudicated as operative.</param>
/// <param name="ScoreNumeric">The evaluated numeric score, if applicable.</param>
/// <param name="ScoreMax">The maximum possible score configured for this evaluation.</param>
/// <param name="GradeLetter">The evaluated letter grade (e.g. 'A', 'B+'), if applicable.</param>
/// <param name="GradeLabel">The evaluated descriptor label (e.g. 'Kompeten', 'Sangat Mahir'), if applicable.</param>
/// <param name="NarrativeFeedback">The qualitative formative feedback or remarks.</param>
/// <param name="EvaluationModel">The evaluation model used ('numeric', 'letter_grade', 'pass_fail', 'competency', 'rubric', 'narrative', 'completion').</param>
/// <param name="EvaluatedBy">The username of the evaluator / teacher.</param>
/// <param name="EvaluatedDate">The date when evaluation occurred.</param>
/// <param name="ResultStatus">The governance lifecycle status ('provisional', 'approved', 'published').</param>
/// <param name="ApprovedBy">The authority username who certified this result, if approved.</param>
/// <param name="ApprovedDate">The certification date, if approved.</param>
/// <param name="PublishedDate">The release date, if published.</param>
/// <param name="Notes">Internal administrative or pedagogical notes.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
/// <param name="CreatedBy">The user who created the record.</param>
/// <param name="UpdatedAt">The UTC modification timestamp, if updated.</param>
/// <param name="UpdatedBy">The user who updated the record, if updated.</param>
public sealed record AcademicResultDto(
    Guid Id,
    Guid AssessmentId,
    Guid? StudentId,
    Guid? ApplicantId,
    int AttemptNumber,
    Guid? ReplacesResultId,
    bool IsFinal,
    decimal? ScoreNumeric,
    decimal? ScoreMax,
    string? GradeLetter,
    string? GradeLabel,
    string? NarrativeFeedback,
    string EvaluationModel,
    string EvaluatedBy,
    DateOnly EvaluatedDate,
    string ResultStatus,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    DateOnly? PublishedDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
