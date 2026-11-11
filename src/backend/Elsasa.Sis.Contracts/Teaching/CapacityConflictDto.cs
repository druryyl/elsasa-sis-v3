namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a capacity, schedule, or authorization conflict condition.
/// </summary>
/// <param name="Id">The unique identifier of the capacity conflict.</param>
/// <param name="TeachingAssignmentId">Optional assignment related to this conflict.</param>
/// <param name="TeachingDemandId">Optional demand related to this conflict.</param>
/// <param name="TeacherProfileId">The teacher profile experiencing the conflict.</param>
/// <param name="ConflictType">Type of conflict (e.g. 'workload_ceiling_breach', 'authorization_missing', 'schedule_overlap').</param>
/// <param name="ConflictClassification">Classification category (e.g. 'workload', 'schedule', 'qualification').</param>
/// <param name="Severity">Conflict severity ('advisory', 'blocking').</param>
/// <param name="Description">Detailed description of conflict cause and impact.</param>
/// <param name="DetectedDate">Date when the conflict was detected.</param>
/// <param name="Status">Conflict resolution status ('unresolved', 'resolved', 'waived_with_exception').</param>
/// <param name="ResolutionAction">Optional action taken to resolve (e.g. 'workload_exception_granted', 'assignment_cancelled').</param>
/// <param name="ResolvedBy">Optional identity of coordinator or authority who resolved the conflict.</param>
/// <param name="ResolvedDate">Optional date when resolved.</param>
/// <param name="ResolutionNotes">Optional resolution notes.</param>
/// <param name="CreatedAt">Timestamp when conflict record was created in UTC.</param>
public sealed record CapacityConflictDto(
    Guid Id,
    Guid? TeachingAssignmentId,
    Guid? TeachingDemandId,
    Guid TeacherProfileId,
    string ConflictType,
    string ConflictClassification,
    string Severity,
    string Description,
    DateOnly DetectedDate,
    string Status,
    string? ResolutionAction,
    string? ResolvedBy,
    DateOnly? ResolvedDate,
    string? ResolutionNotes,
    DateTimeOffset CreatedAt);
