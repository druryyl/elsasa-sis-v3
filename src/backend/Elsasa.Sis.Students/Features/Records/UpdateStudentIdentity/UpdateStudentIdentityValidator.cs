namespace Elsasa.Sis.Students.Features.Records.UpdateStudentIdentity;

using FluentValidation;

/// <summary>
/// Validates <see cref="UpdateStudentIdentityRequest"/> payload (ADR-005).
/// </summary>
public sealed class UpdateStudentIdentityValidator : AbstractValidator<UpdateStudentIdentityRequest>
{
    public UpdateStudentIdentityValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters.");

        RuleFor(x => x.PreferredName)
            .MaximumLength(100).WithMessage("Preferred name cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.PreferredName));

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Date of birth cannot be in the future.")
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.PlaceOfBirth)
            .MaximumLength(100).WithMessage("Place of birth cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.PlaceOfBirth));

        RuleFor(x => x.Gender)
            .MaximumLength(20).WithMessage("Gender cannot exceed 20 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Gender));

        RuleFor(x => x.Nationality)
            .MaximumLength(100).WithMessage("Nationality cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Nationality));

        RuleFor(x => x.GovernmentIdType)
            .MaximumLength(32).WithMessage("Government ID type cannot exceed 32 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.GovernmentIdType));

        RuleFor(x => x.GovernmentIdNumber)
            .MaximumLength(64).WithMessage("Government ID number cannot exceed 64 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.GovernmentIdNumber));

        RuleFor(x => x.Phone)
            .MaximumLength(32).WithMessage("Phone cannot exceed 32 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Email)
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
