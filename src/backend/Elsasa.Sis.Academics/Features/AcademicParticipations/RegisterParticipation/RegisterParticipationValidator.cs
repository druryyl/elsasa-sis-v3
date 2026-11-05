namespace Elsasa.Sis.Academics.Features.AcademicParticipations.RegisterParticipation;

using FluentValidation;

/// <summary>
/// Validator for individual academic participation registration requests (P2-S04).
/// </summary>
public sealed class RegisterParticipationValidator : AbstractValidator<RegisterParticipationRequest>
{
    private static readonly string[] _allowedParticipationTypes =
    [
        "standard",
        "repeat",
        "transferred_in",
        "recognized_prior"
    ];

    private static readonly string[] _allowedSources =
    [
        "assigned",
        "student_selected",
        "learning_plan"
    ];

    public RegisterParticipationValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.LearningUnitId)
            .NotEmpty().WithMessage("Learning unit ID is required.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("Academic period ID is required.");

        RuleFor(x => x.AcademicPeriodLabel)
            .NotEmpty().WithMessage("Academic period label is required.")
            .MaximumLength(100).WithMessage("Academic period label cannot exceed 100 characters.");

        RuleFor(x => x.SectionLabel)
            .MaximumLength(50).WithMessage("Section label cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.SectionLabel));

        RuleFor(x => x.ParticipationType)
            .NotEmpty().WithMessage("Participation type is required.")
            .Must(type => !string.IsNullOrWhiteSpace(type) && _allowedParticipationTypes.Contains(type.Trim().ToLowerInvariant()))
            .WithMessage("Participation type must be one of: standard, repeat, transferred_in, recognized_prior.");

        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Source is required.")
            .Must(source => !string.IsNullOrWhiteSpace(source) && _allowedSources.Contains(source.Trim().ToLowerInvariant()))
            .WithMessage("Source must be one of: assigned, student_selected, learning_plan.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");
    }
}
