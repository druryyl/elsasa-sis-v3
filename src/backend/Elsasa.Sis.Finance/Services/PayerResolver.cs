namespace Elsasa.Sis.Finance.Services;

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

/// <summary>
/// Domain service implementing <see cref="IPayerResolver"/> to auto-match and provision payer identities
/// across identification number (NIK/NPWP), contact email, or phone number (GAP-005, OQ-002, TD-03).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class PayerResolver(
    ISqlConnectionFactory connectionFactory,
    ILogger<PayerResolver>? logger = null,
    ICurrentUser? currentUser = null) : IPayerResolver
{
    private const string _selectByIdentificationSql = """
        SELECT TOP (1) id
        FROM finance.payers
        WHERE identification_number = @IdentificationNumber
        ORDER BY created_at ASC;
        """;

    private const string _selectByEmailSql = """
        SELECT TOP (1) id
        FROM finance.payers
        WHERE email = @Email
        ORDER BY created_at ASC;
        """;

    private const string _selectByPhoneSql = """
        SELECT TOP (1) id
        FROM finance.payers
        WHERE phone_number = @PhoneNumber
        ORDER BY created_at ASC;
        """;

    private const string _getNextSequenceSql = """
        SELECT COALESCE(MAX(TRY_CAST(SUBSTRING(payer_number, 10, 10) AS INT)), 0) + 1
        FROM finance.payers WITH (UPDLOCK, HOLDLOCK)
        WHERE payer_number LIKE @Prefix;
        """;

    private const string _insertPayerSql = """
        INSERT INTO finance.payers (
            id,
            payer_number,
            payer_name,
            payer_type,
            identification_number,
            email,
            phone_number,
            created_at,
            created_by
        ) VALUES (
            @Id,
            @PayerNumber,
            @PayerName,
            @PayerType,
            @IdentificationNumber,
            @Email,
            @PhoneNumber,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    /// <inheritdoc />
    public async Task<Guid> ResolveOrCreatePayerAsync(
        string payerName,
        string? identificationNumber,
        string? email,
        string? phone,
        string? payerType,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var trimmedName = string.IsNullOrWhiteSpace(payerName) ? "Unknown Payer" : payerName.Trim();
        var trimmedId = string.IsNullOrWhiteSpace(identificationNumber) ? null : identificationNumber.Trim();
        var trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        var trimmedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        var resolvedPayerType = string.IsNullOrWhiteSpace(payerType) ? "parent_guardian" : payerType.Trim();
        var auditUser = currentUser?.Username ?? "SYSTEM_PAYER_RESOLVER";

        await using var connection = await connectionFactory.CreateConnectionAsync(ct);

        // 1. Sequential matching against finance.payers
        var existingPayerId = await TryMatchPayerAsync(connection, null, trimmedId, trimmedEmail, trimmedPhone, ct);
        if (existingPayerId.HasValue)
        {
            return existingPayerId.Value;
        }

        // 2. Provision new payer with concurrency protection & sequence numbering
        var year = DateTime.UtcNow.Year;
        var searchPrefix = $"{PayerNumberGenerator.Prefix}-{year}-%";

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await using var tx = await connection.BeginTransactionAsync(ct);

                var nextSeq = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    _getNextSequenceSql,
                    new { Prefix = searchPrefix },
                    transaction: tx,
                    cancellationToken: ct));

                if (nextSeq <= 0)
                {
                    nextSeq = 1;
                }

                var payerNumber = PayerNumberGenerator.Format(nextSeq, year);
                var newPayerId = Guid.NewGuid();

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertPayerSql,
                    new
                    {
                        Id = newPayerId,
                        PayerNumber = payerNumber,
                        PayerName = trimmedName,
                        PayerType = resolvedPayerType,
                        IdentificationNumber = trimmedId,
                        Email = trimmedEmail,
                        PhoneNumber = trimmedPhone,
                        CreatedBy = auditUser
                    },
                    transaction: tx,
                    cancellationToken: ct));

                await tx.CommitAsync(ct);

                logger?.LogInformation(
                    "Created new payer {PayerNumber} ({PayerId}) of type {PayerType} for {PayerName}.",
                    payerNumber,
                    newPayerId,
                    resolvedPayerType,
                    trimmedName);

                return newPayerId;
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                logger?.LogWarning(
                    ex,
                    "Concurrent race encountered while creating payer '{PayerName}'. Attempt {Attempt} of 3.",
                    trimmedName,
                    attempt);

                // Check if another concurrent transaction just inserted a matching payer
                var concurrentMatch = await TryMatchPayerAsync(connection, null, trimmedId, trimmedEmail, trimmedPhone, ct);
                if (concurrentMatch.HasValue)
                {
                    logger?.LogInformation(
                        "Resolved concurrently created payer {PayerId} on retry.",
                        concurrentMatch.Value);
                    return concurrentMatch.Value;
                }

                if (attempt == 3)
                {
                    throw;
                }
            }
        }

        throw new InvalidOperationException("Failed to resolve or create payer after maximum retry attempts.");
    }

    private async Task<Guid?> TryMatchPayerAsync(
        DbConnection connection,
        DbTransaction? tx,
        string? identificationNumber,
        string? email,
        string? phone,
        CancellationToken ct)
    {
        // 1. Exact match on identification_number (if non-empty)
        if (identificationNumber is not null)
        {
            var match = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
                _selectByIdentificationSql,
                new { IdentificationNumber = identificationNumber },
                transaction: tx,
                cancellationToken: ct));

            if (match.HasValue && match.Value != Guid.Empty)
            {
                logger?.LogInformation("Matched payer {PayerId} by identification number.", match.Value);
                return match.Value;
            }
        }

        // 2. Exact match on email (if non-empty)
        if (email is not null)
        {
            var match = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
                _selectByEmailSql,
                new { Email = email },
                transaction: tx,
                cancellationToken: ct));

            if (match.HasValue && match.Value != Guid.Empty)
            {
                logger?.LogInformation("Matched payer {PayerId} by email.", match.Value);
                return match.Value;
            }
        }

        // 3. Exact match on phone_number (if non-empty)
        if (phone is not null)
        {
            var match = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(
                _selectByPhoneSql,
                new { PhoneNumber = phone },
                transaction: tx,
                cancellationToken: ct));

            if (match.HasValue && match.Value != Guid.Empty)
            {
                logger?.LogInformation("Matched payer {PayerId} by phone number.", match.Value);
                return match.Value;
            }
        }

        return null;
    }
}
