namespace Elsasa.Sis.Academics.Features.ReportCards.PublishReportCard;

using Dapper;
using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler for publishing an approved report card to student and family portals (UC-ACD-13, P5-S12).
/// Transitions lifecycle status to 'published', records publication date, and publishes <see cref="ReportCardPublishedEvent"/> post-commit.
/// </summary>
public sealed class PublishReportCardHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static PublishReportCardHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectReportCardSql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            academic_period_id AS AcademicPeriodId,
            academic_period_label AS AcademicPeriodLabel,
            report_type AS ReportType,
            template_name AS TemplateName,
            status AS Status,
            approved_by AS ApprovedBy,
            approved_date AS ApprovedDate,
            published_date AS PublishedDate,
            narrative_comments AS NarrativeComments,
            content_snapshot AS ContentSnapshot,
            created_at AS CreatedAt,
            created_by AS CreatedBy
        FROM academics.report_cards
        WHERE id = @ReportCardId;
        """;

    private const string _selectReportCardEntriesSql = """
        SELECT
            e.id AS Id,
            e.report_card_id AS ReportCardId,
            e.learning_unit_id AS LearningUnitId,
            u.code AS SubjectCode,
            u.name AS SubjectTitle,
            e.academic_result_id AS AcademicResultId,
            r.score_numeric AS ScoreNumeric,
            r.grade_letter AS GradeLetter,
            r.grade_label AS GradeLabel,
            e.display_order AS DisplayOrder,
            COALESCE(e.teacher_comment, r.narrative_feedback) AS TeacherComment
        FROM academics.report_card_entries e
        INNER JOIN academics.learning_units u ON u.id = e.learning_unit_id
        LEFT JOIN academics.academic_results r ON r.id = e.academic_result_id
        WHERE e.report_card_id = @ReportCardId
        ORDER BY e.display_order ASC, u.code ASC;
        """;

    private const string _updatePublishReportCardSql = """
        UPDATE academics.report_cards
        SET
            status = 'published',
            published_date = @PublishedDate,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record ReportCardHeaderRow(
        Guid Id,
        Guid StudentId,
        Guid AcademicPeriodId,
        string AcademicPeriodLabel,
        string ReportType,
        string TemplateName,
        string Status,
        string? ApprovedBy,
        DateOnly? ApprovedDate,
        DateOnly? PublishedDate,
        string? NarrativeComments,
        string? ContentSnapshot,
        DateTimeOffset CreatedAt,
        string CreatedBy);

    private sealed record EntryRow(
        Guid Id,
        Guid ReportCardId,
        Guid LearningUnitId,
        string? SubjectCode,
        string? SubjectTitle,
        Guid? AcademicResultId,
        decimal? ScoreNumeric,
        string? GradeLetter,
        string? GradeLabel,
        int DisplayOrder,
        string? TeacherComment);

    public async Task<Result<ReportCardDto>> HandleAsync(
        Guid reportCardId,
        CancellationToken cancellationToken = default)
    {
        if (reportCardId == Guid.Empty)
        {
            return Result.Failure<ReportCardDto>(
                DomainError.Validation("Academics.ReportCards.InvalidId", "Report card ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch parent report card
        var reportCard = await connection.QuerySingleOrDefaultAsync<ReportCardHeaderRow>(new CommandDefinition(
            _selectReportCardSql,
            new { ReportCardId = reportCardId },
            cancellationToken: cancellationToken));

        if (reportCard is null)
        {
            return Result.Failure<ReportCardDto>(DomainErrors.Academics.ReportCards.NotFound);
        }

        if (string.Equals(reportCard.Status, "published", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ReportCardDto>(DomainErrors.Academics.ReportCards.AlreadyPublished);
        }

        if (!string.Equals(reportCard.Status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ReportCardDto>(DomainErrors.Academics.ReportCards.NotApproved);
        }

        // 2. Fetch constituent entries
        var entries = (await connection.QueryAsync<EntryRow>(new CommandDefinition(
            _selectReportCardEntriesSql,
            new { ReportCardId = reportCardId },
            cancellationToken: cancellationToken))).ToList();

        var nowUtc = DateTime.UtcNow;
        var publishedDateOnly = DateOnly.FromDateTime(nowUtc);
        var publisherUsername = !string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "system.authority";

        // 3. Update report card status to 'published'
        await connection.ExecuteAsync(new CommandDefinition(
            _updatePublishReportCardSql,
            new
            {
                Id = reportCard.Id,
                PublishedDate = publishedDateOnly,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = publisherUsername
            },
            cancellationToken: cancellationToken));

        // 4. Publish post-commit domain event
        await publisher.Publish(
            new ReportCardPublishedEvent(
                reportCard.Id,
                reportCard.StudentId,
                reportCard.AcademicPeriodId,
                reportCard.ReportType,
                DateTimeOffset.UtcNow),
            cancellationToken);

        var responseEntries = entries.Select(e => new ReportCardEntryDto(
            Id: e.Id,
            ReportCardId: e.ReportCardId,
            LearningUnitId: e.LearningUnitId,
            SubjectCode: e.SubjectCode,
            SubjectTitle: e.SubjectTitle,
            AcademicResultId: e.AcademicResultId,
            ScoreNumeric: e.ScoreNumeric,
            GradeLetter: e.GradeLetter,
            GradeLabel: e.GradeLabel,
            DisplayOrder: e.DisplayOrder,
            TeacherComment: e.TeacherComment
        )).ToList();

        var responseDto = new ReportCardDto(
            Id: reportCard.Id,
            StudentId: reportCard.StudentId,
            AcademicPeriodId: reportCard.AcademicPeriodId,
            AcademicPeriodLabel: reportCard.AcademicPeriodLabel,
            ReportType: reportCard.ReportType,
            TemplateName: reportCard.TemplateName,
            Status: "published",
            ApprovedBy: reportCard.ApprovedBy,
            ApprovedDate: reportCard.ApprovedDate,
            PublishedDate: publishedDateOnly,
            NarrativeComments: reportCard.NarrativeComments,
            ContentSnapshot: reportCard.ContentSnapshot,
            Entries: responseEntries,
            CreatedAt: reportCard.CreatedAt,
            CreatedBy: reportCard.CreatedBy);

        return Result.Success(responseDto);
    }
}
