namespace Elsasa.Sis.IntegrationTests.Features.Teaching;

using System.Data;
using Elsasa.Sis.Database;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Database integration test suite verifying DbUp schema migration 20261110_0501 (IS014 / CR-TCH-TEACHER-PROFILE-AUTHORIZATION, P1-S01, P6-S18, ADR-002, ADR-006).
/// Validates script journaling via DatabaseMigrator, provisioning of all 10 authoritative domain tables in schema teaching
/// (teaching_roles, teacher_profiles, capability_types, teaching_capabilities, qualification_types, qualification_evidence,
/// teaching_authorizations, authorization_evidence, supervision_requirements, teaching_record_corrections),
/// filtered unique index UQ_teaching_teacher_profiles_national_id permitting multiple NULLs while enforcing uniqueness for non-null values,
/// check constraints across standing, verification state, and authorization status, cascading deletes, and record correction atomic updates.
/// </summary>
public sealed class TeachingDatabaseSchemaIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261110_0501_Teaching_TeacherProfile_Tables.sql";

    private sealed record ForeignKeyInfo(string Name, byte DeleteReferentialAction);

    [SkippableFact]
    public void TeachingMigration_Should_RunSuccessfullyViaDatabaseMigrator()
    {
        SkipIfDatabaseUnavailable();

        var success = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);

        success.Should().BeTrue("DatabaseMigrator.UpgradeDatabase should succeed idempotently on target database");
    }

    [SkippableFact]
    public async Task TeachingMigration_Should_BeJournaledInSchemaVersions()
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
    public async Task TeachingSchema_Should_ContainAllTenTables()
    {
        SkipIfDatabaseUnavailable();

        const string tablesSql = """
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_TYPE = 'BASE TABLE';
            """;

        var tables = (await QueryAsync<string>(tablesSql)).ToList();

        tables.Should().Contain([
            "teaching_roles",
            "teacher_profiles",
            "capability_types",
            "teaching_capabilities",
            "qualification_types",
            "qualification_evidence",
            "teaching_authorizations",
            "authorization_evidence",
            "supervision_requirements",
            "teaching_record_corrections"
        ]);
    }

    [SkippableFact]
    public async Task TeacherProfilesTable_Should_ContainAllColumnsIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_NAME = 'teacher_profiles';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "teacher_number",
            "full_name",
            "display_name",
            "employee_ref",
            "national_id_or_external_ref",
            "email",
            "phone",
            "teaching_role_id",
            "campus_affiliation",
            "department",
            "standing",
            "effective_from",
            "effective_to",
            "standing_reason",
            "approved_by",
            "approved_date",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('teaching.teacher_profiles') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_teaching_teacher_profiles");
        indexes.Should().Contain("UQ_teaching_teacher_profiles_number");
        indexes.Should().Contain("UQ_teaching_teacher_profiles_national_id");
        indexes.Should().Contain("IX_teaching_profiles_standing_campus");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('teaching.teacher_profiles');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_profiles_role");
        var roleFk = fks.Single(fk => fk.Name == "FK_teaching_profiles_role");
        roleFk.DeleteReferentialAction.Should().Be(0, "teacher profiles role FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task TeachingCapabilitiesTable_Should_ContainAllColumnsIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_NAME = 'teaching_capabilities';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "teacher_profile_id",
            "capability_type_id",
            "capability_code",
            "capability_name",
            "applicable_level",
            "delivery_mode",
            "language_of_instruction",
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
            WHERE object_id = OBJECT_ID('teaching.teaching_capabilities') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_teaching_teaching_capabilities");
        indexes.Should().Contain("UQ_teaching_capabilities_unique");
        indexes.Should().Contain("IX_teaching_capabilities_teacher");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('teaching.teaching_capabilities');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_caps_profile");
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_caps_type");

        var profileFk = fks.Single(fk => fk.Name == "FK_teaching_caps_profile");
        profileFk.DeleteReferentialAction.Should().Be(1, "teaching capabilities profile FK must specify ON DELETE CASCADE");

        var typeFk = fks.Single(fk => fk.Name == "FK_teaching_caps_type");
        typeFk.DeleteReferentialAction.Should().Be(0, "teaching capabilities type FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task QualificationEvidenceTable_Should_ContainAllColumnsIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_NAME = 'qualification_evidence';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "teacher_profile_id",
            "qualification_type_id",
            "title",
            "issuing_authority",
            "credential_identifier",
            "issue_date",
            "expiry_date",
            "document_reference",
            "verification_state",
            "verified_by",
            "verified_date",
            "verification_notes",
            "rejection_reason",
            "restrictions_or_conditions",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('teaching.qualification_evidence') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_teaching_qualification_evidence");
        indexes.Should().Contain("IX_teaching_qualification_evidence_teacher");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('teaching.qualification_evidence');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_qev_profile");
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_qev_type");

        var profileFk = fks.Single(fk => fk.Name == "FK_teaching_qev_profile");
        profileFk.DeleteReferentialAction.Should().Be(1, "qualification evidence profile FK must specify ON DELETE CASCADE");

        var typeFk = fks.Single(fk => fk.Name == "FK_teaching_qev_type");
        typeFk.DeleteReferentialAction.Should().Be(0, "qualification evidence type FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task TeachingAuthorizationsTable_Should_ContainAllColumnsIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_NAME = 'teaching_authorizations';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "authorization_number",
            "teacher_profile_id",
            "authorized_capability_id",
            "scope_description",
            "campus_scope",
            "program_scope",
            "level_scope",
            "delivery_mode_scope",
            "status",
            "unverified_credential_warning",
            "effective_start_date",
            "effective_end_date",
            "conditions_or_restrictions",
            "decision_authority",
            "approved_date",
            "suspension_or_revocation_reason",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('teaching.teaching_authorizations') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_teaching_teaching_authorizations");
        indexes.Should().Contain("UQ_teaching_authorizations_number");
        indexes.Should().Contain("IX_teaching_authorizations_teacher");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('teaching.teaching_authorizations');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_auth_profile");
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_auth_capability");

        var profileFk = fks.Single(fk => fk.Name == "FK_teaching_auth_profile");
        profileFk.DeleteReferentialAction.Should().Be(1, "teaching authorizations profile FK must specify ON DELETE CASCADE");

        var capFk = fks.Single(fk => fk.Name == "FK_teaching_auth_capability");
        capFk.DeleteReferentialAction.Should().Be(0, "teaching authorizations capability FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task AuthorizationEvidenceTable_Should_ContainAllColumnsAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_NAME = 'authorization_evidence';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "authorization_id",
            "qualification_evidence_id",
            "notes",
            "created_at",
            "created_by"
        ]);

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('teaching.authorization_evidence');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_authev_auth");
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_authev_evidence");

        var authFk = fks.Single(fk => fk.Name == "FK_teaching_authev_auth");
        authFk.DeleteReferentialAction.Should().Be(1, "authorization evidence auth FK must specify ON DELETE CASCADE");

        var evidenceFk = fks.Single(fk => fk.Name == "FK_teaching_authev_evidence");
        evidenceFk.DeleteReferentialAction.Should().Be(0, "authorization evidence evidence FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task SupervisionRequirementsTable_Should_ContainAllColumnsIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_NAME = 'supervision_requirements';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "authorization_id",
            "supervising_teacher_id",
            "supervision_type",
            "description",
            "start_date",
            "end_date",
            "status",
            "resolution_notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('teaching.supervision_requirements') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_teaching_supervision_requirements");
        indexes.Should().Contain("IX_teaching_supervision_auth");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('teaching.supervision_requirements');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_sup_auth");
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_sup_mentor");

        var authFk = fks.Single(fk => fk.Name == "FK_teaching_sup_auth");
        authFk.DeleteReferentialAction.Should().Be(1, "supervision requirements auth FK must specify ON DELETE CASCADE");

        var mentorFk = fks.Single(fk => fk.Name == "FK_teaching_sup_mentor");
        mentorFk.DeleteReferentialAction.Should().Be(0, "supervision requirements mentor FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task TeachingRecordCorrectionsTable_Should_ContainAllColumnsIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_NAME = 'teaching_record_corrections';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "correction_number",
            "target_table_name",
            "target_record_id",
            "teacher_profile_id",
            "field_name",
            "original_value",
            "corrected_value",
            "reason_for_correction",
            "supporting_evidence",
            "downstream_impact_reviewed",
            "effective_date_implications",
            "requested_by",
            "requested_date",
            "approved_by",
            "approved_date",
            "status",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('teaching.teaching_record_corrections') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_teaching_teaching_record_corrections");
        indexes.Should().Contain("UQ_teaching_record_corrections_number");
        indexes.Should().Contain("IX_teaching_record_corrections_target");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('teaching.teaching_record_corrections');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_trc_teacher");

        var teacherFk = fks.Single(fk => fk.Name == "FK_teaching_trc_teacher");
        teacherFk.DeleteReferentialAction.Should().Be(2, "teaching record corrections teacher FK must specify ON DELETE SET NULL");
    }

    [SkippableFact]
    public async Task TeacherProfiles_FilteredUniqueIndex_Should_AllowMultipleNulls_And_EnforceUniquenessForNonNullValues()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection);

        const string insertSql = """
            INSERT INTO teaching.teacher_profiles (
                id, teacher_number, full_name, display_name, employee_ref, national_id_or_external_ref,
                teaching_role_id, campus_affiliation, standing, effective_from, created_by
            ) VALUES (
                @Id, @TeacherNumber, @FullName, @DisplayName, @EmployeeRef, @NationalId,
                @RoleId, 'Kampus Utama', 'active', '2026-08-01', 'schema-test'
            );
            """;

        var suffix = Guid.NewGuid().ToString("N")[..8];

        // 1. First profile with NULL national ID
        var idNull1 = Guid.NewGuid();
        await ExecuteAsync(insertSql, new
        {
            Id = idNull1,
            TeacherNumber = $"TCH-NULL1-{suffix}",
            FullName = $"Teacher Null 1 {suffix}",
            DisplayName = "Teacher Null 1",
            EmployeeRef = (string?)null,
            NationalId = (string?)null,
            RoleId = roleId
        });

        // 2. Second profile with NULL national ID must also succeed (filtered unique index permits multiple NULLs)
        var idNull2 = Guid.NewGuid();
        var actNull2 = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = idNull2,
                TeacherNumber = $"TCH-NULL2-{suffix}",
                FullName = $"Teacher Null 2 {suffix}",
                DisplayName = "Teacher Null 2",
                EmployeeRef = (string?)null,
                NationalId = (string?)null,
                RoleId = roleId
            });
        };
        await actNull2.Should().NotThrowAsync("UQ_teaching_teacher_profiles_national_id filtered unique index must allow multiple NULLs");

        // 3. First profile with non-null national ID
        var nationalId = $"3171{Random.Shared.Next(10000000, 99999999)}0001";
        var idNonNull1 = Guid.NewGuid();
        await ExecuteAsync(insertSql, new
        {
            Id = idNonNull1,
            TeacherNumber = $"TCH-NONNULL1-{suffix}",
            FullName = $"Teacher NonNull 1 {suffix}",
            DisplayName = "Teacher NonNull 1",
            EmployeeRef = $"EMP-1-{suffix}",
            NationalId = nationalId,
            RoleId = roleId
        });

        // 4. Second profile with SAME non-null national ID must fail with unique constraint violation (error 2601 or 2627)
        var idNonNull2 = Guid.NewGuid();
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = idNonNull2,
                TeacherNumber = $"TCH-NONNULL2-{suffix}",
                FullName = $"Teacher NonNull 2 {suffix}",
                DisplayName = "Teacher NonNull 2",
                EmployeeRef = $"EMP-2-{suffix}",
                NationalId = nationalId,
                RoleId = roleId
            });
        };
        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate national_id_or_external_ref must trigger SQL unique index violation");

        // 5. Third profile with DIFFERENT non-null national ID must succeed
        var differentNationalId = $"3171{Random.Shared.Next(10000000, 99999999)}0002";
        var idNonNull3 = Guid.NewGuid();
        var actDifferent = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = idNonNull3,
                TeacherNumber = $"TCH-NONNULL3-{suffix}",
                FullName = $"Teacher NonNull 3 {suffix}",
                DisplayName = "Teacher NonNull 3",
                EmployeeRef = $"EMP-3-{suffix}",
                NationalId = differentNationalId,
                RoleId = roleId
            });
        };
        await actDifferent.Should().NotThrowAsync("distinct national_id_or_external_ref must be accepted");
    }

    [SkippableFact]
    public async Task TeacherProfiles_CheckConstraints_Should_EnforceValidStanding()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection);

        const string insertSql = """
            INSERT INTO teaching.teacher_profiles (
                id, teacher_number, full_name, teaching_role_id, campus_affiliation, standing, effective_from, created_by
            ) VALUES (
                @Id, @TeacherNumber, @FullName, @RoleId, 'Kampus Utama', @Standing, '2026-08-01', 'schema-test'
            );
            """;

        // 1. Invalid standing value must trigger check constraint violation (SQL Error 547)
        var invalidAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                TeacherNumber = $"TCH-INV-{Guid.NewGuid():N}"[..20],
                FullName = "Invalid Standing Teacher",
                RoleId = roleId,
                Standing = "unknown_state"
            });
        };

        var ex = await invalidAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid standing must violate CK_teaching_profiles_standing (error 547)");

        // 2. All 5 valid standings must be accepted
        string[] validStandings = ["pending_approval", "active", "restricted", "suspended", "inactive"];
        foreach (var standing in validStandings)
        {
            var validAct = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    TeacherNumber = $"TCH-{standing[..3].ToUpperInvariant()}-{Guid.NewGuid():N}"[..20],
                    FullName = $"Valid {standing} Teacher",
                    RoleId = roleId,
                    Standing = standing
                });
            };
            await validAct.Should().NotThrowAsync($"standing '{standing}' must be accepted by check constraint");
        }
    }

    [SkippableFact]
    public async Task QualificationEvidence_CheckConstraints_Should_EnforceValidVerificationState()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection);
        var qualTypeId = await TeachingIntegrationTestHelpers.EnsureQualificationTypeAsync(connection);

        var teacherId = Guid.NewGuid();
        const string insertTeacherSql = """
            INSERT INTO teaching.teacher_profiles (
                id, teacher_number, full_name, teaching_role_id, campus_affiliation, standing, effective_from, created_by
            ) VALUES (
                @Id, @TeacherNumber, 'Evidence Test Teacher', @RoleId, 'Kampus Utama', 'active', '2026-08-01', 'schema-test'
            );
            """;

        await ExecuteAsync(insertTeacherSql, new
        {
            Id = teacherId,
            TeacherNumber = $"TCH-EV-{Guid.NewGuid():N}"[..20],
            RoleId = roleId
        });

        const string insertEvidenceSql = """
            INSERT INTO teaching.qualification_evidence (
                id, teacher_profile_id, qualification_type_id, title, issuing_authority,
                issue_date, verification_state, created_by
            ) VALUES (
                @Id, @TeacherId, @QualTypeId, 'Test Credential', 'Ministry of Education',
                '2026-01-01', @State, 'schema-test'
            );
            """;

        // 1. Invalid verification state must trigger error 547
        var invalidAct = async () =>
        {
            await ExecuteAsync(insertEvidenceSql, new
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                QualTypeId = qualTypeId,
                State = "invalid_state"
            });
        };

        var ex = await invalidAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid verification state must violate CK_teaching_qev_state (error 547)");

        // 2. All 6 valid states must be accepted
        string[] validStates = ["claimed", "submitted", "verified", "rejected", "expired", "revoked"];
        foreach (var state in validStates)
        {
            var validAct = async () =>
            {
                await ExecuteAsync(insertEvidenceSql, new
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacherId,
                    QualTypeId = qualTypeId,
                    State = state
                });
            };
            await validAct.Should().NotThrowAsync($"verification_state '{state}' must be accepted by check constraint");
        }
    }

    [SkippableFact]
    public async Task TeachingAuthorizations_CheckConstraints_Should_EnforceValidStatus()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection);

        var teacherId = Guid.NewGuid();
        const string insertTeacherSql = """
            INSERT INTO teaching.teacher_profiles (
                id, teacher_number, full_name, teaching_role_id, campus_affiliation, standing, effective_from, created_by
            ) VALUES (
                @Id, @TeacherNumber, 'Auth Test Teacher', @RoleId, 'Kampus Utama', 'active', '2026-08-01', 'schema-test'
            );
            """;

        await ExecuteAsync(insertTeacherSql, new
        {
            Id = teacherId,
            TeacherNumber = $"TCH-AU-{Guid.NewGuid():N}"[..20],
            RoleId = roleId
        });

        const string insertAuthSql = """
            INSERT INTO teaching.teaching_authorizations (
                id, authorization_number, teacher_profile_id, scope_description, campus_scope,
                status, effective_start_date, decision_authority, approved_date, created_by
            ) VALUES (
                @Id, @AuthNumber, @TeacherId, 'Authorized Scope Test', 'All',
                @Status, '2026-08-01', 'Kepala Sekolah', '2026-08-01', 'schema-test'
            );
            """;

        // 1. Invalid authorization status must trigger error 547
        var invalidAct = async () =>
        {
            await ExecuteAsync(insertAuthSql, new
            {
                Id = Guid.NewGuid(),
                AuthNumber = $"AUTH-INV-{Guid.NewGuid():N}"[..20],
                TeacherId = teacherId,
                Status = "pending"
            });
        };

        var ex = await invalidAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid authorization status must violate CK_teaching_auth_status (error 547)");

        // 2. All 6 valid statuses must be accepted
        string[] validStatuses = ["active", "authorized_under_supervision", "restricted", "suspended", "revoked", "expired"];
        foreach (var status in validStatuses)
        {
            var validAct = async () =>
            {
                await ExecuteAsync(insertAuthSql, new
                {
                    Id = Guid.NewGuid(),
                    AuthNumber = $"AUTH-{status[..3].ToUpperInvariant()}-{Guid.NewGuid():N}"[..20],
                    TeacherId = teacherId,
                    Status = status
                });
            };
            await validAct.Should().NotThrowAsync($"authorization status '{status}' must be accepted by check constraint");
        }
    }

    [SkippableFact]
    public async Task SupervisionRequirements_CheckConstraints_Should_EnforceValidTypeAndStatus()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection);

        var teacherId = Guid.NewGuid();
        const string insertTeacherSql = """
            INSERT INTO teaching.teacher_profiles (
                id, teacher_number, full_name, teaching_role_id, campus_affiliation, standing, effective_from, created_by
            ) VALUES (
                @Id, @TeacherNumber, 'Supervision Test Teacher', @RoleId, 'Kampus Utama', 'active', '2026-08-01', 'schema-test'
            );
            """;

        await ExecuteAsync(insertTeacherSql, new
        {
            Id = teacherId,
            TeacherNumber = $"TCH-SU-{Guid.NewGuid():N}"[..20],
            RoleId = roleId
        });

        var authId = Guid.NewGuid();
        const string insertAuthSql = """
            INSERT INTO teaching.teaching_authorizations (
                id, authorization_number, teacher_profile_id, scope_description, campus_scope,
                status, effective_start_date, decision_authority, approved_date, created_by
            ) VALUES (
                @Id, @AuthNumber, @TeacherId, 'Supervision Scope', 'All',
                'authorized_under_supervision', '2026-08-01', 'Kepala Sekolah', '2026-08-01', 'schema-test'
            );
            """;

        await ExecuteAsync(insertAuthSql, new
        {
            Id = authId,
            AuthNumber = $"AUTH-SU-{Guid.NewGuid():N}"[..20],
            TeacherId = teacherId
        });

        const string insertSupSql = """
            INSERT INTO teaching.supervision_requirements (
                id, authorization_id, supervision_type, description, start_date, status, created_by
            ) VALUES (
                @Id, @AuthId, @SupervisionType, 'Supervision condition description', '2026-08-01', @Status, 'schema-test'
            );
            """;

        // 1. Invalid supervision type must fail
        var invalidTypeAct = async () =>
        {
            await ExecuteAsync(insertSupSql, new
            {
                Id = Guid.NewGuid(),
                AuthId = authId,
                SupervisionType = "peer_tutoring",
                Status = "active"
            });
        };
        var exType = await invalidTypeAct.Should().ThrowAsync<SqlException>();
        exType.Which.Number.Should().Be(547, "invalid supervision type must violate CK_teaching_sup_type (error 547)");

        // 2. Invalid status must fail
        var invalidStatusAct = async () =>
        {
            await ExecuteAsync(insertSupSql, new
            {
                Id = Guid.NewGuid(),
                AuthId = authId,
                SupervisionType = "co_teaching",
                Status = "in_progress"
            });
        };
        var exStatus = await invalidStatusAct.Should().ThrowAsync<SqlException>();
        exStatus.Which.Number.Should().Be(547, "invalid supervision status must violate CK_teaching_sup_status (error 547)");
    }

    [SkippableFact]
    public async Task TeachingRecordCorrections_CheckConstraints_Should_EnforceValidStatus()
    {
        SkipIfDatabaseUnavailable();

        const string insertCorrSql = """
            INSERT INTO teaching.teaching_record_corrections (
                id, correction_number, target_table_name, target_record_id, field_name,
                original_value, corrected_value, reason_for_correction, requested_by, requested_date, status, created_by
            ) VALUES (
                @Id, @CorrNumber, 'teacher_profiles', @TargetId, 'display_name',
                'Old Name', 'New Name', 'Typo fix', 'test-user', '2026-08-01', @Status, 'schema-test'
            );
            """;

        // 1. Invalid status must fail
        var invalidStatusAct = async () =>
        {
            await ExecuteAsync(insertCorrSql, new
            {
                Id = Guid.NewGuid(),
                CorrNumber = $"CORR-INV-{Guid.NewGuid():N}"[..20],
                TargetId = Guid.NewGuid(),
                Status = "cancelled"
            });
        };
        var ex = await invalidStatusAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid correction status must violate CK_teaching_trc_status (error 547)");

        // 2. Valid statuses ('requested', 'approved', 'rejected', 'applied') must succeed
        string[] validStatuses = ["requested", "approved", "rejected", "applied"];
        foreach (var status in validStatuses)
        {
            var validAct = async () =>
            {
                await ExecuteAsync(insertCorrSql, new
                {
                    Id = Guid.NewGuid(),
                    CorrNumber = $"CORR-{status[..3].ToUpperInvariant()}-{Guid.NewGuid():N}"[..20],
                    TargetId = Guid.NewGuid(),
                    Status = status
                });
            };
            await validAct.Should().NotThrowAsync($"correction status '{status}' must be accepted by check constraint");
        }
    }

    [SkippableFact]
    public async Task DeletingTeacherProfile_Should_CascadeDeleteChildRecords_And_SetNullOnRecordCorrections()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection);
        var capTypeId = await TeachingIntegrationTestHelpers.EnsureCapabilityTypeAsync(connection);
        var qualTypeId = await TeachingIntegrationTestHelpers.EnsureQualificationTypeAsync(connection);

        // 1. Create Teacher Profile
        var teacherId = Guid.NewGuid();
        const string insertTeacherSql = """
            INSERT INTO teaching.teacher_profiles (
                id, teacher_number, full_name, display_name, teaching_role_id, campus_affiliation, standing, effective_from, created_by
            ) VALUES (
                @Id, @TeacherNumber, 'Cascade Delete Teacher', 'Cascade Teacher', @RoleId, 'Kampus Utama', 'active', '2026-08-01', 'cascade-test'
            );
            """;

        await ExecuteAsync(insertTeacherSql, new
        {
            Id = teacherId,
            TeacherNumber = $"TCH-CAS-{Guid.NewGuid():N}"[..20],
            RoleId = roleId
        });

        // 2. Create Teaching Capability
        var capabilityId = Guid.NewGuid();
        const string insertCapSql = """
            INSERT INTO teaching.teaching_capabilities (
                id, teacher_profile_id, capability_type_id, capability_code, capability_name, created_by
            ) VALUES (
                @Id, @TeacherId, @CapTypeId, 'CAP-CASCADE-MATH', 'Mathematics Instruction', 'cascade-test'
            );
            """;

        await ExecuteAsync(insertCapSql, new
        {
            Id = capabilityId,
            TeacherId = teacherId,
            CapTypeId = capTypeId
        });

        // 3. Create Qualification Evidence
        var evidenceId = Guid.NewGuid();
        const string insertQualSql = """
            INSERT INTO teaching.qualification_evidence (
                id, teacher_profile_id, qualification_type_id, title, issuing_authority, issue_date, verification_state, created_by
            ) VALUES (
                @Id, @TeacherId, @QualTypeId, 'B.Ed Mathematics', 'Universitas Indonesia', '2026-01-01', 'verified', 'cascade-test'
            );
            """;

        await ExecuteAsync(insertQualSql, new
        {
            Id = evidenceId,
            TeacherId = teacherId,
            QualTypeId = qualTypeId
        });

        // 4. Create Teaching Authorization
        var authId = Guid.NewGuid();
        const string insertAuthSql = """
            INSERT INTO teaching.teaching_authorizations (
                id, authorization_number, teacher_profile_id, authorized_capability_id, scope_description, campus_scope,
                status, effective_start_date, decision_authority, approved_date, created_by
            ) VALUES (
                @Id, @AuthNumber, @TeacherId, @CapabilityId, 'Authorized Math Instruction', 'All',
                'authorized_under_supervision', '2026-08-01', 'Kepala Sekolah', '2026-08-01', 'cascade-test'
            );
            """;

        await ExecuteAsync(insertAuthSql, new
        {
            Id = authId,
            AuthNumber = $"AUTH-CAS-{Guid.NewGuid():N}"[..20],
            TeacherId = teacherId,
            CapabilityId = capabilityId
        });

        // 5. Link Qualification Evidence to Authorization (authorization_evidence)
        const string insertAuthEvSql = """
            INSERT INTO teaching.authorization_evidence (
                authorization_id, qualification_evidence_id, created_by
            ) VALUES (
                @AuthId, @EvidenceId, 'cascade-test'
            );
            """;

        await ExecuteAsync(insertAuthEvSql, new
        {
            AuthId = authId,
            EvidenceId = evidenceId
        });

        // 6. Create Supervision Requirement
        var supervisionId = Guid.NewGuid();
        const string insertSupSql = """
            INSERT INTO teaching.supervision_requirements (
                id, authorization_id, supervision_type, description, start_date, status, created_by
            ) VALUES (
                @Id, @AuthId, 'co_teaching', 'Co-teaching with senior faculty', '2026-08-01', 'active', 'cascade-test'
            );
            """;

        await ExecuteAsync(insertSupSql, new
        {
            Id = supervisionId,
            AuthId = authId
        });

        // 7. Create Record Correction referencing this Teacher Profile
        var correctionId = Guid.NewGuid();
        const string insertCorrSql = """
            INSERT INTO teaching.teaching_record_corrections (
                id, correction_number, target_table_name, target_record_id, teacher_profile_id, field_name,
                original_value, corrected_value, reason_for_correction, requested_by, requested_date, status, created_by
            ) VALUES (
                @Id, @CorrNumber, 'teacher_profiles', @TeacherId, @TeacherId, 'display_name',
                'Old Display', 'New Display', 'Name adjustment', 'test-user', '2026-08-01', 'applied', 'cascade-test'
            );
            """;

        await ExecuteAsync(insertCorrSql, new
        {
            Id = correctionId,
            CorrNumber = $"CORR-CAS-{Guid.NewGuid():N}"[..20],
            TeacherId = teacherId
        });

        // ACT: Delete Teacher Profile
        const string deleteTeacherSql = "DELETE FROM teaching.teacher_profiles WHERE id = @Id;";
        var deletedCount = await ExecuteAsync(deleteTeacherSql, new { Id = teacherId });
        deletedCount.Should().Be(1, "teacher profile should be deleted");

        // ASSERT: Cascaded records are deleted
        var remainingProfiles = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.teacher_profiles WHERE id = @Id;", new { Id = teacherId });
        remainingProfiles.Should().Be(0, "teacher profile must be deleted");

        var remainingCapabilities = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.teaching_capabilities WHERE id = @Id;", new { Id = capabilityId });
        remainingCapabilities.Should().Be(0, "teaching capabilities must cascade delete with teacher profile");

        var remainingEvidence = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.qualification_evidence WHERE id = @Id;", new { Id = evidenceId });
        remainingEvidence.Should().Be(0, "qualification evidence must cascade delete with teacher profile");

        var remainingAuth = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.teaching_authorizations WHERE id = @Id;", new { Id = authId });
        remainingAuth.Should().Be(0, "teaching authorizations must cascade delete with teacher profile");

        var remainingAuthEv = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.authorization_evidence WHERE authorization_id = @AuthId;", new { AuthId = authId });
        remainingAuthEv.Should().Be(0, "authorization evidence must cascade delete with teaching authorization");

        var remainingSup = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.supervision_requirements WHERE id = @Id;", new { Id = supervisionId });
        remainingSup.Should().Be(0, "supervision requirements must cascade delete with teaching authorization");

        // ASSERT: Record Correction row is PRESERVED, but its teacher_profile_id is SET TO NULL
        var remainingCorr = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.teaching_record_corrections WHERE id = @Id;", new { Id = correctionId });
        remainingCorr.Should().Be(1, "teaching record corrections must remain for auditability");

        var corrTeacherRef = await ExecuteScalarAsync<Guid?>(
            "SELECT teacher_profile_id FROM teaching.teaching_record_corrections WHERE id = @Id;", new { Id = correctionId });
        corrTeacherRef.Should().BeNull("foreign key FK_teaching_trc_teacher must ON DELETE SET NULL");
    }

    [SkippableFact]
    public async Task RecordCorrection_AtomicUpdate_Should_DirectApplyUpdateTargetTable_And_WriteCorrectionLog_InSingleTransaction()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection);

        // 1. Arrange Teacher Profile
        var teacherId = Guid.NewGuid();
        const string insertTeacherSql = """
            INSERT INTO teaching.teacher_profiles (
                id, teacher_number, full_name, display_name, teaching_role_id, campus_affiliation, standing, effective_from, created_by
            ) VALUES (
                @Id, @TeacherNumber, 'Atomic Test Teacher', 'Original Display Name', @RoleId, 'Kampus Utama', 'active', '2026-08-01', 'atomic-test'
            );
            """;

        await ExecuteAsync(insertTeacherSql, new
        {
            Id = teacherId,
            TeacherNumber = $"TCH-ATM-{Guid.NewGuid():N}"[..20],
            RoleId = roleId
        });

        // 2. Act: Execute atomic direct apply within a single SqlTransaction
        var correctionId = Guid.NewGuid();
        var correctionNumber = $"CORR-ATM-{Guid.NewGuid():N}"[..20];
        const string correctedDisplayName = "Corrected Display Name, Ph.D.";

        await using (var transactionConnection = await OpenConnectionAsync())
        await using (var tx = (SqlTransaction)await transactionConnection.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            try
            {
                // Step A: Update target column in target table
                const string updateTargetSql = """
                    UPDATE teaching.teacher_profiles
                    SET display_name = @CorrectedValue,
                        updated_at = SYSUTCDATETIME(),
                        updated_by = @CurrentUser
                    WHERE id = @TargetRecordId;
                    """;

                using var updateCmd = new SqlCommand(updateTargetSql, transactionConnection, tx);
                updateCmd.Parameters.AddWithValue("@CorrectedValue", correctedDisplayName);
                updateCmd.Parameters.AddWithValue("@CurrentUser", "authority-user");
                updateCmd.Parameters.AddWithValue("@TargetRecordId", teacherId);
                var rowsUpdated = await updateCmd.ExecuteNonQueryAsync();
                rowsUpdated.Should().Be(1);

                // Step B: Insert correction log row with status = 'applied'
                const string insertCorrectionSql = """
                    INSERT INTO teaching.teaching_record_corrections (
                        id, correction_number, target_table_name, target_record_id, teacher_profile_id,
                        field_name, original_value, corrected_value, reason_for_correction,
                        status, requested_by, requested_date, approved_by, approved_date, created_by
                    ) VALUES (
                        @Id, @CorrectionNumber, 'teacher_profiles', @TargetRecordId, @TeacherProfileId,
                        'display_name', 'Original Display Name', @CorrectedValue, 'Official title credential correction',
                        'applied', @CurrentUser, '2026-08-01', @CurrentUser, '2026-08-01', @CurrentUser
                    );
                    """;

                using var insertCmd = new SqlCommand(insertCorrectionSql, transactionConnection, tx);
                insertCmd.Parameters.AddWithValue("@Id", correctionId);
                insertCmd.Parameters.AddWithValue("@CorrectionNumber", correctionNumber);
                insertCmd.Parameters.AddWithValue("@TargetRecordId", teacherId);
                insertCmd.Parameters.AddWithValue("@TeacherProfileId", teacherId);
                insertCmd.Parameters.AddWithValue("@CorrectedValue", correctedDisplayName);
                insertCmd.Parameters.AddWithValue("@CurrentUser", "authority-user");
                var rowsInserted = await insertCmd.ExecuteNonQueryAsync();
                rowsInserted.Should().Be(1);

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // 3. Assert: Both the profile display name update and the audit correction row exist atomically
        var actualDisplayName = await ExecuteScalarAsync<string>(
            "SELECT display_name FROM teaching.teacher_profiles WHERE id = @Id;", new { Id = teacherId });
        actualDisplayName.Should().Be(correctedDisplayName, "target table must reflect corrected value after transaction commit");

        var correctionStatus = await ExecuteScalarAsync<string>(
            "SELECT status FROM teaching.teaching_record_corrections WHERE id = @Id;", new { Id = correctionId });
        correctionStatus.Should().Be("applied", "correction log status must be 'applied' for direct authority correction");

        var originalValue = await ExecuteScalarAsync<string>(
            "SELECT original_value FROM teaching.teaching_record_corrections WHERE id = @Id;", new { Id = correctionId });
        originalValue.Should().Be("Original Display Name", "original value before correction must be preserved");
    }

    [SkippableFact]
    public async Task RecordCorrection_AtomicUpdate_Should_RollbackTargetTableUpdate_When_TransactionFails()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection);

        // 1. Arrange Teacher Profile
        var teacherId = Guid.NewGuid();
        const string initialDisplayName = "Immutable Original Name";
        const string insertTeacherSql = """
            INSERT INTO teaching.teacher_profiles (
                id, teacher_number, full_name, display_name, teaching_role_id, campus_affiliation, standing, effective_from, created_by
            ) VALUES (
                @Id, @TeacherNumber, 'Rollback Test Teacher', @InitialName, @RoleId, 'Kampus Utama', 'active', '2026-08-01', 'atomic-test'
            );
            """;

        await ExecuteAsync(insertTeacherSql, new
        {
            Id = teacherId,
            TeacherNumber = $"TCH-ROL-{Guid.NewGuid():N}"[..20],
            InitialName = initialDisplayName,
            RoleId = roleId
        });

        // 2. Act: Attempt transaction where step 1 updates target table, but step 2 fails with check constraint violation
        var correctionId = Guid.NewGuid();

        var act = async () =>
        {
            await using var transactionConnection = await OpenConnectionAsync();
            await using var tx = (SqlTransaction)await transactionConnection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                // Step A: Update target column in target table
                const string updateTargetSql = """
                    UPDATE teaching.teacher_profiles
                    SET display_name = 'Should Be Rolled Back'
                    WHERE id = @TargetRecordId;
                    """;

                using var updateCmd = new SqlCommand(updateTargetSql, transactionConnection, tx);
                updateCmd.Parameters.AddWithValue("@TargetRecordId", teacherId);
                await updateCmd.ExecuteNonQueryAsync();

                // Step B: Deliberately violate check constraint CK_teaching_trc_status ('invalid_status')
                const string invalidInsertSql = """
                    INSERT INTO teaching.teaching_record_corrections (
                        id, correction_number, target_table_name, target_record_id, field_name,
                        original_value, corrected_value, reason_for_correction,
                        status, requested_by, requested_date, created_by
                    ) VALUES (
                        @Id, 'CORR-FAIL', 'teacher_profiles', @TargetRecordId,
                        'display_name', 'Orig', 'New', 'Reason',
                        'invalid_status', 'user', '2026-08-01', 'atomic-test'
                    );
                    """;

                using var insertCmd = new SqlCommand(invalidInsertSql, transactionConnection, tx);
                insertCmd.Parameters.AddWithValue("@Id", correctionId);
                insertCmd.Parameters.AddWithValue("@TargetRecordId", teacherId);
                await insertCmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        };

        // Assert: Throws SqlException 547
        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547);

        // Assert: Target table was NOT modified (atomicity guaranteed by transaction rollback)
        var actualDisplayName = await ExecuteScalarAsync<string>(
            "SELECT display_name FROM teaching.teacher_profiles WHERE id = @Id;", new { Id = teacherId });
        actualDisplayName.Should().Be(initialDisplayName, "target table must remain unmodified when correction transaction is rolled back");

        var corrExists = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.teaching_record_corrections WHERE id = @Id;", new { Id = correctionId });
        corrExists.Should().Be(0, "failed correction row must not be persisted in database");
    }
}
