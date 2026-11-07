namespace Elsasa.Sis.Academics.Features.LearningProgress.RecordLearningProgress;

using FluentValidation;

/// <summary>
/// Validator for learning progress recording command (P2-S04, UC-ACD-12).
/// Enforces categorical progress states, operational basis types, and mandatory notes for authorized decisions.
/// </summary>
public sealed class RecordLearningProgressValidator : AbstractValidator<RecordLearningProgressCommand>
{
    private static readonly string[] _allowedStatuses =
    [
        "not_started",
        "in_progress",
        "completed",
        "needs_remediation",
        "exempted"
    ];

    private static readonly string[] _allowedBases =
    [
        "assessment_evidence",
        "learning_work",
        "authorized_decision"
    ];

    public RecordLearningProgressValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.LearningUnitId)
            .NotEmpty().WithMessage("Learning unit ID is required.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("Academic period ID is required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => !string.IsNullOrWhiteSpace(status) && _allowedStatuses.Contains(status.Trim().ToLowerInvariant()))
            .WithMessage("Status must be one of: not_started, in_progress, completed, needs_remediation, exempted.");

        RuleFor(x => x.Basis)
            .NotEmpty().WithMessage("Basis is required.")
            .Must(basis => !string.IsNullOrWhiteSpace(basis) && _allowedBases.Contains(basis.Trim().ToLowerInvariant()))
            .WithMessage("Basis must be one of: assessment_evidence, learning_work, authorized_decision.");

        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage("Explanatory notes are required when basis is 'authorized_decision'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Basis) && string.Equals(x.Basis.Trim(), "authorized_decision", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
