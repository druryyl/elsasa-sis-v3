namespace Elsasa.Sis.Students.Features.Admission.Documents.VerifyApplicantDocument;

using FluentValidation;

/// <summary>
/// Validator for document verification requests.
/// </summary>
public sealed class VerifyApplicantDocumentValidator : AbstractValidator<VerifyApplicantDocumentRequest>
{
    private static readonly HashSet<string> _validStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "verified",
        "rejected"
    };

    public VerifyApplicantDocumentValidator()
    {
        RuleFor(x => x.SubmissionStatus)
            .NotEmpty().WithMessage("Submission status is required.")
            .Must(status => status != null && _validStatuses.Contains(status.Trim()))
            .WithMessage("Submission status must be either 'verified' or 'rejected'.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");
    }
}
