namespace Elsasa.Sis.Academics.Features.LearningActivities.CancelLearningActivity;

using FluentValidation;

/// <summary>
/// Validator for <see cref="CancelLearningActivityRequest"/> (P3-S07).
/// </summary>
public sealed class CancelLearningActivityValidator : AbstractValidator<CancelLearningActivityRequest>
{
    public CancelLearningActivityValidator()
    {
        RuleFor(x => x.CancellationReason)
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MinimumLength(3).WithMessage("Cancellation reason must be at least 3 characters.")
            .MaximumLength(1000).WithMessage("Cancellation reason cannot exceed 1000 characters.");
    }
}
