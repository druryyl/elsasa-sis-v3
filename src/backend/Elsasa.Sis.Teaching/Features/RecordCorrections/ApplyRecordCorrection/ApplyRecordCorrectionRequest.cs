namespace Elsasa.Sis.Teaching.Features.RecordCorrections.ApplyRecordCorrection;

/// <summary>
/// Request record for submitting or applying an auditable record correction on a finalized teaching record (TD-06, P5-S14).
/// </summary>
/// <param name="TargetTableName">The target table name ('teacher_profiles' or 'teaching_authorizations').</param>
/// <param name="TargetRecordId">The unique identifier of the target record being corrected.</param>
/// <param name="FieldName">The name of the field/column being corrected.</param>
/// <param name="CorrectedValue">The new corrected value.</param>
/// <param name="ReasonForCorrection">Mandatory audit justification for the correction.</param>
/// <param name="SupportingEvidence">Optional documentary evidence or reference.</param>
/// <param name="DownstreamImpactReviewed">Optional flag indicating whether downstream impact was reviewed (defaults to true).</param>
/// <param name="EffectiveDateImplications">Optional explanation of retroactive/effective date impact.</param>
/// <param name="TeacherProfileId">Optional associated teacher profile identifier.</param>
public sealed record ApplyRecordCorrectionRequest(
    string TargetTableName,
    Guid TargetRecordId,
    string FieldName,
    string CorrectedValue,
    string ReasonForCorrection,
    string? SupportingEvidence = null,
    bool? DownstreamImpactReviewed = null,
    string? EffectiveDateImplications = null,
    Guid? TeacherProfileId = null);
