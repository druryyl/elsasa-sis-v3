namespace Elsasa.Sis.Academics.Features.Assignments.SubmitCoursework;

using FluentValidation;

/// <summary>
/// Validator for coursework submission payloads (TD-07, UC-ACD-01, P5-S11).
/// </summary>
public sealed class SubmitCourseworkValidator : AbstractValidator<SubmitCourseworkRequest>
{
    public SubmitCourseworkValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student ID is required.");

        RuleFor(x => x.SubmissionReference)
            .NotEmpty()
            .WithMessage("Submission reference is required.")
            .MaximumLength(1000)
            .WithMessage("Submission reference must not exceed 1000 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes must not exceed 2000 characters.");
    }
}
