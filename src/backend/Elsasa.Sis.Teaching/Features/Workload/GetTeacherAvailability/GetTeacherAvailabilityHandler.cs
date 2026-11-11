namespace Elsasa.Sis.Teaching.Features.Workload.GetTeacherAvailability;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying a teacher's availability for a given academic period, including active leave windows (P3-S08).
/// </summary>
public sealed class GetTeacherAvailabilityHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeacherAvailabilityHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAvailabilitySql = """
        SELECT TOP 1
            ta.id AS Id,
            ta.teacher_profile_id AS TeacherProfileId,
            ta.academic_period_id AS AcademicPeriodId,
            ta.max_available_hours AS MaxAvailableHours,
            ta.campus_scope AS CampusScope,
            ta.available_delivery_modes AS AvailableDeliveryModes,
            ta.recurring_available_schedule AS RecurringAvailableSchedule,
            ta.effective_start_date AS EffectiveStartDate,
            ta.effective_end_date AS EffectiveEndDate,
            ta.status AS Status,
            ta.notes AS Notes,
            ta.created_at AS CreatedAt
        FROM teaching.teacher_availabilities ta
        WHERE ta.teacher_profile_id = @TeacherId
          AND (@AcademicPeriodId IS NULL OR ta.academic_period_id = @AcademicPeriodId)
          AND ta.status = 'active'
        ORDER BY ta.created_at DESC;
        """;

    private const string _selectActiveWindowsSql = """
        SELECT
            tuw.id AS Id,
            tuw.teacher_profile_id AS TeacherProfileId,
            tuw.start_date AS StartDate,
            tuw.end_date AS EndDate,
            tuw.start_time AS StartTime,
            tuw.end_time AS EndTime,
            tuw.unavailability_type AS UnavailabilityType,
            tuw.external_source_reference AS ExternalSourceReference,
            tuw.reason AS Reason,
            tuw.status AS Status,
            tuw.notes AS Notes,
            tuw.created_at AS CreatedAt
        FROM teaching.teacher_unavailability_windows tuw
        WHERE tuw.teacher_profile_id = @TeacherId
          AND tuw.status = 'active'
          AND tuw.end_date >= @StartDate
          AND tuw.start_date <= @EndDate
        ORDER BY tuw.start_date ASC;
        """;

    public async Task<Result<TeacherAvailabilityDto?>> HandleAsync(
        Guid teacherId,
        Guid? academicPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        if (teacherId == Guid.Empty)
        {
            return Result.Failure<TeacherAvailabilityDto?>(DomainError.Validation(
                "Teaching.InvalidTeacherId",
                "Teacher ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var record = await connection.QuerySingleOrDefaultAsync<TeacherAvailabilityRecord>(new CommandDefinition(
            _selectAvailabilitySql,
            new
            {
                TeacherId = teacherId,
                AcademicPeriodId = academicPeriodId.HasValue && academicPeriodId.Value != Guid.Empty ? academicPeriodId.Value : (Guid?)null
            },
            cancellationToken: cancellationToken));

        if (record is null)
        {
            return Result.Success<TeacherAvailabilityDto?>(null);
        }

        // Query overlapping active unavailability windows
        var windows = (await connection.QueryAsync<TeacherUnavailabilityWindowDto>(new CommandDefinition(
            _selectActiveWindowsSql,
            new
            {
                TeacherId = teacherId,
                StartDate = record.EffectiveStartDate,
                EndDate = record.EffectiveEndDate
            },
            cancellationToken: cancellationToken))).AsList();

        var dto = new TeacherAvailabilityDto(
            record.Id,
            record.TeacherProfileId,
            record.AcademicPeriodId,
            record.MaxAvailableHours,
            record.CampusScope,
            record.AvailableDeliveryModes,
            record.RecurringAvailableSchedule,
            record.EffectiveStartDate,
            record.EffectiveEndDate,
            record.Status,
            record.Notes,
            record.CreatedAt,
            windows);

        return Result.Success<TeacherAvailabilityDto?>(dto);
    }

    private sealed record TeacherAvailabilityRecord(
        Guid Id,
        Guid TeacherProfileId,
        Guid AcademicPeriodId,
        decimal MaxAvailableHours,
        string CampusScope,
        string AvailableDeliveryModes,
        string? RecurringAvailableSchedule,
        DateOnly EffectiveStartDate,
        DateOnly EffectiveEndDate,
        string Status,
        string? Notes,
        DateTimeOffset CreatedAt);
}
