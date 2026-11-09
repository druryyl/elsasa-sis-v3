namespace Elsasa.Sis.Contracts.DemoData;

using System.Text.Json;

/// <summary>
/// Immutable context transfer records threading data between domain seeders.
/// All records are C# 12 immutable records (ADR-009 nullability compliant).
/// </summary>

/// <summary>
/// Result of seeding the Operations domain calendar structure.
/// </summary>
public sealed record OperationsSeedResult(
    Guid AcademicYearId,
    string AcademicYearCode,
    Guid HistoricalPeriodId,
    Guid ActivePeriodId,
    Guid FuturePeriodId,
    Guid SchoolCalendarId,
    DateTime BaseDate);

/// <summary>
/// Result of seeding the Academics domain curriculum structure.
/// </summary>
public sealed record AcademicsStructureSeedResult(
    Guid LearningStructureId,
    IReadOnlyDictionary<string, Guid> SubjectUnitIds,
    IReadOnlyList<Guid> PaceUnitIds);

/// <summary>
/// Result of seeding the Finance domain policies and charge rules.
/// </summary>
public sealed record FinancePolicySeedResult(
    Guid PolicyId,
    Guid TuitionChargeRuleId,
    Guid FacilityChargeRuleId,
    Guid PaceMaterialChargeRuleId);

/// <summary>
/// Deterministic flagship persona identifiers returned by the Students seeder.
/// </summary>
public sealed record FlagshipStudentPersonas(
    Guid JonathanStudentId,
    Guid GraceStudentId,
    Guid BudiStudentId,
    IReadOnlyDictionary<string, Guid> StagedApplicantIds);

/// <summary>
/// Result of seeding the Students domain.
/// </summary>
public sealed record StudentsSeedResult(
    IReadOnlyList<Guid> EnrolledStudentIds,
    IReadOnlyDictionary<string, IReadOnlyList<Guid>> CohortStudentIds,
    FlagshipStudentPersonas Personas);

/// <summary>
/// Result of seeding the Finance domain transactions.
/// </summary>
public sealed record FinanceTransactionsSeedResult(
    int AccountsCreated,
    int InvoicesCreated,
    int PaymentsCreated,
    Guid JonathanUnpaidInvoiceId);

/// <summary>
/// Result of seeding the Academics domain activities.
/// </summary>
public sealed record AcademicsActivitiesSeedResult(
    int ParticipationsCreated,
    int AttendanceRecordsCreated,
    int ResultsCreated,
    int ReportCardsCreated,
    int TranscriptsCreated);

/// <summary>
/// Summary of a complete demo data reset operation.
/// </summary>
public sealed record DemoResetSummary(
    int PurgedSchemas,
    int SeededDomains,
    long ElapsedMilliseconds,
    DateTime AnchoredDate,
    string BaseAcademicYearCode,
    int TotalStudentsSeeded,
    int TotalInvoicesSeeded,
    int TotalAttendanceSeeded,
    int TotalResultsSeeded,
    int TotalReportCardsSeeded);
