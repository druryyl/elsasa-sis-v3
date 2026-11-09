namespace Elsasa.Sis.Academics.Features.AcademicResults.EvaluateStudentLearning;

/// <summary>
/// Request payload for recording a student or applicant evaluation result on an assessment (UC-ACD-06, P4-S08).
/// </summary>
/// <param name="StudentId">The unique identifier of the enrolled student (mutually exclusive with ApplicantId).</param>
/// <param name="ApplicantId">The unique identifier of the admissions applicant (mutually exclusive with StudentId).</param>
/// <param name="ScoreNumeric">The raw numeric score achieved, if applicable.</param>
/// <param name="ScoreMax">Optional override for maximum possible score, if different from assessment specification.</param>
/// <param name="GradeLetter">The letter grade achieved (e.g. 'A', 'B+'), if applicable.</param>
/// <param name="GradeLabel">The descriptor or rubric label (e.g. 'Kompeten', 'Sangat Mahir'), if applicable.</param>
/// <param name="NarrativeFeedback">Qualitative formative feedback or remarks.</param>
/// <param name="EvaluationModel">The evaluation model used ('numeric', 'letter_grade', 'pass_fail', 'competency', 'rubric', 'narrative', 'completion').</param>
/// <param name="PublishImmediately">When true on a formative assessment, teacher autonomously publishes feedback immediately.</param>
/// <param name="Notes">Internal teacher or administrative notes.</param>
public sealed record EvaluateStudentLearningRequest(
    Guid? StudentId,
    Guid? ApplicantId,
    decimal? ScoreNumeric,
    decimal? ScoreMax,
    string? GradeLetter,
    string? GradeLabel,
    string? NarrativeFeedback,
    string EvaluationModel,
    bool? PublishImmediately,
    string? Notes);
