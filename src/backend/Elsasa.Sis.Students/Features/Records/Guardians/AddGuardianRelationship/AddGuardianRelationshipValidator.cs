namespace Elsasa.Sis.Students.Features.Records.Guardians.AddGuardianRelationship;

using FluentValidation;

/// <summary>
/// Validates input requirements for <see cref="AddGuardianRelationshipRequest"/> (ADR-004, ADR-005).
/// </summary>
public sealed class AddGuardianRelationshipValidator : AbstractValidator<AddGuardianRelationshipRequest>
{
    private const string _phonePattern = @"^[+0-9\s\-()]+$";

    public AddGuardianRelationshipValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Guardian full name is required.")
            .MaximumLength(200).WithMessage("Guardian full name cannot exceed 200 characters.");

        RuleFor(x => x.RelationshipType)
            .NotEmpty().WithMessage("Relationship type is required.")
            .MaximumLength(50).WithMessage("Relationship type cannot exceed 50 characters.")
            .Must(GuardianRelationshipTypes.IsValid)
            .WithMessage("Relationship type must be a valid relationship (mother, father, legal_guardian, grandparent, sibling, other).");

        RuleFor(x => x.Phone)
            .MaximumLength(32).WithMessage("Guardian phone cannot exceed 32 characters.")
            .Matches(_phonePattern).WithMessage("Guardian phone number format is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Email)
            .MaximumLength(254).WithMessage("Guardian email cannot exceed 254 characters.")
            .EmailAddress().WithMessage("Guardian email must be a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.GovernmentIdNumber)
            .MaximumLength(64).WithMessage("Government ID number cannot exceed 64 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.GovernmentIdNumber));

        RuleFor(x => x.GovernmentIdType)
            .MaximumLength(32).WithMessage("Government ID type cannot exceed 32 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.GovernmentIdType));

        RuleFor(x => x.Occupation)
            .MaximumLength(100).WithMessage("Guardian occupation cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Occupation));
    }
}
