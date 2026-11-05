namespace Elsasa.Sis.Academics.Features.Transcripts.IssueTranscript;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler officially certifying and issuing a student transcript (UC-ACD-13, TD-05, P5-S13).
/// Compiles complete cumulative academic history, calculates period GPAs, freezes immutable structured JSON snapshot,
/// updates lifecycle status to 'issued', and publishes <see cref="TranscriptIssuedEvent"/> post-commit.
/// </summary>
public sealed class IssueTranscriptHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static IssueTranscriptHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private const string _selectTranscriptSql = """
        SELECT
            t.id AS Id,
            t.student_id AS StudentId,
            t.transcript_type AS TranscriptType,
            t.period_start AS PeriodStart,
            t.period_end AS PeriodEnd,
            t.status AS Status,
            t.purpose AS Purpose,
            t.content_snapshot AS ContentSnapshot
        FROM academics.transcripts t
        WHERE t.id = @TranscriptId;
        """;

    private const string _selectStudentSql = """
        SELECT
            id AS Id,
            full_name AS FullName,
            student_number AS StudentNumber,
            government_id_number AS GovernmentIdNumber
        FROM students.students
        WHERE id = @StudentId;
        """;

    private const string _selectParticipationsSql = """
        SELECT
            p.id AS ParticipationId,
            p.academic_period_id AS AcademicPeriodId,
            p.academic_period_label AS AcademicPeriodLabel,
            p.learning_unit_id AS LearningUnitId,
            u.code AS SubjectCode,
            u.name AS SubjectTitle,
            COALESCE(u.credit_weight, 1.0) AS Credits,
            p.participation_type AS ParticipationType,
            p.external_grade_or_credit AS ExternalGradeOrCredit,
            p.start_date AS StartDate,
            p.completion_date AS CompletionDate
        FROM academics.academic_participations p
        INNER JOIN academics.learning_units u ON u.id = p.learning_unit_id
        WHERE p.student_id = @StudentId
          AND p.status = 'completed'
          AND p.start_date <= @PeriodEnd
          AND (p.completion_date IS NULL OR p.completion_date >= @PeriodStart)
        ORDER BY p.start_date ASC, u.code ASC;
        """;

    private const string _selectApprovedResultsSql = """
        SELECT
            a.learning_unit_id AS LearningUnitId,
            r.score_numeric AS ScoreNumeric,
            r.grade_letter AS GradeLetter,
            r.grade_label AS GradeLabel
        FROM academics.academic_results r
        INNER JOIN academics.assessments a ON a.id = r.assessment_id
        WHERE r.student_id = @StudentId
          AND r.is_final = 1
          AND r.result_status IN ('approved', 'published');
        """;

    private const string _updateIssueTranscriptSql = """
        UPDATE academics.transcripts
        SET
            status = 'issued',
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            issued_date = @IssuedDate,
            content_snapshot = @ContentSnapshot,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record TranscriptHeaderRow(
        Guid Id,
        Guid StudentId,
        string TranscriptType,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        string Status,
        string? Purpose,
        string? ContentSnapshot);

    private sealed record StudentRow(
        Guid Id,
        string FullName,
        string StudentNumber,
        string? GovernmentIdNumber);

    private sealed record ParticipationRow(
        Guid ParticipationId,
        Guid AcademicPeriodId,
        string AcademicPeriodLabel,
        Guid LearningUnitId,
        string SubjectCode,
        string SubjectTitle,
        decimal Credits,
        string ParticipationType,
        string? ExternalGradeOrCredit,
        DateOnly StartDate,
        DateOnly? CompletionDate);

    private sealed record ApprovedResultRow(
        Guid LearningUnitId,
        decimal? ScoreNumeric,
        string? GradeLetter,
        string? GradeLabel);

    public async Task<Result<IssueTranscriptResponse>> HandleAsync(
        Guid transcriptId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch transcript header
        var transcript = await connection.QuerySingleOrDefaultAsync<TranscriptHeaderRow>(new CommandDefinition(
            _selectTranscriptSql,
            new { TranscriptId = transcriptId },
            cancellationToken: cancellationToken));

        if (transcript is null)
        {
            return Result.Failure<IssueTranscriptResponse>(DomainErrors.Academics.Transcripts.NotFound);
        }

        if (string.Equals(transcript.Status, "issued", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<IssueTranscriptResponse>(DomainErrors.Academics.Transcripts.AlreadyIssued);
        }

        // 2. Fetch student profile details
        string studentName;
        string? studentNisn = null;

        try
        {
            var student = await connection.QuerySingleOrDefaultAsync<StudentRow>(new CommandDefinition(
                _selectStudentSql,
                new { StudentId = transcript.StudentId },
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
                studentName = $"Student {transcript.StudentId.ToString()[..8]}";
            }
        }
        catch
        {
            studentName = $"Student {transcript.StudentId.ToString()[..8]}";
        }

        // 3. Gather completed participations
        var participations = (await connection.QueryAsync<ParticipationRow>(new CommandDefinition(
            _selectParticipationsSql,
            new
            {
                StudentId = transcript.StudentId,
                PeriodStart = transcript.PeriodStart,
                PeriodEnd = transcript.PeriodEnd
            },
            cancellationToken: cancellationToken))).ToList();

        // 4. Gather approved results
        var results = (await connection.QueryAsync<ApprovedResultRow>(new CommandDefinition(
            _selectApprovedResultsSql,
            new { StudentId = transcript.StudentId },
            cancellationToken: cancellationToken))).ToList();

        var resultsByUnit = results
            .GroupBy(r => r.LearningUnitId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ScoreNumeric ?? 0m).First());

        // 5. Build completed periods summary
        var periodGroups = participations
            .GroupBy(p => new { p.AcademicPeriodId, p.AcademicPeriodLabel });

        var completedPeriods = new List<TranscriptPeriodSummaryDto>();

        foreach (var group in periodGroups)
        {
            var courses = new List<TranscriptCourseEntryDto>();

            foreach (var part in group)
            {
                var isTransferred = string.Equals(part.ParticipationType, "transferred_in", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(part.ParticipationType, "recognized_prior", StringComparison.OrdinalIgnoreCase);

                decimal? scoreNumeric = null;
                string? gradeLetter = null;

                if (resultsByUnit.TryGetValue(part.LearningUnitId, out var res))
                {
                    scoreNumeric = res.ScoreNumeric;
                    gradeLetter = res.GradeLetter;
                }
                else if (isTransferred && !string.IsNullOrWhiteSpace(part.ExternalGradeOrCredit))
                {
                    if (decimal.TryParse(part.ExternalGradeOrCredit, out var extScore))
                    {
                        scoreNumeric = extScore;
                    }
                    else
                    {
                        gradeLetter = part.ExternalGradeOrCredit;
                    }
                }

                courses.Add(new TranscriptCourseEntryDto(
                    LearningUnitId: part.LearningUnitId,
                    SubjectCode: part.SubjectCode,
                    SubjectTitle: part.SubjectTitle,
                    Credits: part.Credits,
                    ScoreNumeric: scoreNumeric,
                    GradeLetter: gradeLetter,
                    IsTransferred: isTransferred));
            }

            var gpa = TranscriptGpaCalculator.CalculatePeriodGpa(courses);
            completedPeriods.Add(new TranscriptPeriodSummaryDto(
                AcademicPeriodId: group.Key.AcademicPeriodId,
                PeriodLabel: group.Key.AcademicPeriodLabel,
                Courses: courses,
                Gpa: gpa));
        }

        // 6. Freeze snapshot
        var snapshot = new TranscriptSnapshotDto(
            TranscriptId: transcript.Id,
            StudentId: transcript.StudentId,
            StudentName: studentName,
            StudentNisn: studentNisn,
            TranscriptType: transcript.TranscriptType,
            PeriodStart: transcript.PeriodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            PeriodEnd: transcript.PeriodEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Status: "issued",
            Purpose: transcript.Purpose,
            CompletedPeriods: completedPeriods);

        var jsonSnapshot = JsonSerializer.Serialize(snapshot, _jsonOptions);

        var approvedBy = currentUser.Username ?? "authority";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = approvedBy;

        // 7. Atomic update in transaction
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            _updateIssueTranscriptSql,
            new
            {
                Id = transcript.Id,
                ApprovedBy = approvedBy,
                ApprovedDate = today,
                IssuedDate = today,
                ContentSnapshot = jsonSnapshot,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        // 8. Publish post-commit domain event (ADR-003, UC-ACD-13)
        await publisher.Publish(
            new TranscriptIssuedEvent(
                transcript.Id,
                transcript.StudentId,
                transcript.TranscriptType,
                transcript.Purpose),
            cancellationToken);

        return Result.Success(new IssueTranscriptResponse(
            Id: transcript.Id,
            StudentId: transcript.StudentId,
            TranscriptType: transcript.TranscriptType,
            Status: "issued",
            IssuedDate: today,
            ApprovedBy: approvedBy,
            Snapshot: snapshot));
    }
}
