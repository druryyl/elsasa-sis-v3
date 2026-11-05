namespace Elsasa.Sis.Students.Services;

using Dapper;
using Elsasa.Sis.Contracts.Students;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Authoritative implementation of the synchronous Students cross-domain query contract (ADR-003).
/// Provides enrollment status checks for cross-domain features.
/// </summary>
public sealed class StudentsModule(ISqlConnectionFactory connectionFactory) : IStudentsModule
{
    static StudentsModule()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkStudentEnrollmentSql = """
        SELECT TOP 1
            s.id AS Id,
            s.current_status AS CurrentStatus,
            st.status AS Status
        FROM students.students s
        LEFT JOIN students.student_statuses st
            ON st.student_id = s.id AND st.is_current = 1
        WHERE s.id = @StudentId;
        """;

    /// <inheritdoc />
    public async Task<Result<bool>> IsStudentEnrolledAsync(Guid studentId, CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(ct);

            var row = await connection.QuerySingleOrDefaultAsync<StudentRow>(new CommandDefinition(
                _checkStudentEnrollmentSql,
                new { StudentId = studentId },
                cancellationToken: ct));

            if (row is null)
            {
                return Result.Success(false);
            }

            // Prefer student_statuses status if available, otherwise fall back to denormalized current_status
            var statusToCheck = !string.IsNullOrWhiteSpace(row.Status) ? row.Status : row.CurrentStatus;

            // Consider student enrolled if status is 'active' or 'enrolled' (case-insensitive)
            var isEnrolled = string.Equals(statusToCheck, "active", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(statusToCheck, "enrolled", StringComparison.OrdinalIgnoreCase);

            return Result.Success(isEnrolled);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>(
                DomainError.Failure(
                    "Students.EnrollmentCheckFailed",
                    $"Failed to check enrollment for student '{studentId}': {ex.Message}"));
        }
    }

    private sealed record StudentRow(
        Guid Id,
        string CurrentStatus,
        string? Status);
}
