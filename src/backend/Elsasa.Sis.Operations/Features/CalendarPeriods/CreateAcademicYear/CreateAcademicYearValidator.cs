namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;

using FluentValidation;

/// <summary>
/// Validates <see cref="CreateAcademicYearRequest"/> payload rules before processing (ADR-005).
/// </summary>
public sealed class CreateAcademicYearValidator : AbstractValidator<CreateAcademicYearRequest>
{
    public CreateAcademicYearValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(32).WithMessage("Code cannot exceed 32 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be greater than start date.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
