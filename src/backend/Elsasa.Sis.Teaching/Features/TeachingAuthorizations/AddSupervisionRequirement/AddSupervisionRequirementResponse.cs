namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.AddSupervisionRequirement;

/// <summary>
/// Response payload returned after successfully attaching a supervision requirement (P4-S13).
/// </summary>
/// <param name="Id">The unique identifier of the created supervision requirement.</param>
/// <param name="AuthorizationId">The unique identifier of the parent teaching authorization.</param>
/// <param name="SupervisingTeacherId">Optional unique identifier of the designated supervising mentor teacher.</param>
/// <param name="SupervisionType">The type of supervision assigned.</param>
/// <param name="Description">Detailed description of supervision terms and focus areas.</param>
/// <param name="StartDate">The start date of the supervision requirement.</param>
/// <param name="EndDate">Optional scheduled completion or evaluation date.</param>
/// <param name="Status">The initial status of the supervision requirement ('active').</param>
/// <param name="CreatedAt">The UTC timestamp when the requirement was created.</param>
public sealed record AddSupervisionRequirementResponse(
    Guid Id,
    Guid AuthorizationId,
    Guid? SupervisingTeacherId,
    string SupervisionType,
    string Description,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Status,
    DateTimeOffset CreatedAt);
