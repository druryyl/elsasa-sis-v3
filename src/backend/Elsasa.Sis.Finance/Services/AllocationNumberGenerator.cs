namespace Elsasa.Sis.Finance.Services;

using Dapper;
using System.Data;

/// <summary>
/// Thread-safe sequential allocation number generator generating ALC-{Year}-{Sequence:D6} (TD-05).
/// </summary>
public static class AllocationNumberGenerator
{
    public const string Prefix = "ALC";

    private const string _getNextSequenceSql = """
        SELECT COALESCE(MAX(TRY_CAST(RIGHT(allocation_number, 6) AS INT)), 0) + 1
        FROM finance.payment_allocations WITH (UPDLOCK, HOLDLOCK)
        WHERE allocation_number LIKE @PrefixPattern;
        """;

    /// <summary>
    /// Formats a canonical allocation number: ALC-{Year}-{Sequence:D6}.
    /// </summary>
    /// <param name="sequence">The positive sequence number.</param>
    /// <param name="year">Optional calendar year, defaulting to UTC current year.</param>
    /// <returns>Formatted canonical allocation number string.</returns>
    public static string Format(int sequence, int? year = null)
    {
        var y = year ?? DateTime.UtcNow.Year;
        return $"{Prefix}-{y}-{sequence:D6}";
    }

    /// <summary>
    /// Generates the next sequential allocation number inside an active or ambient connection/transaction.
    /// </summary>
    public static async Task<string> GenerateAsync(
        IDbConnection connection,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var year = DateTime.UtcNow.Year;
        var prefixPattern = $"{Prefix}-{year}-%";

        var nextSequence = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _getNextSequenceSql,
            new { PrefixPattern = prefixPattern },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (nextSequence <= 0)
        {
            nextSequence = 1;
        }

        return Format(nextSequence, year);
    }
}
