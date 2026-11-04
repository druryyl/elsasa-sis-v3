namespace Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;

using FluentValidation;

/// <summary>
/// Validator for charge rule creation command enforcing IDR currency, positive amount, and valid due rule definitions.
/// </summary>
public sealed class CreateChargeRuleValidator : AbstractValidator<CreateChargeRuleRequest>
{
    private static readonly string[] _validDueRuleTypes =
    [
        "days_after_issuance",
        "fixed_day_of_month",
        "fixed_calendar_date"
    ];

    public CreateChargeRuleValidator()
    {
        RuleFor(x => x.RuleCode)
            .NotEmpty().WithMessage("Rule code is required.")
            .MaximumLength(64).WithMessage("Rule code must not exceed 64 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.ChargeType)
            .NotEmpty().WithMessage("Charge type is required.")
            .MaximumLength(64).WithMessage("Charge type must not exceed 64 characters.");

        RuleFor(x => x.BaseAmount)
            .GreaterThan(0).WithMessage("BaseAmount must be greater than zero.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Equal("IDR", StringComparer.OrdinalIgnoreCase).WithMessage("CurrencyCode must be 'IDR'.");

        RuleFor(x => x.RecurrencePattern)
            .NotEmpty().WithMessage("Recurrence pattern is required.")
            .MaximumLength(32).WithMessage("Recurrence pattern must not exceed 32 characters.");

        RuleFor(x => x.DueRuleType)
            .NotEmpty().WithMessage("Due rule type is required.")
            .Must(type => _validDueRuleTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("DueRuleType must be one of: 'days_after_issuance', 'fixed_day_of_month', 'fixed_calendar_date'.");

        RuleFor(x => x.DueRuleValue)
            .NotEmpty().WithMessage("Due rule value is required.")
            .DependentRules(() =>
            {
                RuleFor(x => x.DueRuleValue)
                    .Must(val => int.TryParse(val, out var days) && days > 0)
                    .When(x => string.Equals(x.DueRuleType, "days_after_issuance", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("DueRuleValue for 'days_after_issuance' must be a positive integer representing days.");

                RuleFor(x => x.DueRuleValue)
                    .Must(val => int.TryParse(val, out var day) && day is >= 1 and <= 31)
                    .When(x => string.Equals(x.DueRuleType, "fixed_day_of_month", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("DueRuleValue for 'fixed_day_of_month' must be an integer between 1 and 31.");

                RuleFor(x => x.DueRuleValue)
                    .Must(val => DateOnly.TryParse(val, out _))
                    .When(x => string.Equals(x.DueRuleType, "fixed_calendar_date", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("DueRuleValue for 'fixed_calendar_date' must be a valid date format (YYYY-MM-DD).");
            });

        RuleFor(x => x.ApplicableGrade)
            .MaximumLength(64).WithMessage("Applicable grade must not exceed 64 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ApplicableGrade));

        RuleFor(x => x.ApplicableProgram)
            .MaximumLength(100).WithMessage("Applicable program must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ApplicableProgram));
    }
}
