namespace Elsasa.Sis.Students.Features.DemoData;

using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous purger for the Students domain schema (ADR-002, ADR-004).
/// Deletes all demo data from students.* tables in strict reverse dependency order.
/// </summary>
public sealed class StudentsDemoDataPurger(
    ISqlConnectionFactory connectionFactory) : IStudentsDemoDataPurger
{
    private const string _purgeSql = """
        DELETE FROM students.administrative_history_entries;
        DELETE FROM students.student_statuses;
        DELETE FROM students.administrative_placements;
        DELETE FROM students.student_documents;
        DELETE FROM students.student_guardian_relationships;
        DELETE FROM students.guardians;
        DELETE FROM students.enrollments;
        DELETE FROM students.students;
        DELETE FROM students.admission_application_contacts;
        DELETE FROM students.entrance_assessment_evidence;
        DELETE FROM students.applicant_documents;
        DELETE FROM students.admission_decisions;
        DELETE FROM students.admission_applications;
        DELETE FROM students.applicants;
        """;

    public async Task PurgeDomainDataAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(_purgeSql, cancellationToken: cancellationToken));
    }
}