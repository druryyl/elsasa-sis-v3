namespace Elsasa.Sis.Students.Features.Admission.RecordAdmissionDecision;

using FluentValidation;

/// <summary>
/// FluentValidation rules for recording an admission decision (ADR-005).
/// </summary>
public sealed class RecordAdmissionDecisionValidator : AbstractValidator<RecordAdmissionDecisionRequest>
{
    private static readonly string[] _validDecisionTypes =
    [
        "accepted",
        "rejected",
        "waitlisted",
        "withdrawn",
        "expired",
        "deferred",
    ];

    public RecordAdmissionDecisionValidator()
    {
        RuleFor(x => x.DecisionType)
            .NotEmpty()
            .WithMessage("Decision type is required.")
            .Must(type => type != null && _validDecisionTypes.Contains(type.Trim().ToLowerInvariant()))
            .WithMessage($"Decision type must be one of: {string.Join(", ", _validDecisionTypes)}.");

        RuleFor(x => x.DecisionDate)
            .NotEmpty()
            .WithMessage("Decision date is required.");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty()
            .WithMessage("Effective date is required.");

        RuleFor(x => x.SupportingReference)
            .MaximumLength(200)
            .WithMessage("Supporting reference must not exceed 200 characters.");

        RuleFor(x => x.DeferredAdmissionPeriodId)
            .NotEmpty()
            .WithMessage("Deferred admission period is required when decision type is 'deferred'.")
            .When(x => string.Equals(x.DecisionType?.Trim(), "deferred", StringComparison.OrdinalIgnoreCase));
    }
}
