namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.CreateStudentLearningPlan;

using FluentValidation;

/// <summary>
/// Validator for <see cref="CreateStudentLearningPlanRequest"/> enforcing formulation constraints (P2-S04).
/// </summary>
public sealed class CreateStudentLearningPlanValidator : AbstractValidator<CreateStudentLearningPlanRequest>
{
    public CreateStudentLearningPlanValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student ID is required.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty()
            .WithMessage("Academic period ID is required.");

        RuleFor(x => x.AcademicPeriodLabel)
            .NotEmpty()
            .WithMessage("Academic period label is required.")
            .MaximumLength(100)
            .WithMessage("Academic period label cannot exceed 100 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
