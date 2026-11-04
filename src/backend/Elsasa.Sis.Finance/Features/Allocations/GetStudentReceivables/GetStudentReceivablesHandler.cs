namespace Elsasa.Sis.Finance.Features.Allocations.GetStudentReceivables;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Feature handler querying dynamic student receivables from <c>finance.v_student_receivables</c> (TD-05, OQ-003).
/// Supports both matriculated students (<c>student_id</c>) and accepted pre-enrollment candidates (<c>applicant_id</c>).
/// </summary>
[SuppressMessage("Usage", "CA1848:Use the LoggerMessage delegates", Justification = "Standard scoped logging.")]
public sealed class GetStudentReceivablesHandler(
    ISqlConnectionFactory connectionFactory,
    ILogger<GetStudentReceivablesHandler>? logger = null)
{
    static GetStudentReceivablesHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _queryReceivablesSql = """
        SELECT 
            obligation_id AS ObligationId,
            obligation_number AS ObligationNumber,
            student_account_id AS StudentAccountId,
            student_id AS StudentId,
            applicant_id AS ApplicantId,
            charge_type AS ChargeType,
            description AS Description,
            is_mandatory AS IsMandatory,
            due_date AS DueDate,
            original_amount AS OriginalAmount,
            currency_code AS CurrencyCode,
            academic_period_id AS AcademicPeriodId,
            academic_period_label AS AcademicPeriodLabel,
            obligation_status AS ObligationStatus,
            net_obligation AS NetObligation,
            total_paid AS TotalPaid,
            outstanding_amount AS OutstandingAmount,
            CAST(is_overdue AS BIT) AS IsOverdue,
            days_overdue AS DaysOverdue,
            aging_bracket AS AgingBracket,
            receivable_status AS ReceivableStatus
        FROM finance.v_student_receivables
        WHERE (student_id = @StudentId OR applicant_id = @StudentId)
          AND (@OpenOnly IS NULL OR @OpenOnly = 0 OR outstanding_amount > 0)
        ORDER BY 
            CASE 
                WHEN is_mandatory = 1 AND charge_type IN ('enrollment_fee', 'admission_fee') THEN 1
                WHEN is_mandatory = 1 THEN 2
                ELSE 3
            END ASC,
            due_date ASC,
            obligation_number ASC;
        """;

    /// <summary>
    /// Executes the receivables query for the candidate/student identified by <paramref name="studentId"/>.
    /// </summary>
    /// <param name="studentId">The candidate or matriculated student identifier.</param>
    /// <param name="openOnly">Optional flag indicating whether only unpaid/open obligations should be returned.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the aggregated receivables dossier.</returns>
    public async Task<Result<GetStudentReceivablesResponse>> HandleAsync(
        Guid studentId,
        bool? openOnly = null,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            return Result.Failure<GetStudentReceivablesResponse>(
                DomainError.Validation("Finance.Receivables.InvalidStudentId", "A valid student or applicant ID is required."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var items = (await connection.QueryAsync<StudentReceivableDto>(new CommandDefinition(
            _queryReceivablesSql,
            new { StudentId = studentId, OpenOnly = openOnly },
            cancellationToken: cancellationToken))).ToList();

        var openObligations = items.Where(i => i.OutstandingAmount > 0).ToList();
        var totalNetObligation = items.Sum(i => i.NetObligation);
        var totalPaid = items.Sum(i => i.TotalPaid);
        var totalOutstanding = items.Sum(i => i.OutstandingAmount);
        var totalOverdueCount = items.Count(i => i.IsOverdue);

        logger?.LogInformation(
            "Retrieved {Count} receivables ({OpenCount} open, Total Outstanding: {Outstanding} IDR) for student/applicant {StudentId}.",
            items.Count,
            openObligations.Count,
            totalOutstanding,
            studentId);

        var response = new GetStudentReceivablesResponse(
            studentId,
            totalNetObligation,
            totalPaid,
            totalOutstanding,
            totalOverdueCount,
            items,
            openObligations);

        return Result.Success(response);
    }
}
