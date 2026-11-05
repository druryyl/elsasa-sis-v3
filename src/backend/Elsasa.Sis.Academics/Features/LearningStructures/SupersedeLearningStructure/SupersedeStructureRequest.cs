namespace Elsasa.Sis.Academics.Features.LearningStructures.SupersedeLearningStructure;

/// <summary>
/// Request payload to branch an active learning structure into a new draft version (P3-S07, GAP-003, TD-03).
/// </summary>
public sealed record SupersedeStructureRequest(
    string NewVersion,
    DateOnly EffectiveDate,
    Guid? AcademicPeriodId = null,
    string? AcademicPeriodLabel = null);
