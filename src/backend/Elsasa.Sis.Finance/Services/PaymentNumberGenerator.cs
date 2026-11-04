namespace Elsasa.Sis.Finance.Services;

using Dapper;
using System.Data;

/// <summary>
/// Thread-safe sequential payment number generator generating PAY-{Year}-{Sequence:D6}.
/// </summary>
public static class PaymentNumberGenerator
{
    public const string Prefix = "PAY";

    private const string _getNextSequenceSql = """
        SELECT COALESCE(MAX(TRY_CAST(RIGHT(payment_number, 6) AS INT)), 0) + 1
        FROM finance.payments WITH (UPDLOCK, HOLDLOCK)
        WHERE payment_number LIKE @PrefixPattern;
        """;

    /// <summary>
    /// Formats a canonical payment number: PAY-{Year}-{Sequence:D6}.
    /// </summary>
    /// <param name="sequence">The positive sequence number.</param>
    /// <param name="year">Optional calendar year, defaulting to UTC current year.</param>
    /// <returns>Formatted canonical payment number string.</returns>
    public static string Format(int sequence, int? year = null)
    {
        var y = year ?? DateTime.UtcNow.Year;
        return $"{Prefix}-{y}-{sequence:D6}";
    }

    /// <summary>
    /// Generates the next sequential payment number inside an active or ambient connection/transaction.
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
