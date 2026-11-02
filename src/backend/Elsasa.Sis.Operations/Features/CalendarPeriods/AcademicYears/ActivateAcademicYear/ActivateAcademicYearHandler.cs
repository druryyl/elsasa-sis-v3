namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ActivateAcademicYear;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the atomic activation of an approved academic year (ADR-004, ADR-005).
/// Demotes any currently active academic year to 'completed' within a Dapper transaction.
/// </summary>
public sealed class ActivateAcademicYearHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectSql = """
        SELECT
            id AS Id,
            code AS Code,
            name AS Name,
            status AS Status
        FROM operations.academic_years
        WHERE id = @Id;
        """;

    private const string _demoteActiveSql = """
        UPDATE operations.academic_years
        SET status = 'completed',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE status = 'active';
        """;

    private const string _activateTargetSql = """
        UPDATE operations.academic_years
        SET status = 'active',
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    /// <summary>
    /// Activates an approved academic year atomically demoting any existing active year to 'completed'.
    /// </summary>
    public async Task<Result<ActivateAcademicYearResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<AcademicYearRow>(new CommandDefinition(
            _selectSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<ActivateAcademicYearResponse>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{id}' was not found."));
        }

        if (!string.Equals(existing.Status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ActivateAcademicYearResponse>(
                DomainError.Validation(
                    "Operations.InvalidStatusTransition",
                    $"Cannot activate academic year with status '{existing.Status}'. Only 'approved' academic years can be activated."));
        }

        var now = DateTimeOffset.UtcNow;
        var username = currentUser.Username ?? "system";

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // 1. Demote any currently active academic year to 'completed'
        await connection.ExecuteAsync(new CommandDefinition(
            _demoteActiveSql,
            new { UpdatedAt = now, UpdatedBy = username },
            transaction: transaction,
            cancellationToken: cancellationToken));

        // 2. Activate the target academic year
        await connection.ExecuteAsync(new CommandDefinition(
            _activateTargetSql,
            new { Id = id, UpdatedAt = now, UpdatedBy = username },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return Result.Success(new ActivateAcademicYearResponse(
            existing.Id,
            existing.Code,
            existing.Name,
            "active"));
    }

    private sealed record AcademicYearRow(
        Guid Id,
        string Code,
        string Name,
        string Status);
}
