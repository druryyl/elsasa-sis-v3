namespace Elsasa.Sis.Finance.Features.Obligations.BatchAssessObligations;

using FluentValidation;

/// <summary>
/// FluentValidation validator for batch obligation assessment requests.
/// </summary>
public sealed class BatchAssessObligationsValidator : AbstractValidator<BatchAssessObligationsRequest>
{
    private static readonly string[] _allowedModes = ["preview", "commit"];

    public BatchAssessObligationsValidator()
    {
        RuleFor(x => x.ChargeRuleId)
            .NotEmpty()
            .WithMessage("ChargeRuleId is required.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty()
            .WithMessage("AcademicPeriodId is required.");

        RuleFor(x => x.BillingCycleLabel)
            .NotEmpty()
            .WithMessage("BillingCycleLabel is required.")
            .MaximumLength(100)
            .WithMessage("BillingCycleLabel cannot exceed 100 characters.");

        RuleFor(x => x.Mode)
            .NotEmpty()
            .WithMessage("Mode is required.")
            .Must(m => m is not null && _allowedModes.Contains(m.Trim().ToLowerInvariant()))
            .WithMessage("Mode must be either 'preview' or 'commit'.");

        When(x => !string.IsNullOrWhiteSpace(x.TargetCampus), () =>
        {
            RuleFor(x => x.TargetCampus!)
                .MaximumLength(100)
                .WithMessage("TargetCampus cannot exceed 100 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.TargetGrade), () =>
        {
            RuleFor(x => x.TargetGrade!)
                .MaximumLength(64)
                .WithMessage("TargetGrade cannot exceed 64 characters.");
        });
    }
}
