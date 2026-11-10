namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.UpdateTeacherProfile;

using FluentValidation;

/// <summary>
/// FluentValidation validator for mutable teacher profile updates (P2-S05).
/// </summary>
public sealed class UpdateTeacherProfileValidator : AbstractValidator<UpdateTeacherProfileRequest>
{
    private const string _phonePattern = @"^[+0-9\s\-()]+$";

    public UpdateTeacherProfileValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(200).WithMessage("Display name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));

        RuleFor(x => x.Email)
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.")
            .Matches(_phonePattern).WithMessage("Phone number format is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.CampusAffiliation)
            .MaximumLength(100).WithMessage("Campus affiliation cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.CampusAffiliation));

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("Department cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Department));
    }
}
