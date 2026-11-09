namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectPromotionRecommendation;

using FluentValidation;

/// <summary>
/// Validator for promotion recommendation correction requests enforcing mandatory reason length and domain rules (P5-S12, UC-ACD-15).
/// </summary>
public sealed class CorrectPromotionRecommendationRequestValidator : AbstractValidator<CorrectPromotionRecommendationRequest>
{
    private static readonly string[] _validRecommendations = ["promotion_eligible", "retention_recommended", "conditional_promotion"];

    public CorrectPromotionRecommendationRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Correction reason is mandatory.")
            .MinimumLength(10).WithMessage("Correction reason must be at least 10 characters long.")
            .MaximumLength(2000).WithMessage("Correction reason must not exceed 2000 characters.");

        RuleFor(x => x.Recommendation)
            .NotEmpty().WithMessage("Recommendation is required.")
            .Must(r => r is not null && _validRecommendations.Contains(r.Trim().ToLowerInvariant()))
            .WithMessage("Recommendation must be one of: promotion_eligible, retention_recommended, conditional_promotion.");

        RuleFor(x => x.FromLevel)
            .NotEmpty().WithMessage("From level is required.")
            .MaximumLength(50).WithMessage("From level must not exceed 50 characters.");

        RuleFor(x => x.RecommendedToLevel)
            .MaximumLength(50).WithMessage("Recommended to level must not exceed 50 characters.");

        RuleFor(x => x.Conditions)
            .NotEmpty().WithMessage("Conditions are mandatory when recommendation is 'conditional_promotion'.")
            .When(x => string.Equals(x.Recommendation?.Trim(), "conditional_promotion", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.Conditions)
            .MaximumLength(2000).WithMessage("Conditions must not exceed 2000 characters.");

        RuleFor(x => x.SupportingEvidence)
            .NotEmpty().WithMessage("Supporting evidence is required.")
            .MaximumLength(2000).WithMessage("Supporting evidence must not exceed 2000 characters.");

        RuleFor(x => x.ExceptionReason)
            .NotEmpty().WithMessage("Exception reason is mandatory when IsException is true.")
            .MinimumLength(5).WithMessage("Exception reason must be at least 5 characters.")
            .MaximumLength(1000).WithMessage("Exception reason must not exceed 1000 characters.")
            .When(x => x.IsException);

        RuleFor(x => x.CommitteeReference)
            .NotEmpty().WithMessage("Committee reference is mandatory when IsException is true.")
            .MinimumLength(5).WithMessage("Committee reference must be at least 5 characters.")
            .MaximumLength(100).WithMessage("Committee reference must not exceed 100 characters.")
            .When(x => x.IsException);
    }
}

/// <summary>
/// Validator for promotion recommendation correction command enforcing ID existence and invariants (P5-S12, UC-ACD-15).
/// </summary>
public sealed class CorrectPromotionRecommendationCommandValidator : AbstractValidator<CorrectPromotionRecommendationCommand>
{
    private static readonly string[] _validRecommendations = ["promotion_eligible", "retention_recommended", "conditional_promotion"];

    public CorrectPromotionRecommendationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Recommendation ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Correction reason is mandatory.")
            .MinimumLength(10).WithMessage("Correction reason must be at least 10 characters long.")
            .MaximumLength(2000).WithMessage("Correction reason must not exceed 2000 characters.");

        RuleFor(x => x.Recommendation)
            .NotEmpty().WithMessage("Recommendation is required.")
            .Must(r => r is not null && _validRecommendations.Contains(r.Trim().ToLowerInvariant()))
            .WithMessage("Recommendation must be one of: promotion_eligible, retention_recommended, conditional_promotion.");

        RuleFor(x => x.FromLevel)
            .NotEmpty().WithMessage("From level is required.")
            .MaximumLength(50).WithMessage("From level must not exceed 50 characters.");

        RuleFor(x => x.RecommendedToLevel)
            .MaximumLength(50).WithMessage("Recommended to level must not exceed 50 characters.");

        RuleFor(x => x.Conditions)
            .NotEmpty().WithMessage("Conditions are mandatory when recommendation is 'conditional_promotion'.")
            .When(x => string.Equals(x.Recommendation?.Trim(), "conditional_promotion", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.Conditions)
            .MaximumLength(2000).WithMessage("Conditions must not exceed 2000 characters.");

        RuleFor(x => x.SupportingEvidence)
            .NotEmpty().WithMessage("Supporting evidence is required.")
            .MaximumLength(2000).WithMessage("Supporting evidence must not exceed 2000 characters.");

        RuleFor(x => x.ExceptionReason)
            .NotEmpty().WithMessage("Exception reason is mandatory when IsException is true.")
            .MinimumLength(5).WithMessage("Exception reason must be at least 5 characters.")
            .MaximumLength(1000).WithMessage("Exception reason must not exceed 1000 characters.")
            .When(x => x.IsException);

        RuleFor(x => x.CommitteeReference)
            .NotEmpty().WithMessage("Committee reference is mandatory when IsException is true.")
            .MinimumLength(5).WithMessage("Committee reference must be at least 5 characters.")
            .MaximumLength(100).WithMessage("Committee reference must not exceed 100 characters.")
            .When(x => x.IsException);
    }
}
