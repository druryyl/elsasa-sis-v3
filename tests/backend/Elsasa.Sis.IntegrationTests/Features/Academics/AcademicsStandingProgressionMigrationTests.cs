namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using Elsasa.Sis.Database;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Integration test suite verifying DbUp schema migration 20261107_0401 (IS012 / CR-ACD-STANDING-PROGRESSION, P1-S01, P6-S16, ADR-001, ADR-002, ADR-006).
/// Validates script journaling via DatabaseMigrator, provisioning of all 5 authoritative Submodule 4 domain tables in schema academics
/// (learning_progress, academic_standings, promotion_recommendations, academic_interventions, academic_achievements),
/// column names and types, intra-schema foreign keys (with ON DELETE NO ACTION and ON DELETE SET NULL),
/// composite unique constraints (student/unit/period, student/period), and check constraints across all 5 tables.
/// </summary>
public sealed class AcademicsStandingProgressionMigrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261107_0401_Academics_StandingProgression_Tables.sql";

    [SkippableFact]
    public void AcademicsStandingProgressionMigration_Should_RunSuccessfullyViaDatabaseMigrator()
    {
        SkipIfDatabaseUnavailable();

        var success = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);

        success.Should().BeTrue("DatabaseMigrator.UpgradeDatabase should succeed idempotently on target database");
    }

    [SkippableFact]
    public async Task AcademicsStandingProgressionMigration_Should_BeJournaledInSchemaVersions()
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
    public async Task LearningProgressTable_Should_ContainAllColumnsAndDataTypesAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME AS Name, DATA_TYPE AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'learning_progress';
            """;

        var columns = (await QueryAsync<ColumnInfo>(columnsSql)).ToDictionary(c => c.Name, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        columns.Should().ContainKey("id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("student_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("learning_unit_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("academic_period_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("status").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("basis").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("completion_date").WhoseValue.Should().Be("date");
        columns.Should().ContainKey("notes").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("created_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("updated_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("updated_by").WhoseValue.Should().Be("nvarchar");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.learning_progress') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_learning_progress");
        indexes.Should().Contain("IX_academics_learning_progress_student_unit_period");
        indexes.Should().Contain("IX_academics_learning_progress_unit");
        indexes.Should().Contain("IX_academics_learning_progress_period");
        indexes.Should().Contain("IX_academics_learning_progress_status");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.learning_progress');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_learning_progress_unit");
        var unitFk = fks.Single(fk => fk.Name == "FK_academics_learning_progress_unit");
        unitFk.DeleteReferentialAction.Should().Be(0, "learning_progress unit FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task AcademicStandingsTable_Should_ContainAllColumnsAndDataTypesAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME AS Name, DATA_TYPE AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'academic_standings';
            """;

        var columns = (await QueryAsync<ColumnInfo>(columnsSql)).ToDictionary(c => c.Name, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        columns.Should().ContainKey("id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("student_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("academic_period_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("standing").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("policy_reference").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("conditions").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("is_exception").WhoseValue.Should().Be("bit");
        columns.Should().ContainKey("exception_reason").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("committee_reference").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("authorized_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("determined_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("updated_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("updated_by").WhoseValue.Should().Be("nvarchar");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.academic_standings') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_academic_standings");
        indexes.Should().Contain("IX_academics_academic_standings_student_period");
        indexes.Should().Contain("IX_academics_academic_standings_period");
        indexes.Should().Contain("IX_academics_academic_standings_standing");
    }

    [SkippableFact]
    public async Task PromotionRecommendationsTable_Should_ContainAllColumnsAndDataTypesAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME AS Name, DATA_TYPE AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'promotion_recommendations';
            """;

        var columns = (await QueryAsync<ColumnInfo>(columnsSql)).ToDictionary(c => c.Name, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        columns.Should().ContainKey("id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("student_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("academic_period_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("recommendation").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("from_level").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("recommended_to_level").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("conditions").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("supporting_evidence").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("academic_standing_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("is_exception").WhoseValue.Should().Be("bit");
        columns.Should().ContainKey("exception_reason").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("committee_reference").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("authorized_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("issued_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("updated_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("updated_by").WhoseValue.Should().Be("nvarchar");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.promotion_recommendations') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_promotion_recommendations");
        indexes.Should().Contain("IX_academics_promotion_recommendations_student_period");
        indexes.Should().Contain("IX_academics_promotion_recommendations_standing");
        indexes.Should().Contain("IX_academics_promotion_recommendations_period");
        indexes.Should().Contain("IX_academics_promotion_recommendations_recommendation");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.promotion_recommendations');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_promotion_recommendations_standing");
        var standingFk = fks.Single(fk => fk.Name == "FK_academics_promotion_recommendations_standing");
        standingFk.DeleteReferentialAction.Should().Be(2, "promotion_recommendations standing FK must specify ON DELETE SET NULL");
    }

    [SkippableFact]
    public async Task AcademicInterventionsTable_Should_ContainAllColumnsAndDataTypesAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME AS Name, DATA_TYPE AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'academic_interventions';
            """;

        var columns = (await QueryAsync<ColumnInfo>(columnsSql)).ToDictionary(c => c.Name, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        columns.Should().ContainKey("id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("student_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("learning_unit_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("academic_period_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("intervention_type").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("trigger_type").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("trigger_description").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("plan_description").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("status").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("initiated_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("initiated_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("concluded_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("outcome_notes").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("created_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("updated_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("updated_by").WhoseValue.Should().Be("nvarchar");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.academic_interventions') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_academic_interventions");
        indexes.Should().Contain("IX_academics_academic_interventions_student");
        indexes.Should().Contain("IX_academics_academic_interventions_unit");
        indexes.Should().Contain("IX_academics_academic_interventions_period");
        indexes.Should().Contain("IX_academics_academic_interventions_status");
        indexes.Should().Contain("IX_academics_academic_interventions_type");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.academic_interventions');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_academic_interventions_unit");
        var unitFk = fks.Single(fk => fk.Name == "FK_academics_academic_interventions_unit");
        unitFk.DeleteReferentialAction.Should().Be(0, "academic_interventions unit FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task AcademicAchievementsTable_Should_ContainAllColumnsAndDataTypesAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME AS Name, DATA_TYPE AS DataType
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'academic_achievements';
            """;

        var columns = (await QueryAsync<ColumnInfo>(columnsSql)).ToDictionary(c => c.Name, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        columns.Should().ContainKey("id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("student_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("learning_unit_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("academic_period_id").WhoseValue.Should().Be("uniqueidentifier");
        columns.Should().ContainKey("achievement_type").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("title").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("description").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("criteria_met").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("supporting_evidence").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("recognized_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("recognized_at").WhoseValue.Should().Be("date");
        columns.Should().ContainKey("created_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("created_by").WhoseValue.Should().Be("nvarchar");
        columns.Should().ContainKey("updated_at").WhoseValue.Should().Be("datetimeoffset");
        columns.Should().ContainKey("updated_by").WhoseValue.Should().Be("nvarchar");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.academic_achievements') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_academic_achievements");
        indexes.Should().Contain("IX_academics_academic_achievements_student");
        indexes.Should().Contain("IX_academics_academic_achievements_period");
        indexes.Should().Contain("IX_academics_academic_achievements_unit");
        indexes.Should().Contain("IX_academics_academic_achievements_type");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.academic_achievements');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_academic_achievements_unit");
        var unitFk = fks.Single(fk => fk.Name == "FK_academics_academic_achievements_unit");
        unitFk.DeleteReferentialAction.Should().Be(2, "academic_achievements unit FK must specify ON DELETE SET NULL");
    }

    [SkippableFact]
    public async Task LearningProgress_UniqueIndex_Should_RejectDuplicateStudentUnitAndPeriod()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Progress UQ Test");
        var unit1Id = await CreateLearningUnitAsync(structureId, "UNIT-PROG-UQ1", "Progress Unit 1");
        var unit2Id = await CreateLearningUnitAsync(structureId, "UNIT-PROG-UQ2", "Progress Unit 2");

        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();
        var period1Id = Guid.NewGuid();
        var period2Id = Guid.NewGuid();

        const string insertSql = """
            INSERT INTO academics.learning_progress (
                id, student_id, learning_unit_id, academic_period_id, status, basis, created_by
            ) VALUES (
                @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, 'in_progress', 'learning_work', 'test-user'
            );
            """;

        // 1. Initial insert for (student1, unit1, period1) must succeed
        await ExecuteAsync(insertSql, new
        {
            Id = Guid.NewGuid(),
            StudentId = student1Id,
            LearningUnitId = unit1Id,
            AcademicPeriodId = period1Id
        });

        // 2. Duplicate insert with same (student1, unit1, period1) must violate unique index IX_academics_learning_progress_student_unit_period (2601 or 2627)
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = student1Id,
                LearningUnitId = unit1Id,
                AcademicPeriodId = period1Id
            });
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate progress record for same (student, unit, period) must trigger SQL unique constraint violation (error 2601 or 2627)");

        // 3. Same student, same period, but different unit must succeed
        var actDifferentUnit = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = student1Id,
                LearningUnitId = unit2Id,
                AcademicPeriodId = period1Id
            });
        };
        await actDifferentUnit.Should().NotThrowAsync();

        // 4. Same student, same unit, but different period must succeed
        var actDifferentPeriod = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = student1Id,
                LearningUnitId = unit1Id,
                AcademicPeriodId = period2Id
            });
        };
        await actDifferentPeriod.Should().NotThrowAsync();

        // 5. Different student with same unit and period must succeed
        var actDifferentStudent = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = student2Id,
                LearningUnitId = unit1Id,
                AcademicPeriodId = period1Id
            });
        };
        await actDifferentStudent.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task AcademicStandings_UniqueIndex_Should_RejectDuplicateStudentAndPeriod()
    {
        SkipIfDatabaseUnavailable();

        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();
        var period1Id = Guid.NewGuid();
        var period2Id = Guid.NewGuid();

        const string insertSql = """
            INSERT INTO academics.academic_standings (
                id, student_id, academic_period_id, standing, policy_reference, authorized_by, created_by
            ) VALUES (
                @Id, @StudentId, @AcademicPeriodId, 'good_standing', 'POL-2026-01', 'Academic Dean', 'test-user'
            );
            """;

        // 1. Initial insert for (student1, period1) must succeed
        await ExecuteAsync(insertSql, new
        {
            Id = Guid.NewGuid(),
            StudentId = student1Id,
            AcademicPeriodId = period1Id
        });

        // 2. Duplicate insert with same (student1, period1) must violate unique index IX_academics_academic_standings_student_period (2601 or 2627)
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = student1Id,
                AcademicPeriodId = period1Id
            });
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate standing record for same (student, period) must trigger SQL unique constraint violation (error 2601 or 2627)");

        // 3. Same student with different period must succeed
        var actDifferentPeriod = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = student1Id,
                AcademicPeriodId = period2Id
            });
        };
        await actDifferentPeriod.Should().NotThrowAsync();

        // 4. Different student with same period must succeed
        var actDifferentStudent = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = student2Id,
                AcademicPeriodId = period1Id
            });
        };
        await actDifferentStudent.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task PromotionRecommendations_UniqueIndex_Should_RejectDuplicateStudentAndPeriod()
    {
        SkipIfDatabaseUnavailable();

        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();
        var period1Id = Guid.NewGuid();
        var period2Id = Guid.NewGuid();

        const string insertSql = """
            INSERT INTO academics.promotion_recommendations (
                id, student_id, academic_period_id, recommendation, from_level,
                recommended_to_level, supporting_evidence, authorized_by, created_by
            ) VALUES (
                @Id, @StudentId, @AcademicPeriodId, 'promotion_eligible', 'Grade 4',
                'Grade 5', 'All learning objectives met', 'Principal', 'test-user'
            );
            """;

        // 1. Initial insert for (student1, period1) must succeed
        await ExecuteAsync(insertSql, new
        {
            Id = Guid.NewGuid(),
            StudentId = student1Id,
            AcademicPeriodId = period1Id
        });

        // 2. Duplicate insert with same (student1, period1) must violate unique index IX_academics_promotion_recommendations_student_period (2601 or 2627)
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = student1Id,
                AcademicPeriodId = period1Id
            });
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate promotion recommendation for same (student, period) must trigger SQL unique constraint violation (error 2601 or 2627)");

        // 3. Same student with different period must succeed
        var actDifferentPeriod = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = student1Id,
                AcademicPeriodId = period2Id
            });
        };
        await actDifferentPeriod.Should().NotThrowAsync();

        // 4. Different student with same period must succeed
        var actDifferentStudent = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = student2Id,
                AcademicPeriodId = period1Id
            });
        };
        await actDifferentStudent.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task LearningProgress_CheckConstraints_Should_EnforceValidStatusAndBasis()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Progress CK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-PROG-CK", "Progress CK Unit");

        const string insertSql = """
            INSERT INTO academics.learning_progress (
                id, student_id, learning_unit_id, academic_period_id, status, basis, created_by
            ) VALUES (
                @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, @Status, @Basis, 'test-user'
            );
            """;

        // 1. Invalid status must fail (547)
        var actInvalidStatus = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                LearningUnitId = unitId,
                AcademicPeriodId = Guid.NewGuid(),
                Status = "invalid_progress_status",
                Basis = "assessment_evidence"
            });
        };
        var exStatus = await actInvalidStatus.Should().ThrowAsync<SqlException>();
        exStatus.Which.Number.Should().Be(547, "invalid progress status must violate CK_academics_learning_progress_status (547)");

        // 2. Invalid basis must fail (547)
        var actInvalidBasis = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                LearningUnitId = unitId,
                AcademicPeriodId = Guid.NewGuid(),
                Status = "in_progress",
                Basis = "invalid_progress_basis"
            });
        };
        var exBasis = await actInvalidBasis.Should().ThrowAsync<SqlException>();
        exBasis.Which.Number.Should().Be(547, "invalid progress basis must violate CK_academics_learning_progress_basis (547)");

        // 3. Valid status values must all succeed
        string[] validStatuses = ["not_started", "in_progress", "completed", "needs_remediation", "exempted"];
        foreach (var status in validStatuses)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(),
                    LearningUnitId = unitId,
                    AcademicPeriodId = Guid.NewGuid(),
                    Status = status,
                    Basis = "assessment_evidence"
                });
            };
            await act.Should().NotThrowAsync($"status '{status}' should be accepted");
        }

        // 4. Valid basis values must all succeed
        string[] validBases = ["assessment_evidence", "learning_work", "authorized_decision"];
        foreach (var basis in validBases)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(),
                    LearningUnitId = unitId,
                    AcademicPeriodId = Guid.NewGuid(),
                    Status = "in_progress",
                    Basis = basis
                });
            };
            await act.Should().NotThrowAsync($"basis '{basis}' should be accepted");
        }
    }

    [SkippableFact]
    public async Task AcademicStandings_CheckConstraints_Should_EnforceValidStanding()
    {
        SkipIfDatabaseUnavailable();

        const string insertSql = """
            INSERT INTO academics.academic_standings (
                id, student_id, academic_period_id, standing, policy_reference, authorized_by, created_by
            ) VALUES (
                @Id, @StudentId, @AcademicPeriodId, @Standing, 'POL-2026-01', 'Academic Dean', 'test-user'
            );
            """;

        // 1. Invalid standing must fail (547)
        var actInvalidStanding = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                AcademicPeriodId = Guid.NewGuid(),
                Standing = "invalid_standing_value"
            });
        };
        var ex = await actInvalidStanding.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid standing must violate CK_academics_academic_standings_standing (547)");

        // 2. Valid standing values must all succeed
        string[] validStandings = ["good_standing", "conditional_standing", "not_in_good_standing"];
        foreach (var standing in validStandings)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(),
                    AcademicPeriodId = Guid.NewGuid(),
                    Standing = standing
                });
            };
            await act.Should().NotThrowAsync($"standing '{standing}' should be accepted");
        }
    }

    [SkippableFact]
    public async Task PromotionRecommendations_CheckConstraints_Should_EnforceValidRecommendation()
    {
        SkipIfDatabaseUnavailable();

        const string insertSql = """
            INSERT INTO academics.promotion_recommendations (
                id, student_id, academic_period_id, recommendation, from_level,
                recommended_to_level, supporting_evidence, authorized_by, created_by
            ) VALUES (
                @Id, @StudentId, @AcademicPeriodId, @Recommendation, 'Grade 4',
                'Grade 5', 'Evidence summary', 'Principal', 'test-user'
            );
            """;

        // 1. Invalid recommendation must fail (547)
        var actInvalid = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                AcademicPeriodId = Guid.NewGuid(),
                Recommendation = "invalid_recommendation"
            });
        };
        var ex = await actInvalid.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid recommendation must violate CK_academics_promotion_recommendations_recommendation (547)");

        // 2. Valid recommendation values must all succeed
        string[] validRecommendations = ["promotion_eligible", "retention_recommended", "conditional_promotion"];
        foreach (var rec in validRecommendations)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(),
                    AcademicPeriodId = Guid.NewGuid(),
                    Recommendation = rec
                });
            };
            await act.Should().NotThrowAsync($"recommendation '{rec}' should be accepted");
        }
    }

    [SkippableFact]
    public async Task AcademicInterventions_CheckConstraints_Should_EnforceValidTypeTriggerAndStatus()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Intervention CK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-INT-CK", "Intervention CK Unit");

        const string insertSql = """
            INSERT INTO academics.academic_interventions (
                id, student_id, learning_unit_id, academic_period_id, intervention_type,
                trigger_type, trigger_description, plan_description, status, initiated_by, created_by
            ) VALUES (
                @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, @InterventionType,
                @TriggerType, 'Trigger details', 'Support plan', @Status, 'Teacher A', 'test-user'
            );
            """;

        // 1. Invalid intervention_type must fail (547)
        var actInvalidType = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                LearningUnitId = unitId,
                AcademicPeriodId = Guid.NewGuid(),
                InterventionType = "invalid_type",
                TriggerType = "assessment_below_threshold",
                Status = "planned"
            });
        };
        var exType = await actInvalidType.Should().ThrowAsync<SqlException>();
        exType.Which.Number.Should().Be(547, "invalid intervention_type must violate CK_academics_academic_interventions_type (547)");

        // 2. Invalid trigger_type must fail (547)
        var actInvalidTrigger = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                LearningUnitId = unitId,
                AcademicPeriodId = Guid.NewGuid(),
                InterventionType = "remediation",
                TriggerType = "invalid_trigger",
                Status = "planned"
            });
        };
        var exTrigger = await actInvalidTrigger.Should().ThrowAsync<SqlException>();
        exTrigger.Which.Number.Should().Be(547, "invalid trigger_type must violate CK_academics_academic_interventions_trigger (547)");

        // 3. Invalid status must fail (547)
        var actInvalidStatus = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                LearningUnitId = unitId,
                AcademicPeriodId = Guid.NewGuid(),
                InterventionType = "remediation",
                TriggerType = "assessment_below_threshold",
                Status = "invalid_status"
            });
        };
        var exStatus = await actInvalidStatus.Should().ThrowAsync<SqlException>();
        exStatus.Which.Number.Should().Be(547, "invalid status must violate CK_academics_academic_interventions_status (547)");

        // 4. Valid intervention_type values must all succeed
        string[] validTypes =
        [
            "remediation",
            "reassessment",
            "tutoring_recommendation",
            "extension_work",
            "recovery_plan",
            "learning_plan_adjustment",
            "academic_warning"
        ];
        foreach (var type in validTypes)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(),
                    LearningUnitId = unitId,
                    AcademicPeriodId = Guid.NewGuid(),
                    InterventionType = type,
                    TriggerType = "teacher_referral",
                    Status = "planned"
                });
            };
            await act.Should().NotThrowAsync($"intervention_type '{type}' should be accepted");
        }

        // 5. Valid trigger_type values must all succeed
        string[] validTriggers =
        [
            "assessment_below_threshold",
            "missing_work",
            "attendance_concern",
            "standing_concern",
            "teacher_referral"
        ];
        foreach (var trigger in validTriggers)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(),
                    LearningUnitId = unitId,
                    AcademicPeriodId = Guid.NewGuid(),
                    InterventionType = "remediation",
                    TriggerType = trigger,
                    Status = "planned"
                });
            };
            await act.Should().NotThrowAsync($"trigger_type '{trigger}' should be accepted");
        }

        // 6. Valid status values must all succeed
        string[] validStatuses = ["planned", "in_progress", "completed", "cancelled"];
        foreach (var status in validStatuses)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(),
                    LearningUnitId = unitId,
                    AcademicPeriodId = Guid.NewGuid(),
                    InterventionType = "remediation",
                    TriggerType = "teacher_referral",
                    Status = status
                });
            };
            await act.Should().NotThrowAsync($"status '{status}' should be accepted");
        }
    }

    [SkippableFact]
    public async Task AcademicAchievements_CheckConstraints_Should_EnforceValidAchievementType()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum Achievement CK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-ACH-CK", "Achievement CK Unit");

        const string insertSql = """
            INSERT INTO academics.academic_achievements (
                id, student_id, learning_unit_id, academic_period_id, achievement_type,
                title, criteria_met, recognized_by, recognized_at, created_by
            ) VALUES (
                @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, @AchievementType,
                'Honor Award', 'Exceeded expectation', 'Principal', '2026-06-15', 'test-user'
            );
            """;

        // 1. Invalid achievement_type must fail (547)
        var actInvalid = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                LearningUnitId = unitId,
                AcademicPeriodId = Guid.NewGuid(),
                AchievementType = "invalid_achievement_type"
            });
        };
        var ex = await actInvalid.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid achievement_type must violate CK_academics_academic_achievements_type (547)");

        // 2. Valid achievement_type values must all succeed
        string[] validTypes =
        [
            "subject_distinction",
            "academic_honors",
            "mastery_recognition",
            "academic_certification",
            "academic_competition"
        ];
        foreach (var type in validTypes)
        {
            var act = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    StudentId = Guid.NewGuid(),
                    LearningUnitId = unitId,
                    AcademicPeriodId = Guid.NewGuid(),
                    AchievementType = type
                });
            };
            await act.Should().NotThrowAsync($"achievement_type '{type}' should be accepted");
        }
    }

    [SkippableFact]
    public async Task IntraSchemaForeignKeys_Should_EnforceReferentialIntegrity_And_DeleteRules()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Curriculum FK Test");
        var unitId = await CreateLearningUnitAsync(structureId, "UNIT-FK-01", "Unit FK Test 1");
        var nonExistentId = Guid.NewGuid();

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        // 1. Non-existent learning_unit_id on learning_progress must fail (547)
        var actInvalidProgressUnit = async () =>
        {
            await ExecuteAsync("""
                INSERT INTO academics.learning_progress (
                    id, student_id, learning_unit_id, academic_period_id, status, basis, created_by
                ) VALUES (
                    @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, 'in_progress', 'learning_work', 'test-user'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                LearningUnitId = nonExistentId,
                AcademicPeriodId = periodId
            });
        };
        var ex1 = await actInvalidProgressUnit.Should().ThrowAsync<SqlException>();
        ex1.Which.Number.Should().Be(547, "non-existent learning_unit_id on learning_progress must violate FK_academics_learning_progress_unit (547)");

        // 2. Non-existent learning_unit_id on academic_interventions must fail (547)
        var actInvalidInterventionUnit = async () =>
        {
            await ExecuteAsync("""
                INSERT INTO academics.academic_interventions (
                    id, student_id, learning_unit_id, academic_period_id, intervention_type,
                    trigger_type, trigger_description, plan_description, status, initiated_by, created_by
                ) VALUES (
                    @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, 'remediation',
                    'assessment_below_threshold', 'Below passing', 'Plan', 'planned', 'Teacher', 'test-user'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                LearningUnitId = nonExistentId,
                AcademicPeriodId = periodId
            });
        };
        var ex2 = await actInvalidInterventionUnit.Should().ThrowAsync<SqlException>();
        ex2.Which.Number.Should().Be(547, "non-existent learning_unit_id on academic_interventions must violate FK_academics_academic_interventions_unit (547)");

        // 3. Non-existent non-null learning_unit_id on academic_achievements must fail (547)
        var actInvalidAchievementUnit = async () =>
        {
            await ExecuteAsync("""
                INSERT INTO academics.academic_achievements (
                    id, student_id, learning_unit_id, academic_period_id, achievement_type,
                    title, criteria_met, recognized_by, recognized_at, created_by
                ) VALUES (
                    @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, 'subject_distinction',
                    'Honor Title', 'Met criteria', 'Authority', '2026-06-01', 'test-user'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                LearningUnitId = nonExistentId,
                AcademicPeriodId = periodId
            });
        };
        var ex3 = await actInvalidAchievementUnit.Should().ThrowAsync<SqlException>();
        ex3.Which.Number.Should().Be(547, "non-existent learning_unit_id on academic_achievements must violate FK_academics_academic_achievements_unit (547)");

        // 4. Non-existent non-null academic_standing_id on promotion_recommendations must fail (547)
        var actInvalidRecStanding = async () =>
        {
            await ExecuteAsync("""
                INSERT INTO academics.promotion_recommendations (
                    id, student_id, academic_period_id, recommendation, from_level,
                    recommended_to_level, supporting_evidence, academic_standing_id, authorized_by, created_by
                ) VALUES (
                    @Id, @StudentId, @AcademicPeriodId, 'promotion_eligible', 'Grade 4',
                    'Grade 5', 'Evidence', @AcademicStandingId, 'Principal', 'test-user'
                );
                """, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                AcademicPeriodId = periodId,
                AcademicStandingId = nonExistentId
            });
        };
        var ex4 = await actInvalidRecStanding.Should().ThrowAsync<SqlException>();
        ex4.Which.Number.Should().Be(547, "non-existent academic_standing_id on promotion_recommendations must violate FK_academics_promotion_recommendations_standing (547)");

        // 5. ON DELETE NO ACTION: deleting learning_unit referenced by learning_progress must fail (547)
        var deleteTestUnit1 = await CreateLearningUnitAsync(structureId, "UNIT-DEL-NOACT", "Unit Delete NoAction");
        await ExecuteAsync("""
            INSERT INTO academics.learning_progress (
                id, student_id, learning_unit_id, academic_period_id, status, basis, created_by
            ) VALUES (
                @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, 'completed', 'assessment_evidence', 'test-user'
            );
            """, new
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            LearningUnitId = deleteTestUnit1,
            AcademicPeriodId = periodId
        });

        var actDeleteUnit1 = async () =>
        {
            await ExecuteAsync("DELETE FROM academics.learning_units WHERE id = @Id;", new { Id = deleteTestUnit1 });
        };
        var exDel1 = await actDeleteUnit1.Should().ThrowAsync<SqlException>();
        exDel1.Which.Number.Should().Be(547, "deleting learning_unit referenced by learning_progress must be blocked by ON DELETE NO ACTION (547)");

        // 6. ON DELETE SET NULL: deleting academic_standings referenced by promotion_recommendations sets FK to NULL
        var standingId = await CreateAcademicStandingAsync(studentId, periodId, "good_standing");
        var recommendationId = Guid.NewGuid();
        await ExecuteAsync("""
            INSERT INTO academics.promotion_recommendations (
                id, student_id, academic_period_id, recommendation, from_level,
                recommended_to_level, supporting_evidence, academic_standing_id, authorized_by, created_by
            ) VALUES (
                @Id, @StudentId, @AcademicPeriodId, 'promotion_eligible', 'Grade 4',
                'Grade 5', 'Evidence', @StandingId, 'Principal', 'test-user'
            );
            """, new
        {
            Id = recommendationId,
            StudentId = studentId,
            AcademicPeriodId = periodId,
            StandingId = standingId
        });

        // Delete the standing
        await ExecuteAsync("DELETE FROM academics.academic_standings WHERE id = @Id;", new { Id = standingId });

        // Verify the recommendation's academic_standing_id was set to NULL
        var updatedStandingRef = await ExecuteScalarAsync<Guid?>("""
            SELECT academic_standing_id
            FROM academics.promotion_recommendations
            WHERE id = @Id;
            """, new { Id = recommendationId });

        updatedStandingRef.Should().BeNull("deleting academic_standings must set academic_standing_id to NULL on promotion_recommendations");

        // 7. ON DELETE SET NULL: deleting learning_unit referenced by academic_achievements sets FK to NULL
        var deleteTestUnit2 = await CreateLearningUnitAsync(structureId, "UNIT-DEL-SETNULL", "Unit Delete SetNull");
        var achievementId = Guid.NewGuid();
        await ExecuteAsync("""
            INSERT INTO academics.academic_achievements (
                id, student_id, learning_unit_id, academic_period_id, achievement_type,
                title, criteria_met, recognized_by, recognized_at, created_by
            ) VALUES (
                @Id, @StudentId, @LearningUnitId, @AcademicPeriodId, 'subject_distinction',
                'Math Honor', 'All criteria met', 'Principal', '2026-06-15', 'test-user'
            );
            """, new
        {
            Id = achievementId,
            StudentId = studentId,
            LearningUnitId = deleteTestUnit2,
            AcademicPeriodId = periodId
        });

        // Delete the learning unit
        await ExecuteAsync("DELETE FROM academics.learning_units WHERE id = @Id;", new { Id = deleteTestUnit2 });

        // Verify the achievement's learning_unit_id was set to NULL
        var updatedUnitRef = await ExecuteScalarAsync<Guid?>("""
            SELECT learning_unit_id
            FROM academics.academic_achievements
            WHERE id = @Id;
            """, new { Id = achievementId });

        updatedUnitRef.Should().BeNull("deleting learning_unit must set learning_unit_id to NULL on academic_achievements");
    }

    [SkippableFact]
    public async Task SchemaIsolation_AllForeignKeys_MustReferenceAcademicsSchemaOnly()
    {
        SkipIfDatabaseUnavailable();

        const string fkSchemaSql = """
            SELECT
                fk.name AS FkName,
                OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ParentSchema,
                OBJECT_NAME(fk.parent_object_id) AS ParentTable,
                OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ReferencedSchema,
                OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
            FROM sys.foreign_keys fk
            WHERE fk.parent_object_id IN (
                OBJECT_ID('academics.learning_progress'),
                OBJECT_ID('academics.academic_standings'),
                OBJECT_ID('academics.promotion_recommendations'),
                OBJECT_ID('academics.academic_interventions'),
                OBJECT_ID('academics.academic_achievements')
            );
            """;

        var fks = (await QueryAsync<ForeignKeySchemaInfo>(fkSchemaSql)).ToList();

        // Foreign keys must exist only within the academics schema (zero cross-schema FKs)
        fks.Should().NotBeEmpty();
        fks.Should().AllSatisfy(fk =>
        {
            fk.ParentSchema.Should().Be("academics", "parent table must reside in schema academics");
            fk.ReferencedSchema.Should().Be("academics", $"foreign key {fk.FkName} must reference schema academics (zero cross-schema FKs per ADR-001, ADR-002)");
        });
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

    private async Task<Guid> CreateLearningUnitAsync(Guid structureId, string code, string name)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.learning_units (
                id, learning_structure_id, code, name, unit_type, status, created_by
            ) VALUES (
                @Id, @StructureId, @Code, @Name, 'subject', 'draft', 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            StructureId = structureId,
            Code = code,
            Name = name
        });

        return id;
    }

    private async Task<Guid> CreateAcademicStandingAsync(Guid studentId, Guid periodId, string standing)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.academic_standings (
                id, student_id, academic_period_id, standing, policy_reference, authorized_by, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, @Standing, 'POL-2026-01', 'Academic Dean', 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            StudentId = studentId,
            PeriodId = periodId,
            Standing = standing
        });

        return id;
    }

    private sealed record ColumnInfo(string Name, string DataType);
    private sealed record ForeignKeyInfo(string Name, byte DeleteReferentialAction);
    private sealed record ForeignKeySchemaInfo(string FkName, string ParentSchema, string ParentTable, string ReferencedSchema, string ReferencedTable);
}
