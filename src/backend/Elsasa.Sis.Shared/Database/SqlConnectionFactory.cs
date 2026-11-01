namespace Elsasa.Sis.Shared.Database;

using System.Data.Common;

using Microsoft.Data.SqlClient;

/// <summary>
/// SQL Server implementation of <see cref="ISqlConnectionFactory"/> backed by
/// <see cref="SqlConnection"/>.
/// </summary>
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlConnectionFactory"/> class.
    /// </summary>
    /// <param name="connectionString">SQL Server connection string used for every connection.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="connectionString"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "A non-empty SQL Server connection string is required.",
                nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async ValueTask<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
