namespace Elsasa.Sis.Operations.Features.CalendarPeriods.OperationalPeriods.CloseOperationalPeriod;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing operational period closure (ADR-004, ADR-005).
/// </summary>
public sealed class CloseOperationalPeriodHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectPeriodSql = """
        SELECT
            id AS Id,
            status AS Status
        FROM operations.operational_periods
        WHERE id = @Id;
        """;

    private const string _updateStatusSql = """
        UPDATE operations.operational_periods
        SET
            status = 'closed',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    /// <summary>
    /// Closes an operational period.
    /// </summary>
    public async Task<Result<CloseOperationalPeriodResponse>> HandleAsync(
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
            return Result.Failure<CloseOperationalPeriodResponse>(
                DomainError.NotFound(
                    "Operations.OperationalPeriodNotFound",
                    $"Operational period with ID '{id}' was not found."));
        }

        if (string.Equals(period.Status, "closed", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CloseOperationalPeriodResponse>(
                DomainError.Conflict(
                    "Operations.PeriodAlreadyClosed",
                    "The operational period is already closed."));
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

        return Result.Success(new CloseOperationalPeriodResponse(id, "closed", now));
    }

    private sealed record PeriodRow(Guid Id, string Status);
}
