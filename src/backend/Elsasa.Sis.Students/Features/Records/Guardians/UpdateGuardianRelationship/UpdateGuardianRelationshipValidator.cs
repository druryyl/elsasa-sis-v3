namespace Elsasa.Sis.Students.Features.Records.Guardians.UpdateGuardianRelationship;

using FluentValidation;

/// <summary>
/// Validates input requirements for <see cref="UpdateGuardianRelationshipRequest"/> (ADR-004, ADR-005).
/// </summary>
public sealed class UpdateGuardianRelationshipValidator : AbstractValidator<UpdateGuardianRelationshipRequest>
{
    private const string _phonePattern = @"^[+0-9\s\-()]+$";

    public UpdateGuardianRelationshipValidator()
    {
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

        RuleFor(x => x.Occupation)
            .MaximumLength(100).WithMessage("Guardian occupation cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Occupation));
    }
}
