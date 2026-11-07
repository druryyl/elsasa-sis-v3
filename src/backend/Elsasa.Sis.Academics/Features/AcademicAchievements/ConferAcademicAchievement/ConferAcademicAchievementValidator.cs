namespace Elsasa.Sis.Academics.Features.AcademicAchievements.ConferAcademicAchievement;

using FluentValidation;

/// <summary>
/// FluentValidation validator for <see cref="ConferAcademicAchievementCommand"/> (UC-ACD-21, P4-S11).
/// Validates achievement category, title length, and criteria requirements.
/// </summary>
public sealed class ConferAcademicAchievementValidator : AbstractValidator<ConferAcademicAchievementCommand>
{
    private static readonly string[] _validAchievementTypes =
    [
        "subject_distinction",
        "academic_honors",
        "mastery_recognition",
        "academic_certification",
        "academic_competition"
    ];

    public ConferAcademicAchievementValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("StudentId is required.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("AcademicPeriodId is required.");

        RuleFor(x => x.AchievementType)
            .NotEmpty().WithMessage("AchievementType is required.")
            .Must(type => _validAchievementTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"AchievementType must be one of: {string.Join(", ", _validAchievementTypes)}.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters long.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.CriteriaMet)
            .NotEmpty().WithMessage("CriteriaMet is required.")
            .MinimumLength(5).WithMessage("CriteriaMet must be at least 5 characters long.")
            .MaximumLength(1000).WithMessage("CriteriaMet cannot exceed 1000 characters.");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
        });

        When(x => !string.IsNullOrEmpty(x.SupportingEvidence), () =>
        {
            RuleFor(x => x.SupportingEvidence)
                .MaximumLength(2000).WithMessage("SupportingEvidence cannot exceed 2000 characters.");
        });

        RuleFor(x => x.RecognizedAt)
            .NotEmpty().WithMessage("RecognizedAt date is required.");
    }
}
