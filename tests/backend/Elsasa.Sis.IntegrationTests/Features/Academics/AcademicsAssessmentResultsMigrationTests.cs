namespace Elsasa.Sis.IntegrationTests.Features.Academics;

using Elsasa.Sis.Database;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Integration test suite verifying DbUp schema migration 20261105_0402 (IS010 / CR-ACD-ASSESSMENT-RESULTS, P1-S01, P7-S17, ADR-002, ADR-006).
/// Validates script journaling via DatabaseMigrator, provisioning of all 7 authoritative domain tables in schema academics
/// (assessments, assessment_evidence, academic_results, academic_record_corrections, report_cards, report_card_entries, transcripts),
/// columns, indexes, intra-schema foreign keys with ON DELETE NO ACTION, candidate CHECK constraints (student_id XOR applicant_id),
/// unique constraint UQ_academics_report_cards_student_period_type, and enum check constraints across all tables.
/// </summary>
public sealed class AcademicsAssessmentResultsMigrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261105_0402_Academics_AssessmentResults_Tables.sql";

    [SkippableFact]
    public void AcademicsAssessmentResultsMigration_Should_RunSuccessfullyViaDatabaseMigrator()
    {
        SkipIfDatabaseUnavailable();

        var success = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);

        success.Should().BeTrue("DatabaseMigrator.UpgradeDatabase should succeed idempotently on target database");
    }

    [SkippableFact]
    public async Task AcademicsAssessmentResultsMigration_Should_BeJournaledInSchemaVersions()
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
    public async Task AssessmentsTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'assessments';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "learning_unit_id",
            "learning_activity_id",
            "title",
            "assessment_type",
            "assessment_method",
            "max_score",
            "passing_score",
            "reassessment_policy",
            "criteria",
            "assessment_date",
            "assessor_reference",
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
            WHERE object_id = OBJECT_ID('academics.assessments') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_assessments");
        indexes.Should().Contain("IX_academics_assessments_unit_status");
        indexes.Should().Contain("IX_academics_assessments_activity");
        indexes.Should().Contain("IX_academics_assessments_date");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.assessments');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_assessments_unit");
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_assessments_activity");

        fks.Should().AllSatisfy(fk =>
            fk.DeleteReferentialAction.Should().Be(0, "assessments foreign keys must specify ON DELETE NO ACTION"));
    }

    [SkippableFact]
    public async Task AssessmentEvidenceTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'assessment_evidence';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "assessment_id",
            "student_id",
            "applicant_id",
            "submission_id",
            "evidence_type",
            "collected_date",
            "evidence_reference",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.assessment_evidence') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_assessment_evidence");
        indexes.Should().Contain("IX_academics_assessment_evidence_assessment");
        indexes.Should().Contain("IX_academics_assessment_evidence_student");
        indexes.Should().Contain("IX_academics_assessment_evidence_applicant");
        indexes.Should().Contain("IX_academics_assessment_evidence_submission");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.assessment_evidence');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_assessment_evidence_assessment");
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_assessment_evidence_submission");

        fks.Should().AllSatisfy(fk =>
            fk.DeleteReferentialAction.Should().Be(0, "assessment_evidence foreign keys must specify ON DELETE NO ACTION"));
    }

    [SkippableFact]
    public async Task AcademicResultsTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'academic_results';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "assessment_id",
            "student_id",
            "applicant_id",
            "attempt_number",
            "replaces_result_id",
            "is_final",
            "score_numeric",
            "score_max",
            "grade_letter",
            "grade_label",
            "narrative_feedback",
            "evaluation_model",
            "evaluated_by",
            "evaluated_date",
            "result_status",
            "approved_by",
            "approved_date",
            "published_date",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.academic_results') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_academic_results");
        indexes.Should().Contain("IX_academics_academic_results_assessment_student");
        indexes.Should().Contain("IX_academics_academic_results_assessment_applicant");
        indexes.Should().Contain("IX_academics_academic_results_student_final");
        indexes.Should().Contain("IX_academics_academic_results_replaces");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.academic_results');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_academic_results_assessment");
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_academic_results_replaces");

        fks.Should().AllSatisfy(fk =>
            fk.DeleteReferentialAction.Should().Be(0, "academic_results foreign keys must specify ON DELETE NO ACTION"));
    }

    [SkippableFact]
    public async Task AcademicRecordCorrectionsTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'academic_record_corrections';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "record_type",
            "record_id",
            "original_value",
            "corrected_value",
            "reason",
            "evidence",
            "corrected_by",
            "corrected_date",
            "is_material",
            "notes",
            "created_at",
            "created_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.academic_record_corrections') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_academic_record_corrections");
        indexes.Should().Contain("IX_academics_academic_record_corrections_record");
    }

    [SkippableFact]
    public async Task ReportCardsTable_Should_ContainAllColumnsAndIndexesAndUniqueConstraint()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'report_cards';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "student_id",
            "academic_period_id",
            "academic_period_label",
            "report_type",
            "template_name",
            "status",
            "approved_by",
            "approved_date",
            "published_date",
            "narrative_comments",
            "content_snapshot",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.report_cards') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_report_cards");
        indexes.Should().Contain("UQ_academics_report_cards_student_period_type");
        indexes.Should().Contain("IX_academics_report_cards_period_status");
        indexes.Should().Contain("IX_academics_report_cards_student");
    }

    [SkippableFact]
    public async Task ReportCardEntriesTable_Should_ContainAllColumnsAndIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'report_card_entries';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "report_card_id",
            "learning_unit_id",
            "academic_result_id",
            "display_order",
            "teacher_comment",
            "created_at",
            "created_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.report_card_entries') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_report_card_entries");
        indexes.Should().Contain("IX_academics_report_card_entries_card");
        indexes.Should().Contain("IX_academics_report_card_entries_unit");
        indexes.Should().Contain("IX_academics_report_card_entries_result");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('academics.report_card_entries');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_report_card_entries_card");
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_report_card_entries_unit");
        fks.Should().ContainSingle(fk => fk.Name == "FK_academics_report_card_entries_result");

        fks.Should().AllSatisfy(fk =>
            fk.DeleteReferentialAction.Should().Be(0, "report_card_entries foreign keys must specify ON DELETE NO ACTION"));
    }

    [SkippableFact]
    public async Task TranscriptsTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'academics' AND TABLE_NAME = 'transcripts';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "student_id",
            "transcript_type",
            "period_start",
            "period_end",
            "status",
            "approved_by",
            "approved_date",
            "issued_date",
            "purpose",
            "content_snapshot",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('academics.transcripts') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_academics_transcripts");
        indexes.Should().Contain("IX_academics_transcripts_student_type");
        indexes.Should().Contain("IX_academics_transcripts_status");
    }

    [SkippableFact]
    public async Task AssessmentEvidence_CandidateCheckConstraint_Should_EnforceExclusivity()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Evidence Candidate Check Curriculum");
        var unitId = await CreateLearningUnitAsync(structureId, "EVID-CHK-01", "Evidence Candidate Check Unit");
        var assessmentId = await CreateAssessmentAsync(unitId, "Evidence Candidate Check Assessment");

        const string insertEvidenceSql = """
            INSERT INTO academics.assessment_evidence (
                id, assessment_id, student_id, applicant_id, evidence_type, collected_date, created_by
            ) VALUES (
                @Id, @AssessmentId, @StudentId, @ApplicantId, 'test_paper', '2026-09-10', 'test-user'
            );
            """;

        // 1. Both student_id and applicant_id populated must violate candidate CHECK constraint (547)
        var actBoth = async () =>
        {
            await ExecuteAsync(insertEvidenceSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = Guid.NewGuid(),
                ApplicantId = Guid.NewGuid()
            });
        };

        var exBoth = await actBoth.Should().ThrowAsync<SqlException>();
        exBoth.Which.Number.Should().Be(547, "populating both student_id and applicant_id must violate CK_academics_assessment_evidence_candidate (error 547)");

        // 2. Neither student_id nor applicant_id populated must violate candidate CHECK constraint (547)
        var actNeither = async () =>
        {
            await ExecuteAsync(insertEvidenceSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = (Guid?)null,
                ApplicantId = (Guid?)null
            });
        };

        var exNeither = await actNeither.Should().ThrowAsync<SqlException>();
        exNeither.Which.Number.Should().Be(547, "populating neither student_id nor applicant_id must violate CK_academics_assessment_evidence_candidate (error 547)");

        // 3. Exactly student_id populated must succeed
        var actStudentOnly = async () =>
        {
            await ExecuteAsync(insertEvidenceSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = Guid.NewGuid(),
                ApplicantId = (Guid?)null
            });
        };

        await actStudentOnly.Should().NotThrowAsync("populating only student_id must satisfy candidate CHECK constraint");

        // 4. Exactly applicant_id populated must succeed
        var actApplicantOnly = async () =>
        {
            await ExecuteAsync(insertEvidenceSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = (Guid?)null,
                ApplicantId = Guid.NewGuid()
            });
        };

        await actApplicantOnly.Should().NotThrowAsync("populating only applicant_id must satisfy candidate CHECK constraint");
    }

    [SkippableFact]
    public async Task AcademicResults_CandidateCheckConstraint_Should_EnforceExclusivity()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Results Candidate Check Curriculum");
        var unitId = await CreateLearningUnitAsync(structureId, "RES-CHK-01", "Results Candidate Check Unit");
        var assessmentId = await CreateAssessmentAsync(unitId, "Results Candidate Check Assessment");

        const string insertResultSql = """
            INSERT INTO academics.academic_results (
                id, assessment_id, student_id, applicant_id, attempt_number,
                is_final, evaluation_model, evaluated_by, evaluated_date, created_by
            ) VALUES (
                @Id, @AssessmentId, @StudentId, @ApplicantId, 1,
                1, 'numeric', 'teacher-01', '2026-09-10', 'test-user'
            );
            """;

        // 1. Both student_id and applicant_id populated must violate candidate CHECK constraint (547)
        var actBoth = async () =>
        {
            await ExecuteAsync(insertResultSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = Guid.NewGuid(),
                ApplicantId = Guid.NewGuid()
            });
        };

        var exBoth = await actBoth.Should().ThrowAsync<SqlException>();
        exBoth.Which.Number.Should().Be(547, "populating both student_id and applicant_id must violate CK_academics_academic_results_candidate (error 547)");

        // 2. Neither student_id nor applicant_id populated must violate candidate CHECK constraint (547)
        var actNeither = async () =>
        {
            await ExecuteAsync(insertResultSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = (Guid?)null,
                ApplicantId = (Guid?)null
            });
        };

        var exNeither = await actNeither.Should().ThrowAsync<SqlException>();
        exNeither.Which.Number.Should().Be(547, "populating neither student_id nor applicant_id must violate CK_academics_academic_results_candidate (error 547)");

        // 3. Exactly student_id populated must succeed
        var actStudentOnly = async () =>
        {
            await ExecuteAsync(insertResultSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = Guid.NewGuid(),
                ApplicantId = (Guid?)null
            });
        };

        await actStudentOnly.Should().NotThrowAsync("populating only student_id must satisfy candidate CHECK constraint");

        // 4. Exactly applicant_id populated must succeed
        var actApplicantOnly = async () =>
        {
            await ExecuteAsync(insertResultSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = (Guid?)null,
                ApplicantId = Guid.NewGuid()
            });
        };

        await actApplicantOnly.Should().NotThrowAsync("populating only applicant_id must satisfy candidate CHECK constraint");
    }

    [SkippableFact]
    public async Task ReportCards_UniqueConstraint_Should_PreventDuplicateForSameStudentPeriodAndType()
    {
        SkipIfDatabaseUnavailable();

        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        const string insertCardSql = """
            INSERT INTO academics.report_cards (
                id, student_id, academic_period_id, academic_period_label,
                report_type, template_name, status, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, '2026/2027 Ganjil',
                @ReportType, 'Buku Rapor Merdeka', 'draft', 'test-user'
            );
            """;

        // 1. Initial report card for (student, period, 'mid_term') must succeed
        await ExecuteAsync(insertCardSql, new
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            PeriodId = periodId,
            ReportType = "mid_term"
        });

        // 2. Duplicate report card for same (student, period, 'mid_term') must violate unique constraint (2601 or 2627)
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertCardSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                PeriodId = periodId,
                ReportType = "mid_term"
            });
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate (student_id, academic_period_id, report_type) must violate UQ_academics_report_cards_student_period_type");

        // 3. Different report type ('end_of_term') for same student and period must succeed
        var actDifferentType = async () =>
        {
            await ExecuteAsync(insertCardSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                PeriodId = periodId,
                ReportType = "end_of_term"
            });
        };

        await actDifferentType.Should().NotThrowAsync("distinct report_type for same student and period must be permitted");

        // 4. Same report type for different student must succeed
        var actDifferentStudent = async () =>
        {
            await ExecuteAsync(insertCardSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                PeriodId = periodId,
                ReportType = "mid_term"
            });
        };

        await actDifferentStudent.Should().NotThrowAsync("same report_type for different student must be permitted");
    }

    [SkippableFact]
    public async Task IntraSchemaForeignKeys_Should_PreventParentDeletion_WhenDependentRecordsExist()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("FK Enforcement Structure");
        var unitId = await CreateLearningUnitAsync(structureId, "FK-UNIT-01", "FK Test Unit");
        var assessmentId = await CreateAssessmentAsync(unitId, "FK Parent Assessment");

        // 1. Attempting to delete learning_units when an assessment references it must be prevented (ON DELETE NO ACTION)
        var actDeleteUnit = async () =>
        {
            await ExecuteAsync("DELETE FROM academics.learning_units WHERE id = @Id;", new { Id = unitId });
        };

        var exUnit = await actDeleteUnit.Should().ThrowAsync<SqlException>();
        exUnit.Which.Number.Should().Be(547, "deleting learning_units with dependent assessments must violate FK_academics_assessments_unit");

        // 2. Create dependent assessment_evidence
        var evidenceId = Guid.NewGuid();
        const string insertEvidenceSql = """
            INSERT INTO academics.assessment_evidence (
                id, assessment_id, student_id, evidence_type, collected_date, created_by
            ) VALUES (
                @Id, @AssessmentId, @StudentId, 'portfolio_item', '2026-09-12', 'test-user'
            );
            """;
        await ExecuteAsync(insertEvidenceSql, new
        {
            Id = evidenceId,
            AssessmentId = assessmentId,
            StudentId = Guid.NewGuid()
        });

        // Attempting to delete assessment with dependent evidence must fail
        var actDeleteAssessment = async () =>
        {
            await ExecuteAsync("DELETE FROM academics.assessments WHERE id = @Id;", new { Id = assessmentId });
        };

        var exAssessment = await actDeleteAssessment.Should().ThrowAsync<SqlException>();
        exAssessment.Which.Number.Should().Be(547, "deleting assessment with dependent evidence must violate FK_academics_assessment_evidence_assessment");

        // 3. Remove evidence, create academic_results, and verify assessment deletion is still prevented
        await ExecuteAsync("DELETE FROM academics.assessment_evidence WHERE id = @Id;", new { Id = evidenceId });

        var resultId = Guid.NewGuid();
        const string insertResultSql = """
            INSERT INTO academics.academic_results (
                id, assessment_id, student_id, attempt_number, is_final,
                evaluation_model, evaluated_by, evaluated_date, created_by
            ) VALUES (
                @Id, @AssessmentId, @StudentId, 1, 1,
                'numeric', 'teacher-01', '2026-09-12', 'test-user'
            );
            """;
        await ExecuteAsync(insertResultSql, new
        {
            Id = resultId,
            AssessmentId = assessmentId,
            StudentId = Guid.NewGuid()
        });

        var actDeleteAssessmentWithResult = async () =>
        {
            await ExecuteAsync("DELETE FROM academics.assessments WHERE id = @Id;", new { Id = assessmentId });
        };

        var exAssessmentWithResult = await actDeleteAssessmentWithResult.Should().ThrowAsync<SqlException>();
        exAssessmentWithResult.Which.Number.Should().Be(547, "deleting assessment with dependent results must violate FK_academics_academic_results_assessment");

        // 4. Report cards & entries: Attempting to delete report_card when report_card_entries reference it
        var cardId = await CreateReportCardAsync(Guid.NewGuid(), Guid.NewGuid(), "mid_term");
        var entryId = Guid.NewGuid();
        const string insertEntrySql = """
            INSERT INTO academics.report_card_entries (
                id, report_card_id, learning_unit_id, academic_result_id, display_order, created_by
            ) VALUES (
                @Id, @CardId, @UnitId, @ResultId, 1, 'test-user'
            );
            """;
        await ExecuteAsync(insertEntrySql, new
        {
            Id = entryId,
            CardId = cardId,
            UnitId = unitId,
            ResultId = resultId
        });

        var actDeleteCard = async () =>
        {
            await ExecuteAsync("DELETE FROM academics.report_cards WHERE id = @Id;", new { Id = cardId });
        };

        var exCard = await actDeleteCard.Should().ThrowAsync<SqlException>();
        exCard.Which.Number.Should().Be(547, "deleting report_cards with dependent report_card_entries must violate FK_academics_report_card_entries_card");
    }

    [SkippableFact]
    public async Task Assessments_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Assessments Enum Structure");
        var unitId = await CreateLearningUnitAsync(structureId, "ENUM-UNIT-01", "Enum Unit");

        const string insertSql = """
            INSERT INTO academics.assessments (
                id, learning_unit_id, title, assessment_type, assessment_method,
                reassessment_policy, status, created_by
            ) VALUES (
                @Id, @UnitId, 'Check Enum Assessment', @Type, @Method,
                @Policy, @Status, 'test-user'
            );
            """;

        // Invalid assessment_type
        var actInvalidType = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                Type = "invalid_type",
                Method = "written_test",
                Policy = "highest_score",
                Status = "planned"
            });
        };
        (await actInvalidType.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);

        // Invalid assessment_method
        var actInvalidMethod = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                Type = "formative",
                Method = "invalid_method",
                Policy = "highest_score",
                Status = "planned"
            });
        };
        (await actInvalidMethod.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);

        // Invalid reassessment_policy
        var actInvalidPolicy = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                Type = "formative",
                Method = "written_test",
                Policy = "invalid_policy",
                Status = "planned"
            });
        };
        (await actInvalidPolicy.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);

        // Invalid status
        var actInvalidStatus = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                Type = "formative",
                Method = "written_test",
                Policy = "highest_score",
                Status = "invalid_status"
            });
        };
        (await actInvalidStatus.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);
    }

    [SkippableFact]
    public async Task AssessmentEvidence_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Evidence Enum Structure");
        var unitId = await CreateLearningUnitAsync(structureId, "EVID-ENUM-01", "Evidence Enum Unit");
        var assessmentId = await CreateAssessmentAsync(unitId, "Evidence Enum Assessment");

        const string insertSql = """
            INSERT INTO academics.assessment_evidence (
                id, assessment_id, student_id, evidence_type, collected_date, created_by
            ) VALUES (
                @Id, @AssessmentId, @StudentId, @EvidenceType, '2026-09-15', 'test-user'
            );
            """;

        // Invalid evidence_type
        var actInvalidType = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = Guid.NewGuid(),
                EvidenceType = "invalid_evidence"
            });
        };
        (await actInvalidType.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);
    }

    [SkippableFact]
    public async Task AcademicResults_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        var structureId = await CreateLearningStructureAsync("Results Enum Structure");
        var unitId = await CreateLearningUnitAsync(structureId, "RES-ENUM-01", "Results Enum Unit");
        var assessmentId = await CreateAssessmentAsync(unitId, "Results Enum Assessment");

        const string insertSql = """
            INSERT INTO academics.academic_results (
                id, assessment_id, student_id, attempt_number, is_final,
                evaluation_model, evaluated_by, evaluated_date, result_status, created_by
            ) VALUES (
                @Id, @AssessmentId, @StudentId, 1, 1,
                @Model, 'teacher-01', '2026-09-15', @Status, 'test-user'
            );
            """;

        // Invalid evaluation_model
        var actInvalidModel = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = Guid.NewGuid(),
                Model = "invalid_model",
                Status = "provisional"
            });
        };
        (await actInvalidModel.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);

        // Invalid result_status
        var actInvalidStatus = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                StudentId = Guid.NewGuid(),
                Model = "numeric",
                Status = "invalid_status"
            });
        };
        (await actInvalidStatus.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);
    }

    [SkippableFact]
    public async Task AcademicRecordCorrections_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        const string insertSql = """
            INSERT INTO academics.academic_record_corrections (
                id, record_type, record_id, original_value, corrected_value,
                reason, corrected_by, corrected_date, created_by
            ) VALUES (
                @Id, @RecordType, @RecordId, '{"val":1}', '{"val":2}',
                'Audit correction test justification', 'approver-01', '2026-09-15', 'test-user'
            );
            """;

        // Invalid record_type
        var actInvalidType = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                RecordType = "invalid_record_type",
                RecordId = Guid.NewGuid()
            });
        };
        (await actInvalidType.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);

        // Valid record_type
        var actValidType = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                RecordType = "academic_result",
                RecordId = Guid.NewGuid()
            });
        };
        await actValidType.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task ReportCards_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        const string insertSql = """
            INSERT INTO academics.report_cards (
                id, student_id, academic_period_id, academic_period_label,
                report_type, template_name, status, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, '2026/2027 Ganjil',
                @ReportType, 'Buku Rapor Merdeka', @Status, 'test-user'
            );
            """;

        // Invalid report_type
        var actInvalidType = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                PeriodId = Guid.NewGuid(),
                ReportType = "invalid_report_type",
                Status = "draft"
            });
        };
        (await actInvalidType.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);

        // Invalid status
        var actInvalidStatus = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                PeriodId = Guid.NewGuid(),
                ReportType = "mid_term",
                Status = "invalid_card_status"
            });
        };
        (await actInvalidStatus.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);
    }

    [SkippableFact]
    public async Task Transcripts_CheckConstraints_Should_EnforceValidEnums()
    {
        SkipIfDatabaseUnavailable();

        const string insertSql = """
            INSERT INTO academics.transcripts (
                id, student_id, transcript_type, period_start, period_end,
                status, created_by
            ) VALUES (
                @Id, @StudentId, @TranscriptType, '2026-01-01', '2026-12-31',
                @Status, 'test-user'
            );
            """;

        // Invalid transcript_type
        var actInvalidType = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                TranscriptType = "invalid_type",
                Status = "draft"
            });
        };
        (await actInvalidType.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);

        // Invalid status
        var actInvalidStatus = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                TranscriptType = "official",
                Status = "invalid_status"
            });
        };
        (await actInvalidStatus.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(547);
    }

    private async Task<Guid> CreateLearningStructureAsync(string name)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.learning_structures (
                id, name, description, structure_type, status, created_by
            ) VALUES (
                @Id, @Name, 'Structure description', 'curriculum', 'active', 'test-user'
            );
            """;

        await ExecuteAsync(sql, new { Id = id, Name = name });
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

    private async Task<Guid> CreateAssessmentAsync(
        Guid unitId,
        string title = "Test Assessment",
        string type = "formative",
        string method = "written_test",
        string policy = "highest_score",
        string status = "planned")
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.assessments (
                id, learning_unit_id, title, assessment_type, assessment_method,
                max_score, passing_score, reassessment_policy, status, created_by
            ) VALUES (
                @Id, @UnitId, @Title, @Type, @Method, 100.00, 75.00, @Policy, @Status, 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            UnitId = unitId,
            Title = title,
            Type = type,
            Method = method,
            Policy = policy,
            Status = status
        });

        return id;
    }

    private async Task<Guid> CreateReportCardAsync(
        Guid studentId,
        Guid periodId,
        string reportType = "mid_term",
        string status = "draft")
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO academics.report_cards (
                id, student_id, academic_period_id, academic_period_label,
                report_type, template_name, status, created_by
            ) VALUES (
                @Id, @StudentId, @PeriodId, '2026/2027 Ganjil',
                @ReportType, 'Buku Rapor Merdeka', @Status, 'test-user'
            );
            """;

        await ExecuteAsync(sql, new
        {
            Id = id,
            StudentId = studentId,
            PeriodId = periodId,
            ReportType = reportType,
            Status = status
        });

        return id;
    }

    private sealed record ForeignKeyInfo(string Name, byte DeleteReferentialAction);
}
