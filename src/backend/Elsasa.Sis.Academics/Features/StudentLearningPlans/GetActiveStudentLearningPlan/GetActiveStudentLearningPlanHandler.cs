namespace Elsasa.Sis.Academics.Features.StudentLearningPlans.GetActiveStudentLearningPlan;

using Dapper;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler retrieving the active student learning plan for a student in an academic period (SCR-ACD-04, SCR-ACD-05, P2-S06).
/// </summary>
public sealed class GetActiveStudentLearningPlanHandler(ISqlConnectionFactory connectionFactory)
{
    static GetActiveStudentLearningPlanHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectActivePlanSql = """
        -- 1. Active Plan
        SELECT
            p.id AS PlanId,
            p.student_id AS StudentId,
            p.academic_period_id AS AcademicPeriodId,
            p.academic_period_label AS AcademicPeriodLabel,
            p.version AS Version,
            p.status AS Status,
            p.notes AS Notes
        FROM academics.student_learning_plans p
        WHERE p.student_id = @StudentId
          AND p.academic_period_id = @AcademicPeriodId
          AND p.status = 'active';

        -- 2. Constituent Units
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
        INNER JOIN academics.student_learning_plans p ON p.id = u.student_learning_plan_id
        WHERE p.student_id = @StudentId
          AND p.academic_period_id = @AcademicPeriodId
          AND p.status = 'active'
        ORDER BY ISNULL(u.sequence_order, 999999), lu.code ASC;

        -- 3. Adjustments
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
        INNER JOIN academics.student_learning_plans p ON p.id = a.student_learning_plan_id
        WHERE p.student_id = @StudentId
          AND p.academic_period_id = @AcademicPeriodId
          AND p.status = 'active'
        ORDER BY a.effective_date ASC, a.created_at ASC;
        """;

    private sealed record ActivePlanRow(
        Guid PlanId,
        Guid StudentId,
        Guid AcademicPeriodId,
        string AcademicPeriodLabel,
        int Version,
        string Status,
        string? Notes);

    /// <summary>
    /// Executes lookup of the active learning plan for the specified student and period.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the operational academic period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active learning plan DTO or not found error.</returns>
    public async Task<Result<ActiveStudentLearningPlanDto>> HandleAsync(
        Guid studentId,
        Guid academicPeriodId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            return Result.Failure<ActiveStudentLearningPlanDto>(
                DomainError.Validation("Academics.InvalidStudentId", "Student ID cannot be empty."));
        }

        if (academicPeriodId == Guid.Empty)
        {
            return Result.Failure<ActiveStudentLearningPlanDto>(
                DomainError.Validation("Academics.InvalidAcademicPeriodId", "Academic period ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _selectActivePlanSql,
            new { StudentId = studentId, AcademicPeriodId = academicPeriodId },
            cancellationToken: cancellationToken));

        var plan = await multi.ReadSingleOrDefaultAsync<ActivePlanRow>();
        if (plan is null)
        {
            return Result.Failure<ActiveStudentLearningPlanDto>(
                DomainError.NotFound(
                    "Academics.ActiveLearningPlanNotFound",
                    $"No active student learning plan found for student '{studentId}' in academic period '{academicPeriodId}'."));
        }

        var units = (await multi.ReadAsync<SlpLearningUnitDto>()).AsList();
        var adjustments = (await multi.ReadAsync<AcademicPlanAdjustmentDto>()).AsList();

        var activePlanDto = new ActiveStudentLearningPlanDto(
            plan.PlanId,
            plan.StudentId,
            plan.AcademicPeriodId,
            plan.AcademicPeriodLabel,
            plan.Version,
            plan.Status,
            plan.Notes,
            units,
            adjustments);

        return Result.Success(activePlanDto);
    }
}
