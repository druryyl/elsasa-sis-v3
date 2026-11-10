namespace Elsasa.Sis.Teaching.Features.TeachingCapabilities.AddTeachingCapability;

/// <summary>
/// Request payload to record a potential teaching capability on a teacher profile (P2-S08).
/// </summary>
/// <param name="CapabilityTypeId">Taxonomy capability type ID.</param>
/// <param name="CapabilityCode">Unique capability code (e.g. MATH_PRIMARY).</param>
/// <param name="CapabilityName">Human-readable capability name.</param>
/// <param name="ApplicableLevel">Optional applicable educational stage/level (e.g. Primary, Lower Secondary).</param>
/// <param name="DeliveryMode">Optional delivery mode (e.g. in_person, online, hybrid).</param>
/// <param name="LanguageOfInstruction">Optional language of instruction (e.g. en, id).</param>
/// <param name="Notes">Optional pedagogical notes or context.</param>
public sealed record AddTeachingCapabilityRequest(
    Guid CapabilityTypeId,
    string CapabilityCode,
    string CapabilityName,
    string? ApplicableLevel = null,
    string? DeliveryMode = null,
    string? LanguageOfInstruction = null,
    string? Notes = null);
