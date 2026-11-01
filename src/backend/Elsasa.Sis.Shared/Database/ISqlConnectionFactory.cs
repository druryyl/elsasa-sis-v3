namespace Elsasa.Sis.Shared.Database;

using System.Data.Common;

/// <summary>
/// Creates database connections for a single unit of work.
/// </summary>
/// <remarks>
/// Connections are acquired per operation and disposed by the caller via
/// <c>await using</c> (ADR-004, coding-conventions.md section 6).
/// </remarks>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Creates and opens a new database connection.
    /// </summary>
    /// <param name="cancellationToken">Token that cancels connection establishment.</param>
    /// <returns>An open connection owned by the caller.</returns>
    ValueTask<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
