namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.SubmitStudentLearningPlan;

using FluentValidation;

/// <summary>
/// Validator for student learning plan submission requests (P3-S08).
/// </summary>
public sealed class SubmitStudentLearningPlanValidator : AbstractValidator<SubmitStudentLearningPlanRequest>
{
    public SubmitStudentLearningPlanValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .WithMessage("Submission notes must not exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
