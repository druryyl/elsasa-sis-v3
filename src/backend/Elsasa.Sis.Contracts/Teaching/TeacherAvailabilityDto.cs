namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing a teacher's declared baseline weekly capacity,
/// preferred delivery modes, and recurring schedule slots for an academic period.
/// </summary>
/// <param name="Id">The unique identifier of the availability record.</param>
/// <param name="TeacherProfileId">The declaring teacher profile identifier.</param>
/// <param name="AcademicPeriodId">Target academic period identifier.</param>
/// <param name="MaxAvailableHours">Declared maximum available teaching hours per week.</param>
/// <param name="CampusScope">Campus availability scope ('All' or specific campus).</param>
/// <param name="AvailableDeliveryModes">Supported delivery modes ('All', 'in_person', 'online', 'hybrid').</param>
/// <param name="RecurringAvailableSchedule">Serialized JSON representation of weekly recurring time blocks (e.g. Mon-Fri morning/afternoon).</param>
/// <param name="EffectiveStartDate">The date from which this availability declaration takes effect.</param>
/// <param name="EffectiveEndDate">The date until which this availability declaration remains effective.</param>
/// <param name="Status">Lifecycle status ('active', 'revoked', 'superseded').</param>
/// <param name="Notes">Optional teacher preferences or constraint notes.</param>
/// <param name="CreatedAt">Timestamp when this record was created in UTC.</param>
/// <param name="ActiveUnavailabilityWindows">Optional list of temporary leave/blackout windows within this period.</param>
public sealed record TeacherAvailabilityDto(
    Guid Id,
    Guid TeacherProfileId,
    Guid AcademicPeriodId,
    decimal MaxAvailableHours,
    string CampusScope,
    string AvailableDeliveryModes,
    string? RecurringAvailableSchedule,
    DateOnly EffectiveStartDate,
    DateOnly EffectiveEndDate,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TeacherUnavailabilityWindowDto>? ActiveUnavailabilityWindows = null);
