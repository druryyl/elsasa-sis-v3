namespace Elsasa.Sis.Teaching.Features.TeachingAssignments.ProposeTeachingAssignment;

using FluentValidation;

/// <summary>
/// Validator for proposing a new teaching assignment (TD-01, TD-04, P3-S06).
/// </summary>
public sealed class ProposeTeachingAssignmentValidator : AbstractValidator<ProposeTeachingAssignmentRequest>
{
    private static readonly string[] _allowedScopes =
    [
        "learning_unit",
        "cohort_group",
        "individual_student",
        "extracurricular_group"
    ];

    public ProposeTeachingAssignmentValidator()
    {
        RuleFor(x => x.TeachingDemandId)
            .NotEmpty()
            .WithMessage("Teaching demand ID is required.");

        RuleFor(x => x.TeacherProfileId)
            .NotEmpty()
            .WithMessage("Teacher profile ID is required.");

        RuleFor(x => x.AssignmentRoleId)
            .NotEmpty()
            .WithMessage("Assignment role ID is required.");

        RuleFor(x => x.AssignmentScope)
            .NotEmpty()
            .WithMessage("Assignment scope is required.")
            .Must(scope => _allowedScopes.Contains(scope?.Trim().ToLowerInvariant()))
            .WithMessage("Assignment scope must be one of: learning_unit, cohort_group, individual_student, extracurricular_group.");

        RuleFor(x => x.ScopeReferenceId)
            .NotEmpty()
            .WithMessage("Scope reference ID is required.");

        RuleFor(x => x.ScopeLabel)
            .NotEmpty()
            .WithMessage("Scope label is required.")
            .MaximumLength(255)
            .WithMessage("Scope label must not exceed 255 characters.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty()
            .WithMessage("Academic period ID is required.");

        RuleFor(x => x.EffectiveStartDate)
            .LessThanOrEqualTo(x => x.EffectiveEndDate)
            .WithMessage("Effective start date must be before or equal to effective end date.");

        RuleFor(x => x.WorkloadCommitmentValue)
            .GreaterThanOrEqualTo(0.00m)
            .WithMessage("Workload commitment value must be greater than or equal to 0.")
            .LessThanOrEqualTo(100.00m)
            .WithMessage("Workload commitment value must not exceed 100 hours.");

        RuleFor(x => x.WorkloadUnit)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.WorkloadUnit))
            .WithMessage("Workload unit must not exceed 50 characters.");

        RuleFor(x => x.AssignmentNumber)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.AssignmentNumber))
            .WithMessage("Assignment number must not exceed 50 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes))
            .WithMessage("Notes must not exceed 4000 characters.");
    }
}
