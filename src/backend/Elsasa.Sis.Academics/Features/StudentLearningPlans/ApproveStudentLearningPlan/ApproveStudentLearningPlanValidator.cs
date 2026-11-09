namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ApproveStudentLearningPlan;

using FluentValidation;

/// <summary>
/// Validator for student learning plan approval requests (P3-S08).
/// </summary>
public sealed class ApproveStudentLearningPlanValidator : AbstractValidator<ApproveStudentLearningPlanRequest>
{
    private static readonly string[] _allowedTypes = ["initial_approval", "revision_approval"];

    public ApproveStudentLearningPlanValidator()
    {
        RuleFor(x => x.ApprovalType)
            .NotEmpty()
            .WithMessage("Approval type is required.")
            .Must(t => _allowedTypes.Contains(t?.Trim().ToLowerInvariant()))
            .WithMessage("Approval type must be either 'initial_approval' or 'revision_approval'.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .WithMessage("Approval notes must not exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
