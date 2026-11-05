namespace Elsasa.Sis.Academics.Features.Transcripts.GetStudentTranscript;

using System.Text.Json;
using Dapper;
using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying the academic transcript for a student (UC-ACD-13, P5-S13).
/// Returns frozen structured JSON snapshot for issued transcripts, or dynamically compiled live preview for draft transcripts.
/// </summary>
public sealed class GetStudentTranscriptHandler(ISqlConnectionFactory connectionFactory)
{
    static GetStudentTranscriptHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private const string _selectLatestTranscriptSql = """
        SELECT TOP 1
            t.id AS Id,
            t.student_id AS StudentId,
            t.transcript_type AS TranscriptType,
            t.period_start AS PeriodStart,
            t.period_end AS PeriodEnd,
            t.status AS Status,
            t.approved_by AS ApprovedBy,
            t.approved_date AS ApprovedDate,
            t.issued_date AS IssuedDate,
            t.purpose AS Purpose,
            t.content_snapshot AS ContentSnapshot,
            t.notes AS Notes,
            t.created_at AS CreatedAt,
            t.created_by AS CreatedBy
        FROM academics.transcripts t
        WHERE t.student_id = @StudentId
          AND (@TranscriptType IS NULL OR t.transcript_type = @TranscriptType)
        ORDER BY
            CASE WHEN t.status = 'issued' THEN 1 WHEN t.status = 'approved' THEN 2 ELSE 3 END,
            t.issued_date DESC,
            t.created_at DESC;
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

    private sealed record TranscriptRow(
        Guid Id,
        Guid StudentId,
        string TranscriptType,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        string Status,
        string? ApprovedBy,
        DateOnly? ApprovedDate,
        DateOnly? IssuedDate,
        string? Purpose,
        string? ContentSnapshot,
        string? Notes,
        DateTimeOffset CreatedAt,
        string CreatedBy);

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

    public async Task<Result<TranscriptDetailDto>> HandleAsync(
        Guid studentId,
        string? transcriptType = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var transcript = await connection.QuerySingleOrDefaultAsync<TranscriptRow>(new CommandDefinition(
            _selectLatestTranscriptSql,
            new
            {
                StudentId = studentId,
                TranscriptType = transcriptType?.Trim().ToLowerInvariant()
            },
            cancellationToken: cancellationToken));

        if (transcript is null)
        {
            return Result.Failure<TranscriptDetailDto>(DomainErrors.Academics.Transcripts.NotFound);
        }

        // 1. If issued and snapshot is present, render from frozen snapshot for 100% reproducibility (TD-05)
        if (string.Equals(transcript.Status, "issued", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(transcript.ContentSnapshot))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<TranscriptSnapshotDto>(transcript.ContentSnapshot, _jsonOptions);
                if (snapshot is not null)
                {
                    return Result.Success(new TranscriptDetailDto(
                        Id: transcript.Id,
                        StudentId: transcript.StudentId,
                        StudentName: snapshot.StudentName,
                        StudentNisn: snapshot.StudentNisn,
                        TranscriptType: transcript.TranscriptType,
                        PeriodStart: transcript.PeriodStart,
                        PeriodEnd: transcript.PeriodEnd,
                        Status: transcript.Status,
                        Purpose: transcript.Purpose,
                        ApprovedBy: transcript.ApprovedBy,
                        ApprovedDate: transcript.ApprovedDate,
                        IssuedDate: transcript.IssuedDate,
                        Snapshot: snapshot,
                        CompletedPeriods: snapshot.CompletedPeriods,
                        Notes: transcript.Notes,
                        CreatedAt: transcript.CreatedAt,
                        CreatedBy: transcript.CreatedBy));
                }
            }
            catch
            {
                // Fall back to live compilation if snapshot deserialization encounters unexpected format
            }
        }

        // 2. Draft / Live Preview compilation
        string studentName;
        string? studentNisn = null;

        try
        {
            var student = await connection.QuerySingleOrDefaultAsync<StudentRow>(new CommandDefinition(
                _selectStudentSql,
                new { StudentId = studentId },
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
                studentName = $"Student {studentId.ToString()[..8]}";
            }
        }
        catch
        {
            studentName = $"Student {studentId.ToString()[..8]}";
        }

        var participations = (await connection.QueryAsync<ParticipationRow>(new CommandDefinition(
            _selectParticipationsSql,
            new
            {
                StudentId = transcript.StudentId,
                PeriodStart = transcript.PeriodStart,
                PeriodEnd = transcript.PeriodEnd
            },
            cancellationToken: cancellationToken))).ToList();

        var results = (await connection.QueryAsync<ApprovedResultRow>(new CommandDefinition(
            _selectApprovedResultsSql,
            new { StudentId = transcript.StudentId },
            cancellationToken: cancellationToken))).ToList();

        var resultsByUnit = results
            .GroupBy(r => r.LearningUnitId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ScoreNumeric ?? 0m).First());

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

        return Result.Success(new TranscriptDetailDto(
            Id: transcript.Id,
            StudentId: transcript.StudentId,
            StudentName: studentName,
            StudentNisn: studentNisn,
            TranscriptType: transcript.TranscriptType,
            PeriodStart: transcript.PeriodStart,
            PeriodEnd: transcript.PeriodEnd,
            Status: transcript.Status,
            Purpose: transcript.Purpose,
            ApprovedBy: transcript.ApprovedBy,
            ApprovedDate: transcript.ApprovedDate,
            IssuedDate: transcript.IssuedDate,
            Snapshot: null,
            CompletedPeriods: completedPeriods,
            Notes: transcript.Notes,
            CreatedAt: transcript.CreatedAt,
            CreatedBy: transcript.CreatedBy));
    }
}
