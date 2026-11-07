namespace Elsasa.Sis.Academics.Features.AcademicInterventions.InitiateAcademicIntervention;

using FluentValidation;

/// <summary>
/// Validator enforcing domain invariants for academic intervention initiation (UC-ACD-11, P4-S10, TD-04).
/// </summary>
public sealed class InitiateAcademicInterventionValidator : AbstractValidator<InitiateAcademicInterventionCommand>
{
    private static readonly HashSet<string> _validInterventionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "remediation",
        "reassessment",
        "tutoring_recommendation",
        "extension_work",
        "recovery_plan",
        "learning_plan_adjustment",
        "academic_warning"
    };

    private static readonly HashSet<string> _validTriggerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "assessment_below_threshold",
        "missing_work",
        "attendance_concern",
        "standing_concern",
        "teacher_referral"
    };

    public InitiateAcademicInterventionValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.LearningUnitId)
            .NotEmpty().WithMessage("Learning unit ID is required.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("Academic period ID is required.");

        RuleFor(x => x.InterventionType)
            .NotEmpty().WithMessage("Intervention type is required.")
            .Must(t => _validInterventionTypes.Contains(t))
            .WithMessage($"Intervention type must be one of: {string.Join(", ", _validInterventionTypes)}.");

        RuleFor(x => x.TriggerType)
            .NotEmpty().WithMessage("Trigger type is required.")
            .Must(t => _validTriggerTypes.Contains(t))
            .WithMessage($"Trigger type must be one of: {string.Join(", ", _validTriggerTypes)}.");

        RuleFor(x => x.TriggerDescription)
            .NotEmpty().WithMessage("Trigger description is required.")
            .MaximumLength(1000).WithMessage("Trigger description cannot exceed 1000 characters.");

        RuleFor(x => x.PlanDescription)
            .NotEmpty().WithMessage("Plan description is required.")
            .MinimumLength(5).WithMessage("Plan description must be at least 5 characters.")
            .MaximumLength(2000).WithMessage("Plan description cannot exceed 2000 characters.");
    }
}
