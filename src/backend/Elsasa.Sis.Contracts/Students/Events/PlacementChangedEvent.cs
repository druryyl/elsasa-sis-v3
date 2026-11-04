namespace Elsasa.Sis.Contracts.Students.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when a student's administrative placement has been modified (ADR-003).
/// Triggers post-commit side-effects across operational domains.
/// </summary>
/// <param name="StudentId">The unique identifier of the placed student.</param>
/// <param name="PlacementId">The unique identifier of the new administrative placement record.</param>
/// <param name="Campus">The campus assigned to the student.</param>
/// <param name="SchoolLevel">The school level assigned to the student.</param>
/// <param name="Grade">The grade assigned to the student.</param>
/// <param name="AcademicYear">The academic year assigned to the student.</param>
/// <param name="EffectiveDate">The date when this placement change takes effect.</param>
/// <param name="Program">Optional academic program.</param>
/// <param name="Stream">Optional academic stream or track.</param>
/// <param name="Cohort">Optional cohort identifier or group code.</param>
/// <param name="Homeroom">Optional homeroom identifier or room code.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record PlacementChangedEvent(
    Guid StudentId,
    Guid PlacementId,
    string Campus,
    string SchoolLevel,
    string Grade,
    string AcademicYear,
    DateOnly EffectiveDate,
    string? Program,
    string? Stream,
    string? Cohort,
    string? Homeroom,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlacementChangedEvent"/> record defaulting <see cref="OccurredOn"/> to <see cref="DateTimeOffset.UtcNow"/>.
    /// </summary>
    /// <param name="studentId">The unique identifier of the placed student.</param>
    /// <param name="placementId">The unique identifier of the new administrative placement record.</param>
    /// <param name="campus">The campus assigned to the student.</param>
    /// <param name="schoolLevel">The school level assigned to the student.</param>
    /// <param name="grade">The grade assigned to the student.</param>
    /// <param name="academicYear">The academic year assigned to the student.</param>
    /// <param name="effectiveDate">The date when this placement change takes effect.</param>
    /// <param name="program">Optional academic program.</param>
    /// <param name="stream">Optional academic stream or track.</param>
    /// <param name="cohort">Optional cohort identifier or group code.</param>
    /// <param name="homeroom">Optional homeroom identifier or room code.</param>
    public PlacementChangedEvent(
        Guid studentId,
        Guid placementId,
        string campus,
        string schoolLevel,
        string grade,
        string academicYear,
        DateOnly effectiveDate,
        string? program = null,
        string? stream = null,
        string? cohort = null,
        string? homeroom = null)
        : this(
            studentId,
            placementId,
            campus,
            schoolLevel,
            grade,
            academicYear,
            effectiveDate,
            program,
            stream,
            cohort,
            homeroom,
            DateTimeOffset.UtcNow)
    {
    }
}
