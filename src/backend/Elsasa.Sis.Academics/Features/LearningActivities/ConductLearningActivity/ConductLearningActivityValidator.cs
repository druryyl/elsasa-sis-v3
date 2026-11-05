namespace Elsasa.Sis.Academics.Features.LearningActivities.ConductLearningActivity;

using FluentValidation;

/// <summary>
/// Validator for <see cref="ConductLearningActivityRequest"/> (P3-S07).
/// </summary>
public sealed class ConductLearningActivityValidator : AbstractValidator<ConductLearningActivityRequest>
{
    public ConductLearningActivityValidator()
    {
        RuleFor(x => x.ActualTeacherName)
            .MaximumLength(200).WithMessage("Actual teacher name cannot exceed 200 characters.");
    }
}
