namespace Elsasa.Sis.Academics.Features.AssessmentEvidence.RecordEvidence;

using FluentValidation;

/// <summary>
/// Validator for <see cref="RecordEvidenceRequest"/> (ACD-WF-08, ACD-WF-20, UC-ACD-07, GAP-07, P3-S06).
/// </summary>
public sealed class RecordEvidenceValidator : AbstractValidator<RecordEvidenceRequest>
{
    private static readonly string[] _allowedTypes =
    [
        "test_paper",
        "oral_response",
        "project_artifact",
        "portfolio_item",
        "observation",
        "practical_demonstration",
        "other"
    ];

    public RecordEvidenceValidator()
    {
        RuleFor(x => x)
            .Must(x =>
            {
                var hasStudent = x.StudentId.HasValue && x.StudentId.Value != Guid.Empty;
                var hasApplicant = x.ApplicantId.HasValue && x.ApplicantId.Value != Guid.Empty;
                return hasStudent ^ hasApplicant;
            })
            .WithMessage("Exactly one of student_id or applicant_id must be populated.");

        RuleFor(x => x.EvidenceType)
            .NotEmpty().WithMessage("Evidence type is required.")
            .Must(type => type != null && _allowedTypes.Contains(type.Trim().ToLowerInvariant()))
            .WithMessage($"Evidence type must be one of: {string.Join(", ", _allowedTypes)}.");

        RuleFor(x => x.CollectedDate)
            .NotEmpty().WithMessage("Collected date is required.")
            .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Collected date cannot be in the future.");

        RuleFor(x => x.EvidenceReference)
            .MaximumLength(500).WithMessage("Evidence reference cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.EvidenceReference));

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.SubmissionId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Submission ID cannot be empty if specified.");
    }
}
