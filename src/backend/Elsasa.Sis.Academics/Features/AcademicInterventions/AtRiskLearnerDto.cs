namespace Elsasa.Sis.Academics.Features.AcademicInterventions;

/// <summary>
/// Data transfer object representing an at-risk learner dynamically evaluated by the On-Demand At-Risk Query Engine (UC-ACD-11, P4-S10, TD-04).
/// Provides pre-populated trigger context for 1-click intervention formulation.
/// </summary>
/// <param name="StudentId">The unique identifier of the at-risk student.</param>
/// <param name="StudentNumber">The student administrative ID number.</param>
/// <param name="StudentName">The full legal name of the student.</param>
/// <param name="GradeLevel">The grade level or cohort label.</param>
/// <param name="LearningUnitId">The unique identifier of the target learning unit under risk.</param>
/// <param name="LearningUnitCode">The curriculum unit code.</param>
/// <param name="LearningUnitName">The curriculum unit display name.</param>
/// <param name="AcademicPeriodId">The academic period identifier.</param>
/// <param name="SectionLabel">The cohort section label (e.g. '4A').</param>
/// <param name="TriggerType">The primary detected trigger type for 1-click intervention pre-population.</param>
/// <param name="TriggerDescription">The contextual summary describing the observed deficiency or threshold breach.</param>
/// <param name="DetectedTriggers">All detected risk indicators for this student and learning unit.</param>
/// <param name="HasAssessmentBelowThreshold">Indicates whether the operative score is below the passing score / KKM.</param>
/// <param name="HasMissingWork">Indicates whether overdue assignments exist without student submissions.</param>
/// <param name="HasAttendanceConcern">Indicates whether presence attendance rate is below 75%.</param>
/// <param name="HasStandingConcern">Indicates whether academic standing is conditional or not in good standing.</param>
/// <param name="OperativeScore">The lowest or operative score on assessments below threshold, if applicable.</param>
/// <param name="PassingScore">The passing threshold score (KKM) configured for the unit, if applicable.</param>
/// <param name="MissingAssignmentsCount">The number of overdue assignments missing submissions.</param>
/// <param name="AttendanceRate">The calculated attendance presence percentage (0-100), if sessions exist.</param>
/// <param name="CurrentStanding">The determined academic standing in the active period, if evaluated.</param>
/// <param name="ActiveInterventionsCount">The count of active intervention plans ('planned' or 'in_progress') already initiated for this student and unit.</param>
public sealed record AtRiskLearnerDto(
    Guid StudentId,
    string? StudentNumber,
    string? StudentName,
    string? GradeLevel,
    Guid LearningUnitId,
    string? LearningUnitCode,
    string? LearningUnitName,
    Guid AcademicPeriodId,
    string? SectionLabel,
    string TriggerType,
    string TriggerDescription,
    IReadOnlyList<string> DetectedTriggers,
    bool HasAssessmentBelowThreshold,
    bool HasMissingWork,
    bool HasAttendanceConcern,
    bool HasStandingConcern,
    decimal? OperativeScore,
    decimal? PassingScore,
    int MissingAssignmentsCount,
    decimal? AttendanceRate,
    string? CurrentStanding,
    int ActiveInterventionsCount);
