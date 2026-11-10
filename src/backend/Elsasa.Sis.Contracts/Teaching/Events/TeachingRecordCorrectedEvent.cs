namespace Elsasa.Sis.Contracts.Teaching.Events;

using Elsasa.Sis.Contracts.Common;

/// <summary>
/// In-memory domain event published when an auditable record correction is applied to a finalized teaching record (ADR-003).
/// </summary>
/// <param name="CorrectionId">The unique identifier of the applied correction record.</param>
/// <param name="CorrectionNumber">The authoritative correction number (e.g., CORR-2026-0001).</param>
/// <param name="TargetTable">The database table where the correction was applied.</param>
/// <param name="TargetRecordId">The unique identifier of the target record that was corrected.</param>
/// <param name="FieldName">The name of the field that was corrected.</param>
/// <param name="ApprovedBy">The identity of the authority who approved and applied the correction.</param>
/// <param name="OccurredOn">The timestamp when this event occurred in UTC.</param>
public sealed record TeachingRecordCorrectedEvent(
    Guid CorrectionId,
    string CorrectionNumber,
    string TargetTable,
    Guid TargetRecordId,
    string FieldName,
    string ApprovedBy,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeachingRecordCorrectedEvent"/> record with current UTC timestamp.
    /// </summary>
    public TeachingRecordCorrectedEvent(
        Guid correctionId,
        string correctionNumber,
        string targetTable,
        Guid targetRecordId,
        string fieldName,
        string approvedBy)
        : this(correctionId, correctionNumber, targetTable, targetRecordId, fieldName, approvedBy, DateTimeOffset.UtcNow)
    {
    }
}
