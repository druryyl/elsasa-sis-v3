namespace Elsasa.Sis.Teaching.Features.Workload.DeclareTeacherAvailability;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler declaring teacher weekly availability and superseding prior active records (TD-04, P3-S08).
/// Enforces single active availability record per teacher per academic period.
/// </summary>
public sealed class DeclareTeacherAvailabilityHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static DeclareTeacherAvailabilityHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkTeacherSql = """
        SELECT TOP 1 1
        FROM teaching.teacher_profiles
        WHERE id = @TeacherId;
        """;

    private const string _supersedePreviousActiveSql = """
        UPDATE teaching.teacher_availabilities
        SET status = 'superseded',
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE teacher_profile_id = @TeacherId
          AND academic_period_id = @AcademicPeriodId
          AND status = 'active';
        """;

    private const string _insertAvailabilitySql = """
        INSERT INTO teaching.teacher_availabilities (
            id,
            teacher_profile_id,
            academic_period_id,
            max_available_hours,
            campus_scope,
            available_delivery_modes,
            recurring_available_schedule,
            effective_start_date,
            effective_end_date,
            status,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeacherId,
            @AcademicPeriodId,
            @MaxAvailableHours,
            @CampusScope,
            @AvailableDeliveryModes,
            @RecurringAvailableSchedule,
            @EffectiveStartDate,
            @EffectiveEndDate,
            'active',
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<DeclareTeacherAvailabilityResponse>> HandleAsync(
        Guid teacherId,
        DeclareTeacherAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (teacherId == Guid.Empty)
        {
            return Result.Failure<DeclareTeacherAvailabilityResponse>(DomainError.Validation(
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
            return Result.Failure<DeclareTeacherAvailabilityResponse>(DomainError.NotFound(
                "Teaching.TeacherNotFound",
                $"Teacher profile with ID '{teacherId}' does not exist."));
        }

        var auditUser = currentUser.Username ?? "system";
        var availabilityId = Guid.NewGuid();
        var campusScope = string.IsNullOrWhiteSpace(request.CampusScope) ? "All" : request.CampusScope.Trim();
        var deliveryModes = string.IsNullOrWhiteSpace(request.AvailableDeliveryModes) ? "All" : request.AvailableDeliveryModes.Trim();

        // 2. Execute supersede and insert within explicit transaction
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var supersededCount = await connection.ExecuteAsync(new CommandDefinition(
                _supersedePreviousActiveSql,
                new
                {
                    TeacherId = teacherId,
                    AcademicPeriodId = request.AcademicPeriodId,
                    UpdatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _insertAvailabilitySql,
                new
                {
                    Id = availabilityId,
                    TeacherId = teacherId,
                    AcademicPeriodId = request.AcademicPeriodId,
                    MaxAvailableHours = request.MaxAvailableHours,
                    CampusScope = campusScope,
                    AvailableDeliveryModes = deliveryModes,
                    RecurringAvailableSchedule = string.IsNullOrWhiteSpace(request.RecurringAvailableSchedule) ? null : request.RecurringAvailableSchedule.Trim(),
                    EffectiveStartDate = request.EffectiveStartDate,
                    EffectiveEndDate = request.EffectiveEndDate,
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    CreatedBy = auditUser
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            return Result.Success(new DeclareTeacherAvailabilityResponse(
                availabilityId,
                teacherId,
                request.AcademicPeriodId,
                request.MaxAvailableHours,
                "active",
                supersededCount,
                DateTimeOffset.UtcNow));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
