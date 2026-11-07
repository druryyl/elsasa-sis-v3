namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.AddSlpLearningUnit;

using FluentValidation;

/// <summary>
/// Validator for <see cref="AddSlpLearningUnitRequest"/> enforcing constituent unit constraints (P2-S05).
/// </summary>
public sealed class AddSlpLearningUnitValidator : AbstractValidator<AddSlpLearningUnitRequest>
{
    public AddSlpLearningUnitValidator()
    {
        RuleFor(x => x.LearningUnitId)
            .NotEmpty()
            .WithMessage("Learning unit ID is required.");

        RuleFor(x => x.SequenceOrder)
            .GreaterThan(0)
            .WithMessage("Sequence order must be greater than zero.")
            .When(x => x.SequenceOrder.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
