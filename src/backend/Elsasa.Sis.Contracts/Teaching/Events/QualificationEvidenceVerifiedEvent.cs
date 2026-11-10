namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when qualification documentary evidence has been authenticated and verified (ADR-003).
/// </summary>
/// <param name="EvidenceId">The unique identifier of the qualification evidence record.</param>
/// <param name="TeacherProfileId">The unique identifier of the owning teacher profile.</param>
/// <param name="Title">The title of the verified credential.</param>
/// <param name="VerificationState">The resulting verification state ('verified').</param>
/// <param name="VerifiedBy">The user identity who verified the credential.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record QualificationEvidenceVerifiedEvent(
    Guid EvidenceId,
    Guid TeacherProfileId,
    string Title,
    string VerificationState,
    string VerifiedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationEvidenceVerifiedEvent"/> record with current UTC timestamp.
    /// </summary>
    public QualificationEvidenceVerifiedEvent(
        Guid evidenceId,
        Guid teacherProfileId,
        string title,
        string verificationState,
        string verifiedBy)
        : this(evidenceId, teacherProfileId, title, verificationState, verifiedBy, DateTimeOffset.UtcNow)
    {
    }
}
