namespace Elsasa.Sis.Teaching.Features.TeachingCapabilities.AddTeachingCapability;

using FluentValidation;

/// <summary>
/// Validator for AddTeachingCapabilityRequest (P2-S08).
/// </summary>
public sealed class AddTeachingCapabilityValidator : AbstractValidator<AddTeachingCapabilityRequest>
{
    public AddTeachingCapabilityValidator()
    {
        RuleFor(x => x.CapabilityTypeId)
            .NotEmpty().WithMessage("Capability type ID is required.");

        RuleFor(x => x.CapabilityCode)
            .NotEmpty().WithMessage("Capability code is required.")
            .MaximumLength(100).WithMessage("Capability code must not exceed 100 characters.");

        RuleFor(x => x.CapabilityName)
            .NotEmpty().WithMessage("Capability name is required.")
            .MaximumLength(200).WithMessage("Capability name must not exceed 200 characters.");

        RuleFor(x => x.ApplicableLevel)
            .MaximumLength(100).WithMessage("Applicable level must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.ApplicableLevel));

        RuleFor(x => x.DeliveryMode)
            .MaximumLength(50).WithMessage("Delivery mode must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.DeliveryMode));

        RuleFor(x => x.LanguageOfInstruction)
            .MaximumLength(50).WithMessage("Language of instruction must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.LanguageOfInstruction));

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
