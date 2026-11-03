namespace Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.UpdateAssessmentEvidence;

using FluentValidation;

/// <summary>
/// Validates <see cref="UpdateAssessmentEvidenceRequest"/> payload (ADR-005).
/// </summary>
public sealed class UpdateAssessmentEvidenceValidator : AbstractValidator<UpdateAssessmentEvidenceRequest>
{
    public UpdateAssessmentEvidenceValidator()
    {
        RuleFor(x => x.AssessmentType)
            .NotEmpty().WithMessage("Assessment type is required.")
            .MaximumLength(100).WithMessage("Assessment type cannot exceed 100 characters.");

        RuleFor(x => x.Score)
            .MaximumLength(50).WithMessage("Score cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Score));

        RuleFor(x => x.Source)
            .MaximumLength(100).WithMessage("Source cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Source));

        RuleFor(x => x.AssessmentDate)
            .Must(d => !d.HasValue || d.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("Assessment date cannot be in the future.");
    }
}
