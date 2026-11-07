namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.AddAcademicPlanAdjustment;

using FluentValidation;

/// <summary>
/// FluentValidation rules for <see cref="AddAcademicPlanAdjustmentRequest"/> (P3-S07, TD-03).
/// Enforces:
/// - AdjustmentType must be one of: accommodation, substitution, exemption.
/// - Description not empty, max 2000 chars.
/// - Rationale not empty, min 10 chars, max 2000 chars.
/// - EffectiveDate not empty / not default.
/// - EndDate >= EffectiveDate when provided.
/// - Optional Notes max 4000 chars.
/// </summary>
public sealed class AddAcademicPlanAdjustmentValidator : AbstractValidator<AddAcademicPlanAdjustmentRequest>
{
    private static readonly string[] _validAdjustmentTypes = ["accommodation", "substitution", "exemption"];

    public AddAcademicPlanAdjustmentValidator()
    {
        RuleFor(x => x.AdjustmentType)
            .NotEmpty().WithMessage("Adjustment type is required.")
            .Must(type => !string.IsNullOrWhiteSpace(type) && _validAdjustmentTypes.Contains(type.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Adjustment type must be one of: 'accommodation', 'substitution', 'exemption'.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Rationale)
            .NotEmpty().WithMessage("Rationale is required.")
            .MinimumLength(10).WithMessage("Rationale must be at least 10 characters.")
            .MaximumLength(2000).WithMessage("Rationale must not exceed 2000 characters.");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required.")
            .NotEqual(default(DateOnly)).WithMessage("Effective date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.EffectiveDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be on or after the effective date.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes must not exceed 4000 characters.");
    }
}
