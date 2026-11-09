namespace Elsasa.Sis.Contracts.Academics.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published post-commit when a transcript is officially issued and snapshot is frozen (ADR-003, UC-ACD-13).
/// </summary>
/// <param name="TranscriptId">The unique identifier of the issued transcript.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="TranscriptType">The transcript type ('official' or 'unofficial').</param>
/// <param name="Purpose">The declared issuance purpose, or null if unspecified.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record TranscriptIssuedEvent(
    Guid TranscriptId,
    Guid StudentId,
    string TranscriptType,
    string? Purpose,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranscriptIssuedEvent"/> record with current UTC timestamp.
    /// </summary>
    /// <param name="transcriptId">The unique identifier of the issued transcript.</param>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="transcriptType">The transcript type ('official' or 'unofficial').</param>
    /// <param name="purpose">The declared issuance purpose, or null if unspecified.</param>
    public TranscriptIssuedEvent(
        Guid transcriptId,
        Guid studentId,
        string transcriptType,
        string? purpose = null)
        : this(transcriptId, studentId, transcriptType, purpose, DateTimeOffset.UtcNow)
    {
    }
}
