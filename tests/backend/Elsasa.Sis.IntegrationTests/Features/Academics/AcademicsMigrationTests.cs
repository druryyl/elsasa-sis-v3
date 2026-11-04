namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using Elsasa.Sis.Database;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Integration test suite verifying DbUp schema migration 20261104_0401 (IS008 / CR-ACD-LEARNING-STRUCTURE-REQUIREMENTS, P1-S01, P4-S12, ADR-002, ADR-006).
/// Validates script journaling via DatabaseMigrator, provisioning of all 3 authoritative domain tables in schema academics
/// (learning_structures, learning_units, academic_requirements), columns, indexes, foreign key referential actions,
/// unique constraint enforcement on scoped unit codes, and check constraint validation on status, unit type, and scope values.
/// </summary>
public sealed class AcademicsMigrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261104_0401_Academics_LearningStructure_Tables.sql";

    [SkippableFact]
    public void AcademicsMigration_Should_RunSuccessfullyViaDatabaseMigrator()
    {
        SkipIfDatabaseUnavailable();

        var success = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);

        success.Should().BeTrue("DatabaseMigrator.UpgradeDatabase should succeed idempotently on target database");
    }

    [SkippableFact]
    public async Task AcademicsMigration_Should_BeJournaledInSchemaVersions()
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
    public async Task LearningStructuresTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'learning_structures';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "name",
            "description",
            "academic_period_id",
            "academic_period_label",
            "version",
            "effective_date",
            "end_date",
            "status",
            "approved_by",
            "approved_date",
            "supersedes_id",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.learning_structures') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_learning_structures");
        indexes.Should().Contain("IX_academics_learning_structures_period_status");
        indexes.Should().Contain("IX_academics_learning_structures_supersedes");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.learning_structures');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_learning_structures_supersedes");
        var supersedesFk = fks.Single(fk => fk.Name == "FK_academics_learning_structures_supersedes");
        supersedesFk.DeleteReferentialAction.Should().Be(0, "supersedes learning structure FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task LearningUnitsTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'learning_units';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "learning_structure_id",
            "parent_unit_id",
            "code",
            "name",
            "unit_type",
            "level",
            "description",
            "credit_weight",
            "status",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.learning_units') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_learning_units");
        indexes.Should().Contain("UQ_academics_learning_units_structure_code");
        indexes.Should().Contain("IX_academics_learning_units_structure_parent");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.learning_units');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        var fkNames = fks.Select(fk => fk.Name).ToList();
        fkNames.Should().Contain("FK_academics_learning_units_structure");
        fkNames.Should().Contain("FK_academics_learning_units_parent");

        fks.Should().AllSatisfy(fk =>
            fk.DeleteReferentialAction.Should().Be(0, "learning units foreign keys must specify ON DELETE NO ACTION"));
    }

    [SkippableFact]
    public async Task AcademicRequirementsTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'academic_requirements';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "name",
            "requirement_scope",
            "learning_structure_id",
            "learning_unit_id",
            "level",
            "requirement_type",
            "description",
            "version",
            "effective_date",
            "end_date",
            "status",
            "approved_by",
            "approved_date",
            "supersedes_id",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.academic_requirements') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_academic_requirements");
        indexes.Should().Contain("IX_academics_academic_requirements_structure_scope");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.academic_requirements');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        var fkNames = fks.Select(fk => fk.Name).ToList();
        fkNames.Should().Contain("FK_academics_academic_requirements_structure");
        fkNames.Should().Contain("FK_academics_academic_requirements_unit");
        fkNames.Should().Contain("FK_academics_academic_requirements_supersedes");

        fks.Should().AllSatisfy(fk =>
            fk.DeleteReferentialAction.Should().Be(0, "academic requirements foreign keys must specify ON DELETE NO ACTION"));
    }

    [SkippableFact]
    public async Task LearningUnits_UniqueConstraint_Should_RejectDuplicateCodes_InSameStructure()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum A");

        const string insertUnitSql = """
            INSERT INTO academics.learning_units (
                id, learning_structure_id, code, name, unit_type, status, created_by
            ) VALUES (
                @Id, @StructureId, @Code, @Name, 'subject', 'draft', 'test-user'
            );
            """;

        // First unit insertion should succeed
        await ExecuteAsync(insertUnitSql, new
        {
            Id = Guid.NewGuid(),
            StructureId = structureId,
            Code = "MATH101",
            Name = "General Mathematics"
        });

        // Second unit with same code in the same structure must violate unique constraint UQ_academics_learning_units_structure_code
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertUnitSql, new
            {
                Id = Guid.NewGuid(),
                StructureId = structureId,
                Code = "MATH101",
                Name = "Advanced Mathematics"
            });
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate unit code within the same structure must trigger SQL unique constraint violation (error 2601 or 2627)");

        // But the same code in a different structure should succeed
        var structureId2 = await CreateLearningStructureAsync("Curriculum B");
        var actDifferentStructure = async () =>
        {
            await ExecuteAsync(insertUnitSql, new
            {
                Id = Guid.NewGuid(),
                StructureId = structureId2,
                Code = "MATH101",
                Name = "General Mathematics"
            });
        };

        await actDifferentStructure.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task LearningStructures_CheckConstraint_Should_RejectInvalidStatus()
    {
        SkipIfDatabaseUnavailable();

        const string sql = """
            INSERT INTO academics.learning_structures (
                id, name, academic_period_id, academic_period_label, version, effective_date, status, created_by
            ) VALUES (
                @Id, 'Test Structure', @PeriodId, '2026/2027 Ganjil', '1.0', '2026-08-01', @Status, 'test-user'
            );
            """;

        var act = async () =>
        {
            await ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                PeriodId = Guid.NewGuid(),
                Status = "invalid_status"
            });
        };

        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid learning structure status must trigger SQL check constraint violation (error 547)");
    }

    [SkippableFact]
    public async Task LearningUnits_CheckConstraint_Should_RejectInvalidUnitType()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Unit Type Test");

        const string sql = """
            INSERT INTO academics.learning_units (
                id, learning_structure_id, code, name, unit_type, status, created_by
            ) VALUES (
                @Id, @StructureId, 'CODE01', 'Test Unit', @UnitType, 'draft', 'test-user'
            );
            """;

        var act = async () =>
        {
            await ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                StructureId = structureId,
                UnitType = "invalid_unit_type"
            });
        };

        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid unit type must trigger SQL check constraint violation (error 547)");
    }

    [SkippableFact]
    public async Task LearningUnits_CheckConstraint_Should_RejectInvalidStatus()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Unit Status Test");

        const string sql = """
            INSERT INTO academics.learning_units (
                id, learning_structure_id, code, name, unit_type, status, created_by
            ) VALUES (
                @Id, @StructureId, 'CODE02', 'Test Unit', 'subject', @Status, 'test-user'
            );
            """;

        var act = async () =>
        {
            await ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                StructureId = structureId,
                Status = "invalid_status"
            });
        };

        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid unit status must trigger SQL check constraint violation (error 547)");
    }

    [SkippableFact]
    public async Task LearningUnits_CheckConstraint_Should_RejectSelfParent()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Self Parent Test");
        var unitId = Guid.NewGuid();

        const string sql = """
            INSERT INTO academics.learning_units (
                id, learning_structure_id, parent_unit_id, code, name, unit_type, status, created_by
            ) VALUES (
                @Id, @StructureId, @ParentUnitId, 'CODE03', 'Self Parenting Unit', 'subject', 'draft', 'test-user'
            );
            """;

        var act = async () =>
        {
            await ExecuteAsync(sql, new
            {
                Id = unitId,
                StructureId = structureId,
                ParentUnitId = unitId
            });
        };

        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "self-parenting unit must trigger SQL check constraint violation (error 547)");
    }

    [SkippableFact]
    public async Task AcademicRequirements_CheckConstraint_Should_RejectInvalidScope()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Requirement Scope Test");

        const string sql = """
            INSERT INTO academics.academic_requirements (
                id, name, requirement_scope, learning_structure_id, requirement_type, description, version, effective_date, status, created_by
            ) VALUES (
                @Id, 'Test Requirement', @Scope, @StructureId, 'completion', 'Completion description', '1.0', '2026-08-01', 'draft', 'test-user'
            );
            """;

        var act = async () =>
        {
            await ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                Scope = "invalid_scope",
                StructureId = structureId
            });
        };

        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid requirement scope must trigger SQL check constraint violation (error 547)");
    }

    [SkippableFact]
    public async Task AcademicRequirements_CheckConstraint_Should_RejectInvalidType()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Requirement Type Test");

        const string sql = """
            INSERT INTO academics.academic_requirements (
                id, name, requirement_scope, learning_structure_id, requirement_type, description, version, effective_date, status, created_by
            ) VALUES (
                @Id, 'Test Requirement', 'program', @StructureId, @Type, 'Completion description', '1.0', '2026-08-01', 'draft', 'test-user'
            );
            """;

        var act = async () =>
        {
            await ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                Type = "invalid_type",
                StructureId = structureId
            });
        };

        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid requirement type must trigger SQL check constraint violation (error 547)");
    }

    [SkippableFact]
    public async Task AcademicRequirements_CheckConstraint_Should_RejectInvalidStatus()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Requirement Status Test");

        const string sql = """
            INSERT INTO academics.academic_requirements (
                id, name, requirement_scope, learning_structure_id, requirement_type, description, version, effective_date, status, created_by
            ) VALUES (
                @Id, 'Test Requirement', 'program', @StructureId, 'completion', 'Completion description', '1.0', '2026-08-01', @Status, 'test-user'
            );
            """;

        var act = async () =>
        {
            await ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                Status = "invalid_status",
                StructureId = structureId
            });
        };

        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid requirement status must trigger SQL check constraint violation (error 547)");
    }

    private async Task<Guid> CreateLearningStructureAsync(string name)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.learning_structures (
                id, name, academic_period_id, academic_period_label, version, effective_date, status, created_by
            ) VALUES (
                @Id, @Name, @PeriodId, '2026/2027 Ganjil', '1.0', '2026-08-01', 'draft', 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            Name = name,
            PeriodId = Guid.NewGuid()
        });

        return id;
    }

    private sealed record ForeignKeyInfo(string Name, byte DeleteReferentialAction);
}
