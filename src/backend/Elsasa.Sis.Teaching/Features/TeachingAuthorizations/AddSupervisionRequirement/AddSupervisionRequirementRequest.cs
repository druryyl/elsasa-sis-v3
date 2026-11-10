namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.AddSupervisionRequirement;

/// <summary>
/// Request payload to attach a supervision requirement to a teaching authorization (TD-05, P4-S13).
/// </summary>
/// <param name="SupervisionType">The type of supervision ('co_teaching', 'periodic_observation', 'lesson_plan_review', 'general_mentorship').</param>
/// <param name="Description">Detailed description of supervision terms, focus areas, and required evidence.</param>
/// <param name="StartDate">The start date of the supervision requirement.</param>
/// <param name="EndDate">Optional scheduled completion or evaluation date.</param>
/// <param name="SupervisingTeacherId">Optional unique identifier of the designated supervising mentor teacher.</param>
public sealed record AddSupervisionRequirementRequest(
    string SupervisionType,
    string Description,
    DateOnly StartDate,
    DateOnly? EndDate = null,
    Guid? SupervisingTeacherId = null);
