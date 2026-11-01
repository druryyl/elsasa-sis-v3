namespace Elsasa.Sis.Contracts.Finance;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Synchronous cross-domain query contract for the Finance domain (ADR-003).
/// </summary>
public interface IFinanceModule
{
    /// <summary>
    /// Checks whether the mandatory enrollment fee has been confirmed paid for an applicant in a given academic year.
    /// </summary>
    /// <param name="applicantId">The unique identifier of the applicant.</param>
    /// <param name="academicYearId">The unique identifier of the academic year.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing <see langword="true"/> if the fee has been paid; otherwise <see langword="false"/>.</returns>
    Task<Result<bool>> IsEnrollmentFeePaidAsync(Guid applicantId, Guid academicYearId, CancellationToken ct = default);
}
