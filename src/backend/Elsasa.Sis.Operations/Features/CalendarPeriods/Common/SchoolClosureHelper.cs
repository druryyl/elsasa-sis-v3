namespace Elsasa.Sis.Operations.Features.CalendarPeriods.Common;

using System.Data;
using System.Globalization;
using Dapper;

/// <summary>
/// Shared helper for generating unique closure tracking numbers (TD-05, UC-OPS-12).
/// </summary>
public static class SchoolClosureHelper
{
    /// <summary>
    /// Generates a unique traceable closure number (e.g., "CLS-2026-001") within an active transaction.
    /// </summary>
    public static async Task<string> GenerateClosureNumberAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        int year,
        CancellationToken cancellationToken)
    {
        var prefix = $"CLS-{year}-";

        const string selectSql = """
            SELECT TOP 1 closure_number
            FROM operations.school_closures WITH (UPDLOCK, HOLDLOCK)
            WHERE closure_number LIKE @Prefix + '%'
            ORDER BY closure_number DESC;
            """;

        var lastNumber = await connection.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
            selectSql,
            new { Prefix = prefix },
            transaction: transaction,
            cancellationToken: cancellationToken));

        var nextSeq = 1;
        if (lastNumber is not null && lastNumber.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var suffix = lastNumber[prefix.Length..];
            if (int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                nextSeq = parsed + 1;
            }
        }

        return $"{prefix}{nextSeq:D3}";
    }
}
