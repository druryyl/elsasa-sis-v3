namespace Elsasa.Sis.Academics.Features.Assessments.UpdateAssessment;

using FluentValidation;

/// <summary>
/// Validator for <see cref="UpdateAssessmentRequest"/> (UC-ACD-05, P2-S04).
/// </summary>
public sealed class UpdateAssessmentValidator : AbstractValidator<UpdateAssessmentRequest>
{
    private static readonly string[] _allowedPolicies = ["capped_at_passing", "highest_score", "latest_attempt", "average", "none"];

    public UpdateAssessmentValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.ReassessmentPolicy)
            .Must(policy => string.IsNullOrWhiteSpace(policy) || _allowedPolicies.Contains(policy.Trim().ToLowerInvariant()))
            .WithMessage($"Reassessment policy must be one of: {string.Join(", ", _allowedPolicies)}.");

        RuleFor(x => x.MaxScore)
            .GreaterThanOrEqualTo(0).WithMessage("Max score cannot be negative.")
            .When(x => x.MaxScore.HasValue);

        RuleFor(x => x.PassingScore)
            .GreaterThanOrEqualTo(0).WithMessage("Passing score cannot be negative.")
            .When(x => x.PassingScore.HasValue);

        RuleFor(x => x.PassingScore)
            .Must((request, passingScore) => !passingScore.HasValue || !request.MaxScore.HasValue || passingScore.Value <= request.MaxScore.Value)
            .WithMessage("Passing score must be less than or equal to max score.")
            .When(x => x.PassingScore.HasValue && x.MaxScore.HasValue);

        RuleFor(x => x.AssessorReference)
            .MaximumLength(200).WithMessage("Assessor reference cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.AssessorReference));

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
