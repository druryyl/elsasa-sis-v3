namespace Elsasa.Sis.Finance.Features.Payers.CreatePayer;

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

/// <summary>
/// Feature handler executing explicit registration of sponsors, embassies, or parent payers (SCR-FIN-02, TD-03).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class CreatePayerHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser? currentUser = null,
    ILogger<CreatePayerHandler>? logger = null)
{
    static CreatePayerHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkDuplicateIdentificationSql = """
        SELECT 1
        FROM finance.payers
        WHERE identification_number = @IdentificationNumber;
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
            billing_address,
            bank_account_name,
            bank_account_number,
            bank_name,
            notes,
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
            @BillingAddress,
            @BankAccountName,
            @BankAccountNumber,
            @BankName,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<CreatePayerResponse>> HandleAsync(
        CreatePayerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmedName = request.Name.Trim();
        var trimmedId = string.IsNullOrWhiteSpace(request.IdentificationNumber) ? null : request.IdentificationNumber.Trim();
        var trimmedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var trimmedPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var resolvedType = string.IsNullOrWhiteSpace(request.Type) ? "parent_guardian" : request.Type.Trim();
        var billingAddress = string.IsNullOrWhiteSpace(request.BillingAddress) ? null : request.BillingAddress.Trim();
        var bankAccountName = string.IsNullOrWhiteSpace(request.BankAccountName) ? null : request.BankAccountName.Trim();
        var bankAccountNumber = string.IsNullOrWhiteSpace(request.BankAccountNumber) ? null : request.BankAccountNumber.Trim();
        var bankName = string.IsNullOrWhiteSpace(request.BankName) ? null : request.BankName.Trim();
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var auditUser = currentUser?.Username ?? "SYSTEM";

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Guard against duplicate identification number if provided
        if (trimmedId is not null)
        {
            var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkDuplicateIdentificationSql,
                new { IdentificationNumber = trimmedId },
                cancellationToken: cancellationToken));

            if (exists)
            {
                return Result.Failure<CreatePayerResponse>(DomainError.Conflict(
                    "Finance.Payer.DuplicateIdentification",
                    $"A payer with identification number '{trimmedId}' already exists."));
            }
        }

        // 2. Provision new payer with sequence generation and concurrency retry
        var year = DateTime.UtcNow.Year;
        var searchPrefix = $"{PayerNumberGenerator.Prefix}-{year}-%";

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var tx = await connection.BeginTransactionAsync(cancellationToken);

                var nextSeq = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    _getNextSequenceSql,
                    new { Prefix = searchPrefix },
                    transaction: tx,
                    cancellationToken: cancellationToken));

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
                        PayerType = resolvedType,
                        IdentificationNumber = trimmedId,
                        Email = trimmedEmail,
                        PhoneNumber = trimmedPhone,
                        BillingAddress = billingAddress,
                        BankAccountName = bankAccountName,
                        BankAccountNumber = bankAccountNumber,
                        BankName = bankName,
                        Notes = notes,
                        CreatedBy = auditUser
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

                await tx.CommitAsync(cancellationToken);

                logger?.LogInformation(
                    "Created payer {PayerNumber} ({PayerId}) of type {PayerType} for '{PayerName}'.",
                    payerNumber,
                    newPayerId,
                    resolvedType,
                    trimmedName);

                return Result.Success(new CreatePayerResponse(
                    newPayerId,
                    payerNumber,
                    trimmedName,
                    resolvedType));
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                logger?.LogWarning(
                    ex,
                    "Concurrent race encountered while creating payer '{PayerName}'. Attempt {Attempt} of 3.",
                    trimmedName,
                    attempt);

                if (attempt == 3)
                {
                    return Result.Failure<CreatePayerResponse>(DomainError.Conflict(
                        "Finance.Payer.ConcurrentConflict",
                        "Unable to allocate a unique payer number due to concurrent activity. Please retry."));
                }
            }
        }

        return Result.Failure<CreatePayerResponse>(DomainError.Failure(
            "Finance.Payer.CreationFailed",
            "Failed to create payer record after multiple attempts."));
    }
}
