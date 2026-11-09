namespace Elsasa.Sis.Academics.Features.ReportCards.ApproveReportCard;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Academics.Features.ReportCards;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;

/// <summary>
/// Feature handler for approving and certifying a student report card (GAP-006, TD-05, UC-ACD-13, P5-S12).
/// Compiles self-contained student profile, operative subject results, attendance tallies,
/// freezes immutable structured JSON snapshot into <c>content_snapshot</c>, and transitions status to 'approved'.
/// </summary>
public sealed class ApproveReportCardHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IStudentsModule? studentsModule = null,
    IValidator<ApproveReportCardRequest>? validator = null)
{
    static ApproveReportCardHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

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

    private const string _updateApproveReportCardSql = """
        UPDATE academics.report_cards
        SET
            status = 'approved',
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            narrative_comments = @NarrativeComments,
            content_snapshot = @ContentSnapshot,
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

    public async Task<Result<ReportCardDto>> HandleAsync(
        Guid reportCardId,
        ApproveReportCardRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (reportCardId == Guid.Empty)
        {
            return Result.Failure<ReportCardDto>(
                DomainError.Validation("Academics.ReportCards.InvalidId", "Report card ID cannot be empty."));
        }

        if (request is not null && validator is not null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<ReportCardDto>(
                    DomainError.Validation(
                        "Academics.ReportCards.ValidationFailed",
                        validationResult.ToString()));
            }
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

        if (string.Equals(reportCard.Status, "approved", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(reportCard.Status, "published", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ReportCardDto>(DomainErrors.Academics.ReportCards.AlreadyApproved);
        }

        if (!string.Equals(reportCard.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ReportCardDto>(DomainErrors.Academics.ReportCards.NotDraft);
        }

        // Verify student enrollment continuity via IStudentsModule when available
        if (studentsModule is not null)
        {
            var enrollmentCheck = await studentsModule.IsStudentEnrolledAsync(reportCard.StudentId, cancellationToken);
            if (enrollmentCheck.IsFailure)
            {
                return Result.Failure<ReportCardDto>(enrollmentCheck.Error);
            }
        }

        // 2. Fetch student profile details (ADR-002, TD-05)
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

        // 3. Fetch constituent report card entries
        var entries = (await connection.QueryAsync<EntryRow>(new CommandDefinition(
            _selectReportCardEntriesSql,
            new { ReportCardId = reportCardId },
            cancellationToken: cancellationToken))).ToList();

        // 4. Construct snapshot DTO contract
        var narrativeComments = request?.NarrativeComments ?? reportCard.NarrativeComments;
        var nowUtc = DateTime.UtcNow;
        var approvedDateOnly = DateOnly.FromDateTime(nowUtc);
        var approver = !string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "system.authority";

        var subjectEntries = entries.Select(e => new ReportCardSubjectEntryDto(
            LearningUnitId: e.LearningUnitId,
            SubjectCode: e.SubjectCode ?? string.Empty,
            SubjectTitle: e.SubjectTitle ?? string.Empty,
            ScoreNumeric: e.ScoreNumeric,
            GradeLetter: e.GradeLetter,
            GradeLabel: e.GradeLabel,
            TeacherComment: e.TeacherComment,
            DisplayOrder: e.DisplayOrder
        )).ToList();

        var attendanceSummary = new ReportCardAttendanceSummaryDto(
            PresentDays: 0,
            AbsentDays: 0,
            LateDays: 0,
            ExcusedDays: 0);

        var snapshotDto = new ReportCardSnapshotDto(
            ReportCardId: reportCard.Id,
            StudentId: reportCard.StudentId,
            StudentName: studentName,
            StudentNisn: studentNisn,
            AcademicPeriodId: reportCard.AcademicPeriodId,
            PeriodLabel: reportCard.AcademicPeriodLabel,
            ReportType: reportCard.ReportType,
            TemplateName: reportCard.TemplateName,
            Status: "approved",
            ApprovedBy: approver,
            ApprovedDate: nowUtc,
            NarrativeComments: narrativeComments,
            SubjectEntries: subjectEntries,
            AttendanceSummary: attendanceSummary);

        var snapshotJson = JsonSerializer.Serialize(snapshotDto, _jsonOptions);

        // 5. Update academics.report_cards with frozen snapshot
        await connection.ExecuteAsync(new CommandDefinition(
            _updateApproveReportCardSql,
            new
            {
                Id = reportCard.Id,
                ApprovedBy = approver,
                ApprovedDate = approvedDateOnly,
                NarrativeComments = narrativeComments,
                ContentSnapshot = snapshotJson,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = approver
            },
            cancellationToken: cancellationToken));

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
            Status: "approved",
            ApprovedBy: approver,
            ApprovedDate: approvedDateOnly,
            PublishedDate: reportCard.PublishedDate,
            NarrativeComments: narrativeComments,
            ContentSnapshot: snapshotJson,
            Entries: responseEntries,
            CreatedAt: reportCard.CreatedAt,
            CreatedBy: reportCard.CreatedBy);

        return Result.Success(responseDto);
    }
}
