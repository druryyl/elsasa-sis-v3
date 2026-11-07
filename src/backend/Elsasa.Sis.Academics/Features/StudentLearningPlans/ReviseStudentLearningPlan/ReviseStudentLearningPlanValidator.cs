namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ReviseStudentLearningPlan;

using FluentValidation;

/// <summary>
/// Validator for student learning plan revision requests (UC-ACD-09, P3-S09, TD-02).
/// </summary>
public sealed class ReviseStudentLearningPlanValidator : AbstractValidator<ReviseStudentLearningPlanRequest>
{
    public ReviseStudentLearningPlanValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .WithMessage("Revision notes must not exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
