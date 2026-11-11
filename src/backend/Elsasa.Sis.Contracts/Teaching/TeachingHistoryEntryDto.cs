namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing an entry in the immutable append-only teaching history audit log.
/// </summary>
/// <param name="Id">The unique identifier of the history log entry.</param>
/// <param name="TeacherProfileId">The teacher profile whose history is tracked.</param>
/// <param name="EventType">Type of teaching event (e.g. 'AssignmentApproved', 'WorkloadExceptionApproved').</param>
/// <param name="EntityType">Target entity table or concept (e.g. 'teaching_assignments', 'workload_commitments').</param>
/// <param name="EntityId">Identifier of the affected entity.</param>
/// <param name="EffectiveDate">Business date when the action took effect.</param>
/// <param name="EventTimestamp">Timestamp when this audit event was stamped in UTC.</param>
/// <param name="ActorRole">Role of the acting user (e.g. 'Teaching Coordinator', 'Teaching Authority').</param>
/// <param name="ActorId">Identity of the acting user.</param>
/// <param name="EventSummary">Concise human-readable description of what transpired.</param>
/// <param name="ChangePayload">Optional JSON serialized snapshot of change delta.</param>
/// <param name="Notes">Optional contextual commentary.</param>
/// <param name="CreatedAt">Timestamp when this log record was persisted in UTC.</param>
public sealed record TeachingHistoryEntryDto(
    Guid Id,
    Guid TeacherProfileId,
    string EventType,
    string EntityType,
    Guid EntityId,
    DateOnly EffectiveDate,
    DateTimeOffset EventTimestamp,
    string ActorRole,
    string ActorId,
    string EventSummary,
    string? ChangePayload,
    string? Notes,
    DateTimeOffset CreatedAt);
