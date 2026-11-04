namespace Elsasa.Sis.Finance.Features.Allocations.SuggestAllocations;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler for smart payment allocation auto-suggestions (TD-05, OQ-003).
/// Evaluates open obligations from finance.v_student_receivables, orders by policy priority,
/// and greedily allocates payment funds.
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class SuggestAllocationsHandler(
    ISqlConnectionFactory connectionFactory,
    ILogger<SuggestAllocationsHandler>? logger = null)
{
    static SuggestAllocationsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectOpenReceivablesSql = """
        SELECT 
            obligation_id AS ObligationId,
            obligation_number AS ObligationNumber,
            charge_type AS ChargeType,
            description AS Description,
            is_mandatory AS IsMandatory,
            due_date AS DueDate,
            original_amount AS OriginalAmount,
            total_paid AS TotalPaid,
            outstanding_amount AS OutstandingAmount
        FROM finance.v_student_receivables
        WHERE outstanding_amount > 0
          AND (
              (@StudentId IS NOT NULL AND (student_id = @StudentId OR applicant_id = @StudentId))
           OR (@ApplicantId IS NOT NULL AND (applicant_id = @ApplicantId OR student_id = @ApplicantId))
          )
        ORDER BY 
            CASE 
                WHEN is_mandatory = 1 AND charge_type IN ('enrollment_fee', 'admission_fee') THEN 1
                WHEN is_mandatory = 1 THEN 2
                ELSE 3
            END ASC,
            due_date ASC,
            obligation_number ASC;
        """;

    public async Task<Result<SuggestAllocationsResponse>> HandleAsync(
        SuggestAllocationsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PaymentAmount <= 0)
        {
            return Result.Failure<SuggestAllocationsResponse>(
                DomainError.Validation("Finance.Allocation.InvalidAmount", "Payment amount must be greater than zero."));
        }

        var studentId = request.StudentId == Guid.Empty ? null : request.StudentId;
        var applicantId = request.ApplicantId == Guid.Empty ? null : request.ApplicantId;

        if (!studentId.HasValue && !applicantId.HasValue)
        {
            return Result.Failure<SuggestAllocationsResponse>(
                DomainError.Validation("Finance.Allocation.CandidateRequired", "Either studentId or applicantId must be specified."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = (await connection.QueryAsync<OpenObligationRow>(new CommandDefinition(
            _selectOpenReceivablesSql,
            new { StudentId = studentId, ApplicantId = applicantId },
            cancellationToken: cancellationToken))).ToList();

        var remainingFunds = request.PaymentAmount;
        var suggestions = new List<AllocationSuggestionItemDto>();

        foreach (var row in rows)
        {
            if (remainingFunds <= 0)
            {
                break;
            }

            var allocate = Math.Min(row.OutstandingAmount, remainingFunds);
            if (allocate > 0)
            {
                suggestions.Add(new AllocationSuggestionItemDto(
                    row.ObligationId,
                    row.ObligationNumber,
                    row.ChargeType,
                    row.Description,
                    row.IsMandatory,
                    row.DueDate,
                    row.OriginalAmount,
                    row.TotalPaid,
                    row.OutstandingAmount,
                    allocate,
                    row.OutstandingAmount - allocate));

                remainingFunds -= allocate;
            }
        }

        var totalAllocated = request.PaymentAmount - remainingFunds;

        logger?.LogInformation(
            "Auto-suggested {Count} allocations totaling {TotalAllocated} IDR for student {StudentId} / applicant {ApplicantId} (Remaining: {Remaining} IDR).",
            suggestions.Count,
            totalAllocated,
            studentId,
            applicantId,
            remainingFunds);

        var response = new SuggestAllocationsResponse(
            studentId,
            applicantId,
            request.PaymentAmount,
            totalAllocated,
            remainingFunds,
            suggestions);

        return Result.Success(response);
    }

    private sealed record OpenObligationRow(
        Guid ObligationId,
        string ObligationNumber,
        string ChargeType,
        string Description,
        bool IsMandatory,
        DateOnly DueDate,
        long OriginalAmount,
        long TotalPaid,
        long OutstandingAmount);
}
