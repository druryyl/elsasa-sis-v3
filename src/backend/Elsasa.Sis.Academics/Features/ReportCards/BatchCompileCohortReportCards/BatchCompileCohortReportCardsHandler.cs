namespace Elsasa.Sis.Academics.Features.ReportCards.BatchCompileCohortReportCards;

using Dapper;
using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;

/// <summary>
/// Feature handler for batch compiling cohort report cards using set-based SQL operations (TD-07, UC-ACD-13, P5-S11).
/// Identifies all active students in the cohort section, creates draft report cards idempotently
/// using INSERT ... WHERE NOT EXISTS, and populates itemized entries matching operative approved results.
/// </summary>
public sealed class BatchCompileCohortReportCardsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IValidator<BatchCompileCohortReportCardsRequest>? validator = null)
{
    static BatchCompileCohortReportCardsHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _batchCompileSql = """
        SET NOCOUNT ON;

        DECLARE @CreatedAt DATETIMEOFFSET(7) = @Now;
        DECLARE @CreatedBy NVARCHAR(256) = @CurrentUser;
        DECLARE @TrimmedReportType NVARCHAR(50) = LOWER(RTRIM(LTRIM(@ReportType)));
        DECLARE @TrimmedTemplateName NVARCHAR(100) = RTRIM(LTRIM(@TemplateName));
        DECLARE @TrimmedPeriodLabel NVARCHAR(100) = RTRIM(LTRIM(@AcademicPeriodLabel));
        DECLARE @TrimmedSection NVARCHAR(50) = NULLIF(RTRIM(LTRIM(@SectionLabel)), '');

        CREATE TABLE #CandidateStudents (
            student_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY
        );

        INSERT INTO #CandidateStudents (student_id)
        SELECT DISTINCT ap.student_id
        FROM academics.academic_participations ap
        WHERE ap.academic_period_id = @AcademicPeriodId
          AND ap.status = 'in_progress'
          AND (@TrimmedSection IS NULL OR ap.section_label = @TrimmedSection);

        DECLARE @SkippedCount INT = (
            SELECT COUNT(*)
            FROM #CandidateStudents c
            WHERE EXISTS (
                SELECT 1
                FROM academics.report_cards rc
                WHERE rc.student_id = c.student_id
                  AND rc.academic_period_id = @AcademicPeriodId
                  AND rc.report_type = @TrimmedReportType
            )
        );

        CREATE TABLE #NewReportCards (
            report_card_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            student_id UNIQUEIDENTIFIER NOT NULL
        );

        INSERT INTO #NewReportCards (report_card_id, student_id)
        SELECT NEWID(), c.student_id
        FROM #CandidateStudents c
        WHERE NOT EXISTS (
            SELECT 1
            FROM academics.report_cards rc
            WHERE rc.student_id = c.student_id
              AND rc.academic_period_id = @AcademicPeriodId
              AND rc.report_type = @TrimmedReportType
        );

        DECLARE @CompiledCount INT = (SELECT COUNT(*) FROM #NewReportCards);

        IF @CompiledCount > 0
        BEGIN
            INSERT INTO academics.report_cards (
                id,
                student_id,
                academic_period_id,
                academic_period_label,
                report_type,
                template_name,
                status,
                created_at,
                created_by
            )
            SELECT
                n.report_card_id,
                n.student_id,
                @AcademicPeriodId,
                @TrimmedPeriodLabel,
                @TrimmedReportType,
                @TrimmedTemplateName,
                'draft',
                @CreatedAt,
                @CreatedBy
            FROM #NewReportCards n;

            ;WITH RankedResults AS (
                SELECT 
                    ar.id AS ResultId,
                    ar.student_id AS StudentId,
                    a.learning_unit_id AS LearningUnitId,
                    ar.narrative_feedback AS NarrativeFeedback,
                    ROW_NUMBER() OVER (
                        PARTITION BY ar.student_id, a.learning_unit_id
                        ORDER BY 
                            CASE WHEN a.assessment_type = 'summative' THEN 0 ELSE 1 END,
                            ar.evaluated_date DESC,
                            ar.created_at DESC
                    ) AS rn
                FROM academics.academic_results ar
                INNER JOIN academics.assessments a ON a.id = ar.assessment_id
                WHERE ar.is_final = 1
                  AND ar.result_status IN ('approved', 'published')
                  AND ar.student_id IN (SELECT student_id FROM #NewReportCards)
            ),
            OrderedParticipations AS (
                SELECT
                    n.report_card_id AS ReportCardId,
                    ap.learning_unit_id AS LearningUnitId,
                    rr.ResultId AS AcademicResultId,
                    rr.NarrativeFeedback AS TeacherComment,
                    ROW_NUMBER() OVER (
                        PARTITION BY n.report_card_id 
                        ORDER BY lu.code, lu.name
                    ) AS DisplayOrder
                FROM #NewReportCards n
                INNER JOIN academics.academic_participations ap 
                    ON ap.student_id = n.student_id
                   AND ap.academic_period_id = @AcademicPeriodId
                   AND ap.status = 'in_progress'
                INNER JOIN academics.learning_units lu 
                    ON lu.id = ap.learning_unit_id
                LEFT JOIN RankedResults rr 
                    ON rr.StudentId = n.student_id 
                   AND rr.LearningUnitId = ap.learning_unit_id 
                   AND rr.rn = 1
            )
            INSERT INTO academics.report_card_entries (
                id,
                report_card_id,
                learning_unit_id,
                academic_result_id,
                display_order,
                teacher_comment,
                created_at,
                created_by
            )
            SELECT
                NEWID(),
                op.ReportCardId,
                op.LearningUnitId,
                op.AcademicResultId,
                op.DisplayOrder,
                op.TeacherComment,
                @CreatedAt,
                @CreatedBy
            FROM OrderedParticipations op;
        END;

        DROP TABLE #NewReportCards;
        DROP TABLE #CandidateStudents;

        SELECT @CompiledCount AS CompiledCount, @SkippedCount AS SkippedExistingCount;
        """;

    /// <summary>
    /// Executes set-based batch cohort report card compilation.
    /// </summary>
    /// <param name="request">Batch compilation request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>BatchCompilationSummaryDto containing compiled and skipped counts.</returns>
    public async Task<Result<BatchCompilationSummaryDto>> HandleAsync(
        BatchCompileCohortReportCardsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<BatchCompilationSummaryDto>(DomainError.Validation(
                    "Academics.ReportCards.ValidationFailed",
                    validationResult.ToString()));
            }
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var user = currentUser.Username ?? "system";

        var summary = await connection.QuerySingleAsync<BatchCompilationSummaryDto>(new CommandDefinition(
            _batchCompileSql,
            new
            {
                request.AcademicPeriodId,
                request.AcademicPeriodLabel,
                request.ReportType,
                request.TemplateName,
                request.SectionLabel,
                Now = now,
                CurrentUser = user
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(summary);
    }
}
