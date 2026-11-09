namespace Elsasa.Sis.Academics.Features.Assessments.ConductAssessment;

using FluentValidation;

/// <summary>
/// Validator for <see cref="ConductAssessmentRequest"/> (UC-ACD-05, P2-S04).
/// </summary>
public sealed class ConductAssessmentValidator : AbstractValidator<ConductAssessmentRequest>
{
    public ConductAssessmentValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
