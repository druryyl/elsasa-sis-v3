namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.ApproveAcademicYear;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the transition of an academic year from 'draft' to 'approved' (ADR-004, ADR-005).
/// </summary>
public sealed class ApproveAcademicYearHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectSql = """
        SELECT
            id AS Id,
            code AS Code,
            name AS Name,
            start_date AS StartDate,
            end_date AS EndDate,
            status AS Status
        FROM operations.academic_years
        WHERE id = @Id;
        """;

    private const string _updateSql = """
        UPDATE operations.academic_years
        SET status = 'approved',
            approved_by = @ApprovedBy,
            approved_date = @ApprovedDate,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    /// <summary>
    /// Approves an academic year record, capturing approver identity and date.
    /// </summary>
    public async Task<Result<ApproveAcademicYearResponse>> HandleAsync(
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
            return Result.Failure<ApproveAcademicYearResponse>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{id}' was not found."));
        }

        if (!string.Equals(existing.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ApproveAcademicYearResponse>(
                DomainError.Validation(
                    "Operations.InvalidStatusTransition",
                    $"Cannot approve academic year with status '{existing.Status}'. Only 'draft' academic years can be approved."));
        }

        var approvedBy = currentUser.Username ?? "system";
        var approvedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = approvedBy;

        await connection.ExecuteAsync(new CommandDefinition(
            _updateSql,
            new
            {
                Id = id,
                ApprovedBy = approvedBy,
                ApprovedDate = approvedDate,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy,
            },
            cancellationToken: cancellationToken));

        return Result.Success(new ApproveAcademicYearResponse(
            existing.Id,
            existing.Code,
            existing.Name,
            "approved",
            approvedBy,
            approvedDate));
    }

    private sealed record AcademicYearRow(
        Guid Id,
        string Code,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status);
}
