namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.RegisterTeacherProfile;

using Dapper;
using Elsasa.Sis.Shared.Database;
using FluentValidation;

/// <summary>
/// FluentValidation validator enforcing candidate intake invariants (P2-S05).
/// </summary>
public sealed class RegisterTeacherProfileValidator : AbstractValidator<RegisterTeacherProfileRequest>
{
    private const string _phonePattern = @"^[+0-9\s\-()]+$";
    private const string _teacherNumberPattern = @"^[A-Za-z0-9\-_]{3,50}$";

    public RegisterTeacherProfileValidator(ISqlConnectionFactory? connectionFactory = null)
    {
        RuleFor(x => x.TeacherNumber)
            .MaximumLength(50).WithMessage("Teacher number cannot exceed 50 characters.")
            .Matches(_teacherNumberPattern).WithMessage("Teacher number format is invalid. Must contain 3 to 50 alphanumeric characters, hyphens, or underscores.")
            .When(x => !string.IsNullOrWhiteSpace(x.TeacherNumber));

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters.");

        RuleFor(x => x.TeachingRoleId)
            .NotEmpty().WithMessage("Teaching role ID is required.");

        if (connectionFactory is not null)
        {
            RuleFor(x => x.TeachingRoleId)
                .MustAsync(async (roleId, ct) =>
                {
                    await using var connection = await connectionFactory.CreateConnectionAsync(ct);
                    return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                        "SELECT TOP 1 1 FROM teaching.teaching_roles WHERE id = @Id AND is_active = 1",
                        new { Id = roleId },
                        cancellationToken: ct));
                })
                .WithMessage("The specified teaching role does not exist or is inactive.")
                .When(x => x.TeachingRoleId != Guid.Empty);
        }

        RuleFor(x => x.CampusAffiliation)
            .NotEmpty().WithMessage("Campus affiliation is required.")
            .MaximumLength(100).WithMessage("Campus affiliation cannot exceed 100 characters.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.")
            .Must(d => d != default).WithMessage("Effective from date must be a valid date.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .WithMessage("Effective to date must be on or after effective from date.")
            .When(x => x.EffectiveTo.HasValue);

        RuleFor(x => x.Email)
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.")
            .Matches(_phonePattern).WithMessage("Phone number format is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.DisplayName)
            .MaximumLength(200).WithMessage("Display name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));

        RuleFor(x => x.EmployeeRef)
            .MaximumLength(100).WithMessage("Employee reference cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.EmployeeRef));

        RuleFor(x => x.NationalIdOrExternalRef)
            .MaximumLength(100).WithMessage("National ID or external reference cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.NationalIdOrExternalRef));

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("Department cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Department));
    }
}
