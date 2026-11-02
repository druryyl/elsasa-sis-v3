namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolCalendars.CreateSchoolCalendar;

using FluentValidation;

/// <summary>
/// Validates <see cref="CreateSchoolCalendarRequest"/> payload rules before processing (ADR-005).
/// </summary>
public sealed class CreateSchoolCalendarValidator : AbstractValidator<CreateSchoolCalendarRequest>
{
    public CreateSchoolCalendarValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.ScopeType)
            .MaximumLength(32).WithMessage("ScopeType cannot exceed 32 characters.")
            .When(x => !string.IsNullOrEmpty(x.ScopeType));

        RuleFor(x => x.ScopeReference)
            .MaximumLength(100).WithMessage("ScopeReference cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.ScopeReference));

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
