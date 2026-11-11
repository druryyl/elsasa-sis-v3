namespace Elsasa.Sis.Teaching.Features.Workload.DeclareTeacherAvailability;

/// <summary>
/// Request contract for declaring recurring weekly teacher availability (TD-04, P3-S08).
/// </summary>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="MaxAvailableHours">Declared maximum available teaching hours per week.</param>
/// <param name="EffectiveStartDate">Effective start date of this availability declaration.</param>
/// <param name="EffectiveEndDate">Effective end date of this availability declaration.</param>
/// <param name="CampusScope">Campus scope (e.g. 'All' or specific campus).</param>
/// <param name="AvailableDeliveryModes">Supported delivery modes ('All', 'in_person', 'online', 'hybrid').</param>
/// <param name="RecurringAvailableSchedule">Serialized JSON representation of weekly recurring blocks (Monday-Friday morning/afternoon).</param>
/// <param name="Notes">Optional teacher preferences, constraints, or remarks.</param>
public sealed record DeclareTeacherAvailabilityRequest(
    Guid AcademicPeriodId,
    decimal MaxAvailableHours,
    DateOnly EffectiveStartDate,
    DateOnly EffectiveEndDate,
    string? CampusScope = null,
    string? AvailableDeliveryModes = null,
    string? RecurringAvailableSchedule = null,
    string? Notes = null);
