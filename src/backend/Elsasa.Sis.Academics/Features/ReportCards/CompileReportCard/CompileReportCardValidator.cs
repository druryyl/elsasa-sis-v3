namespace Elsasa.Sis.Academics.Features.ReportCards.CompileReportCard;

using FluentValidation;

/// <summary>
/// Validator for <see cref="CompileReportCardRequest"/> (UC-ACD-13, P5-S11).
/// </summary>
public sealed class CompileReportCardValidator : AbstractValidator<CompileReportCardRequest>
{
    private static readonly string[] _allowedTypes = ["mid_term", "end_of_term"];

    public CompileReportCardValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.AcademicPeriodId)
            .NotEmpty().WithMessage("Academic period ID is required.");

        RuleFor(x => x.AcademicPeriodLabel)
            .NotEmpty().WithMessage("Academic period label is required.")
            .MaximumLength(100).WithMessage("Academic period label cannot exceed 100 characters.");

        RuleFor(x => x.ReportType)
            .NotEmpty().WithMessage("Report type is required.")
            .Must(type => type != null && _allowedTypes.Contains(type.Trim().ToLowerInvariant()))
            .WithMessage($"Report type must be one of: {string.Join(", ", _allowedTypes)}.");

        RuleFor(x => x.TemplateName)
            .NotEmpty().WithMessage("Template name is required.")
            .MaximumLength(100).WithMessage("Template name cannot exceed 100 characters.");

        RuleFor(x => x.NarrativeComments)
            .MaximumLength(4000).WithMessage("Narrative comments cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrEmpty(x.NarrativeComments));
    }
}
