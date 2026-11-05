namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing an active curriculum framework / learning structure.
/// </summary>
/// <param name="Id">The unique identifier of the learning structure.</param>
/// <param name="Name">The display name of the learning structure.</param>
/// <param name="AcademicPeriodId">The unique identifier of the associated operational period.</param>
/// <param name="AcademicPeriodLabel">The display label or code of the associated operational period.</param>
/// <param name="Version">The semantic version identifier (e.g., '2024.1').</param>
/// <param name="EffectiveDate">The date when this structure became effective.</param>
/// <param name="Status">The governance lifecycle status (e.g., 'active').</param>
/// <param name="ApprovedBy">The user identifier who approved this structure.</param>
/// <param name="ApprovedDate">The date when this structure was approved.</param>
public sealed record ActiveLearningStructureDto(
    Guid Id,
    string Name,
    Guid AcademicPeriodId,
    string AcademicPeriodLabel,
    string Version,
    DateOnly EffectiveDate,
    string Status,
    string? ApprovedBy,
    DateOnly? ApprovedDate);
