namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlanById;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler retrieving a single student learning plan by identifier with constituent units, adjustments, and approvals (P2-S06, SCR-ACD-04).
/// </summary>
public sealed class GetStudentLearningPlanByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetStudentLearningPlanByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _multiQuerySql = """
        -- 1. Master Plan Record
        SELECT
            p.id AS Id,
            p.student_id AS StudentId,
            s.full_name AS StudentName,
            s.student_number AS StudentNis,
            p.academic_period_id AS AcademicPeriodId,
            p.academic_period_label AS AcademicPeriodLabel,
            p.status AS Status,
            p.version AS Version,
            p.notes AS Notes,
            p.created_at AS CreatedAt,
            p.created_by AS CreatedBy,
            p.updated_at AS UpdatedAt,
            p.updated_by AS UpdatedBy
        FROM academics.student_learning_plans p
        LEFT JOIN students.students s ON s.id = p.student_id
        WHERE p.id = @Id;

        -- 2. Constituent Learning Units
        SELECT
            u.id AS Id,
            u.learning_unit_id AS LearningUnitId,
            lu.code AS UnitCode,
            lu.name AS Title,
            u.sequence_order AS SequenceOrder,
            u.is_required AS IsRequired,
            u.notes AS Notes
        FROM academics.slp_learning_units u
        INNER JOIN academics.learning_units lu ON lu.id = u.learning_unit_id
        WHERE u.student_learning_plan_id = @Id
        ORDER BY ISNULL(u.sequence_order, 999999), lu.code ASC;

        -- 3. Academic Plan Adjustments
        SELECT
            a.id AS Id,
            a.adjustment_type AS AdjustmentType,
            a.description AS Description,
            a.rationale AS Rationale,
            a.effective_date AS EffectiveDate,
            a.end_date AS EndDate,
            a.approved_by AS ApprovedBy,
            a.approved_date AS ApprovedDate,
            a.notes AS Notes
        FROM academics.academic_plan_adjustments a
        WHERE a.student_learning_plan_id = @Id
        ORDER BY a.effective_date ASC, a.created_at ASC;

        -- 4. Approvals History
        SELECT
            ap.id AS Id,
            ap.plan_version AS PlanVersion,
            ap.approved_by AS ApprovedBy,
            ap.approved_date AS ApprovedDate,
            ap.approval_type AS ApprovalType,
            ap.notes AS Notes
        FROM academics.slp_approvals ap
        WHERE ap.student_learning_plan_id = @Id
        ORDER BY ap.created_at DESC;
        """;

    private sealed record PlanMasterRow(
        Guid Id,
        Guid StudentId,
        string? StudentName,
        string? StudentNis,
        Guid AcademicPeriodId,
        string AcademicPeriodLabel,
        string Status,
        int Version,
        string? Notes,
        DateTimeOffset CreatedAt,
        string CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy);

    /// <summary>
    /// Projects detailed learning plan record for the specified identifier.
    /// </summary>
    /// <param name="id">The learning plan identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the plan detail response or not found error.</returns>
    public async Task<Result<StudentLearningPlanDetailDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<StudentLearningPlanDetailDto>(
                DomainError.Validation("Academics.InvalidPlanId", "Plan ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _multiQuerySql,
            new { Id = id },
            cancellationToken: cancellationToken));

        var master = await multi.ReadSingleOrDefaultAsync<PlanMasterRow>();
        if (master is null)
        {
            return Result.Failure<StudentLearningPlanDetailDto>(
                DomainError.NotFound(
                    "Academics.StudentLearningPlanNotFound",
                    $"Student learning plan with ID '{id}' was not found."));
        }

        var units = (await multi.ReadAsync<SlpLearningUnitDto>()).AsList();
        var adjustments = (await multi.ReadAsync<AcademicPlanAdjustmentDto>()).AsList();
        var approvals = (await multi.ReadAsync<SlpApprovalDto>()).AsList();

        var detail = new StudentLearningPlanDetailDto(
            master.Id,
            master.StudentId,
            master.StudentName,
            master.StudentNis,
            master.AcademicPeriodId,
            master.AcademicPeriodLabel,
            master.Status,
            master.Version,
            master.Notes,
            master.CreatedAt,
            master.CreatedBy,
            master.UpdatedAt,
            master.UpdatedBy,
            units,
            adjustments,
            approvals);

        return Result.Success(detail);
    }
}
