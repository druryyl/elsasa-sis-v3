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

    /// <summary>
    /// Retrieves the approved teaching assignment for a specific learning unit on an effective date.
    /// </summary>
    /// <param name="learningUnitId">The unique identifier of the academic learning unit.</param>
    /// <param name="effectiveDate">The date on which the assignment must be active and approved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the approved teaching assignment DTO if found; otherwise null.</returns>
    Task<Result<TeachingAssignmentDto?>> GetApprovedAssignmentForLearningUnitAsync(Guid learningUnitId, DateOnly effectiveDate, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all active teaching assignments for a teacher within an academic period.
    /// </summary>
    /// <param name="teacherId">The unique identifier of the teacher profile.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing a read-only list of active teaching assignment DTOs.</returns>
    Task<Result<IReadOnlyList<TeachingAssignmentDto>>> GetTeacherActiveAssignmentsAsync(Guid teacherId, Guid academicPeriodId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves declared teacher weekly availability and active temporary leave windows for an academic period.
    /// </summary>
    /// <param name="teacherId">The unique identifier of the teacher profile.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the teacher availability DTO if declared; otherwise null.</returns>
    Task<Result<TeacherAvailabilityDto?>> GetTeacherAvailabilityAsync(Guid teacherId, Guid academicPeriodId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a teacher is actively assigned to an approved scope (e.g. learning_unit or cohort_group) on an effective date.
    /// </summary>
    /// <param name="teacherId">The unique identifier of the teacher profile.</param>
    /// <param name="scopeType">The assignment scope category ('learning_unit', 'cohort_group', 'individual_student', 'extracurricular_group').</param>
    /// <param name="scopeReferenceId">The unique identifier of the scope entity.</param>
    /// <param name="effectiveDate">The date on which the assignment must be active and approved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing <c>true</c> if actively assigned; otherwise <c>false</c>.</returns>
    Task<Result<bool>> IsTeacherAssignedToScopeAsync(Guid teacherId, string scopeType, Guid scopeReferenceId, DateOnly effectiveDate, CancellationToken ct = default);
}

