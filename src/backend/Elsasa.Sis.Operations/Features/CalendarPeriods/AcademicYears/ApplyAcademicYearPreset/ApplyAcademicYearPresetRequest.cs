namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ApplyAcademicYearPreset;

/// <summary>
/// Optional request payload for applying institutional period presets (ADR-005).
/// </summary>
/// <param name="PresetType">Identifier of the preset structure (e.g. "indonesian-standard-2-semester"). Defaults to standard.</param>
public sealed record ApplyAcademicYearPresetRequest(string? PresetType = null);
