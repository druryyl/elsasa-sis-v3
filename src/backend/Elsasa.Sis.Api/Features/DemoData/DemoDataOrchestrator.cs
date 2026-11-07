namespace Elsasa.Sis.Api.Features.DemoData;

using System.Diagnostics;
using Elsasa.Sis.Contracts.DemoData;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Master orchestrator for demo data lifecycle operations (ADR-001, ADR-002, ADR-006, TD-01, TD-02, TD-08, OQ-004).
/// Coordinates reverse-dependency purges (Academics → Finance → Students → Operations)
/// and forward topological seeding, guarded by an asynchronous semaphore and
/// production environment check.
/// </summary>
public sealed partial class DemoDataOrchestrator : IDisposable
{
    private readonly ILogger<DemoDataOrchestrator> _logger;
    private readonly IHostEnvironment _environment;
    private readonly DemoDataOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    // Domain purgers (reverse dependency order: Academics → Finance → Students → Operations)
    private readonly IAcademicsDemoDataPurger _academicsPurger;
    private readonly IFinanceDemoDataPurger _financePurger;
    private readonly IStudentsDemoDataPurger _studentsPurger;
    private readonly IOperationsDemoDataPurger _operationsPurger;

    // Domain seeders (forward topological order)
    private readonly IOperationsDemoDataSeeder _operationsSeeder;
    private readonly IAcademicsStructureDemoDataSeeder _academicsStructureSeeder;
    private readonly IFinancePolicyDemoDataSeeder _financePolicySeeder;
    private readonly IStudentsDemoDataSeeder _studentsSeeder;
    private readonly IFinanceTransactionsDemoDataSeeder _financeTransactionsSeeder;
    private readonly IAcademicsActivitiesDemoDataSeeder _academicsActivitiesSeeder;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoDataOrchestrator"/> class.
    /// </summary>
    public DemoDataOrchestrator(
        ILogger<DemoDataOrchestrator> logger,
        IHostEnvironment environment,
        IOptions<DemoDataOptions> options,
        IAcademicsDemoDataPurger academicsPurger,
        IFinanceDemoDataPurger financePurger,
        IStudentsDemoDataPurger studentsPurger,
        IOperationsDemoDataPurger operationsPurger,
        IOperationsDemoDataSeeder operationsSeeder,
        IAcademicsStructureDemoDataSeeder academicsStructureSeeder,
        IFinancePolicyDemoDataSeeder financePolicySeeder,
        IStudentsDemoDataSeeder studentsSeeder,
        IFinanceTransactionsDemoDataSeeder financeTransactionsSeeder,
        IAcademicsActivitiesDemoDataSeeder academicsActivitiesSeeder)
    {
        _logger = logger;
        _environment = environment;
        _options = options.Value;
        _academicsPurger = academicsPurger;
        _financePurger = financePurger;
        _studentsPurger = studentsPurger;
        _operationsPurger = operationsPurger;
        _operationsSeeder = operationsSeeder;
        _academicsStructureSeeder = academicsStructureSeeder;
        _financePolicySeeder = financePolicySeeder;
        _studentsSeeder = studentsSeeder;
        _financeTransactionsSeeder = financeTransactionsSeeder;
        _academicsActivitiesSeeder = academicsActivitiesSeeder;
    }

    /// <summary>
    /// Executes the complete demo data reset and seed cycle (ADR-001, ADR-002, TD-01, TD-08).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of the demo data reset operation.</returns>
    public Task<DemoResetSummary> ResetAndSeedAsync(CancellationToken cancellationToken = default)
        => ExecuteResetAsync(cleanOnly: false, cancellationToken: cancellationToken);

    /// <summary>
    /// Executes the complete demo data reset operation: purge all domains in reverse dependency order,
    /// then seed in forward topological order.
    /// </summary>
    /// <param name="cleanOnly">If true, only performs the purge phase and skips seeding.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of the demo data reset operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if running in Production environment or if DemoData is disabled in configuration.
    /// </exception>
    public async Task<DemoResetSummary> ExecuteResetAsync(
        bool cleanOnly = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Production safeguard (ADR-006, OQ-004)
        if (_environment.IsProduction())
        {
            LogProductionBlock(_logger);
            throw new InvalidOperationException("Demo data operations are prohibited in Production environment.");
        }

        // Configuration check (ADR-006)
        if (!_options.Enabled)
        {
            LogDisabled(_logger);
            throw new InvalidOperationException("Demo data operations are disabled via configuration (DemoData:Enabled = false).");
        }

        // Serialize execution (TD-08)
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            LogStarting(_logger, cleanOnly);

            var today = DateTime.UtcNow.Date;

            // ─── REVERSE PURGE PHASE (Academics → Finance → Students → Operations) ───
            var purgeStopwatch = Stopwatch.StartNew();
            int purgedSchemas = 0;

            // 1. Academics (leaf domain - no downstream dependencies)
            LogPurgingAcademics(_logger);
            await _academicsPurger.PurgeDomainDataAsync(cancellationToken);
            purgedSchemas++;
            LogPurgedAcademics(_logger, purgeStopwatch.ElapsedMilliseconds);

            // 2. Finance
            LogPurgingFinance(_logger);
            await _financePurger.PurgeDomainDataAsync(cancellationToken);
            purgedSchemas++;
            LogPurgedFinance(_logger, purgeStopwatch.ElapsedMilliseconds);

            // 3. Students
            LogPurgingStudents(_logger);
            await _studentsPurger.PurgeDomainDataAsync(cancellationToken);
            purgedSchemas++;
            LogPurgedStudents(_logger, purgeStopwatch.ElapsedMilliseconds);

            // 4. Operations (root domain - no upstream dependencies)
            LogPurgingOperations(_logger);
            await _operationsPurger.PurgeDomainDataAsync(cancellationToken);
            purgedSchemas++;
            LogPurgedOperations(_logger, purgeStopwatch.ElapsedMilliseconds);

            LogPurgeComplete(_logger, purgedSchemas, purgeStopwatch.ElapsedMilliseconds);

            // If clean-only flag is set, stop after purge
            if (cleanOnly)
            {
                stopwatch.Stop();
                LogCleanOnlySkipSeed(_logger, stopwatch.ElapsedMilliseconds);

                return new DemoResetSummary(
                    PurgedSchemas: purgedSchemas,
                    SeededDomains: 0,
                    ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                    AnchoredDate: today,
                    BaseAcademicYearCode: "N/A",
                    TotalStudentsSeeded: 0,
                    TotalInvoicesSeeded: 0,
                    TotalAttendanceSeeded: 0,
                    TotalResultsSeeded: 0,
                    TotalReportCardsSeeded: 0);
            }

            // ─── FORWARD TOPOLOGICAL SEED PHASE ───
            var seedStopwatch = Stopwatch.StartNew();
            int seededDomains = 0;

            // 1. OperationsSeeder.SeedOperationsAsync(Today) → OperationsSeedResult
            LogSeedingOperations(_logger);
            var operationsResult = await _operationsSeeder.SeedOperationsAsync(today, cancellationToken);
            seededDomains++;
            LogOperationsSeeded(_logger, operationsResult.AcademicYearCode, operationsResult.ActivePeriodId);

            // 2. AcademicsStructureSeeder.SeedAcademicsStructureAsync(OperationsSeedResult) → AcademicsStructureSeedResult
            LogSeedingAcademicsStructure(_logger);
            var academicsStructureResult = await _academicsStructureSeeder.SeedAcademicsStructureAsync(operationsResult, cancellationToken);
            seededDomains++;
            LogAcademicsStructureSeeded(_logger, academicsStructureResult.LearningStructureId, academicsStructureResult.SubjectUnitIds.Count, academicsStructureResult.PaceUnitIds.Count);

            // 3. FinancePolicySeeder.SeedFinancePoliciesAsync(OperationsSeedResult) → FinancePolicySeedResult
            LogSeedingFinancePolicies(_logger);
            var financePolicyResult = await _financePolicySeeder.SeedFinancePoliciesAsync(operationsResult, cancellationToken);
            seededDomains++;
            LogFinancePoliciesSeeded(_logger, financePolicyResult.PolicyId, financePolicyResult.TuitionChargeRuleId, financePolicyResult.FacilityChargeRuleId, financePolicyResult.PaceMaterialChargeRuleId);

            // 4. StudentsSeeder.SeedStudentsAsync(OperationsSeedResult, AcademicsStructureSeedResult, FinancePolicySeedResult) → StudentsSeedResult
            LogSeedingStudents(_logger);
            var studentsResult = await _studentsSeeder.SeedStudentsAsync(
                operationsResult,
                academicsStructureResult,
                financePolicyResult,
                cancellationToken);
            seededDomains++;
            LogStudentsSeeded(_logger, studentsResult.EnrolledStudentIds.Count,
                studentsResult.CohortStudentIds.GetValueOrDefault("ELC")?.Count ?? 0,
                studentsResult.CohortStudentIds.GetValueOrDefault("JHLC")?.Count ?? 0,
                studentsResult.CohortStudentIds.GetValueOrDefault("HSLC")?.Count ?? 0,
                studentsResult.Personas.StagedApplicantIds.Count);

            // 5. FinanceTransactionsSeeder.SeedFinanceTransactionsAsync(OperationsSeedResult, FinancePolicySeedResult, StudentsSeedResult) → FinanceTransactionsSeedResult
            LogSeedingFinanceTransactions(_logger);
            var financeTransactionsResult = await _financeTransactionsSeeder.SeedFinanceTransactionsAsync(
                operationsResult,
                financePolicyResult,
                studentsResult,
                cancellationToken);
            seededDomains++;
            LogFinanceTransactionsSeeded(_logger, financeTransactionsResult.AccountsCreated, financeTransactionsResult.InvoicesCreated, financeTransactionsResult.PaymentsCreated, financeTransactionsResult.JonathanUnpaidInvoiceId);

            // 6. AcademicsActivitiesSeeder.SeedAcademicsActivitiesAsync(OperationsSeedResult, AcademicsStructureSeedResult, StudentsSeedResult) → AcademicsActivitiesSeedResult
            LogSeedingAcademicsActivities(_logger);
            var academicsActivitiesResult = await _academicsActivitiesSeeder.SeedAcademicsActivitiesAsync(
                operationsResult,
                academicsStructureResult,
                studentsResult,
                cancellationToken);
            seededDomains++;
            LogAcademicsActivitiesSeeded(_logger, academicsActivitiesResult.ParticipationsCreated, academicsActivitiesResult.AttendanceRecordsCreated, academicsActivitiesResult.ResultsCreated, academicsActivitiesResult.ReportCardsCreated, academicsActivitiesResult.TranscriptsCreated);

            LogSeedPhaseComplete(_logger, seededDomains, seedStopwatch.ElapsedMilliseconds);

            stopwatch.Stop();

            // Build summary
            var summary = new DemoResetSummary(
                PurgedSchemas: purgedSchemas,
                SeededDomains: seededDomains,
                ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
                AnchoredDate: today,
                BaseAcademicYearCode: operationsResult.AcademicYearCode,
                TotalStudentsSeeded: studentsResult.EnrolledStudentIds.Count,
                TotalInvoicesSeeded: financeTransactionsResult.InvoicesCreated,
                TotalAttendanceSeeded: academicsActivitiesResult.AttendanceRecordsCreated,
                TotalResultsSeeded: academicsActivitiesResult.ResultsCreated,
                TotalReportCardsSeeded: academicsActivitiesResult.ReportCardsCreated);

            LogCompleted(_logger, stopwatch.ElapsedMilliseconds, summary);

            return summary;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _semaphore.Dispose();
        _disposed = true;
    }

    // ─────────────────────────────────────────────────────────────────────
    // LoggerMessage delegates (CA1848 compliant)
    // ─────────────────────────────────────────────────────────────────────

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Demo data operations are prohibited in Production environment.")]
    private static partial void LogProductionBlock(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Demo data operations are disabled via configuration (DemoData:Enabled = false).")]
    private static partial void LogDisabled(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Starting demo data reset operation (CleanOnly: {CleanOnly})")]
    private static partial void LogStarting(ILogger logger, bool cleanOnly);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Purging Academics domain demo data...")]
    private static partial void LogPurgingAcademics(ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Academics domain purged in {ElapsedMs}ms")]
    private static partial void LogPurgedAcademics(ILogger logger, long elapsedMs);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Purging Finance domain demo data...")]
    private static partial void LogPurgingFinance(ILogger logger);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Finance domain purged in {ElapsedMs}ms")]
    private static partial void LogPurgedFinance(ILogger logger, long elapsedMs);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Purging Students domain demo data...")]
    private static partial void LogPurgingStudents(ILogger logger);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Students domain purged in {ElapsedMs}ms")]
    private static partial void LogPurgedStudents(ILogger logger, long elapsedMs);

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Purging Operations domain demo data...")]
    private static partial void LogPurgingOperations(ILogger logger);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Operations domain purged in {ElapsedMs}ms")]
    private static partial void LogPurgedOperations(ILogger logger, long elapsedMs);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "Reverse purge phase completed: {PurgedSchemas} schemas purged in {TotalMs}ms")]
    private static partial void LogPurgeComplete(ILogger logger, int purgedSchemas, long totalMs);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "Clean-only mode: Skipping seed phase. Total duration: {TotalMs}ms")]
    private static partial void LogCleanOnlySkipSeed(ILogger logger, long totalMs);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "Seeding Operations domain (calendar structure)...")]
    private static partial void LogSeedingOperations(ILogger logger);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "Operations seeded: AcademicYear={AcademicYearCode}, ActivePeriod={ActivePeriodId}")]
    private static partial void LogOperationsSeeded(ILogger logger, string academicYearCode, Guid activePeriodId);

    [LoggerMessage(EventId = 16, Level = LogLevel.Information, Message = "Seeding Academics structure (curriculum)...")]
    private static partial void LogSeedingAcademicsStructure(ILogger logger);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = "Academics structure seeded: LearningStructure={LearningStructureId}, Subjects={SubjectCount}, PACE Units={PaceCount}")]
    private static partial void LogAcademicsStructureSeeded(ILogger logger, Guid learningStructureId, int subjectCount, int paceCount);

    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = "Seeding Finance policies (fees & charge rules)...")]
    private static partial void LogSeedingFinancePolicies(ILogger logger);

    [LoggerMessage(EventId = 19, Level = LogLevel.Information, Message = "Finance policies seeded: Policy={PolicyId}, TuitionRule={TuitionRuleId}, FacilityRule={FacilityRuleId}, PaceRule={PaceRuleId}")]
    private static partial void LogFinancePoliciesSeeded(ILogger logger, Guid policyId, Guid tuitionRuleId, Guid facilityRuleId, Guid paceRuleId);

    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "Seeding Students domain (applicants, enrolled, guardians, placements)...")]
    private static partial void LogSeedingStudents(ILogger logger);

    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = "Students seeded: Enrolled={EnrolledCount}, ELC={ELCCount}, JHLC={JHLCCount}, HSLC={HSLCCount}, Applicants={ApplicantCount}")]
    private static partial void LogStudentsSeeded(ILogger logger, int enrolledCount, int elcCount, int jhlcCount, int hslcCount, int applicantCount);

    [LoggerMessage(EventId = 22, Level = LogLevel.Information, Message = "Seeding Finance transactions (accounts, obligations, billing, payments)...")]
    private static partial void LogSeedingFinanceTransactions(ILogger logger);

    [LoggerMessage(EventId = 23, Level = LogLevel.Information, Message = "Finance transactions seeded: Accounts={Accounts}, Invoices={Invoices}, Payments={Payments}, JonathanInvoice={JonathanInvoiceId}")]
    private static partial void LogFinanceTransactionsSeeded(ILogger logger, int accounts, int invoices, int payments, Guid jonathanInvoiceId);

    [LoggerMessage(EventId = 24, Level = LogLevel.Information, Message = "Seeding Academics activities (participations, attendance, results, report cards, transcripts)...")]
    private static partial void LogSeedingAcademicsActivities(ILogger logger);

    [LoggerMessage(EventId = 25, Level = LogLevel.Information, Message = "Academics activities seeded: Participations={Participations}, Attendance={Attendance}, Results={Results}, ReportCards={ReportCards}, Transcripts={Transcripts}")]
    private static partial void LogAcademicsActivitiesSeeded(ILogger logger, int participations, int attendance, int results, int reportCards, int transcripts);

    [LoggerMessage(EventId = 26, Level = LogLevel.Information, Message = "Forward seed phase completed: {SeededDomains} domains seeded in {TotalMs}ms")]
    private static partial void LogSeedPhaseComplete(ILogger logger, int seededDomains, long totalMs);

    [LoggerMessage(EventId = 27, Level = LogLevel.Information, Message = "Demo data reset completed successfully in {TotalMs}ms: {Summary}")]
    private static partial void LogCompleted(ILogger logger, long totalMs, DemoResetSummary summary);
}