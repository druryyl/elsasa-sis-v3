namespace Elsasa.Sis.Students.Features.Records.Placements.ChangeAdministrativePlacement;

using FluentValidation;

/// <summary>
/// Validates <see cref="ChangeAdministrativePlacementRequest"/> payloads (ADR-005, TD-04).
/// </summary>
public sealed class ChangeAdministrativePlacementValidator : AbstractValidator<ChangeAdministrativePlacementRequest>
{
    public ChangeAdministrativePlacementValidator()
    {
        RuleFor(x => x.Campus)
            .NotEmpty().WithMessage("Campus is required.")
            .MaximumLength(100).WithMessage("Campus cannot exceed 100 characters.");

        RuleFor(x => x.SchoolLevel)
            .NotEmpty().WithMessage("School level is required.")
            .MaximumLength(64).WithMessage("School level cannot exceed 64 characters.");

        RuleFor(x => x.Grade)
            .NotEmpty().WithMessage("Grade is required.")
            .MaximumLength(64).WithMessage("Grade cannot exceed 64 characters.");

        RuleFor(x => x.AcademicYear)
            .NotEmpty().WithMessage("Academic year is required.")
            .MaximumLength(32).WithMessage("Academic year cannot exceed 32 characters.");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required.")
            .NotEqual(default(DateOnly)).WithMessage("Effective date must be a valid date.");

        RuleFor(x => x.Program)
            .MaximumLength(100).WithMessage("Program cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Program));

        RuleFor(x => x.Stream)
            .MaximumLength(100).WithMessage("Stream cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Stream));

        RuleFor(x => x.Cohort)
            .MaximumLength(50).WithMessage("Cohort cannot exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cohort));

        RuleFor(x => x.Homeroom)
            .MaximumLength(100).WithMessage("Homeroom cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Homeroom));

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));

        RuleFor(x => x.AuthorizedBy)
            .MaximumLength(100).WithMessage("Authorized by cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.AuthorizedBy));

        RuleFor(x => x.DecisionReference)
            .MaximumLength(100).WithMessage("Decision reference cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.DecisionReference));
    }
}
