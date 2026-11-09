namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;

using FluentValidation;

/// <summary>
/// Validator for academic result correction requests enforcing mandatory reason length and value sanity (UC-ACD-19, TD-06, P6-S14).
/// </summary>
public sealed class CorrectApprovedResultValidator : AbstractValidator<CorrectApprovedResultRequest>
{
    public CorrectApprovedResultValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Correction reason is mandatory.")
            .MinimumLength(10).WithMessage("Correction reason must be at least 10 characters long.")
            .MaximumLength(2000).WithMessage("Correction reason must not exceed 2000 characters.");

        RuleFor(x => x)
            .Must(x => x.HasAnyCorrection)
            .WithMessage("At least one corrected value field (score_numeric, score_max, grade_letter, grade_label, or narrative_feedback) must be provided.");

        When(x => x.EffectiveScoreNumeric.HasValue, () =>
        {
            RuleFor(x => x.EffectiveScoreNumeric!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Corrected numeric score must be greater than or equal to 0.");

            When(x => x.EffectiveScoreMax.HasValue, () =>
            {
                RuleFor(x => x.EffectiveScoreNumeric!.Value)
                    .LessThanOrEqualTo(x => x.EffectiveScoreMax!.Value)
                    .WithMessage("Corrected numeric score cannot exceed corrected maximum score.");
            });
        });

        When(x => x.EffectiveScoreMax.HasValue, () =>
        {
            RuleFor(x => x.EffectiveScoreMax!.Value)
                .GreaterThan(0)
                .WithMessage("Corrected maximum score must be greater than 0.");
        });

        When(x => !string.IsNullOrEmpty(x.EffectiveGradeLetter), () =>
        {
            RuleFor(x => x.EffectiveGradeLetter)
                .MaximumLength(10)
                .WithMessage("Corrected grade letter must not exceed 10 characters.");
        });

        When(x => !string.IsNullOrEmpty(x.EffectiveGradeLabel), () =>
        {
            RuleFor(x => x.EffectiveGradeLabel)
                .MaximumLength(100)
                .WithMessage("Corrected grade label must not exceed 100 characters.");
        });

        When(x => !string.IsNullOrEmpty(x.Evidence), () =>
        {
            RuleFor(x => x.Evidence)
                .MaximumLength(2000)
                .WithMessage("Evidence reference must not exceed 2000 characters.");
        });
    }
}
