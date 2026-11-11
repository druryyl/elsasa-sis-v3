namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a configured teaching assignment role (e.g. Guru Utama, Wali Kelas).
/// </summary>
/// <param name="Id">The unique identifier of the assignment role.</param>
/// <param name="Code">The unique role code (e.g. 'LEAD_TEACHER', 'HOMEROOM_ADVISOR').</param>
/// <param name="Name">The display name of the assignment role.</param>
/// <param name="Description">Optional detailed description of role responsibilities.</param>
/// <param name="IsLeadRole">Indicates whether this role carries lead instructional responsibility.</param>
/// <param name="CountsTowardWorkload">Indicates whether assignments in this role count toward weekly workload.</param>
/// <param name="IsActive">Indicates whether the role is active for new assignments.</param>
/// <param name="CreatedAt">The timestamp when this role was created in UTC.</param>
public sealed record AssignmentRoleDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsLeadRole,
    bool CountsTowardWorkload,
    bool IsActive,
    DateTimeOffset CreatedAt);
