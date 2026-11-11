namespace Elsasa.Sis.Teaching.Features.Workload.CreateUnavailabilityWindow;

/// <summary>
/// Request contract for recording a temporary leave, official training, illness, or blackout window (TD-04, P3-S08).
/// </summary>
/// <param name="StartDate">Start date of the leave/blackout window.</param>
/// <param name="EndDate">End date of the leave/blackout window.</param>
/// <param name="UnavailabilityType">Leave category ('medical_leave', 'official_training', 'personal_leave', 'blackout').</param>
/// <param name="Reason">Detailed justification or explanation for the unavailability.</param>
/// <param name="StartTime">Optional daily start time for partial day restrictions.</param>
/// <param name="EndTime">Optional daily end time for partial day restrictions.</param>
/// <param name="ExternalSourceReference">Optional HR leave application or external reference.</param>
/// <param name="EmergencyCoverageRequired">Flag indicating whether emergency coverage/substitution is required.</param>
/// <param name="Notes">Optional coordinator notes.</param>
public sealed record CreateUnavailabilityWindowRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    string UnavailabilityType,
    string Reason,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    string? ExternalSourceReference = null,
    bool EmergencyCoverageRequired = false,
    string? Notes = null);
