namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a concise summary of a teacher profile for directories, cross-domain inquiries, and work queues.
/// </summary>
/// <param name="Id">The unique identifier of the teacher profile.</param>
/// <param name="TeacherNumber">The authoritative institutional teacher number.</param>
/// <param name="FullName">The legal full name of the teacher.</param>
/// <param name="DisplayName">Optional professional display name with credentials.</param>
/// <param name="TeachingRoleId">The unique identifier of the assigned teaching role.</param>
/// <param name="TeachingRoleName">The display name of the assigned teaching role.</param>
/// <param name="CampusAffiliation">The campus affiliation ('All' or specific campus).</param>
/// <param name="Department">Optional department affiliation.</param>
/// <param name="Standing">The teacher standing state (pending_approval, active, restricted, suspended, inactive).</param>
/// <param name="Email">Optional institutional contact email.</param>
/// <param name="Phone">Optional contact phone number.</param>
/// <param name="HasUnverifiedWarning">Indicates whether any active authorization has unverified credential warnings.</param>
/// <param name="ActiveAuthorizationsCount">The count of active authorizations currently held by the teacher.</param>
public sealed record TeacherSummaryDto(
    Guid Id,
    string TeacherNumber,
    string FullName,
    string? DisplayName,
    Guid TeachingRoleId,
    string TeachingRoleName,
    string CampusAffiliation,
    string? Department,
    string Standing,
    string? Email = null,
    string? Phone = null,
    bool HasUnverifiedWarning = false,
    int ActiveAuthorizationsCount = 0);
