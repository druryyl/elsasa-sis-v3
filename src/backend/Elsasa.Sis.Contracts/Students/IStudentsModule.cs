namespace Elsasa.Sis.Contracts.Students;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Synchronous cross-domain query contract for the Students domain (ADR-003).
/// </summary>
public interface IStudentsModule
{
    /// <summary>
    /// Checks whether a student has an active/completed enrollment.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing <see langword="true"/> if the student is enrolled; otherwise <see langword="false"/>.</returns>
    Task<Result<bool>> IsStudentEnrolledAsync(Guid studentId, CancellationToken ct = default);
}
