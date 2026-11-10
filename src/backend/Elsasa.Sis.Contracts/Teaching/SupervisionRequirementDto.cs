namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a mentoring or supervision requirement attached to a conditional teaching authorization.
/// </summary>
/// <param name="Id">The unique identifier of the supervision requirement.</param>
/// <param name="AuthorizationId">The unique identifier of the parent teaching authorization.</param>
/// <param name="SupervisingTeacherId">Optional unique identifier of the designated supervising mentor teacher.</param>
/// <param name="SupervisingTeacherName">Optional display name of the designated supervising mentor teacher.</param>
/// <param name="SupervisionType">The type of supervision (co_teaching, periodic_observation, lesson_plan_review, general_mentorship).</param>
/// <param name="Description">Detailed description of supervision terms and focus areas.</param>
/// <param name="StartDate">The effective start date of the supervision requirement.</param>
/// <param name="EndDate">Optional concluding date for the supervision condition.</param>
/// <param name="Status">The supervision lifecycle status (active, completed, waived, escalated).</param>
/// <param name="ResolutionNotes">Optional notes recorded upon completion, waiver, or escalation.</param>
public sealed record SupervisionRequirementDto(
    Guid Id,
    Guid AuthorizationId,
    Guid? SupervisingTeacherId,
    string? SupervisingTeacherName,
    string SupervisionType,
    string Description,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Status,
    string? ResolutionNotes);
