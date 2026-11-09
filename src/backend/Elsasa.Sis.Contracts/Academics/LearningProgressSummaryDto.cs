namespace Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Strongly-typed contract DTO representing aggregated learning progress counts for a student within an academic period (ADR-001, UC-ACD-11).
/// </summary>
/// <param name="Status">The categorical progress status (e.g., 'not_started', 'in_progress', 'completed', 'needs_remediation', 'exempted').</param>
/// <param name="UnitCount">The number of learning units in this progress status.</param>
public sealed record LearningProgressSummaryDto(
    string Status,
    int UnitCount)
{
    /// <summary>
    /// Gets the count of learning units in this status (alias for <see cref="UnitCount"/>).
    /// </summary>
    public int Count => UnitCount;
}
