namespace Elsasa.Sis.Teaching.Features.TeachingCapabilities.GetCapabilityTypes;

/// <summary>
/// Strongly-typed DTO representing an active capability taxonomy type (P2-S08).
/// </summary>
/// <param name="Id">The unique identifier of the capability type.</param>
/// <param name="Code">The institutional code (e.g. SUBJECT, STAGE, DELIVERY_MODE, LANGUAGE, SPECIALIST).</param>
/// <param name="Name">The display name of the taxonomy category.</param>
/// <param name="Description">Optional description of the taxonomy category.</param>
/// <param name="IsActive">Indicates whether this category is currently active.</param>
public sealed record CapabilityTypeDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive);
