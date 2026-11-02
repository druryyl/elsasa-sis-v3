namespace Elsasa.Sis.Contracts.Operations;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Synchronous cross-domain query contract for the Operations domain calendar and periods (ADR-001, ADR-003, TD-03).
/// Allows compile-time isolated synchronous inquiries from Students, Finance, and other modules.
/// </summary>
public interface IOperationsCalendarQueryService
{
    /// <summary>
    /// Retrieves the currently active academic year, if any.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the active academic year DTO, or <see langword="null"/> if none is active.</returns>
    Task<Result<ActiveAcademicYearDto?>> GetActiveAcademicYearAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves an operational period by its unique identifier.
    /// </summary>
    /// <param name="periodId">The unique identifier of the operational period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the operational period DTO, or <see langword="null"/> if not found.</returns>
    Task<Result<OperationalPeriodDto?>> GetOperationalPeriodByIdAsync(Guid periodId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all operational periods of type 'admission_window' that are open on the specified date.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the read-only list of active admission windows.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Parameter name 'date' is specified by ADR-003 and TD-03.")]
    Task<Result<IReadOnlyList<OperationalPeriodDto>>> GetActiveAdmissionWindowsAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Checks whether the specified date is an instructional day for the designated campus.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <param name="campus">The target campus or "All" for institution-wide scope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing <see langword="true"/> if the date is instructional; otherwise <see langword="false"/>.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Parameter name 'date' is specified by ADR-003 and TD-03.")]
    Task<Result<bool>> IsInstructionalDayAsync(DateOnly date, string campus = "All", CancellationToken ct = default);
}

/// <summary>
/// Strongly-typed contract DTO representing an active academic year.
/// </summary>
public sealed record ActiveAcademicYearDto(
    Guid Id,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status);

/// <summary>
/// Strongly-typed contract DTO representing an operational period (e.g. semester, term, admission window).
/// </summary>
public sealed record OperationalPeriodDto(
    Guid Id,
    Guid AcademicYearId,
    string PeriodCode,
    string Name,
    string PeriodType,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ParentPeriodId,
    string Status);
