namespace Elsasa.Sis.Students.Features.Enrollment.Services;

using System.Globalization;
using Dapper;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// SQL Server sequence-backed implementation of <see cref="IStudentNumberGenerator"/>
/// producing gap-free, monotonically increasing student numbers in the format STD-{EnrollmentYear}-{Sequence:D4} (ADR-004, TD-02).
/// </summary>
public sealed class SqlStudentNumberGenerator(ISqlConnectionFactory connectionFactory) : IStudentNumberGenerator
{
    private const string _nextSequenceSql = "SELECT NEXT VALUE FOR students.student_number_seq;";

    /// <inheritdoc />
    public async Task<string> GenerateNextStudentNumberAsync(int enrollmentYear, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        await using var connection = await connectionFactory.CreateConnectionAsync(ct);
        var nextVal = await connection.ExecuteScalarAsync<int>(new CommandDefinition(_nextSequenceSql, cancellationToken: ct));

        return FormatStudentNumber(enrollmentYear, nextVal);
    }

    /// <summary>
    /// Formats the sequential student number according to institutional standard STD-{EnrollmentYear}-{Sequence:D4}.
    /// </summary>
    /// <param name="enrollmentYear">Calendar or academic enrollment year (e.g. 2026).</param>
    /// <param name="sequence">Sequence value from students.student_number_seq.</param>
    /// <returns>Formatted student number string.</returns>
    public static string FormatStudentNumber(int enrollmentYear, int sequence)
    {
        return string.Create(CultureInfo.InvariantCulture, $"STD-{enrollmentYear}-{sequence:D4}");
    }
}
