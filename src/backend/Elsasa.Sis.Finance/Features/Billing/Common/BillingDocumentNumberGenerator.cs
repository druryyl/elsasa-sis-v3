namespace Elsasa.Sis.Finance.Features.Billing.Common;

using Dapper;
using System.Data;

/// <summary>
/// Thread-safe and query-driven sequential billing document number generator.
/// Generates BIL-{Year}-{Sequence:D5} for point-in-time bills and STM-{Year}-{Sequence:D5} for periodic statements.
/// </summary>
public static class BillingDocumentNumberGenerator
{
    private const string _getNextSequenceSql = """
        SELECT COALESCE(MAX(TRY_CAST(RIGHT(document_number, 5) AS INT)), 0) + 1
        FROM finance.billing_documents
        WHERE document_type = @DocumentType AND document_number LIKE @Prefix;
        """;

    public static async Task<string> GenerateAsync(
        string documentType,
        IDbConnection connection,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefixCode = documentType.Equals("statement", StringComparison.OrdinalIgnoreCase) ? "STM" : "BIL";
        var searchPrefix = $"{prefixCode}-{year}-%";

        var nextSequence = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _getNextSequenceSql,
            new { DocumentType = documentType.ToLowerInvariant(), Prefix = searchPrefix },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (nextSequence <= 0)
        {
            nextSequence = 1;
        }

        return $"{prefixCode}-{year}-{nextSequence:D5}";
    }
}
