namespace Elsasa.Sis.Operations.Features.CalendarPeriods.CreateAcademicYear;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using Microsoft.Data.SqlClient;

/// <summary>
/// Feature handler executing the creation of an academic year in the Operations schema (ADR-004, ADR-005).
/// </summary>
public sealed class CreateAcademicYearHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _checkDuplicateCodeSql = """
        SELECT COUNT(1)
        FROM operations.academic_years
        WHERE code = @Code;
        """;

    private const string _insertAcademicYearSql = """
        INSERT INTO operations.academic_years (id, code, name, start_date, end_date, is_active, created_at, created_by)
        VALUES (@Id, @Code, @Name, @StartDate, @EndDate, @IsActive, @CreatedAt, @CreatedBy);
        """;

    /// <summary>
    /// Handles the creation of a new academic year record.
    /// </summary>
    /// <param name="request">The creation request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful result carrying the created academic year response, or a domain error.</returns>
    public async Task<Result<CreateAcademicYearResponse>> HandleAsync(
        CreateAcademicYearRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Check logical business constraint: code uniqueness
        var existingCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _checkDuplicateCodeSql,
            new { request.Code },
            cancellationToken: cancellationToken));

        if (existingCount > 0)
        {
            return Result.Failure<CreateAcademicYearResponse>(
                DomainError.Conflict(
                    "Operations.DuplicateAcademicYearCode",
                    $"Academic year with code '{request.Code}' already exists."));
        }

        // 2. Perform atomic insert
        var academicYearId = Guid.NewGuid();
        const bool isActive = true;
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = currentUser.Username;

        var parameters = new
        {
            Id = academicYearId,
            request.Code,
            request.Name,
            request.StartDate,
            request.EndDate,
            IsActive = isActive,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
        };

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _insertAcademicYearSql,
                parameters,
                cancellationToken: cancellationToken));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return Result.Failure<CreateAcademicYearResponse>(
                DomainError.Conflict(
                    "Operations.DuplicateAcademicYearCode",
                    $"Academic year with code '{request.Code}' already exists."));
        }

        return Result.Success(new CreateAcademicYearResponse(
            academicYearId,
            request.Code,
            request.Name,
            request.StartDate,
            request.EndDate,
            isActive));
    }
}
