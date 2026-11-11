namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing an institutional workload policy defining measurement units,
/// capacity limits, warning thresholds, and countable effort inclusions.
/// </summary>
/// <param name="Id">The unique identifier of the workload policy.</param>
/// <param name="Code">The unique policy code (e.g. 'WPOL-2026-PRIMARY').</param>
/// <param name="Name">The descriptive name of the policy.</param>
/// <param name="AcademicYearId">The academic year this policy governs.</param>
/// <param name="Version">The version identifier of the policy.</param>
/// <param name="WorkloadUnit">Measurement unit for workload (e.g. 'Teaching Hours').</param>
/// <param name="MaxStandardCapacity">Maximum standard weekly workload before warning.</param>
/// <param name="WarningThreshold">Warning threshold where allocations approach limit.</param>
/// <param name="ProhibitedThreshold">Prohibited ceiling requiring formal authority exception approval.</param>
/// <param name="IncludesAssessmentEffort">Indicates whether grading effort counts toward workload.</param>
/// <param name="IncludesAdvisingEffort">Indicates whether student/homeroom advising effort counts toward workload.</param>
/// <param name="IncludesPreparationTime">Indicates whether lesson preparation counts toward workload.</param>
/// <param name="CalculationRulesSummary">Summary description of calculation formulas and rules.</param>
/// <param name="EffectiveStartDate">The date this policy becomes effective.</param>
/// <param name="EffectiveEndDate">Optional end date for policy validity.</param>
/// <param name="Status">Policy status ('draft', 'active', 'superseded', 'archived').</param>
/// <param name="ApprovedBy">Authority who approved this policy.</param>
/// <param name="ApprovedDate">Date this policy was approved.</param>
/// <param name="CreatedAt">Timestamp when this policy record was created in UTC.</param>
public sealed record WorkloadPolicyDto(
    Guid Id,
    string Code,
    string Name,
    Guid AcademicYearId,
    string Version,
    string WorkloadUnit,
    decimal MaxStandardCapacity,
    decimal WarningThreshold,
    decimal ProhibitedThreshold,
    bool IncludesAssessmentEffort,
    bool IncludesAdvisingEffort,
    bool IncludesPreparationTime,
    string CalculationRulesSummary,
    DateOnly EffectiveStartDate,
    DateOnly? EffectiveEndDate,
    string Status,
    string ApprovedBy,
    DateOnly ApprovedDate,
    DateTimeOffset CreatedAt);
