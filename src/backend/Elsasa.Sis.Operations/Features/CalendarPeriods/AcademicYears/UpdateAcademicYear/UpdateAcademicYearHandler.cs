namespace Elsasa.Sis.Operations.Features.CalendarPeriods.AcademicYears.UpdateAcademicYear;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler executing the update of a draft academic year (ADR-004, ADR-005).
/// </summary>
public sealed class UpdateAcademicYearHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectAcademicYearSql = """
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

    private const string _checkDuplicateCodeSql = """
        SELECT COUNT(1)
        FROM operations.academic_years
        WHERE code = @Code AND id <> @Id;
        """;

    private const string _updateAcademicYearSql = """
        UPDATE operations.academic_years
        SET code = @Code,
            name = @Name,
            start_date = @StartDate,
            end_date = @EndDate,
            notes = @Notes,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    /// <summary>
    /// Updates the code, name, dates, and notes on an existing draft academic year.
    /// </summary>
    public async Task<Result<UpdateAcademicYearResponse>> HandleAsync(
        Guid id,
        UpdateAcademicYearRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<AcademicYearRow>(new CommandDefinition(
            _selectAcademicYearSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<UpdateAcademicYearResponse>(
                DomainError.NotFound(
                    "Operations.AcademicYearNotFound",
                    $"Academic year with ID '{id}' was not found."));
        }

        if (!string.Equals(existing.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateAcademicYearResponse>(
                DomainError.Validation(
                    "Operations.AcademicYearNotModifiable",
                    $"Only academic years in 'draft' status can be modified directly. Current status is '{existing.Status}'."));
        }

        var duplicateCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkDuplicateCodeSql,
            new { request.Code, Id = id },
            cancellationToken: cancellationToken));

        if (duplicateCount > 0)
        {
            return Result.Failure<UpdateAcademicYearResponse>(
                DomainError.Conflict(
                    "Operations.DuplicateAcademicYearCode",
                    $"Academic year with code '{request.Code}' already exists."));
        }

        var updatedAt = DateTimeOffset.UtcNow;
        var updatedBy = currentUser.Username;

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _updateAcademicYearSql,
                new
                {
                    Id = id,
                    request.Code,
                    request.Name,
                    request.StartDate,
                    request.EndDate,
                    request.Notes,
                    UpdatedAt = updatedAt,
                    UpdatedBy = updatedBy,
                },
                cancellationToken: cancellationToken));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return Result.Failure<UpdateAcademicYearResponse>(
                DomainError.Conflict(
                    "Operations.DuplicateAcademicYearCode",
                    $"Academic year with code '{request.Code}' already exists."));
        }

        return Result.Success(new UpdateAcademicYearResponse(
            id,
            request.Code,
            request.Name,
            request.StartDate,
            request.EndDate,
            existing.Status,
            request.Notes));
    }

    private sealed record AcademicYearRow(
        Guid Id,
        string Code,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        string Status);
}
