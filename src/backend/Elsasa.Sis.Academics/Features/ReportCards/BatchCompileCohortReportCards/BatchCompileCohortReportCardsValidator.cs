namespace Elsasa.Sis.Academics.Features.ReportCards.BatchCompileCohortReportCards;

using FluentValidation;

/// <summary>
/// Validator for <see cref="BatchCompileCohortReportCardsRequest"/> (TD-07, P5-S11).
/// </summary>
public sealed class BatchCompileCohortReportCardsValidator : AbstractValidator<BatchCompileCohortReportCardsRequest>
{
    private static readonly string[] _allowedTypes = ["mid_term", "end_of_term"];

    public BatchCompileCohortReportCardsValidator()
    {
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

        RuleFor(x => x.SectionLabel)
            .MaximumLength(50).WithMessage("Section label cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.SectionLabel));
    }
}
