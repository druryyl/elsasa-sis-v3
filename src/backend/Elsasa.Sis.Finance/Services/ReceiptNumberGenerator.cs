namespace Elsasa.Sis.Finance.Services;

using Dapper;
using System.Data;

/// <summary>
/// Concurrency-safe sequential receipt voucher number generator generating REC-{Year}-{Sequence:D6}
/// inside a SQL transaction using WITH (UPDLOCK, HOLDLOCK) to prevent duplicate numbers or gaps during concurrent checkout (TD-04, OQ-004).
/// </summary>
public static class ReceiptNumberGenerator
{
    public const string Prefix = "REC";

    private const string _getNextSequenceSql = """
        SELECT COALESCE(MAX(TRY_CAST(RIGHT(receipt_number, 6) AS INT)), 0) + 1
        FROM finance.payments WITH (UPDLOCK, HOLDLOCK)
        WHERE receipt_number LIKE @PrefixPattern;
        """;

    /// <summary>
    /// Formats a canonical receipt voucher number: REC-{Year}-{Sequence:D6}.
    /// </summary>
    /// <param name="sequence">The positive sequence number.</param>
    /// <param name="year">Optional calendar year, defaulting to UTC current year.</param>
    /// <returns>Formatted canonical receipt number string.</returns>
    public static string Format(int sequence, int? year = null)
    {
        var y = year ?? DateTime.UtcNow.Year;
        return $"{Prefix}-{y}-{sequence:D6}";
    }

    /// <summary>
    /// Generates the next sequential receipt voucher number inside an active SQL transaction using UPDLOCK and HOLDLOCK.
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
