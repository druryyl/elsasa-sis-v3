namespace Elsasa.Sis.Contracts.Students.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when an admission application has been formally accepted (ADR-003).
/// </summary>
/// <param name="ApplicantId">The unique identifier of the accepted applicant.</param>
/// <param name="ApplicationId">The unique identifier of the admission application.</param>
/// <param name="AcademicYearId">The target academic year identifier for admission.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record ApplicantAcceptedEvent(
    Guid ApplicantId,
    Guid ApplicationId,
    Guid AcademicYearId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicantAcceptedEvent"/> record with the current UTC timestamp.
    /// </summary>
    /// <param name="applicantId">The unique identifier of the accepted applicant.</param>
    /// <param name="applicationId">The unique identifier of the admission application.</param>
    /// <param name="academicYearId">The target academic year identifier for admission.</param>
    public ApplicantAcceptedEvent(Guid applicantId, Guid applicationId, Guid academicYearId)
        : this(applicantId, applicationId, academicYearId, DateTimeOffset.UtcNow)
    {
    }
}
