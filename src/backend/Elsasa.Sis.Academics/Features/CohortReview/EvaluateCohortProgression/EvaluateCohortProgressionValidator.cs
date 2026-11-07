namespace Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortProgression;

using FluentValidation;

/// <summary>
/// Validator for <see cref="EvaluateCohortProgressionRequest"/>.
/// </summary>
public sealed class EvaluateCohortProgressionValidator : AbstractValidator<EvaluateCohortProgressionRequest>
{
    public EvaluateCohortProgressionValidator()
    {
        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty()
            .WithMessage("AcademicPeriodId is required.");

        RuleFor(x => x.GradeLevel)
            .NotEmpty()
            .WithMessage("GradeLevel is required.")
            .MaximumLength(50)
            .WithMessage("GradeLevel must not exceed 50 characters.");
    }
}
