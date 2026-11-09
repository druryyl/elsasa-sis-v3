namespace Elsasa.Sis.Academics.Features.AcademicResults;

/// <summary>
/// Detailed academic result DTO including candidate identity, attempt lineage, rubric scores, and lifecycle state (P4-S10, SCR-ACD-02).
/// </summary>
/// <param name="Id">The unique identifier of the academic result.</param>
/// <param name="AssessmentId">The unique identifier of the parent assessment.</param>
/// <param name="StudentId">The student identifier, if evaluated for an enrolled student.</param>
/// <param name="ApplicantId">The applicant identifier, if evaluated for an admissions entrance candidate.</param>
/// <param name="StudentNumber">The student administrative registration number, if applicable.</param>
/// <param name="CandidateName">The candidate full name (student or applicant).</param>
/// <param name="AttemptNumber">The attempt sequence number (1 for initial attempt, 2+ for retakes).</param>
/// <param name="ReplacesResultId">The ID of the superseded attempt result, if applicable.</param>
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
public sealed record AcademicResultDetailDto(
    Guid Id,
    Guid AssessmentId,
    Guid? StudentId,
    Guid? ApplicantId,
    string? StudentNumber,
    string? CandidateName,
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
