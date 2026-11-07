namespace Elsasa.Sis.Academics.Features.CohortReview.EvaluateCohortStanding;

using FluentValidation;

/// <summary>
/// Validator for <see cref="EvaluateCohortStandingRequest"/>.
/// </summary>
public sealed class EvaluateCohortStandingValidator : AbstractValidator<EvaluateCohortStandingRequest>
{
    public EvaluateCohortStandingValidator()
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
