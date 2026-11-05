namespace Elsasa.Sis.Academics.Features.ReportCards;

/// <summary>
/// Execution summary of batch cohort report cards compilation (TD-07, UC-ACD-13).
/// </summary>
/// <param name="CompiledCount">Total number of newly compiled draft report cards.</param>
/// <param name="SkippedExistingCount">Total number of students skipped due to existing report cards.</param>
public sealed record BatchCompilationSummaryDto(
    int CompiledCount,
    int SkippedExistingCount);
