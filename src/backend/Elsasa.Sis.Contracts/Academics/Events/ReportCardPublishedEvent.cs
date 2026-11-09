namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a certified report card is released to self-service portals (ADR-003, UC-ACD-13).
/// </summary>
/// <param name="ReportCardId">The unique identifier of the published report card.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="AcademicPeriodId">The unique identifier of the academic period.</param>
/// <param name="ReportType">The report card type ('mid_term' or 'end_of_term').</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record ReportCardPublishedEvent(
    Guid ReportCardId,
    Guid StudentId,
    Guid AcademicPeriodId,
    string ReportType,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReportCardPublishedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="reportCardId">The unique identifier of the published report card.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="reportType">The report card type ('mid_term' or 'end_of_term').</param>
    public ReportCardPublishedEvent(
        Guid reportCardId,
        Guid studentId,
        Guid academicPeriodId,
        string reportType)
        : this(reportCardId, studentId, academicPeriodId, reportType, DateTimeOffset.UtcNow)
    {
    }
}
