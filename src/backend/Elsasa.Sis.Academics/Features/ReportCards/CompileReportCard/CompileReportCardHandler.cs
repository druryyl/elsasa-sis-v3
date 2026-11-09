namespace Elsasa.Sis.Academics.Features.ReportCards.CompileReportCard;

using Dapper;
using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler for compiling an individual student report card (UC-ACD-13, ACD-WF-12, P5-S11).
/// Verifies uniqueness guard, gathers active participations and approved operative results,
/// and atomically inserts the parent draft report card and itemized subject entries.
/// </summary>
public sealed class CompileReportCardHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IValidator<CompileReportCardRequest>? validator = null)
{
    static CompileReportCardHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkExistingReportCardSql = """
        SELECT 1
        FROM academics.report_cards
        WHERE student_id = @StudentId
          AND academic_period_id = @AcademicPeriodId
          AND report_type = @ReportType;
        """;

    private const string _selectParticipationsWithResultsSql = """
        WITH RankedResults AS (
            SELECT 
                ar.id AS ResultId,
                a.learning_unit_id AS LearningUnitId,
                ar.score_numeric AS ScoreNumeric,
                ar.grade_letter AS GradeLetter,
                ar.grade_label AS GradeLabel,
                ar.narrative_feedback AS NarrativeFeedback,
                ROW_NUMBER() OVER (
                    PARTITION BY a.learning_unit_id
                    ORDER BY 
                        CASE WHEN a.assessment_type = 'summative' THEN 0 ELSE 1 END,
                        ar.evaluated_date DESC,
                        ar.created_at DESC
                ) AS rn
            FROM academics.academic_results ar
            INNER JOIN academics.assessments a ON a.id = ar.assessment_id
            WHERE ar.student_id = @StudentId
              AND ar.is_final = 1
              AND ar.result_status IN ('approved', 'published')
        )
        SELECT 
            ap.learning_unit_id AS LearningUnitId,
            lu.code AS SubjectCode,
            lu.title AS SubjectTitle,
            rr.ResultId AS AcademicResultId,
            rr.ScoreNumeric,
            rr.GradeLetter,
            rr.GradeLabel,
            rr.NarrativeFeedback AS TeacherComment
        FROM academics.academic_participations ap
        INNER JOIN academics.learning_units lu ON lu.id = ap.learning_unit_id
        LEFT JOIN RankedResults rr ON rr.LearningUnitId = ap.learning_unit_id AND rr.rn = 1
        WHERE ap.student_id = @StudentId
          AND ap.academic_period_id = @AcademicPeriodId
          AND ap.status = 'in_progress'
        ORDER BY lu.code, lu.title;
        """;

    private const string _insertReportCardSql = """
        INSERT INTO academics.report_cards (
            id,
            student_id,
            academic_period_id,
            academic_period_label,
            report_type,
            template_name,
            status,
            narrative_comments,
            content_snapshot,
            notes,
            created_at,
            created_by
        ) VALUES (
            @Id,
            @StudentId,
            @AcademicPeriodId,
            @AcademicPeriodLabel,
            @ReportType,
            @TemplateName,
            'draft',
            @NarrativeComments,
            NULL,
            NULL,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private const string _insertReportCardEntrySql = """
        INSERT INTO academics.report_card_entries (
            id,
            report_card_id,
            learning_unit_id,
            academic_result_id,
            display_order,
            teacher_comment,
            created_at,
            created_by
        ) VALUES (
            @Id,
            @ReportCardId,
            @LearningUnitId,
            @AcademicResultId,
            @DisplayOrder,
            @TeacherComment,
            @CreatedAt,
            @CreatedBy
        );
        """;

    private sealed record ParticipationResultRow(
        Guid LearningUnitId,
        string? SubjectCode,
        string? SubjectTitle,
        Guid? AcademicResultId,
        decimal? ScoreNumeric,
        string? GradeLetter,
        string? GradeLabel,
        string? TeacherComment);

    /// <summary>
    /// Executes validation and atomic report card compilation.
    /// </summary>
    /// <param name="request">Compilation request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled draft ReportCardDto or domain error.</returns>
    public async Task<Result<ReportCardDto>> HandleAsync(
        CompileReportCardRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<ReportCardDto>(DomainError.Validation(
                    "Academics.ReportCards.ValidationFailed",
                    validationResult.ToString()));
            }
        }

        var normalizedReportType = request.ReportType.Trim().ToLowerInvariant();
        var normalizedTemplateName = request.TemplateName.Trim();
        var normalizedPeriodLabel = request.AcademicPeriodLabel.Trim();
        var normalizedNarrative = string.IsNullOrWhiteSpace(request.NarrativeComments)
            ? null
            : request.NarrativeComments.Trim();

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Uniqueness Guard: verify no existing report card for this student, period, and report_type
        var existingCount = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            _checkExistingReportCardSql,
            new
            {
                request.StudentId,
                request.AcademicPeriodId,
                ReportType = normalizedReportType
            },
            cancellationToken: cancellationToken));

        if (existingCount.HasValue && existingCount.Value > 0)
        {
            return Result.Failure<ReportCardDto>(DomainErrors.Academics.ReportCards.AlreadyExists);
        }

        // 2. Query student's active participations and matching operative approved results
        var participationRows = (await connection.QueryAsync<ParticipationResultRow>(new CommandDefinition(
            _selectParticipationsWithResultsSql,
            new
            {
                request.StudentId,
                request.AcademicPeriodId
            },
            cancellationToken: cancellationToken))).ToList();

        if (participationRows.Count == 0)
        {
            return Result.Failure<ReportCardDto>(DomainErrors.Academics.ReportCards.NoActiveParticipations);
        }

        // 3. Atomically insert parent report card and entries in a SQL transaction
        var reportCardId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertReportCardSql,
                new
                {
                    Id = reportCardId,
                    request.StudentId,
                    request.AcademicPeriodId,
                    AcademicPeriodLabel = normalizedPeriodLabel,
                    ReportType = normalizedReportType,
                    TemplateName = normalizedTemplateName,
                    NarrativeComments = normalizedNarrative,
                    CreatedAt = now,
                    CreatedBy = createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            var entryDtos = new List<ReportCardEntryDto>(participationRows.Count);
            var displayOrder = 1;

            foreach (var row in participationRows)
            {
                var entryId = Guid.NewGuid();

                await connection.ExecuteAsync(new CommandDefinition(
                    _insertReportCardEntrySql,
                    new
                    {
                        Id = entryId,
                        ReportCardId = reportCardId,
                        row.LearningUnitId,
                        row.AcademicResultId,
                        DisplayOrder = displayOrder,
                        row.TeacherComment,
                        CreatedAt = now,
                        CreatedBy = createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                entryDtos.Add(new ReportCardEntryDto(
                    entryId,
                    reportCardId,
                    row.LearningUnitId,
                    row.SubjectCode,
                    row.SubjectTitle,
                    row.AcademicResultId,
                    row.ScoreNumeric,
                    row.GradeLetter,
                    row.GradeLabel,
                    displayOrder,
                    row.TeacherComment));

                displayOrder++;
            }

            await transaction.CommitAsync(cancellationToken);

            var reportCardDto = new ReportCardDto(
                reportCardId,
                request.StudentId,
                request.AcademicPeriodId,
                normalizedPeriodLabel,
                normalizedReportType,
                normalizedTemplateName,
                "draft",
                null,
                null,
                null,
                normalizedNarrative,
                null,
                entryDtos,
                now,
                createdBy);

            return Result.Success(reportCardDto);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<ReportCardDto>(DomainErrors.Academics.ReportCards.AlreadyExists);
        }
    }
}
