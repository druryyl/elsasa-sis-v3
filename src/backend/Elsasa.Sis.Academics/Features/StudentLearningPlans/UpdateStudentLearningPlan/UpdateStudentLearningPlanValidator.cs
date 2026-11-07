namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.UpdateStudentLearningPlan;

using FluentValidation;

/// <summary>
/// Validator for <see cref="UpdateStudentLearningPlanRequest"/> (P2-S04).
/// </summary>
public sealed class UpdateStudentLearningPlanValidator : AbstractValidator<UpdateStudentLearningPlanRequest>
{
    public UpdateStudentLearningPlanValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
