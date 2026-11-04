namespace Elsasa.Sis.Finance.Features.Billing.GenerateStatement;

using FluentValidation;

/// <summary>
/// Validator for compile financial statement request.
/// </summary>
public sealed class GenerateStatementValidator : AbstractValidator<GenerateStatementRequest>
{
    public GenerateStatementValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student ID is required.");

        RuleFor(x => x.BillingPeriodStart)
            .LessThanOrEqualTo(x => x.BillingPeriodEnd)
            .WithMessage("BillingPeriodStart must be earlier than or equal to BillingPeriodEnd.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
