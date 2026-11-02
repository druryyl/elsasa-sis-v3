namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ApplyAcademicYearPreset;

/// <summary>
/// Response payload returned after applying academic year presets (ADR-005).
/// </summary>
/// <param name="AcademicYearId">The parent academic year identifier.</param>
/// <param name="CreatedPeriods">The generated operational periods.</param>
public sealed record ApplyAcademicYearPresetResponse(
    Guid AcademicYearId,
    IReadOnlyList<OperationalPeriodPresetItemResponse> CreatedPeriods);

/// <summary>
/// Summary response of an individual operational period generated via preset.
/// </summary>
public sealed record OperationalPeriodPresetItemResponse(
    Guid Id,
    Guid AcademicYearId,
    string PeriodCode,
    string Name,
    string PeriodType,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string? Notes);
