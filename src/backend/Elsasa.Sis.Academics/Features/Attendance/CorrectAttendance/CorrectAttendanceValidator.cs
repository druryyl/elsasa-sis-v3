namespace Elsasa.Sis.Academics.Features.Attendance.CorrectAttendance;

using FluentValidation;

/// <summary>
/// Validator for attendance correction requests enforcing status domains and justification rules (P4-S09, UC-ACD-15).
/// </summary>
public sealed class CorrectAttendanceValidator : AbstractValidator<CorrectAttendanceRequest>
{
    private static readonly string[] _validStatuses = ["present", "absent", "late", "excused_absence", "partial"];

    public CorrectAttendanceValidator()
    {
        RuleFor(x => x.NewStatus)
            .NotEmpty().WithMessage("New attendance status is required.")
            .Must(status => status != null && _validStatuses.Contains(status.Trim().ToLowerInvariant()))
            .WithMessage("Attendance status must be one of: 'present', 'absent', 'late', 'excused_absence', 'partial'.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Correction reason is mandatory.")
            .MinimumLength(5).WithMessage("Correction reason must be at least 5 characters.")
            .MaximumLength(2000).WithMessage("Correction reason must not exceed 2000 characters.");

        RuleFor(x => x.Evidence)
            .MaximumLength(2000).WithMessage("Evidence reference must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Evidence));
    }
}
