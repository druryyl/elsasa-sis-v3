namespace Elsasa.Sis.Academics.Features.LearningUnits.CreateLearningUnit;

using FluentValidation;

/// <summary>
/// Validator for learning unit creation requests (P3-S08).
/// </summary>
public sealed class CreateLearningUnitValidator : AbstractValidator<CreateLearningUnitRequest>
{
    private static readonly string[] _allowedUnitTypes =
    [
        "subject",
        "course",
        "module",
        "pace",
        "project",
        "competency",
        "unit",
        "other"
    ];

    public CreateLearningUnitValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Unit code is required.")
            .MaximumLength(50).WithMessage("Unit code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Unit name is required.")
            .MaximumLength(200).WithMessage("Unit name cannot exceed 200 characters.");

        RuleFor(x => x.UnitType)
            .NotEmpty().WithMessage("Unit type is required.")
            .Must(type => !string.IsNullOrWhiteSpace(type) && _allowedUnitTypes.Contains(type.Trim().ToLowerInvariant()))
            .WithMessage("Unit type must be one of: subject, course, module, pace, project, competency, unit, other.");

        RuleFor(x => x.Level)
            .MaximumLength(50).WithMessage("Level cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Level));

        RuleFor(x => x.CreditWeight)
            .GreaterThanOrEqualTo(0).WithMessage("Credit weight must be greater than or equal to 0.")
            .When(x => x.CreditWeight.HasValue);
    }
}
