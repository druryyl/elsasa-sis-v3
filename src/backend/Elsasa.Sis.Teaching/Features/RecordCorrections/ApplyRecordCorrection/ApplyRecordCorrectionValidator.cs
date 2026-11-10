namespace Elsasa.Sis.Teaching.Features.RecordCorrections.ApplyRecordCorrection;

using FluentValidation;

/// <summary>
/// FluentValidation validator for <see cref="ApplyRecordCorrectionRequest"/> (P5-S14).
/// </summary>
public sealed class ApplyRecordCorrectionValidator : AbstractValidator<ApplyRecordCorrectionRequest>
{
    public ApplyRecordCorrectionValidator()
    {
        RuleFor(x => x.TargetTableName)
            .NotEmpty().WithMessage("Target table name is required.")
            .Must(t => RecordCorrectionColumnHelper.TryNormalizeTargetTable(t, out _))
            .WithMessage("Target table must be 'teacher_profiles' or 'teaching_authorizations'.");

        RuleFor(x => x.TargetRecordId)
            .NotEmpty().WithMessage("Target record ID is required.");

        RuleFor(x => x.FieldName)
            .NotEmpty().WithMessage("Field name is required.")
            .Must((req, field) =>
            {
                if (!RecordCorrectionColumnHelper.TryNormalizeTargetTable(req.TargetTableName, out var canonicalTable))
                {
                    return false;
                }

                return RecordCorrectionColumnHelper.TryGetColumnMetadata(canonicalTable, field, out _);
            })
            .WithMessage("Field name is not recognized or not eligible for record correction on the target table.");

        RuleFor(x => x.CorrectedValue)
            .NotNull().WithMessage("Corrected value is required.");

        RuleFor(x => x.ReasonForCorrection)
            .NotEmpty().WithMessage("Reason for correction is mandatory.")
            .MaximumLength(2000).WithMessage("Reason for correction must not exceed 2000 characters.");
    }
}
