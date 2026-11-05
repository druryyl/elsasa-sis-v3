namespace Elsasa.Sis.IntegrationTests.Fixtures;

using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Shared.Common;

/// <summary>
/// Controllable test double for <see cref="IStudentsModule"/> in integration tests (ADR-003, P6-S16).
/// Allows integration tests to simulate enrolled and unenrolled student states for synchronous enrollment guards.
/// </summary>
public sealed class TestStudentsModule : IStudentsModule
{
    private readonly HashSet<Guid> _enrolledStudents = [];
    private readonly HashSet<Guid> _unenrolledStudents = [];
    private bool _alwaysEnrolled = true;

    /// <summary>
    /// Explicitly marks a student as actively enrolled.
    /// </summary>
    public void MarkStudentEnrolled(Guid studentId)
    {
        _enrolledStudents.Add(studentId);
        _unenrolledStudents.Remove(studentId);
    }

    /// <summary>
    /// Explicitly marks a student as not enrolled.
    /// </summary>
    public void MarkStudentUnenrolled(Guid studentId)
    {
        _unenrolledStudents.Add(studentId);
        _enrolledStudents.Remove(studentId);
    }

    /// <summary>
    /// Controls whether unknown students default to enrolled (<see langword="true"/>) or not enrolled (<see langword="false"/>).
    /// </summary>
    public void SetAlwaysEnrolled(bool alwaysEnrolled)
    {
        _alwaysEnrolled = alwaysEnrolled;
    }

    /// <summary>
    /// Resets the stub state to default behavior (all enrolled).
    /// </summary>
    public void Reset()
    {
        _enrolledStudents.Clear();
        _unenrolledStudents.Clear();
        _alwaysEnrolled = true;
    }

    /// <inheritdoc />
    public Task<Result<bool>> IsStudentEnrolledAsync(Guid studentId, CancellationToken ct = default)
    {
        if (_unenrolledStudents.Contains(studentId))
        {
            return Task.FromResult(Result.Success(false));
        }

        if (_alwaysEnrolled || _enrolledStudents.Contains(studentId))
        {
            return Task.FromResult(Result.Success(true));
        }

        return Task.FromResult(Result.Success(false));
    }
}
