namespace Elsasa.Sis.Teaching.Features.Workload.RecalculatePeriodWorkload;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for on-demand period capacity recalculation, idempotently reconciling active assignments and exceptions (TD-04, P3-S09).
/// </summary>
public sealed class RecalculatePeriodWorkloadHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static RecalculatePeriodWorkloadHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _getActiveWorkloadPolicySql = """
        SELECT TOP 1
            wp.id AS Id,
            wp.max_standard_capacity AS MaxStandardCapacity,
            wp.warning_threshold AS WarningThreshold,
            wp.prohibited_threshold AS ProhibitedThreshold
        FROM teaching.workload_policies wp
        WHERE wp.status = 'active'
        ORDER BY wp.effective_start_date DESC, wp.created_at DESC;
        """;

    private const string _recalculateCommitmentsSql = """
        WITH TeacherPool AS (
            SELECT id AS teacher_profile_id
            FROM teaching.teacher_profiles
            WHERE standing = 'active'
            UNION
            SELECT DISTINCT teacher_profile_id
            FROM teaching.teaching_assignments
            WHERE academic_period_id = @AcademicPeriodId
            UNION
            SELECT DISTINCT teacher_profile_id
            FROM teaching.workload_commitments
            WHERE academic_period_id = @AcademicPeriodId
        ),
        AssignmentAggregates AS (
            SELECT
                ta.teacher_profile_id,
                ISNULL(SUM(CASE WHEN ta.status = 'proposed' AND ar.counts_toward_workload = 1 THEN ta.workload_commitment_value ELSE 0 END), 0) AS planned_workload_hours,
                ISNULL(SUM(CASE WHEN ta.status = 'approved' AND ar.counts_toward_workload = 1 THEN ta.workload_commitment_value ELSE 0 END), 0) AS committed_workload_hours,
                ISNULL(SUM(CASE WHEN ta.status = 'approved' THEN 1 ELSE 0 END), 0) AS active_assignment_count
            FROM teaching.teaching_assignments ta
            INNER JOIN teaching.assignment_roles ar ON ar.id = ta.assignment_role_id
            WHERE ta.academic_period_id = @AcademicPeriodId
            GROUP BY ta.teacher_profile_id
        ),
        ExceptionAggregates AS (
            SELECT
                we.teacher_profile_id,
                COUNT(1) AS approved_exception_count
            FROM teaching.workload_exceptions we
            INNER JOIN teaching.teaching_assignments ta ON ta.id = we.teaching_assignment_id
            WHERE ta.academic_period_id = @AcademicPeriodId
              AND we.status = 'approved'
            GROUP BY we.teacher_profile_id
        ),
        CalculatedPool AS (
            SELECT
                tp.teacher_profile_id,
                @AcademicPeriodId AS academic_period_id,
                @WorkloadPolicyId AS workload_policy_id,
                ISNULL(aa.planned_workload_hours, 0) AS planned_workload_hours,
                ISNULL(aa.committed_workload_hours, 0) AS committed_workload_hours,
                @MaxCapacityLimitHours AS max_capacity_limit_hours,
                (@MaxCapacityLimitHours - ISNULL(aa.committed_workload_hours, 0)) AS remaining_capacity_hours,
                ISNULL(aa.active_assignment_count, 0) AS active_assignment_count,
                ISNULL(ea.approved_exception_count, 0) AS approved_exception_count,
                CASE
                    WHEN ISNULL(aa.committed_workload_hours, 0) > @ProhibitedThreshold THEN
                        CASE WHEN ISNULL(ea.approved_exception_count, 0) > 0 THEN 'overloaded_with_exception' ELSE 'breached' END
                    WHEN ISNULL(aa.committed_workload_hours, 0) >= @WarningThreshold THEN 'approaching_limit'
                    WHEN ISNULL(aa.committed_workload_hours, 0) = 0 THEN 'under_allocated'
                    ELSE 'normal'
                END AS status
            FROM TeacherPool tp
            LEFT JOIN AssignmentAggregates aa ON aa.teacher_profile_id = tp.teacher_profile_id
            LEFT JOIN ExceptionAggregates ea ON ea.teacher_profile_id = tp.teacher_profile_id
        )
        MERGE teaching.workload_commitments AS target
        USING CalculatedPool AS source
        ON (target.teacher_profile_id = source.teacher_profile_id AND target.academic_period_id = source.academic_period_id)
        WHEN MATCHED THEN
            UPDATE SET
                workload_policy_id = source.workload_policy_id,
                planned_workload_hours = source.planned_workload_hours,
                committed_workload_hours = source.committed_workload_hours,
                max_capacity_limit_hours = source.max_capacity_limit_hours,
                remaining_capacity_hours = source.remaining_capacity_hours,
                status = source.status,
                active_assignment_count = source.active_assignment_count,
                approved_exception_count = source.approved_exception_count,
                as_of_timestamp = SYSUTCDATETIME(),
                updated_at = SYSUTCDATETIME(),
                updated_by = @CurrentUserName
        WHEN NOT MATCHED THEN
            INSERT (
                id,
                teacher_profile_id,
                academic_period_id,
                workload_policy_id,
                planned_workload_hours,
                committed_workload_hours,
                max_capacity_limit_hours,
                remaining_capacity_hours,
                status,
                active_assignment_count,
                approved_exception_count,
                as_of_timestamp,
                created_at,
                created_by
            )
            VALUES (
                NEWID(),
                source.teacher_profile_id,
                source.academic_period_id,
                source.workload_policy_id,
                source.planned_workload_hours,
                source.committed_workload_hours,
                source.max_capacity_limit_hours,
                source.remaining_capacity_hours,
                source.status,
                source.active_assignment_count,
                source.approved_exception_count,
                SYSUTCDATETIME(),
                SYSUTCDATETIME(),
                @CurrentUserName
            );
        """;

    private const string _countCommitmentsSql = """
        SELECT COUNT(1)
        FROM teaching.workload_commitments
        WHERE academic_period_id = @AcademicPeriodId;
        """;

    public async Task<Result<RecalculatePeriodWorkloadResponse>> HandleAsync(
        RecalculatePeriodWorkloadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.AcademicPeriodId == Guid.Empty)
        {
            return Result.Failure<RecalculatePeriodWorkloadResponse>(DomainError.Validation(
                "Teaching.InvalidAcademicPeriodId",
                "Academic period identifier cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Retrieve active institutional workload policy
        var policy = await connection.QueryFirstOrDefaultAsync<ActivePolicyRow>(new CommandDefinition(
            _getActiveWorkloadPolicySql,
            cancellationToken: cancellationToken));

        if (policy is null)
        {
            return Result.Failure<RecalculatePeriodWorkloadResponse>(DomainError.NotFound(
                "Teaching.ActiveWorkloadPolicyNotFound",
                "No active workload policy found to govern workload capacity limits."));
        }

        var auditUser = currentUser.Username ?? "system";

        // 2. Execute reconciliation within transaction
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _recalculateCommitmentsSql,
                new
                {
                    AcademicPeriodId = request.AcademicPeriodId,
                    WorkloadPolicyId = policy.Id,
                    MaxCapacityLimitHours = policy.MaxStandardCapacity,
                    WarningThreshold = policy.WarningThreshold,
                    ProhibitedThreshold = policy.ProhibitedThreshold,
                    CurrentUserName = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            var totalCommitments = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countCommitmentsSql,
                new { AcademicPeriodId = request.AcademicPeriodId },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            var response = new RecalculatePeriodWorkloadResponse(
                request.AcademicPeriodId,
                totalCommitments,
                DateTimeOffset.UtcNow,
                $"Successfully recalculated and synchronized workload commitments for {totalCommitments} faculty members in academic period '{request.AcademicPeriodId}'.");

            return Result.Success(response);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed record ActivePolicyRow(
        Guid Id,
        decimal MaxStandardCapacity,
        decimal WarningThreshold,
        decimal ProhibitedThreshold);
}
