namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a potential teaching capability of a teacher profile.
/// A capability describes pedagogical suitability only; it does not commit capacity or authorize teaching.
/// </summary>
/// <param name="Id">The unique identifier of the teaching capability.</param>
/// <param name="TeacherProfileId">The unique identifier of the owning teacher profile.</param>
/// <param name="CapabilityTypeId">The unique identifier of the capability taxonomy type.</param>
/// <param name="CapabilityCode">The specific capability identifier code (e.g., MATH_PRIMARY, ENG_JUNIOR).</param>
/// <param name="CapabilityName">The display name of the capability.</param>
/// <param name="ApplicableLevel">Optional applicable educational stage or level (e.g., Primary, Lower Secondary).</param>
/// <param name="DeliveryMode">Optional delivery mode (e.g., in_person, online, hybrid).</param>
/// <param name="LanguageOfInstruction">Optional language of instruction (e.g., en, id).</param>
/// <param name="Status">The capability status (active or inactive).</param>
/// <param name="Notes">Optional pedagogical notes or context.</param>
public sealed record TeachingCapabilityDto(
    Guid Id,
    Guid TeacherProfileId,
    Guid CapabilityTypeId,
    string CapabilityCode,
    string CapabilityName,
    string? ApplicableLevel,
    string? DeliveryMode,
    string? LanguageOfInstruction,
    string Status,
    string? Notes);
