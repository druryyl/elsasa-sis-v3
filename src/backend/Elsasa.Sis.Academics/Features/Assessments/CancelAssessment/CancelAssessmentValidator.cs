namespace Elsasa.Sis.Academics.Features.Assessments.CancelAssessment;

using FluentValidation;

/// <summary>
/// Validator for <see cref="CancelAssessmentRequest"/> (UC-ACD-05, P2-S04).
/// </summary>
public sealed class CancelAssessmentValidator : AbstractValidator<CancelAssessmentRequest>
{
    public CancelAssessmentValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MinimumLength(5).WithMessage("Cancellation reason must be at least 5 characters.")
            .MaximumLength(2000).WithMessage("Cancellation reason cannot exceed 2000 characters.");
    }
}
