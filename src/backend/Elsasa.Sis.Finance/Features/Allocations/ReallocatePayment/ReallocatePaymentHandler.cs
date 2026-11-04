namespace Elsasa.Sis.Finance.Features.Allocations.ReallocatePayment;

using Dapper;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler for payment allocation correction, reallocation, and reversal with append-only lineage (TD-05, OQ-003).
/// Enforces pessimistic locking, marks prior allocations as superseded (with replacement link) or reversed,
/// inserts new replacement allocations, and recalculates settled/assessed obligation statuses.
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class ReallocatePaymentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser? currentUser = null,
    ILogger<ReallocatePaymentHandler>? logger = null)
{
    static ReallocatePaymentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectPaymentLockSql = """
        SELECT 
            id AS Id,
            payment_number AS PaymentNumber,
            student_account_id AS StudentAccountId,
            payment_amount AS PaymentAmount,
            confirmation_status AS ConfirmationStatus
        FROM finance.payments WITH (UPDLOCK, HOLDLOCK)
        WHERE id = @PaymentId;
        """;

    private const string _selectPriorAllocationsLockSql = """
        SELECT 
            id AS Id,
            allocation_number AS AllocationNumber,
            student_account_id AS StudentAccountId,
            obligation_id AS ObligationId,
            payment_id AS PaymentId,
            allocated_amount AS AllocatedAmount,
            status AS Status
        FROM finance.payment_allocations WITH (UPDLOCK, HOLDLOCK)
        WHERE id IN @PriorAllocationIds AND payment_id = @PaymentId;
        """;

    private const string _selectOtherActiveAllocationsSumSql = """
        SELECT ISNULL(SUM(allocated_amount), 0)
        FROM finance.payment_allocations WITH (UPDLOCK, HOLDLOCK)
        WHERE payment_id = @PaymentId 
          AND status = 'active' 
          AND id NOT IN @PriorAllocationIds;
        """;

    private const string _selectObligationLockSql = """
        SELECT 
            fo.id AS ObligationId,
            fo.obligation_number AS ObligationNumber,
            fo.student_account_id AS StudentAccountId,
            fo.original_amount AS OriginalAmount,
            fo.status AS Status,
            ISNULL(SUM(pa.allocated_amount), 0) AS TotalPaid
        FROM finance.financial_obligations fo WITH (UPDLOCK, HOLDLOCK)
        LEFT JOIN finance.payment_allocations pa WITH (UPDLOCK, HOLDLOCK)
          ON pa.obligation_id = fo.id AND pa.status = 'active' AND pa.id NOT IN @PriorAllocationIds
        WHERE fo.id = @ObligationId
        GROUP BY fo.id, fo.obligation_number, fo.student_account_id, fo.original_amount, fo.status;
        """;

    private const string _insertReplacementAllocationSql = """
        INSERT INTO finance.payment_allocations (
            id,
            allocation_number,
            student_account_id,
            obligation_id,
            source_type,
            payment_id,
            credit_balance_id,
            allocated_amount,
            currency_code,
            allocation_date,
            allocated_by,
            status,
            superseded_by_id,
            reversal_reason,
            notes,
            created_at,
            created_by
        ) VALUES (
            @Id,
            @AllocationNumber,
            @StudentAccountId,
            @ObligationId,
            'payment',
            @PaymentId,
            NULL,
            @AllocatedAmount,
            'IDR',
            @AllocationDate,
            @AllocatedBy,
            'active',
            NULL,
            NULL,
            @Notes,
            @AllocationDate,
            @AllocatedBy
        );
        """;

    private const string _updateSupersededAllocationSql = """
        UPDATE finance.payment_allocations
        SET status = 'superseded',
            superseded_by_id = @SupersededById,
            reversal_reason = @ReversalReason,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @PriorAllocationId;
        """;

    private const string _updateReversedAllocationSql = """
        UPDATE finance.payment_allocations
        SET status = 'reversed',
            superseded_by_id = NULL,
            reversal_reason = @ReversalReason,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @PriorAllocationId;
        """;

    private const string _selectObligationSettlementStatusSql = """
        SELECT 
            fo.id AS ObligationId,
            fo.original_amount AS OriginalAmount,
            ISNULL(SUM(pa.allocated_amount), 0) AS TotalPaid
        FROM finance.financial_obligations fo
        LEFT JOIN finance.payment_allocations pa ON pa.obligation_id = fo.id AND pa.status = 'active'
        WHERE fo.id = @ObligationId
        GROUP BY fo.id, fo.original_amount;
        """;

    private const string _updateObligationStatusSql = """
        UPDATE finance.financial_obligations
        SET status = @NewStatus,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @ObligationId;
        """;

    public async Task<Result<ReallocatePaymentResponse>> HandleAsync(
        ReallocatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditUser = currentUser?.Username ?? "CASHIER_COUNTER";
        var now = DateTimeOffset.UtcNow;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 1. Lock and validate payment
        var payment = await connection.QueryFirstOrDefaultAsync<PaymentLockRow>(new CommandDefinition(
            _selectPaymentLockSql,
            new { PaymentId = request.PaymentId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (payment is null)
        {
            return Result.Failure<ReallocatePaymentResponse>(
                DomainError.NotFound("Finance.Payment.NotFound", $"Payment with ID '{request.PaymentId}' was not found."));
        }

        if (!string.Equals(payment.ConfirmationStatus, "confirmed", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ReallocatePaymentResponse>(
                DomainError.Conflict(
                    "Finance.Allocation.PaymentNotConfirmed",
                    $"Payment '{payment.PaymentNumber}' is not confirmed (status: '{payment.ConfirmationStatus}'). Only confirmed payments can be reallocated."));
        }

        // 2. Lock and validate prior allocations
        var priorAllocations = (await connection.QueryAsync<PriorAllocationLockRow>(new CommandDefinition(
            _selectPriorAllocationsLockSql,
            new { PriorAllocationIds = request.PriorAllocationIds, PaymentId = request.PaymentId },
            transaction: transaction,
            cancellationToken: cancellationToken))).ToList();

        if (priorAllocations.Count != request.PriorAllocationIds.Distinct().Count())
        {
            return Result.Failure<ReallocatePaymentResponse>(
                DomainError.NotFound(
                    "Finance.Allocation.PriorAllocationNotFound",
                    "One or more target prior allocations were not found for this payment."));
        }

        foreach (var prior in priorAllocations)
        {
            if (!string.Equals(prior.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<ReallocatePaymentResponse>(
                    DomainError.Conflict(
                        "Finance.Allocation.PriorNotActive",
                        $"Prior allocation '{prior.AllocationNumber}' is not active (current status: '{prior.Status}'). Only active allocations can be superseded or reversed."));
            }
        }

        // 3. Validate payment funds limit
        var otherActiveAllocated = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            _selectOtherActiveAllocationsSumSql,
            new { PaymentId = request.PaymentId, PriorAllocationIds = request.PriorAllocationIds },
            transaction: transaction,
            cancellationToken: cancellationToken));

        var replacementAllocations = request.ReplacementAllocations ?? [];
        var replacementTotal = replacementAllocations.Sum(r => r.Amount);

        if (otherActiveAllocated + replacementTotal > payment.PaymentAmount)
        {
            return Result.Failure<ReallocatePaymentResponse>(
                DomainError.Conflict(
                    "Finance.Allocation.AmountExceedsPayment",
                    $"Total replacement allocations ({replacementTotal} IDR) plus other active allocations ({otherActiveAllocated} IDR) exceeds gross payment amount ({payment.PaymentAmount} IDR)."));
        }

        // 4. Validate target obligations for replacements
        foreach (var replacement in replacementAllocations)
        {
            var obligation = await connection.QueryFirstOrDefaultAsync<ObligationLockRow>(new CommandDefinition(
                _selectObligationLockSql,
                new { ObligationId = replacement.ObligationId, PriorAllocationIds = request.PriorAllocationIds },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (obligation is null)
            {
                return Result.Failure<ReallocatePaymentResponse>(
                    DomainError.NotFound(
                        "Finance.Allocation.ObligationNotFound",
                        $"Financial obligation with ID '{replacement.ObligationId}' was not found."));
            }

            if (string.Equals(obligation.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<ReallocatePaymentResponse>(
                    DomainError.Conflict(
                        "Finance.Allocation.ObligationCancelled",
                        $"Obligation '{obligation.ObligationNumber}' has been cancelled and cannot accept payment allocations."));
            }

            var outstandingAmount = obligation.OriginalAmount - obligation.TotalPaid;
            if (replacement.Amount > outstandingAmount)
            {
                return Result.Failure<ReallocatePaymentResponse>(
                    DomainError.Conflict(
                        "Finance.Allocation.AmountExceedsOutstanding",
                        $"Replacement allocation amount ({replacement.Amount} IDR) exceeds remaining outstanding amount ({outstandingAmount} IDR) for obligation '{obligation.ObligationNumber}'."));
            }
        }

        // 5. Execute insertions and status transitions
        var replacementResults = new List<ReallocationResultItemDto>();
        var supersededIds = new List<Guid>();
        var reversedIds = new List<Guid>();

        if (replacementAllocations.Count > 0)
        {
            // Insert all replacement allocations
            foreach (var replacement in replacementAllocations)
            {
                var newAllocationId = Guid.NewGuid();
                var newAllocationNumber = await AllocationNumberGenerator.GenerateAsync(connection, transaction, cancellationToken);
                var note = string.IsNullOrWhiteSpace(replacement.Notes) ? request.Notes : replacement.Notes;

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertReplacementAllocationSql,
                    new
                    {
                        Id = newAllocationId,
                        AllocationNumber = newAllocationNumber,
                        StudentAccountId = payment.StudentAccountId,
                        ObligationId = replacement.ObligationId,
                        PaymentId = payment.Id,
                        AllocatedAmount = replacement.Amount,
                        AllocationDate = now,
                        AllocatedBy = auditUser,
                        Notes = note
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                replacementResults.Add(new ReallocationResultItemDto(
                    newAllocationId,
                    newAllocationNumber,
                    replacement.ObligationId,
                    string.Empty, // Populated after query or tracking
                    replacement.Amount,
                    "active",
                    now));
            }

            // Link prior allocations as superseded by the replacement allocation lineage
            var primarySupersededById = replacementResults[0].AllocationId;

            foreach (var prior in priorAllocations)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _updateSupersededAllocationSql,
                    new
                    {
                        PriorAllocationId = prior.Id,
                        SupersededById = primarySupersededById,
                        ReversalReason = request.ReversalReason.Trim(),
                        UpdatedBy = auditUser
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                supersededIds.Add(prior.Id);
            }
        }
        else
        {
            // Reversal without replacement -> mark rows as 'reversed'
            foreach (var prior in priorAllocations)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _updateReversedAllocationSql,
                    new
                    {
                        PriorAllocationId = prior.Id,
                        ReversalReason = request.ReversalReason.Trim(),
                        UpdatedBy = auditUser
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                reversedIds.Add(prior.Id);
            }
        }

        // 6. Recalculate obligation statuses for all affected obligations
        var affectedObligationIds = priorAllocations.Select(p => p.ObligationId)
            .Concat(replacementAllocations.Select(r => r.ObligationId))
            .Distinct()
            .ToList();

        foreach (var obligationId in affectedObligationIds)
        {
            var settlement = await connection.QueryFirstOrDefaultAsync<ObligationSettlementRow>(new CommandDefinition(
                _selectObligationSettlementStatusSql,
                new { ObligationId = obligationId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (settlement is not null)
            {
                var newStatus = settlement.TotalPaid >= settlement.OriginalAmount ? "settled" : "assessed";

                await connection.ExecuteAsync(new CommandDefinition(
                    _updateObligationStatusSql,
                    new
                    {
                        ObligationId = obligationId,
                        NewStatus = newStatus,
                        UpdatedBy = auditUser
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }
        }

        await transaction.CommitAsync(cancellationToken);

        var finalActiveAllocated = otherActiveAllocated + replacementTotal;
        var remainingUnallocated = payment.PaymentAmount - finalActiveAllocated;

        logger?.LogInformation(
            "User {User} reallocated payment {PaymentNumber} ({PaymentId}): {SupersededCount} superseded, {ReversedCount} reversed, {ReplacementCount} created. Reason: {Reason}.",
            auditUser,
            payment.PaymentNumber,
            payment.Id,
            supersededIds.Count,
            reversedIds.Count,
            replacementResults.Count,
            request.ReversalReason);

        var response = new ReallocatePaymentResponse(
            payment.Id,
            payment.PaymentNumber,
            request.ReversalReason.Trim(),
            supersededIds,
            reversedIds,
            payment.PaymentAmount,
            finalActiveAllocated,
            remainingUnallocated,
            replacementResults);

        return Result.Success(response);
    }

    private sealed record PaymentLockRow(
        Guid Id,
        string PaymentNumber,
        Guid StudentAccountId,
        long PaymentAmount,
        string ConfirmationStatus);

    private sealed record PriorAllocationLockRow(
        Guid Id,
        string AllocationNumber,
        Guid StudentAccountId,
        Guid ObligationId,
        Guid PaymentId,
        long AllocatedAmount,
        string Status);

    private sealed record ObligationLockRow(
        Guid ObligationId,
        string ObligationNumber,
        Guid StudentAccountId,
        long OriginalAmount,
        string Status,
        long TotalPaid);

    private sealed record ObligationSettlementRow(
        Guid ObligationId,
        long OriginalAmount,
        long TotalPaid);
}
