namespace Elsasa.Sis.Operations.Features.CalendarPeriods.Common;

using System.Data;
using System.Globalization;
using Dapper;

/// <summary>
/// Shared helper for version bumping and amendment sequence tracking in the Operations domain (ADR-004, TD-05).
/// </summary>
public static class CalendarAmendmentHelper
{
    /// <summary>
    /// Increments the minor version of a calendar (e.g., "1.0" -> "1.1", "1.9" -> "1.10").
    /// </summary>
    public static string BumpMinorVersion(string? currentVersion)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
        {
            return "1.1";
        }

        var parts = currentVersion.Trim().Split('.');
        if (parts.Length >= 2 &&
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var major) &&
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
        {
            return $"{major}.{minor + 1}";
        }

        if (int.TryParse(currentVersion, NumberStyles.Integer, CultureInfo.InvariantCulture, out var singleMajor))
        {
            return $"{singleMajor}.1";
        }

        return $"{currentVersion}.1";
    }

    /// <summary>
    /// Generates a unique traceable amendment code (e.g., "AMD-CAL-2026-001") within an active transaction.
    /// </summary>
    public static async Task<string> GenerateAmendmentNumberAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        int year,
        CancellationToken cancellationToken)
    {
        var prefix = $"AMD-CAL-{year}-";

        const string selectSql = """
            SELECT TOP 1 amendment_number
            FROM operations.calendar_amendments WITH (UPDLOCK, HOLDLOCK)
            WHERE amendment_number LIKE @Prefix + '%'
            ORDER BY amendment_number DESC;
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
