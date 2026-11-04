namespace Elsasa.Sis.Academics.Services;

using System.Data;
using Elsasa.Sis.Shared.Common;

/// <summary>
/// Contract for the deep clone-on-supersede curriculum versioning engine (GAP-003, OQ-001, TD-03).
/// Atomically duplicates an active learning structure, all child learning units (preserving 2-level
/// hierarchy via remapped IDs), and academic requirements into a new draft framework.
/// </summary>
public interface ICurriculumCloningService
{
    /// <summary>
    /// Executes a deep clone of the specified predecessor active structure into a new draft structure.
    /// </summary>
    /// <param name="predecessorId">Primary key of the active learning structure to supersede.</param>
    /// <param name="newVersion">New version identifier (for example "2025.1").</param>
    /// <param name="effectiveDate">Operational date on which the cloned version takes effect.</param>
    /// <param name="newPeriodId">Optional updated academic period ID; if null, inherits predecessor period.</param>
    /// <param name="newPeriodLabel">Optional updated academic period label; if null, inherits predecessor label.</param>
    /// <param name="currentUser">Username or identifier of the user executing the supersede.</param>
    /// <param name="connection">Open database connection.</param>
    /// <param name="transaction">Active database transaction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the new cloned learning structure ID on success, or a domain error.</returns>
    Task<Result<Guid>> CloneStructureToDraftAsync(
        Guid predecessorId,
        string newVersion,
        DateOnly effectiveDate,
        Guid? newPeriodId,
        string? newPeriodLabel,
        string currentUser,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken ct = default);
}
