namespace Elsasa.Sis.Finance.Features.Allocations.AllocatePayment;

using Dapper;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler for atomic payment allocation against obligations (TD-05, OQ-003).
/// Enforces row locking (UPDLOCK, HOLDLOCK), confirms payment eligibility, verifies fund and
/// obligation limits, inserts active allocations, and updates settled obligation statuses.
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class AllocatePaymentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser? currentUser = null,
    ILogger<AllocatePaymentHandler>? logger = null)
{
    static AllocatePaymentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectPaymentLockSql = """
        SELECT 
            id AS Id,
            payment_number AS PaymentNumber,
            student_account_id AS StudentAccountId,
            student_id AS StudentId,
            applicant_id AS ApplicantId,
            payment_amount AS PaymentAmount,
            confirmation_status AS ConfirmationStatus
        FROM finance.payments WITH (UPDLOCK, HOLDLOCK)
        WHERE id = @PaymentId;
        """;

    private const string _selectActiveAllocationsSumSql = """
        SELECT ISNULL(SUM(allocated_amount), 0)
        FROM finance.payment_allocations WITH (UPDLOCK, HOLDLOCK)
        WHERE payment_id = @PaymentId AND status = 'active';
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
          ON pa.obligation_id = fo.id AND pa.status = 'active'
        WHERE fo.id = @ObligationId
        GROUP BY fo.id, fo.obligation_number, fo.student_account_id, fo.original_amount, fo.status;
        """;

    private const string _insertAllocationSql = """
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

    private const string _updateObligationStatusSql = """
        UPDATE finance.financial_obligations
        SET status = @NewStatus,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE id = @ObligationId;
        """;

    public async Task<Result<AllocatePaymentResponse>> HandleAsync(
        AllocatePaymentRequest request,
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
            return Result.Failure<AllocatePaymentResponse>(
                DomainError.NotFound("Finance.Payment.NotFound", $"Payment with ID '{request.PaymentId}' was not found."));
        }

        if (!string.Equals(payment.ConfirmationStatus, "confirmed", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<AllocatePaymentResponse>(
                DomainError.Conflict(
                    "Finance.Allocation.PaymentNotConfirmed",
                    $"Payment '{payment.PaymentNumber}' is not confirmed (status: '{payment.ConfirmationStatus}'). Only confirmed payments can be allocated."));
        }

        // 2. Lock and validate payment available balance
        var existingActiveAllocated = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            _selectActiveAllocationsSumSql,
            new { PaymentId = request.PaymentId },
            transaction: transaction,
            cancellationToken: cancellationToken));

        var requestedTotal = request.Allocations.Sum(a => a.Amount);

        if (existingActiveAllocated + requestedTotal > payment.PaymentAmount)
        {
            return Result.Failure<AllocatePaymentResponse>(
                DomainError.Conflict(
                    "Finance.Allocation.AmountExceedsPayment",
                    $"Total requested allocation ({requestedTotal} IDR) plus existing active allocations ({existingActiveAllocated} IDR) exceeds gross payment amount ({payment.PaymentAmount} IDR)."));
        }

        // 3. For each requested item, lock obligation, validate invariant, and insert allocation
        var results = new List<AllocationResultItemDto>();

        foreach (var item in request.Allocations)
        {
            var obligation = await connection.QueryFirstOrDefaultAsync<ObligationLockRow>(new CommandDefinition(
                _selectObligationLockSql,
                new { ObligationId = item.ObligationId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            if (obligation is null)
            {
                return Result.Failure<AllocatePaymentResponse>(
                    DomainError.NotFound(
                        "Finance.Allocation.ObligationNotFound",
                        $"Financial obligation with ID '{item.ObligationId}' was not found."));
            }

            if (string.Equals(obligation.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<AllocatePaymentResponse>(
                    DomainError.Conflict(
                        "Finance.Allocation.ObligationCancelled",
                        $"Obligation '{obligation.ObligationNumber}' has been cancelled and cannot accept payment allocations."));
            }

            var outstandingAmount = obligation.OriginalAmount - obligation.TotalPaid;
            if (item.Amount > outstandingAmount)
            {
                return Result.Failure<AllocatePaymentResponse>(
                    DomainError.Conflict(
                        "Finance.Allocation.AmountExceedsOutstanding",
                        $"Allocation amount ({item.Amount} IDR) exceeds remaining outstanding amount ({outstandingAmount} IDR) for obligation '{obligation.ObligationNumber}'."));
            }

            var allocationId = Guid.NewGuid();
            var allocationNumber = await AllocationNumberGenerator.GenerateAsync(connection, transaction, cancellationToken);
            var note = string.IsNullOrWhiteSpace(item.Notes) ? request.Notes : item.Notes;

            await connection.ExecuteAsync(new CommandDefinition(
                _insertAllocationSql,
                new
                {
                    Id = allocationId,
                    AllocationNumber = allocationNumber,
                    StudentAccountId = payment.StudentAccountId,
                    ObligationId = item.ObligationId,
                    PaymentId = payment.Id,
                    AllocatedAmount = item.Amount,
                    AllocationDate = now,
                    AllocatedBy = auditUser,
                    Notes = note
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // If obligation is 100% satisfied, update obligation status to 'settled'
            if (outstandingAmount - item.Amount == 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    _updateObligationStatusSql,
                    new
                    {
                        ObligationId = item.ObligationId,
                        NewStatus = "settled",
                        UpdatedBy = auditUser
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
            }

            results.Add(new AllocationResultItemDto(
                allocationId,
                allocationNumber,
                item.ObligationId,
                obligation.ObligationNumber,
                item.Amount,
                "active",
                now));
        }

        await transaction.CommitAsync(cancellationToken);

        var cumulativeAllocated = existingActiveAllocated + requestedTotal;
        var remainingUnallocated = payment.PaymentAmount - cumulativeAllocated;

        logger?.LogInformation(
            "User {User} allocated {TotalAllocated} IDR across {Count} obligations from payment {PaymentNumber} ({PaymentId}). Remaining: {Remaining} IDR.",
            auditUser,
            requestedTotal,
            results.Count,
            payment.PaymentNumber,
            payment.Id,
            remainingUnallocated);

        var response = new AllocatePaymentResponse(
            payment.Id,
            payment.PaymentNumber,
            payment.PaymentAmount,
            requestedTotal,
            cumulativeAllocated,
            remainingUnallocated,
            results);

        return Result.Success(response);
    }

    private sealed record PaymentLockRow(
        Guid Id,
        string PaymentNumber,
        Guid StudentAccountId,
        Guid? StudentId,
        Guid? ApplicantId,
        long PaymentAmount,
        string ConfirmationStatus);

    private sealed record ObligationLockRow(
        Guid ObligationId,
        string ObligationNumber,
        Guid StudentAccountId,
        long OriginalAmount,
        string Status,
        long TotalPaid);
}
