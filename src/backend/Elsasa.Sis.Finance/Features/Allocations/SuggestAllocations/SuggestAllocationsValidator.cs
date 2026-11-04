namespace Elsasa.Sis.Finance.Features.Allocations.SuggestAllocations;

using FluentValidation;

/// <summary>
/// FluentValidation validator for SuggestAllocationsRequest.
/// </summary>
public sealed class SuggestAllocationsValidator : AbstractValidator<SuggestAllocationsRequest>
{
    public SuggestAllocationsValidator()
    {
        RuleFor(x => x.PaymentAmount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero.");

        RuleFor(x => x)
            .Must(x => (x.StudentId.HasValue && x.StudentId.Value != Guid.Empty) ||
                       (x.ApplicantId.HasValue && x.ApplicantId.Value != Guid.Empty))
            .WithMessage("Either studentId or applicantId must be specified.");
    }
}
