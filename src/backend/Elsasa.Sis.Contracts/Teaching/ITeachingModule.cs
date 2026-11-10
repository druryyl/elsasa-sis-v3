namespace Elsasa.Sis.Contracts.Teaching;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Synchronous cross-domain query contract for the Teaching domain (ADR-001, ADR-003, ADR-010).
/// Exposes public teacher profile and authorization inquiries for external domains (Academics, Operations).
/// </summary>
public interface ITeachingModule
{
    /// <summary>
    /// Retrieves a teacher's full profile dossier, including role, capabilities, qualifications,
    /// authorizations, and supervision requirements.
    /// </summary>
    /// <param name="teacherId">The unique identifier of the teacher profile.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the teacher profile DTO if found; otherwise null.</returns>
    Task<Result<TeacherProfileDto?>> GetTeacherProfileAsync(Guid teacherId, CancellationToken ct = default);

    /// <summary>
    /// Evaluates whether a teacher holds an active, valid teaching authorization for a given subject or capability,
    /// campus, and effective date.
    /// </summary>
    /// <param name="teacherId">The unique identifier of the teacher profile.</param>
    /// <param name="subjectOrCapability">The subject code/name or capability to check.</param>
    /// <param name="campus">The campus identifier or name.</param>
    /// <param name="effectiveDate">The date on which authorization must be valid.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing true if authorized; otherwise false.</returns>
    Task<Result<bool>> IsTeacherAuthorizedAsync(Guid teacherId, string subjectOrCapability, string campus, DateOnly effectiveDate, CancellationToken ct = default);

    /// <summary>
    /// Retrieves active teachers in good standing, optionally filtered by campus affiliation.
    /// </summary>
    /// <param name="campus">Optional campus filter; if null, teachers across all campuses are returned.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of active teacher summaries.</returns>
    Task<Result<IReadOnlyList<TeacherSummaryDto>>> GetActiveTeachersAsync(string? campus = null, CancellationToken ct = default);
}
