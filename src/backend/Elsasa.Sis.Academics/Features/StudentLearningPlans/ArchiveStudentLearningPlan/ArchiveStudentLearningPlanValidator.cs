namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.ArchiveStudentLearningPlan;

using FluentValidation;

/// <summary>
/// Validator for <see cref="ArchiveStudentLearningPlanRequest"/> (P2-S04).
/// </summary>
public sealed class ArchiveStudentLearningPlanValidator : AbstractValidator<ArchiveStudentLearningPlanRequest>
{
    private static readonly string[] _allowedStatuses = ["archived", "completed"];

    public ArchiveStudentLearningPlanValidator()
    {
        RuleFor(x => x.TargetStatus)
            .Must(status => string.IsNullOrWhiteSpace(status) || _allowedStatuses.Contains(status.Trim().ToLowerInvariant()))
            .WithMessage("Target status must be either 'archived' or 'completed'.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
