namespace Elsasa.Sis.Teaching.Features.TeachingCapabilities.AddTeachingCapability;

/// <summary>
/// Response payload for AddTeachingCapability containing the capability ID (P2-S08).
/// </summary>
/// <param name="Id">The unique identifier of the created or reactivated teaching capability.</param>
public sealed record AddTeachingCapabilityResponse(Guid Id);
