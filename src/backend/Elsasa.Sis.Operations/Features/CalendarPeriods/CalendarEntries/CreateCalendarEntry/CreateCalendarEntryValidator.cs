namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CalendarEntries.CreateCalendarEntry;

using FluentValidation;

/// <summary>
/// Validates <see cref="CreateCalendarEntryRequest"/> payload rules before processing (ADR-005).
/// </summary>
public sealed class CreateCalendarEntryValidator : AbstractValidator<CreateCalendarEntryRequest>
{
    public CreateCalendarEntryValidator()
    {
        RuleFor(x => x.EntryDate)
            .NotEmpty().WithMessage("Entry date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.EntryDate)
            .WithMessage("End date must be greater than or equal to entry date.")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.Designation)
            .NotEmpty().WithMessage("Designation is required.")
            .MaximumLength(32).WithMessage("Designation cannot exceed 32 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Description cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.AppliesTo)
            .MaximumLength(100).WithMessage("AppliesTo cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.AppliesTo));

        RuleFor(x => x.AmendmentReason)
            .MaximumLength(2000).WithMessage("AmendmentReason cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.AmendmentReason));
    }
}
