namespace Elsasa.Sis.Contracts.Teaching;

/// <summary>
/// Strongly-typed contract DTO representing an auditable record correction on a finalized teaching record.
/// Enforces "No Silent Overwrites" with mandatory before/after delta tracking and justification logging.
/// </summary>
/// <param name="Id">The unique identifier of the correction record.</param>
/// <param name="CorrectionNumber">The unique correction identifier (e.g., CORR-2026-0001).</param>
/// <param name="TargetTableName">The database table where the correction applies.</param>
/// <param name="TargetRecordId">The unique identifier of the target record.</param>
/// <param name="TeacherProfileId">Optional associated teacher profile identifier.</param>
/// <param name="FieldName">The name of the modified field.</param>
/// <param name="OriginalValue">The value before correction.</param>
/// <param name="CorrectedValue">The new authoritative corrected value.</param>
/// <param name="ReasonForCorrection">Mandatory audit justification for the correction.</param>
/// <param name="SupportingEvidence">Optional documentary evidence or reference supporting the correction.</param>
/// <param name="DownstreamImpactReviewed">Indicates whether downstream impact was reviewed.</param>
/// <param name="EffectiveDateImplications">Optional explanation of retroactive/effective date impact.</param>
/// <param name="RequestedBy">The user who requested or submitted the correction.</param>
/// <param name="RequestedDate">The date when the correction was requested.</param>
/// <param name="ApprovedBy">Optional identity of the Teaching Authority who approved/applied the correction.</param>
/// <param name="ApprovedDate">Optional date when the correction was approved/applied.</param>
/// <param name="Status">The correction status (requested, approved, rejected, applied).</param>
public sealed record TeachingRecordCorrectionDto(
    Guid Id,
    string CorrectionNumber,
    string TargetTableName,
    Guid TargetRecordId,
    Guid? TeacherProfileId,
    string FieldName,
    string OriginalValue,
    string CorrectedValue,
    string ReasonForCorrection,
    string? SupportingEvidence,
    bool DownstreamImpactReviewed,
    string? EffectiveDateImplications,
    string RequestedBy,
    DateOnly RequestedDate,
    string? ApprovedBy,
    DateOnly? ApprovedDate,
    string Status);
