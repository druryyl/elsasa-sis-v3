namespace Elsasa.Sis.Academics.Features.LearningActivities.PlanLearningActivity;

using FluentValidation;

/// <summary>
/// Validator for <see cref="PlanLearningActivityRequest"/> enforcing database check constraints and business invariants (TD-03, P3-S07).
/// </summary>
public sealed class PlanLearningActivityValidator : AbstractValidator<PlanLearningActivityRequest>
{
    private static readonly HashSet<string> _validActivityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "lesson", "tutorial", "laboratory", "independent_work", "project",
        "discussion", "practice", "examination", "remediation", "homeroom_session", "other"
    };

    public PlanLearningActivityValidator()
    {
        RuleFor(x => x.LearningUnitId)
            .NotEmpty().WithMessage("Learning unit ID is required.");

        RuleFor(x => x.ActivityType)
            .NotEmpty().WithMessage("Activity type is required.")
            .Must(type => _validActivityTypes.Contains(type))
            .WithMessage("Activity type must be one of: lesson, tutorial, laboratory, independent_work, project, discussion, practice, examination, remediation, homeroom_session, other.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.ActivityDate)
            .NotEmpty().WithMessage("Activity date is required.");

        RuleFor(x => x.SectionLabel)
            .MaximumLength(50).WithMessage("Section label cannot exceed 50 characters.");

        RuleFor(x => x.TeacherName)
            .MaximumLength(200).WithMessage("Teacher name cannot exceed 200 characters.");

        RuleFor(x => x)
            .Must(x => !x.StartTime.HasValue || !x.EndTime.HasValue || x.StartTime.Value < x.EndTime.Value)
            .WithMessage("Start time must be earlier than end time.")
            .WithName("StartTime");
    }
}
