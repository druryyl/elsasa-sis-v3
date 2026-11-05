namespace Elsasa.Sis.Academics.Features.Attendance.RecordSessionAttendance;

using FluentValidation;

/// <summary>
/// Validator for <see cref="RecordSessionAttendanceRequest"/> (P4-S08, TD-04).
/// Enforces non-empty entries collection, valid attendance status literals, and no duplicate student entries.
/// </summary>
public sealed class RecordSessionAttendanceValidator : AbstractValidator<RecordSessionAttendanceRequest>
{
    private static readonly HashSet<string> _validStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "present",
        "absent",
        "late",
        "excused_absence",
        "partial"
    };

    public RecordSessionAttendanceValidator()
    {
        RuleFor(x => x.Entries)
            .NotNull().WithMessage("Attendance entries are required.")
            .NotEmpty().WithMessage("At least one attendance entry must be provided.");

        RuleFor(x => x.Entries)
            .Must(entries => entries is null || entries.Select(e => e.StudentId).Distinct().Count() == entries.Count)
            .WithMessage("Attendance entries cannot contain duplicate students.")
            .When(x => x.Entries is not null && x.Entries.Count > 0);

        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.StudentId)
                .NotEmpty().WithMessage("Student ID is required.");

            entry.RuleFor(e => e.Status)
                .NotEmpty().WithMessage("Attendance status is required.")
                .Must(s => _validStatuses.Contains(s))
                .WithMessage("Attendance status must be one of: present, absent, late, excused_absence, partial.");

            entry.RuleFor(e => e.Evidence)
                .MaximumLength(4000).WithMessage("Evidence reference cannot exceed 4000 characters.");

            entry.RuleFor(e => e.Notes)
                .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.");
        });
    }
}
