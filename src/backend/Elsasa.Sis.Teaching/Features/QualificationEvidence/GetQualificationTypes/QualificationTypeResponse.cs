namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.GetQualificationTypes;

/// <summary>
/// Response record representing a recognized qualification category and renewal policy (TD-02, P3-S09).
/// </summary>
/// <param name="Id">The unique identifier of the qualification type.</param>
/// <param name="Code">The unique code of the qualification type.</param>
/// <param name="Name">The display name of the qualification type.</param>
/// <param name="Description">Optional description of the qualification type.</param>
/// <param name="RequiresRenewal">Whether this credential requires recurring renewal.</param>
/// <param name="DefaultValidityMonths">Default validity duration in months when applicable.</param>
/// <param name="IsActive">Whether the qualification type is active.</param>
public sealed record QualificationTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool RequiresRenewal,
    int? DefaultValidityMonths,
    bool IsActive);
