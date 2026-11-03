namespace Elsasa.Sis.IntegrationTests.Features.Students.Admission;

using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests verifying the DbUp schema migration script for Student Admission (ADR-002, ADR-006, P1-S01).
/// Validates script execution logging, table evolutions, indexes, sequence generation, and foreign key constraints.
/// </summary>
public sealed class StudentsAdmissionMigrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261103_0201_Students_Admission_Tables.sql";

    [SkippableFact]
    public async Task AdmissionMigration_Should_BeJournaledInSchemaVersions()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        // Act
        const string checkScriptSql = """
            SELECT COUNT(1)
            FROM dbo._SchemaVersions
            WHERE ScriptName LIKE '%' + @ScriptName;
            """;

        var count = await ExecuteScalarAsync<int>(checkScriptSql, new { ScriptName = _migrationScriptName });

        // Assert
        count.Should().Be(1, $"migration script '{_migrationScriptName}' should be applied and journaled");
    }

    [SkippableFact]
    public async Task ApplicantsTable_Should_ContainAllEvolvedColumnsAndIndexes()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        // Act 1: Verify evolved columns in students.applicants
        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'students' AND TABLE_NAME = 'applicants';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        // Assert columns
        columns.Should().Contain([
            "id",
            "first_name",
            "last_name",
            "full_name",
            "date_of_birth",
            "gender",
            "preferred_name",
            "place_of_birth",
            "nationality",
            "government_id_type",
            "government_id_number",
            "address",
            "previous_school_name",
            "previous_school_address",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        // Act 2: Verify indexes
        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('students.applicants') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();

        indexes.Should().Contain("ux_students_applicants_gov_id");
        indexes.Should().Contain("ix_students_applicants_name_dob");
    }

    [SkippableFact]
    public async Task AdmissionApplicationsTable_Should_ContainAllEvolvedColumnsAndIndexes()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        // Act 1: Verify evolved columns in students.admission_applications
        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'students' AND TABLE_NAME = 'admission_applications';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        // Assert columns
        columns.Should().Contain([
            "id",
            "applicant_id",
            "academic_year_id",
            "admission_period_id",
            "application_number",
            "status",
            "application_type",
            "requested_entry_level",
            "requested_campus",
            "requested_program",
            "registration_date",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        // Act 2: Verify indexes
        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('students.admission_applications') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();

        indexes.Should().Contain("ux_students_admission_applications_active");
        indexes.Should().Contain("ix_students_admission_applications_period_status");
        indexes.Should().Contain("ix_students_admission_applications_reg_date");
    }

    [SkippableFact]
    public async Task ApplicationNumberSequence_Should_ExistAndGenerateValues()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        // Act
        const string seqSql = "SELECT NEXT VALUE FOR students.admission_application_number_seq;";
        var seqVal1 = await ExecuteScalarAsync<int>(seqSql);
        var seqVal2 = await ExecuteScalarAsync<int>(seqSql);

        // Assert
        seqVal1.Should().BeGreaterThan(0);
        seqVal2.Should().Be(seqVal1 + 1);
    }

    [SkippableFact]
    public async Task NewAdmissionDomainTables_Should_ExistWithNoActionForeignKeys()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        // Act 1: Verify 4 new tables exist
        const string tablesSql = """
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'students'
              AND TABLE_NAME IN (
                  'admission_decisions',
                  'applicant_documents',
                  'entrance_assessment_evidence',
                  'admission_application_contacts'
              );
            """;

        var tables = (await QueryAsync<string>(tablesSql)).ToList();

        tables.Should().HaveCount(4);
        tables.Should().Contain("admission_decisions");
        tables.Should().Contain("applicant_documents");
        tables.Should().Contain("entrance_assessment_evidence");
        tables.Should().Contain("admission_application_contacts");

        // Act 2: Verify foreign key delete actions are NO ACTION (0)
        const string fkSql = """
            SELECT delete_referential_action
            FROM sys.foreign_keys
            WHERE parent_object_id IN (
                OBJECT_ID('students.admission_decisions'),
                OBJECT_ID('students.applicant_documents'),
                OBJECT_ID('students.entrance_assessment_evidence'),
                OBJECT_ID('students.admission_application_contacts')
            );
            """;

        var deleteActions = (await QueryAsync<int>(fkSql)).ToList();
        deleteActions.Should().NotBeEmpty();
        deleteActions.Should().AllSatisfy(action => action.Should().Be(0, "all foreign keys must specify ON DELETE NO ACTION"));
    }
}
