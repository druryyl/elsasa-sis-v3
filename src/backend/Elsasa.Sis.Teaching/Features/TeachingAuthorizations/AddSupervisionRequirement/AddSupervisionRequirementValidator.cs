namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.AddSupervisionRequirement;

using FluentValidation;

/// <summary>
/// FluentValidation rules for <see cref="AddSupervisionRequirementRequest"/> (P4-S13).
/// </summary>
public sealed class AddSupervisionRequirementValidator : AbstractValidator<AddSupervisionRequirementRequest>
{
    private static readonly HashSet<string> _allowedSupervisionTypes =
    [
        "co_teaching",
        "periodic_observation",
        "lesson_plan_review",
        "general_mentorship"
    ];

    public AddSupervisionRequirementValidator()
    {
        RuleFor(x => x.SupervisionType)
            .NotEmpty().WithMessage("Supervision type is required.")
            .Must(type => !string.IsNullOrWhiteSpace(type) && _allowedSupervisionTypes.Contains(type.Trim().ToLowerInvariant()))
            .WithMessage("Supervision type must be one of: co_teaching, periodic_observation, lesson_plan_review, general_mentorship.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .Must(d => d != default).WithMessage("Start date must be a valid date.");

        When(x => x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate!.Value)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be on or after start date.");
        });
    }
}
