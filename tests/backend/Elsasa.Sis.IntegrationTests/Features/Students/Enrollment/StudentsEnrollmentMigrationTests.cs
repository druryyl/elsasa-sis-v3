namespace Elsasa.Sis.IntegrationTests.Features.Students.Enrollment;

using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests verifying DbUp schema migration 20261103_0301 (P1-S01, P7-S11, ADR-002, ADR-006).
/// Validates script journaling, sequence creation, table structures, partial unique indexes, and referential actions.
/// </summary>
public sealed class StudentsEnrollmentMigrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261103_0301_Students_EnrollmentIdentity_Tables.sql";

    [SkippableFact]
    public async Task EnrollmentMigration_Should_BeJournaledInSchemaVersions()
    {
        SkipIfDatabaseUnavailable();

        const string checkScriptSql = """
            SELECT COUNT(1)
            FROM dbo._SchemaVersions
            WHERE ScriptName LIKE '%' + @ScriptName;
            """;

        var count = await ExecuteScalarAsync<int>(checkScriptSql, new { ScriptName = _migrationScriptName });
        count.Should().Be(1, $"migration script '{_migrationScriptName}' should be applied and journaled");
    }

    [SkippableFact]
    public async Task StudentNumberSequence_Should_ExistAndGenerateIncrementalValues()
    {
        SkipIfDatabaseUnavailable();

        const string seqSql = "SELECT NEXT VALUE FOR students.student_number_seq;";
        var seqVal1 = await ExecuteScalarAsync<int>(seqSql);
        var seqVal2 = await ExecuteScalarAsync<int>(seqSql);

        seqVal1.Should().BeGreaterThan(0);
        seqVal2.Should().Be(seqVal1 + 1);
    }

    [SkippableFact]
    public async Task StudentsTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'students' AND TABLE_NAME = 'students';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "student_number",
            "applicant_id",
            "full_name",
            "preferred_name",
            "date_of_birth",
            "place_of_birth",
            "gender",
            "nationality",
            "government_id_type",
            "government_id_number",
            "address",
            "phone",
            "email",
            "is_independent",
            "current_status",
            "enrollment_date",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('students.students') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("ux_students_students_student_number");
        indexes.Should().Contain("ux_students_students_applicant_id");
        indexes.Should().Contain("ux_students_students_gov_id");
        indexes.Should().Contain("ix_students_students_status_name");
    }

    [SkippableFact]
    public async Task EnrollmentsTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'students' AND TABLE_NAME = 'enrollments';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "student_id",
            "application_id",
            "enrollment_type",
            "enrollment_date",
            "initial_campus",
            "initial_level",
            "initial_academic_year",
            "initial_program",
            "initial_cohort",
            "approved_by",
            "enrollment_payment_confirmed",
            "payment_reference",
            "payment_waived",
            "waiver_reference",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('students.enrollments') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("ix_students_enrollments_student_date");
        indexes.Should().Contain("ix_students_enrollments_application");
    }

    [SkippableFact]
    public async Task GuardiansAndRelationships_Should_ContainExpectedIndexesAndPartialUniqueIndex()
    {
        SkipIfDatabaseUnavailable();

        const string guardianIndexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('students.guardians') AND name IS NOT NULL;
            """;

        var guardianIndexes = (await QueryAsync<string>(guardianIndexesSql)).ToList();
        guardianIndexes.Should().Contain("ux_students_guardians_gov_id");
        guardianIndexes.Should().Contain("ix_students_guardians_name");

        const string relIndexesSql = """
            SELECT name, has_filter, filter_definition
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('students.student_guardian_relationships') AND name = 'ux_students_rel_primary_active';
            """;

        var relIndex = (await QueryAsync<dynamic>(relIndexesSql)).Single();
        ((bool)relIndex.has_filter).Should().BeTrue();
        string filterDef = (string)relIndex.filter_definition;
        filterDef.Should().Contain("is_primary_contact");
        filterDef.Should().Contain("is_active");
    }

    [SkippableFact]
    public async Task StudentDocumentsTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'students' AND TABLE_NAME = 'student_documents';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "student_id",
            "document_type",
            "submission_status",
            "submitted_date",
            "verified_date",
            "verified_by",
            "file_reference",
            "file_name",
            "file_size_bytes",
            "content_type",
            "valid_until",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('students.student_documents') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("ix_students_student_documents_student_status");
        indexes.Should().Contain("ix_students_docs_student_type");
    }

    [SkippableFact]
    public async Task EnrollmentDomainForeignKeys_Should_SpecifyNoAction()
    {
        SkipIfDatabaseUnavailable();

        const string fkSql = """
            SELECT delete_referential_action
            FROM sys.foreign_keys
            WHERE parent_object_id IN (
                OBJECT_ID('students.students'),
                OBJECT_ID('students.enrollments'),
                OBJECT_ID('students.student_guardian_relationships'),
                OBJECT_ID('students.student_documents')
            );
            """;

        var deleteActions = (await QueryAsync<int>(fkSql)).ToList();
        deleteActions.Should().NotBeEmpty();
        deleteActions.Should().AllSatisfy(action => action.Should().Be(0, "all enrollment foreign keys must specify ON DELETE NO ACTION"));
    }
}
