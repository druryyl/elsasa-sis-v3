namespace Elsasa.Sis.Academics.Features.ReportCards.GetReportCardById;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying detailed report card representation (UC-ACD-13, GAP-006, TD-05, P5-S12).
/// Renders from frozen immutable JSON snapshot for approved/published records, or dynamically compiles live draft preview.
/// </summary>
public sealed class GetReportCardByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetReportCardByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

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

    private const string _selectStudentSql = """
        SELECT
            id AS Id,
            full_name AS FullName,
            student_number AS StudentNumber,
            government_id_number AS GovernmentIdNumber,
            date_of_birth AS DateOfBirth
        FROM students.students
        WHERE id = @StudentId;
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

    private sealed record ReportCardRow(
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

    private sealed record StudentRow(
        Guid Id,
        string FullName,
        string StudentNumber,
        string? GovernmentIdNumber,
        DateOnly? DateOfBirth);

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

    public async Task<Result<ReportCardDetailDto>> HandleAsync(
        Guid reportCardId,
        CancellationToken cancellationToken = default)
    {
        if (reportCardId == Guid.Empty)
        {
            return Result.Failure<ReportCardDetailDto>(
                DomainError.Validation("Academics.ReportCards.InvalidId", "Report card ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var reportCard = await connection.QuerySingleOrDefaultAsync<ReportCardRow>(new CommandDefinition(
            _selectReportCardSql,
            new { ReportCardId = reportCardId },
            cancellationToken: cancellationToken));

        if (reportCard is null)
        {
            return Result.Failure<ReportCardDetailDto>(DomainErrors.Academics.ReportCards.NotFound);
        }

        // 1. If approved or published and snapshot is present, render directly from immutable frozen snapshot
        var isCertified = string.Equals(reportCard.Status, "approved", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(reportCard.Status, "published", StringComparison.OrdinalIgnoreCase);

        if (isCertified && !string.IsNullOrWhiteSpace(reportCard.ContentSnapshot))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<ReportCardSnapshotDto>(reportCard.ContentSnapshot, _jsonOptions);
                if (snapshot is not null)
                {
                    var snapshotEntries = snapshot.SubjectEntries.Select(se => new ReportCardEntryDto(
                        Id: Guid.Empty,
                        ReportCardId: reportCard.Id,
                        LearningUnitId: se.LearningUnitId,
                        SubjectCode: se.SubjectCode,
                        SubjectTitle: se.SubjectTitle,
                        AcademicResultId: null,
                        ScoreNumeric: se.ScoreNumeric,
                        GradeLetter: se.GradeLetter,
                        GradeLabel: se.GradeLabel,
                        DisplayOrder: se.DisplayOrder,
                        TeacherComment: se.TeacherComment
                    )).ToList();

                    return Result.Success(new ReportCardDetailDto(
                        Id: reportCard.Id,
                        StudentId: reportCard.StudentId,
                        StudentName: snapshot.StudentName,
                        StudentNisn: snapshot.StudentNisn,
                        AcademicPeriodId: reportCard.AcademicPeriodId,
                        PeriodLabel: snapshot.PeriodLabel,
                        ReportType: reportCard.ReportType,
                        TemplateName: reportCard.TemplateName,
                        Status: reportCard.Status,
                        ApprovedBy: reportCard.ApprovedBy,
                        ApprovedDate: reportCard.ApprovedDate,
                        PublishedDate: reportCard.PublishedDate,
                        NarrativeComments: reportCard.NarrativeComments ?? snapshot.NarrativeComments,
                        ContentSnapshot: reportCard.ContentSnapshot,
                        Snapshot: snapshot,
                        Entries: snapshotEntries,
                        AttendanceSummary: snapshot.AttendanceSummary,
                        CreatedAt: reportCard.CreatedAt,
                        CreatedBy: reportCard.CreatedBy));
                }
            }
            catch
            {
                // Fall back to live projection below if deserialization fails
            }
        }

        // 2. Draft or fallback preview: dynamically project from database tables
        string studentName;
        string? studentNisn = null;

        try
        {
            var student = await connection.QuerySingleOrDefaultAsync<StudentRow>(new CommandDefinition(
                _selectStudentSql,
                new { StudentId = reportCard.StudentId },
                cancellationToken: cancellationToken));

            if (student is not null)
            {
                studentName = student.FullName;
                studentNisn = !string.IsNullOrWhiteSpace(student.GovernmentIdNumber)
                    ? student.GovernmentIdNumber
                    : student.StudentNumber;
            }
            else
            {
                studentName = $"Student {reportCard.StudentId.ToString()[..8]}";
            }
        }
        catch
        {
            studentName = $"Student {reportCard.StudentId.ToString()[..8]}";
        }

        var liveEntries = (await connection.QueryAsync<EntryRow>(new CommandDefinition(
            _selectReportCardEntriesSql,
            new { ReportCardId = reportCardId },
            cancellationToken: cancellationToken)))
            .Select(e => new ReportCardEntryDto(
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
                TeacherComment: e.TeacherComment))
            .ToList();

        var attendanceDefault = new ReportCardAttendanceSummaryDto(0, 0, 0, 0);

        return Result.Success(new ReportCardDetailDto(
            Id: reportCard.Id,
            StudentId: reportCard.StudentId,
            StudentName: studentName,
            StudentNisn: studentNisn,
            AcademicPeriodId: reportCard.AcademicPeriodId,
            PeriodLabel: reportCard.AcademicPeriodLabel,
            ReportType: reportCard.ReportType,
            TemplateName: reportCard.TemplateName,
            Status: reportCard.Status,
            ApprovedBy: reportCard.ApprovedBy,
            ApprovedDate: reportCard.ApprovedDate,
            PublishedDate: reportCard.PublishedDate,
            NarrativeComments: reportCard.NarrativeComments,
            ContentSnapshot: reportCard.ContentSnapshot,
            Snapshot: null,
            Entries: liveEntries,
            AttendanceSummary: attendanceDefault,
            CreatedAt: reportCard.CreatedAt,
            CreatedBy: reportCard.CreatedBy));
    }
}
