namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.ActivateOperationalPeriod;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing operational period activation (ADR-004, ADR-005).
/// </summary>
public sealed class ActivateOperationalPeriodHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectPeriodSql = """
        SELECT
            p.id AS Id,
            p.period_code AS PeriodCode,
            p.name AS Name,
            p.status AS Status,
            p.academic_year_id AS AcademicYearId,
            p.parent_period_id AS ParentPeriodId,
            ay.status AS AcademicYearStatus,
            parent.status AS ParentPeriodStatus
        FROM operations.operational_periods p
        LEFT JOIN operations.academic_years ay ON ay.id = p.academic_year_id
        LEFT JOIN operations.operational_periods parent ON parent.id = p.parent_period_id
        WHERE p.id = @Id;
        """;

    private const string _updateStatusSql = """
        UPDATE operations.operational_periods
        SET
            status = 'active',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    /// <summary>
    /// Activates an operational period after verifying lifecycle pre-conditions.
    /// </summary>
    public async Task<Result<ActivateOperationalPeriodResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var period = await connection.QuerySingleOrDefaultAsync<PeriodRow>(new CommandDefinition(
            _selectPeriodSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (period is null)
        {
            return Result.Failure<ActivateOperationalPeriodResponse>(
                DomainError.NotFound(
                    "Operations.OperationalPeriodNotFound",
                    $"Operational period with ID '{id}' was not found."));
        }

        if (string.Equals(period.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ActivateOperationalPeriodResponse>(
                DomainError.Conflict(
                    "Operations.PeriodAlreadyActive",
                    "The operational period is already active."));
        }

        if (string.Equals(period.Status, "closed", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ActivateOperationalPeriodResponse>(
                DomainError.Validation(
                    "Operations.ClosedPeriodCannotBeReactivated",
                    "The operational period is closed and cannot be reactivated directly. Use authorized record correction instead."));
        }

        var isYearEligible = string.Equals(period.AcademicYearStatus, "approved", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(period.AcademicYearStatus, "active", StringComparison.OrdinalIgnoreCase);
        if (!isYearEligible)
        {
            return Result.Failure<ActivateOperationalPeriodResponse>(
                DomainError.Validation(
                    "Operations.ParentAcademicYearNotEligible",
                    $"Parent academic year is in '{period.AcademicYearStatus}' status. Only periods in approved or active academic years can be activated."));
        }

        if (period.ParentPeriodId.HasValue)
        {
            var isParentPeriodEligible = string.Equals(period.ParentPeriodStatus, "active", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(period.ParentPeriodStatus, "approved", StringComparison.OrdinalIgnoreCase);
            if (!isParentPeriodEligible)
            {
                return Result.Failure<ActivateOperationalPeriodResponse>(
                    DomainError.Validation(
                        "Operations.ParentPeriodNotActiveOrApproved",
                        $"Parent operational period is in '{period.ParentPeriodStatus}' status. Parent period must be active or approved before child period can be activated."));
            }
        }

        var now = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _updateStatusSql,
            new
            {
                Id = id,
                UpdatedAt = now,
                UpdatedBy = updatedBy,
            },
            cancellationToken: cancellationToken));

        return Result.Success(new ActivateOperationalPeriodResponse(
            period.Id,
            period.PeriodCode,
            period.Name,
            "active",
            now));
    }

    private sealed record PeriodRow(
        Guid Id,
        string PeriodCode,
        string Name,
        string Status,
        Guid AcademicYearId,
        Guid? ParentPeriodId,
        string? AcademicYearStatus,
        string? ParentPeriodStatus);
}
