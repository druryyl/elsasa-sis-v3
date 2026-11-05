namespace Elsasa.Sis.Academics.Features.AcademicParticipations.RecognizePriorCredit;

using FluentValidation;

/// <summary>
/// Validator for prior credit recognition requests (P2-S05).
/// </summary>
public sealed class RecognizePriorCreditValidator : AbstractValidator<RecognizePriorCreditRequest>
{
    private static readonly string[] _allowedTypes = ["transferred_in", "recognized_prior"];

    public RecognizePriorCreditValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.LearningUnitId)
            .NotEmpty().WithMessage("Learning unit ID is required.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("Academic period ID is required.");

        RuleFor(x => x.AcademicPeriodLabel)
            .NotEmpty().WithMessage("Academic period label is required.")
            .MaximumLength(100).WithMessage("Academic period label must not exceed 100 characters.");

        RuleFor(x => x.ParticipationType)
            .NotEmpty().WithMessage("Participation type is required.")
            .Must(type => type != null && _allowedTypes.Contains(type.Trim().ToLowerInvariant()))
            .WithMessage("Participation type must be either 'transferred_in' or 'recognized_prior'.");

        RuleFor(x => x.SourceInstitution)
            .MaximumLength(200).WithMessage("Source institution must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.SourceInstitution));

        RuleFor(x => x.ExternalGradeOrCredit)
            .MaximumLength(100).WithMessage("External grade or credit must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.ExternalGradeOrCredit));

        RuleFor(x => x.RecognitionBasis)
            .MaximumLength(4000).WithMessage("Recognition basis must not exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.RecognitionBasis));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.CompletionDate)
            .NotEmpty().WithMessage("Completion date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Completion date cannot precede start date.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes must not exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
