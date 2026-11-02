namespace Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures.GetSchoolClosuresList;

using Dapper;
using Elsasa.Sis.Operations.Features.CalendarPeriods.SchoolClosures;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying campus closures declared for an academic year (ADR-004, ADR-005, UC-OPS-12).
/// </summary>
public sealed class GetSchoolClosuresListHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectAcademicYearExistsSql = """
        SELECT 1
        FROM operations.academic_years
        WHERE id = @AcademicYearId;
        """;

    private const string _selectClosuresSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            closure_number AS ClosureNumber,
            reason_category AS ReasonCategory,
            reason_details AS ReasonDetails,
            start_date AS StartDate,
            end_date AS EndDate,
            affected_campus AS AffectedCampus,
            affected_population AS AffectedPopulation,
            status AS Status,
            authorized_by AS AuthorizedBy,
            authorized_date AS AuthorizedDate,
            communication_status AS CommunicationStatus,
            recovery_plan_notes AS RecoveryPlanNotes,
            created_at AS CreatedAt,
            created_by AS CreatedBy,
            updated_at AS UpdatedAt,
            updated_by AS UpdatedBy
        FROM operations.school_closures
        WHERE academic_year_id = @AcademicYearId
        ORDER BY start_date DESC, created_at DESC;
        """;

    /// <summary>
    /// Retrieves all school closures declared under the specified academic year.
    /// </summary>
    public async Task<Result<IReadOnlyList<SchoolClosureResponse>>> HandleAsync(
        Guid academicYearId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var academicYearExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _selectAcademicYearExistsSql,
            new { AcademicYearId = academicYearId },
            cancellationToken: cancellationToken));

        if (!academicYearExists)
        {
            return Result.Failure<IReadOnlyList<SchoolClosureResponse>>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{academicYearId}' was not found."));
        }

        var closures = await connection.QueryAsync<SchoolClosureResponse>(new CommandDefinition(
            _selectClosuresSql,
            new { AcademicYearId = academicYearId },
            cancellationToken: cancellationToken));

        return Result.Success<IReadOnlyList<SchoolClosureResponse>>(closures.AsList());
    }
}
