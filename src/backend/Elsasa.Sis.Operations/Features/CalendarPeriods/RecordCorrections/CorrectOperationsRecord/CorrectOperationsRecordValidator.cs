namespace Elsasa.Sis.Operations.Features.CalendarPeriods.RecordCorrections.CorrectOperationsRecord;

using FluentValidation;

/// <summary>
/// Validator for record correction requests ensuring target table constraints and non-empty justifications (UC-OPS-13).
/// </summary>
public sealed class CorrectOperationsRecordValidator : AbstractValidator<CorrectOperationsRecordRequest>
{
    private static readonly string[] _allowedTables = ["academic_years", "operational_periods"];

    public CorrectOperationsRecordValidator()
    {
        RuleFor(x => x.TargetTableName)
            .NotEmpty().WithMessage("Target table name is required.")
            .Must(t => _allowedTables.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Target table must be academic_years or operational_periods.");

        RuleFor(x => x.TargetRecordId)
            .NotEmpty().WithMessage("Target record ID is required.");

        RuleFor(x => x.FieldName)
            .NotEmpty().WithMessage("Field name is required.")
            .MaximumLength(64).WithMessage("Field name cannot exceed 64 characters.");

        RuleFor(x => x.JustificationReason)
            .NotEmpty().WithMessage("Justification reason is required.")
            .MinimumLength(5).WithMessage("Justification reason must be at least 5 characters.")
            .MaximumLength(1000).WithMessage("Justification reason cannot exceed 1000 characters.");

        RuleFor(x => x.NewValue)
            .MaximumLength(2000).WithMessage("New value cannot exceed 2000 characters.");

        RuleFor(x => x.AuthorizedBy)
            .MaximumLength(100).WithMessage("Authorized by cannot exceed 100 characters.");
    }
}
