namespace Elsasa.Sis.Students.Features.Admission.RegisterApplication;

using FluentValidation;

/// <summary>
/// Validates <see cref="RegisterApplicationRequest"/> input requirements prior to command execution (ADR-004, ADR-005).
/// </summary>
public sealed class RegisterApplicationValidator : AbstractValidator<RegisterApplicationRequest>
{
    private const string _phonePattern = @"^[+0-9\s\-()]+$";

    public RegisterApplicationValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Applicant full name is required.")
            .MaximumLength(200).WithMessage("Applicant full name cannot exceed 200 characters.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Date of birth cannot be in the future.");

        RuleFor(x => x.Email)
            .MaximumLength(254).WithMessage("Applicant email cannot exceed 254 characters.")
            .EmailAddress().WithMessage("Applicant email must be a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(32).WithMessage("Applicant phone cannot exceed 32 characters.")
            .Matches(_phonePattern).WithMessage("Applicant phone number format is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.GovernmentIdNumber)
            .MaximumLength(64).WithMessage("Government ID number cannot exceed 64 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.GovernmentIdNumber));

        RuleFor(x => x.GovernmentIdType)
            .MaximumLength(32).WithMessage("Government ID type cannot exceed 32 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.GovernmentIdType));

        RuleFor(x => x.AdmissionPeriodId)
            .NotEmpty().WithMessage("Admission period ID is required.");

        RuleFor(x => x.RequestedEntryLevel)
            .NotEmpty().WithMessage("Requested entry level is required.")
            .MaximumLength(64).WithMessage("Requested entry level cannot exceed 64 characters.");

        RuleFor(x => x.RequestedCampus)
            .MaximumLength(100).WithMessage("Requested campus cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.RequestedCampus));

        RuleFor(x => x.RequestedProgram)
            .MaximumLength(100).WithMessage("Requested program cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.RequestedProgram));

        RuleFor(x => x.ApplicationType)
            .MaximumLength(32).WithMessage("Application type cannot exceed 32 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ApplicationType));

        RuleFor(x => x.Contacts)
            .NotNull().WithMessage("Contacts collection is required.")
            .NotEmpty().WithMessage("At least one contact is required.")
            .Must(c => c != null && c.Any(contact => contact.IsPrimary))
            .WithMessage("At least one primary guardian contact is required.");

        RuleForEach(x => x.Contacts!)
            .SetValidator(new RegisterApplicationContactValidator())
            .When(x => x.Contacts is { Count: > 0 });
    }
}

/// <summary>
/// Validates individual contact details within the application payload.
/// </summary>
public sealed class RegisterApplicationContactValidator : AbstractValidator<RegisterApplicationContactRequest>
{
    private const string _phonePattern = @"^[+0-9\s\-()]+$";

    public RegisterApplicationContactValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Contact full name is required.")
            .MaximumLength(200).WithMessage("Contact full name cannot exceed 200 characters.");

        RuleFor(x => x.Relationship)
            .NotEmpty().WithMessage("Contact relationship is required.")
            .MaximumLength(50).WithMessage("Contact relationship cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .MaximumLength(254).WithMessage("Contact email cannot exceed 254 characters.")
            .EmailAddress().WithMessage("Contact email must be a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(32).WithMessage("Contact phone cannot exceed 32 characters.")
            .Matches(_phonePattern).WithMessage("Contact phone number format is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x)
            .Must(c => !string.IsNullOrWhiteSpace(c.Phone) || !string.IsNullOrWhiteSpace(c.Email))
            .WithMessage("A contact must have either a phone number or an email address.");
    }
}
