namespace Elsasa.Sis.Teaching.Features.Workload.CreateUnavailabilityWindow;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler creating a temporary leave or blackout window for a teacher (TD-04, P3-S08).
/// Does NOT silently cancel active assignments; triggers conflict awareness.
/// </summary>
public sealed class CreateUnavailabilityWindowHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static CreateUnavailabilityWindowHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkTeacherSql = """
        SELECT TOP 1 1
        FROM teaching.teacher_profiles
        WHERE id = @TeacherId;
        """;

    private const string _insertWindowSql = """
        INSERT INTO teaching.teacher_unavailability_windows (
            id,
            teacher_profile_id,
            start_date,
            end_date,
            start_time,
            end_time,
            unavailability_type,
            external_source_reference,
            reason,
            status,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeacherId,
            @StartDate,
            @EndDate,
            @StartTime,
            @EndTime,
            @UnavailabilityType,
            @ExternalSourceReference,
            @Reason,
            'active',
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _countOverlappingAssignmentsSql = """
        SELECT COUNT(1)
        FROM teaching.teaching_assignments
        WHERE teacher_profile_id = @TeacherId
          AND status IN ('proposed', 'approved')
          AND effective_start_date <= @EndDate
          AND effective_end_date >= @StartDate;
        """;

    public async Task<Result<CreateUnavailabilityWindowResponse>> HandleAsync(
        Guid teacherId,
        CreateUnavailabilityWindowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (teacherId == Guid.Empty)
        {
            return Result.Failure<CreateUnavailabilityWindowResponse>(DomainError.Validation(
                "Teaching.InvalidTeacherId",
                "Teacher ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify teacher profile exists
        var teacherExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkTeacherSql,
            new { TeacherId = teacherId },
            cancellationToken: cancellationToken));

        if (!teacherExists)
        {
            return Result.Failure<CreateUnavailabilityWindowResponse>(DomainError.NotFound(
                "Teaching.TeacherNotFound",
                $"Teacher profile with ID '{teacherId}' does not exist."));
        }

        var windowId = Guid.NewGuid();
        var auditUser = currentUser.Username ?? "system";
        var type = request.UnavailabilityType.Trim().ToLowerInvariant();

        // 2. Insert unavailability window record
        await connection.ExecuteAsync(new CommandDefinition(
            _insertWindowSql,
            new
            {
                Id = windowId,
                TeacherId = teacherId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                UnavailabilityType = type,
                ExternalSourceReference = string.IsNullOrWhiteSpace(request.ExternalSourceReference) ? null : request.ExternalSourceReference.Trim(),
                Reason = request.Reason.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        // 3. Count overlapping active teaching assignments to raise conflict awareness
        var affectedAssignments = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countOverlappingAssignmentsSql,
            new
            {
                TeacherId = teacherId,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            },
            cancellationToken: cancellationToken));

        return Result.Success(new CreateUnavailabilityWindowResponse(
            windowId,
            teacherId,
            request.StartDate,
            request.EndDate,
            type,
            "active",
            affectedAssignments,
            DateTimeOffset.UtcNow));
    }
}
