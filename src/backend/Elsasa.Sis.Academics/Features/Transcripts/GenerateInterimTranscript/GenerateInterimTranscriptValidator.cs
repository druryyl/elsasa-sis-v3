namespace Elsasa.Sis.Academics.Features.Transcripts.GenerateInterimTranscript;

using FluentValidation;

/// <summary>
/// Validator for <see cref="GenerateInterimTranscriptRequest"/> ensuring valid student ID, transcript type, date range, and bounds.
/// </summary>
public sealed class GenerateInterimTranscriptValidator : AbstractValidator<GenerateInterimTranscriptRequest>
{
    private static readonly string[] _allowedTypes = ["official", "unofficial"];

    public GenerateInterimTranscriptValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student ID is required.");

        RuleFor(x => x.TranscriptType)
            .NotEmpty()
            .WithMessage("Transcript type is required.")
            .Must(t => _allowedTypes.Contains(t?.Trim().ToLowerInvariant()))
            .WithMessage("Transcript type must be either 'official' or 'unofficial'.");

        RuleFor(x => x.PeriodStart)
            .NotEmpty()
            .WithMessage("Period start date is required.");

        RuleFor(x => x.PeriodEnd)
            .NotEmpty()
            .WithMessage("Period end date is required.")
            .GreaterThanOrEqualTo(x => x.PeriodStart)
            .WithMessage("Period end date must be on or after period start date.");

        RuleFor(x => x.Purpose)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Purpose))
            .WithMessage("Purpose cannot exceed 200 characters.");
    }
}
