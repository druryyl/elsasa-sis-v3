namespace Elsasa.Sis.Academics.Features.Transcripts.GenerateInterimTranscript;

using Dapper;
using Elsasa.Sis.Academics.Common;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using FluentValidation;

/// <summary>
/// Feature handler generating a draft cumulative interim academic transcript (UC-ACD-13, P5-S13).
/// Compiles completed course participations across specified periods, marks transfer credits [T],
/// calculates period GPAs, and inserts a draft record into <c>academics.transcripts</c>.
/// </summary>
public sealed class GenerateInterimTranscriptHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IStudentsModule studentsModule,
    IValidator<GenerateInterimTranscriptRequest>? validator = null)
{
    static GenerateInterimTranscriptHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

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

    private const string _insertTranscriptSql = """
        INSERT INTO academics.transcripts (
            id,
            student_id,
            transcript_type,
            period_start,
            period_end,
            status,
            approved_by,
            approved_date,
            issued_date,
            purpose,
            content_snapshot,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @StudentId,
            @TranscriptType,
            @PeriodStart,
            @PeriodEnd,
            'draft',
            NULL,
            NULL,
            NULL,
            @Purpose,
            NULL,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

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

    public async Task<Result<TranscriptDto>> HandleAsync(
        GenerateInterimTranscriptRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. FluentValidation
        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.Failure<TranscriptDto>(
                    DomainError.Validation(
                        "Academics.Transcripts.ValidationFailed",
                        "One or more validation errors occurred.",
                        validationResult.ToDictionary()));
            }
        }

        // 2. Student Enrollment Verification
        var enrollmentResult = await studentsModule.IsStudentEnrolledAsync(request.StudentId, cancellationToken);
        if (enrollmentResult.IsFailure || !enrollmentResult.Value)
        {
            return Result.Failure<TranscriptDto>(DomainErrors.Academics.Transcripts.StudentNotEnrolled);
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 3. Gather completed participations
        var participations = (await connection.QueryAsync<ParticipationRow>(new CommandDefinition(
            _selectParticipationsSql,
            new
            {
                StudentId = request.StudentId,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd
            },
            cancellationToken: cancellationToken))).ToList();

        // 4. Gather approved operative results
        var results = (await connection.QueryAsync<ApprovedResultRow>(new CommandDefinition(
            _selectApprovedResultsSql,
            new { StudentId = request.StudentId },
            cancellationToken: cancellationToken))).ToList();

        var resultsByUnit = results
            .GroupBy(r => r.LearningUnitId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ScoreNumeric ?? 0m).First());

        // 5. Compile completed periods and calculate GPAs
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

        // 6. Insert draft transcript
        var transcriptId = Guid.NewGuid();
        var transcriptType = request.TranscriptType.Trim().ToLowerInvariant();
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _insertTranscriptSql,
            new
            {
                Id = transcriptId,
                StudentId = request.StudentId,
                TranscriptType = transcriptType,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                Purpose = request.Purpose,
                Notes = request.Notes,
                CreatedAt = createdAt,
                CreatedBy = createdBy
            },
            cancellationToken: cancellationToken));

        var responseDto = new TranscriptDto(
            Id: transcriptId,
            StudentId: request.StudentId,
            TranscriptType: transcriptType,
            PeriodStart: request.PeriodStart,
            PeriodEnd: request.PeriodEnd,
            Status: "draft",
            Purpose: request.Purpose,
            ApprovedBy: null,
            ApprovedDate: null,
            IssuedDate: null,
            ContentSnapshot: null,
            Notes: request.Notes,
            CreatedAt: createdAt,
            CreatedBy: createdBy,
            CompletedPeriods: completedPeriods);

        return Result.Success(responseDto);
    }
}
