namespace Elsasa.Sis.Contracts.Operations;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Synchronous cross-domain query contract for the Operations domain (ADR-003).
/// </summary>
public interface IOperationsModule
{
    /// <summary>
    /// Checks whether an academic period is active.
    /// </summary>
    /// <param name="periodId">The unique identifier of the academic period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing <see langword="true"/> if the period is active; otherwise <see langword="false"/>.</returns>
    Task<Result<bool>> IsActiveAcademicPeriodAsync(Guid periodId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the unique identifier of the current active academic year, if any.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the active academic year identifier, or <see langword="null"/> if none is active.</returns>
    Task<Result<Guid?>> GetActiveAcademicYearIdAsync(CancellationToken ct = default);
}
