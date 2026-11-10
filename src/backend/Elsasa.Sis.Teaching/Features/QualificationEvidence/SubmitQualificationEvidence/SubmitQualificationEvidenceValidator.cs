namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.SubmitQualificationEvidence;

using FluentValidation;

/// <summary>
/// Validator for candidate qualification evidence submission (P3-S09).
/// </summary>
public sealed class SubmitQualificationEvidenceValidator : AbstractValidator<SubmitQualificationEvidenceRequest>
{
    public SubmitQualificationEvidenceValidator()
    {
        RuleFor(x => x.QualificationTypeId)
            .NotEmpty()
            .WithMessage("Qualification type identifier is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.IssuingAuthority)
            .NotEmpty()
            .WithMessage("Issuing authority is required.")
            .MaximumLength(200)
            .WithMessage("Issuing authority cannot exceed 200 characters.");

        RuleFor(x => x.CredentialIdentifier)
            .MaximumLength(100)
            .WithMessage("Credential identifier cannot exceed 100 characters.");

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("Issue date is required.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .When(x => x.ExpiryDate.HasValue)
            .WithMessage("Expiry date must be on or after the issue date.");

        RuleFor(x => x.DocumentReference)
            .MaximumLength(500)
            .WithMessage("Document reference cannot exceed 500 characters.");

        RuleFor(x => x.RestrictionsOrConditions)
            .MaximumLength(1000)
            .WithMessage("Restrictions or conditions cannot exceed 1000 characters.");
    }
}
