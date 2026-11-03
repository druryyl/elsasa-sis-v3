namespace Elsasa.Sis.Students.Features.Records.Documents.VerifyStudentDocument;

using FluentValidation;

/// <summary>
/// Validator for student document verification requests (ADR-004, ADR-005, P6-S09).
/// </summary>
public sealed class VerifyStudentDocumentValidator : AbstractValidator<VerifyStudentDocumentRequest>
{
    private static readonly HashSet<string> _validStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "verified",
        "rejected"
    };

    public VerifyStudentDocumentValidator()
    {
        RuleFor(x => x.SubmissionStatus)
            .NotEmpty().WithMessage("Submission status is required.")
            .Must(status => status != null && _validStatuses.Contains(status.Trim()))
            .WithMessage("Submission status must be either 'verified' or 'rejected'.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");
    }
}
