namespace Elsasa.Sis.Contracts.DemoData;

/// <summary>
/// Result summary of seeding Stage 2 Teaching domain assignments, availability, workload, and substitutions.
/// </summary>
/// <param name="DemandsCreated">The count of teaching demands seeded.</param>
/// <param name="EvaluationsCreated">The count of candidate eligibility evaluations seeded.</param>
/// <param name="AssignmentsCreated">The count of teaching assignments seeded.</param>
/// <param name="ConflictsCreated">The count of capacity conflicts seeded.</param>
/// <param name="AvailabilitiesCreated">The count of teacher weekly availability declarations seeded.</param>
/// <param name="UnavailabilityWindowsCreated">The count of temporary leave / blackout windows seeded.</param>
/// <param name="WorkloadCommitmentsCreated">The count of workload commitment snapshots seeded.</param>
/// <param name="WorkloadExceptionsCreated">The count of workload exceptions seeded.</param>
/// <param name="SubstitutionsCreated">The count of emergency substitutions seeded.</param>
/// <param name="ReassignmentsCreated">The count of permanent reassignments seeded.</param>
/// <param name="CompletionsCreated">The count of assignment completions seeded.</param>
/// <param name="HistoryEntriesCreated">The count of teaching history log entries seeded.</param>
public sealed record TeachingAssignmentsSeedResult(
    int DemandsCreated,
    int EvaluationsCreated,
    int AssignmentsCreated,
    int ConflictsCreated,
    int AvailabilitiesCreated,
    int UnavailabilityWindowsCreated,
    int WorkloadCommitmentsCreated,
    int WorkloadExceptionsCreated,
    int SubstitutionsCreated,
    int ReassignmentsCreated,
    int CompletionsCreated,
    int HistoryEntriesCreated);
