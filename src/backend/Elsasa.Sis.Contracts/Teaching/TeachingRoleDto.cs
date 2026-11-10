namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a recognized teaching role.
/// </summary>
/// <param name="Id">The unique identifier of the teaching role.</param>
/// <param name="Code">The institutional code (e.g., GURU_TETAP, GURU_KONTRAK, GURU_TAMU, RELAWAN).</param>
/// <param name="Name">The display name of the role.</param>
/// <param name="Description">Optional description of role responsibilities.</param>
/// <param name="RequiresQualificationCheck">Indicates whether this role mandates formal qualification verification.</param>
/// <param name="IsActive">Indicates whether this role is currently selectable.</param>
public sealed record TeachingRoleDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool RequiresQualificationCheck,
    bool IsActive);
