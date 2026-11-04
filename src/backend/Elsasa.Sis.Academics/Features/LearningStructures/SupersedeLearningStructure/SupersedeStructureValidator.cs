namespace Elsasa.Sis.Academics.Features.LearningStructures.SupersedeLearningStructure;

using FluentValidation;

/// <summary>
/// Validator for branching/superseding a learning structure framework (P3-S07, ADR-005).
/// </summary>
public sealed class SupersedeStructureValidator : AbstractValidator<SupersedeStructureRequest>
{
    public SupersedeStructureValidator()
    {
        RuleFor(x => x.NewVersion)
            .NotEmpty().WithMessage("New version string is required.")
            .MaximumLength(50).WithMessage("Version must not exceed 50 characters.");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required.");

        When(x => !string.IsNullOrWhiteSpace(x.AcademicPeriodLabel), () =>
        {
            RuleFor(x => x.AcademicPeriodLabel!)
                .MaximumLength(100).WithMessage("Academic period label must not exceed 100 characters.");
        });
    }
}
