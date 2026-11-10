namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.VerifyQualificationEvidence;

using FluentValidation;

/// <summary>
/// Validator for <see cref="VerifyQualificationEvidenceRequest"/>.
/// </summary>
public sealed class VerifyQualificationEvidenceValidator : AbstractValidator<VerifyQualificationEvidenceRequest>
{
    public VerifyQualificationEvidenceValidator()
    {
        RuleFor(x => x.VerificationNotes)
            .MaximumLength(4000)
            .WithMessage("Verification notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.VerificationNotes));
    }
}
