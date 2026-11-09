namespace Elsasa.Sis.Academics.Features.AcademicResults.EvaluateStudentLearning;

using FluentValidation;

/// <summary>
/// Validator for <see cref="EvaluateStudentLearningRequest"/> enforcing candidate exclusivity, evaluation model enumeration,
/// score non-negativity and maximum boundaries (UC-ACD-06, P4-S08).
/// </summary>
public sealed class EvaluateStudentLearningValidator : AbstractValidator<EvaluateStudentLearningRequest>
{
    private static readonly string[] _validEvaluationModels =
    [
        "numeric",
        "letter_grade",
        "pass_fail",
        "competency",
        "rubric",
        "narrative",
        "completion"
    ];

    public EvaluateStudentLearningValidator()
    {
        RuleFor(x => x)
            .Must(x => (x.StudentId.HasValue && !x.ApplicantId.HasValue) || (!x.StudentId.HasValue && x.ApplicantId.HasValue))
            .WithMessage("Exactly one candidate reference (student_id XOR applicant_id) must be provided.");

        When(x => x.StudentId.HasValue, () =>
        {
            RuleFor(x => x.StudentId!.Value)
                .NotEmpty()
                .WithMessage("student_id must not be empty GUID.");
        });

        When(x => x.ApplicantId.HasValue, () =>
        {
            RuleFor(x => x.ApplicantId!.Value)
                .NotEmpty()
                .WithMessage("applicant_id must not be empty GUID.");
        });

        RuleFor(x => x.EvaluationModel)
            .NotEmpty()
            .WithMessage("evaluation_model is required.")
            .Must(m => m != null && _validEvaluationModels.Contains(m.Trim().ToLowerInvariant()))
            .WithMessage($"evaluation_model must be one of: {string.Join(", ", _validEvaluationModels)}");

        When(x => x.ScoreNumeric.HasValue, () =>
        {
            RuleFor(x => x.ScoreNumeric!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("score_numeric cannot be negative.");

            When(x => x.ScoreMax.HasValue, () =>
            {
                RuleFor(x => x.ScoreNumeric!.Value)
                    .LessThanOrEqualTo(x => x.ScoreMax!.Value)
                    .WithMessage("score_numeric cannot exceed score_max.");
            });
        });

        When(x => x.ScoreMax.HasValue, () =>
        {
            RuleFor(x => x.ScoreMax!.Value)
                .GreaterThan(0)
                .WithMessage("score_max must be greater than zero.");
        });

        When(x => !string.IsNullOrEmpty(x.GradeLetter), () =>
        {
            RuleFor(x => x.GradeLetter!)
                .MaximumLength(10)
                .WithMessage("grade_letter cannot exceed 10 characters.");
        });

        When(x => !string.IsNullOrEmpty(x.GradeLabel), () =>
        {
            RuleFor(x => x.GradeLabel!)
                .MaximumLength(100)
                .WithMessage("grade_label cannot exceed 100 characters.");
        });
    }
}
