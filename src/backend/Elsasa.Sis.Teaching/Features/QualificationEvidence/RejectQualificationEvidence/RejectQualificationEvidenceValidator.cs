namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.RejectQualificationEvidence;

using FluentValidation;

/// <summary>
/// Validator for <see cref="RejectQualificationEvidenceRequest"/>.
/// </summary>
public sealed class RejectQualificationEvidenceValidator : AbstractValidator<RejectQualificationEvidenceRequest>
{
    public RejectQualificationEvidenceValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .WithMessage("Rejection reason is mandatory.")
            .MaximumLength(4000)
            .WithMessage("Rejection reason cannot exceed 4000 characters.");
    }
}
