namespace Elsasa.Sis.Students.Features.Enrollment.Services;

/// <summary>
/// Domain service interface for generating authoritative, sequential, gap-free student numbers (ADR-004, TD-02).
/// </summary>
public interface IStudentNumberGenerator
{
    /// <summary>
    /// Generates the next sequential student number formatted as STD-{EnrollmentYear}-{Sequence:D4}.
    /// </summary>
    /// <param name="enrollmentYear">The calendar or academic year of enrollment (e.g. 2026).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An authoritative formatted student number string.</returns>
    Task<string> GenerateNextStudentNumberAsync(int enrollmentYear, CancellationToken ct = default);
}
