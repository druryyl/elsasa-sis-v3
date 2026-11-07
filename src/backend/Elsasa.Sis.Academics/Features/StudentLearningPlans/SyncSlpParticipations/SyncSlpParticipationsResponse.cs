namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.SyncSlpParticipations;

/// <summary>
/// Response payload returned after synchronizing constituent units of an active student learning plan into academic participations (UC-ACD-10, P4-S10).
/// </summary>
/// <param name="PlanId">The unique identifier of the synchronized student learning plan.</param>
/// <param name="StudentId">The unique identifier of the student.</param>
/// <param name="SyncedCount">The number of new academic participation records inserted.</param>
/// <param name="UpdatedCount">The number of existing academic participation records reconciled with the plan.</param>
/// <param name="TotalUnits">The total number of required constituent learning units on the active plan.</param>
public sealed record SyncSlpParticipationsResponse(
    Guid PlanId,
    Guid StudentId,
    int SyncedCount,
    int UpdatedCount,
    int TotalUnits);
