namespace Elsasa.Sis.Academics.Features.Assignments.RecordSubmissionException;

using FluentValidation;

/// <summary>
/// Validator for recording an assignment submission policy exception (TD-06, P5-S11).
/// </summary>
public sealed class RecordSubmissionExceptionValidator : AbstractValidator<RecordSubmissionExceptionRequest>
{
    private static readonly HashSet<string> _validExceptionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "excused",
        "exempted"
    };

    public RecordSubmissionExceptionValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student ID is required.");

        RuleFor(x => x.ExceptionType)
            .NotEmpty()
            .WithMessage("Exception type is required.")
            .Must(type => !string.IsNullOrWhiteSpace(type) && _validExceptionTypes.Contains(type.Trim()))
            .WithMessage("Exception type must be either 'excused' or 'exempted'.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Justification reason is required.")
            .MinimumLength(3)
            .WithMessage("Justification reason must be at least 3 characters.")
            .MaximumLength(2000)
            .WithMessage("Justification reason must not exceed 2000 characters.");
    }
}
