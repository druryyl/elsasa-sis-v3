namespace Elsasa.Sis.Academics.Features.AcademicRequirements.CreateAcademicRequirement;

using FluentValidation;

/// <summary>
/// Validator for <see cref="CreateAcademicRequirementRequest"/> (ADR-005, P3-S09).
/// </summary>
public sealed class CreateAcademicRequirementValidator : AbstractValidator<CreateAcademicRequirementRequest>
{
    private static readonly string[] _validScopes = ["program", "level", "learning_unit"];
    private static readonly string[] _validTypes = ["completion", "prerequisite", "credit", "progression", "graduation", "assessment_policy"];

    public CreateAcademicRequirementValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");

        RuleFor(x => x.RequirementScope)
            .NotEmpty().WithMessage("Requirement scope is required.")
            .Must(scope => scope is not null && _validScopes.Contains(scope.Trim().ToLowerInvariant()))
            .WithMessage("Requirement scope must be 'program', 'level', or 'learning_unit'.");

        RuleFor(x => x.RequirementType)
            .NotEmpty().WithMessage("Requirement type is required.")
            .Must(type => type is not null && _validTypes.Contains(type.Trim().ToLowerInvariant()))
            .WithMessage("Requirement type must be 'completion', 'prerequisite', 'credit', 'progression', 'graduation', or 'assessment_policy'.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(4000).WithMessage("Description cannot exceed 4000 characters.");

        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version is required.")
            .MaximumLength(50).WithMessage("Version cannot exceed 50 characters.");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty().WithMessage("Effective date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.EffectiveDate)
            .WithMessage("End date must be greater than or equal to effective date.")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        When(x => string.Equals(x.RequirementScope?.Trim(), "learning_unit", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.LearningUnitId)
                .NotNull().WithMessage("Learning unit ID is required when requirement scope is 'learning_unit'.")
                .Must(id => id.HasValue && id.Value != Guid.Empty)
                .WithMessage("Learning unit ID must not be empty when requirement scope is 'learning_unit'.");
        });

        When(x => string.Equals(x.RequirementScope?.Trim(), "level", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Level)
                .NotEmpty().WithMessage("Level is required when requirement scope is 'level'.")
                .MaximumLength(50).WithMessage("Level cannot exceed 50 characters.");
        });
    }
}
