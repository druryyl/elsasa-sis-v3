namespace Elsasa.Sis.IntegrationTests.Features.Teaching;

using System.Data;
using Elsasa.Sis.Database;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Database integration test suite verifying DbUp schema migration 20261110_0502 (CR-TCH-ASSIGNMENT-WORKLOAD, P1-S01, P6-S18, ADR-002, ADR-006).
/// Validates script journaling via DatabaseMigrator, provisioning of all 14 Stage 2 domain tables in schema teaching,
/// partial unique index on active teacher availabilities, unique constraints, check constraints across discrete lifecycles,
/// cascading deletes, and intra-schema referential integrity.
/// </summary>
public sealed class TeachingStage2SchemaIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261110_0502_Teaching_AssignmentWorkload_Tables.sql";

    private sealed record ForeignKeyInfo(string Name, byte DeleteReferentialAction);

    [SkippableFact]
    public void TeachingStage2Migration_Should_RunSuccessfullyViaDatabaseMigrator()
    {
        SkipIfDatabaseUnavailable();

        var success = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);

        success.Should().BeTrue("DatabaseMigrator.UpgradeDatabase should succeed idempotently on target database");
    }

    [SkippableFact]
    public void TeachingStage2Migration_Should_BeIdempotentOnSubsequentRuns()
    {
        SkipIfDatabaseUnavailable();

        var firstRun = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);
        firstRun.Should().BeTrue("first migration run should succeed");

        var secondRun = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);
        secondRun.Should().BeTrue("subsequent migration run should succeed idempotently");
    }

    [SkippableFact]
    public async Task TeachingStage2Migration_Should_BeJournaledInSchemaVersions()
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
    public async Task TeachingSchema_Should_ContainAllFourteenStage2Tables()
    {
        SkipIfDatabaseUnavailable();

        const string tablesSql = """
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_TYPE = 'BASE TABLE';
            """;

        var tables = (await QueryAsync<string>(tablesSql)).ToList();

        tables.Should().Contain([
            "assignment_roles",
            "workload_policies",
            "teaching_demands",
            "teacher_eligibility_evaluations",
            "teaching_assignments",
            "capacity_conflicts",
            "teacher_availabilities",
            "teacher_unavailability_windows",
            "workload_commitments",
            "workload_exceptions",
            "teaching_substitutions",
            "teaching_reassignments",
            "assignment_completions",
            "teaching_history_entries"
        ]);
    }

    [SkippableFact]
    public async Task TeachingDemandsTable_Should_ContainAllColumnsIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_NAME = 'teaching_demands';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "demand_code",
            "source_domain",
            "source_work_reference",
            "source_work_label",
            "academic_period_id",
            "required_capability_code",
            "required_assignment_role_id",
            "expected_workload_hours",
            "workload_unit",
            "requires_assessment_authority",
            "requested_start_date",
            "requested_end_date",
            "campus_scope",
            "required_teachers_count",
            "status",
            "requested_by",
            "requested_at",
            "decline_reason",
            "clarification_notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('teaching.teaching_demands') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_teaching_teaching_demands");
        indexes.Should().Contain("UQ_teaching_demands_code");
        indexes.Should().Contain("IX_teaching_demands_period_status");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('teaching.teaching_demands');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_dem_role");
        var roleFk = fks.Single(fk => fk.Name == "FK_teaching_dem_role");
        roleFk.DeleteReferentialAction.Should().Be(0, "FK_teaching_dem_role must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task TeachingAssignmentsTable_Should_ContainAllColumnsIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'teaching' AND TABLE_NAME = 'teaching_assignments';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "assignment_number",
            "teaching_demand_id",
            "teacher_profile_id",
            "assignment_role_id",
            "assignment_scope",
            "scope_reference_id",
            "scope_label",
            "academic_period_id",
            "effective_start_date",
            "effective_end_date",
            "workload_commitment_value",
            "workload_unit",
            "status",
            "approved_by",
            "approved_date",
            "teacher_acknowledged",
            "teacher_acknowledged_date",
            "predecessor_assignment_id",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('teaching.teaching_assignments') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("PK_teaching_teaching_assignments");
        indexes.Should().Contain("UQ_teaching_assignments_number");
        indexes.Should().Contain("IX_teaching_assignments_teacher_period");
        indexes.Should().Contain("IX_teaching_assignments_scope");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('teaching.teaching_assignments');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_asn_demand");
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_asn_teacher");
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_asn_role");
        fks.Should().ContainSingle(fk => fk.Name == "FK_teaching_asn_predecessor");

        fks.Single(fk => fk.Name == "FK_teaching_asn_demand").DeleteReferentialAction.Should().Be(0);
        fks.Single(fk => fk.Name == "FK_teaching_asn_teacher").DeleteReferentialAction.Should().Be(0);
        fks.Single(fk => fk.Name == "FK_teaching_asn_role").DeleteReferentialAction.Should().Be(0);
        fks.Single(fk => fk.Name == "FK_teaching_asn_predecessor").DeleteReferentialAction.Should().Be(0);
    }

    [SkippableFact]
    public async Task TeachingDemands_CheckConstraint_Should_EnforceDiscreteStatuses()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);

        const string insertSql = """
            INSERT INTO teaching.teaching_demands (
                id, demand_code, source_domain, source_work_reference, source_work_label,
                academic_period_id, required_capability_code, required_assignment_role_id,
                expected_workload_hours, workload_unit, requires_assessment_authority,
                requested_start_date, requested_end_date, campus_scope, required_teachers_count,
                status, requested_by, requested_at, created_at, created_by
            ) VALUES (
                @Id, @DemandCode, 'Academics', NEWID(), 'Matematika SD',
                NEWID(), 'MATH_ELEM', @RoleId,
                4.00, 'Teaching Hours', 1,
                '2026-08-01', '2026-12-15', 'Kampus Utama', 1,
                @Status, 'coordinator', SYSUTCDATETIME(), SYSUTCDATETIME(), 'test'
            );
            """;

        // 1. Invalid status must fail with check constraint violation (547)
        var invalidAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                DemandCode = $"DEM-INV-{Guid.NewGuid():N}"[..20],
                RoleId = roleId,
                Status = "invalid_dem_status"
            });
        };
        var ex = await invalidAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid status must violate CK_teaching_dem_status");

        // 2. All 7 discrete valid statuses must succeed
        var validStatuses = new[]
        {
            "received", "accepted_for_staffing", "returned_for_clarification",
            "changed", "cancelled", "declined", "staffed"
        };

        foreach (var status in validStatuses)
        {
            var validAct = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    DemandCode = $"DEM-OK-{Guid.NewGuid():N}"[..20],
                    RoleId = roleId,
                    Status = status
                });
            };
            await validAct.Should().NotThrowAsync($"status '{status}' must be accepted by CK_teaching_dem_status");
        }
    }

    [SkippableFact]
    public async Task TeachingAssignments_CheckConstraint_Should_EnforceDiscreteStatuses()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, roleId);

        const string insertSql = """
            INSERT INTO teaching.teaching_assignments (
                id, assignment_number, teaching_demand_id, teacher_profile_id,
                assignment_role_id, assignment_scope, scope_reference_id, scope_label,
                academic_period_id, effective_start_date, effective_end_date,
                workload_commitment_value, workload_unit, status,
                teacher_acknowledged, created_at, created_by
            ) VALUES (
                @Id, @AssignmentNumber, @DemandId, @TeacherId,
                @RoleId, 'learning_unit', NEWID(), 'Scope Unit',
                NEWID(), '2026-08-01', '2026-12-15',
                4.00, 'Teaching Hours', @Status,
                0, SYSUTCDATETIME(), 'test'
            );
            """;

        // 1. Invalid status must fail with check constraint violation (547)
        var invalidAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                AssignmentNumber = $"ASN-INV-{Guid.NewGuid():N}"[..20],
                DemandId = demandId,
                TeacherId = teacherId,
                RoleId = roleId,
                Status = "pending_invalid"
            });
        };
        var ex = await invalidAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid status must violate CK_teaching_asn_status");

        // 2. All 8 valid statuses must succeed
        var validStatuses = new[]
        {
            "proposed", "approved", "rejected", "withdrawn",
            "returned_for_revision", "suspended", "cancelled", "completed"
        };

        foreach (var status in validStatuses)
        {
            var validAct = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    AssignmentNumber = $"ASN-OK-{Guid.NewGuid():N}"[..20],
                    DemandId = demandId,
                    TeacherId = teacherId,
                    RoleId = roleId,
                    Status = status
                });
            };
            await validAct.Should().NotThrowAsync($"status '{status}' must be accepted by CK_teaching_asn_status");
        }
    }

    [SkippableFact]
    public async Task WorkloadCommitments_CheckConstraint_Should_EnforceDiscreteStatuses()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var policyId = await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);

        const string insertSql = """
            INSERT INTO teaching.workload_commitments (
                id, teacher_profile_id, academic_period_id, workload_policy_id,
                planned_workload_hours, committed_workload_hours, max_capacity_limit_hours, remaining_capacity_hours,
                status, active_assignment_count, approved_exception_count, created_at, created_by
            ) VALUES (
                @Id, @TeacherId, @AcademicPeriodId, @PolicyId,
                0.00, 16.00, 24.00, 8.00,
                @Status, 2, 0, SYSUTCDATETIME(), 'test'
            );
            """;

        // 1. Invalid status must fail with check constraint violation (547)
        var invalidAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                AcademicPeriodId = Guid.NewGuid(),
                PolicyId = policyId,
                Status = "unspecified_status"
            });
        };
        var ex = await invalidAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid status must violate CK_teaching_wcom_status");

        // 2. All 5 valid statuses must succeed
        var validStatuses = new[]
        {
            "normal", "under_allocated", "approaching_limit", "overloaded_with_exception", "breached"
        };

        foreach (var status in validStatuses)
        {
            var validAct = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacherId,
                    AcademicPeriodId = Guid.NewGuid(),
                    PolicyId = policyId,
                    Status = status
                });
            };
            await validAct.Should().NotThrowAsync($"status '{status}' must be accepted by CK_teaching_wcom_status");
        }
    }

    [SkippableFact]
    public async Task WorkloadExceptions_CheckConstraint_Should_EnforceDiscreteStatuses()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, roleId);
        var assignmentId = await TeachingIntegrationTestHelpers.EnsureTeachingAssignmentAsync(connection, demandId, teacherId, roleId);
        var policyId = await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);

        const string insertSql = """
            INSERT INTO teaching.workload_exceptions (
                id, exception_number, teaching_assignment_id, teacher_profile_id, workload_policy_id,
                exceeded_hours, justification, recommended_by, recommended_date,
                effective_start_date, effective_end_date, status, created_at, created_by
            ) VALUES (
                @Id, @ExceptionNumber, @AssignmentId, @TeacherId, @PolicyId,
                4.00, 'Special circumstances require overload', 'Coordinator', '2026-08-01',
                '2026-08-01', '2026-12-15', @Status, SYSUTCDATETIME(), 'test'
            );
            """;

        // 1. Invalid status must fail with check constraint violation (547)
        var invalidAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                ExceptionNumber = $"WEX-INV-{Guid.NewGuid():N}"[..20],
                AssignmentId = assignmentId,
                TeacherId = teacherId,
                PolicyId = policyId,
                Status = "pending_validation"
            });
        };
        var ex = await invalidAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid status must violate CK_teaching_wexc_status");

        // 2. All 4 valid statuses must succeed
        var validStatuses = new[] { "requested", "recommended", "approved", "rejected" };

        foreach (var status in validStatuses)
        {
            var validAct = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    ExceptionNumber = $"WEX-OK-{Guid.NewGuid():N}"[..20],
                    AssignmentId = assignmentId,
                    TeacherId = teacherId,
                    PolicyId = policyId,
                    Status = status
                });
            };
            await validAct.Should().NotThrowAsync($"status '{status}' must be accepted by CK_teaching_wexc_status");
        }
    }

    [SkippableFact]
    public async Task TeachingSubstitutions_CheckConstraint_Should_EnforceDiscreteStatuses()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var origTeacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var subTeacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, roleId);
        var assignmentId = await TeachingIntegrationTestHelpers.EnsureTeachingAssignmentAsync(connection, demandId, origTeacherId, roleId);

        const string insertSql = """
            INSERT INTO teaching.teaching_substitutions (
                id, substitution_number, original_assignment_id, original_teacher_id, substitute_teacher_id,
                coverage_start_date, coverage_end_date, coverage_scope_type, reason, workload_impact_hours,
                status, approved_by, approved_date, created_at, created_by
            ) VALUES (
                @Id, @SubNumber, @AssignmentId, @OrigTeacherId, @SubTeacherId,
                '2026-09-01', '2026-09-15', 'full', 'Medical Leave Coverage', 4.00,
                @Status, 'Teaching Coordinator', '2026-09-01', SYSUTCDATETIME(), 'test'
            );
            """;

        // 1. Invalid status must fail with check constraint violation (547)
        var invalidAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                SubNumber = $"SUB-INV-{Guid.NewGuid():N}"[..20],
                AssignmentId = assignmentId,
                OrigTeacherId = origTeacherId,
                SubTeacherId = subTeacherId,
                Status = "pending_clearance"
            });
        };
        var ex = await invalidAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid status must violate CK_teaching_sub_status");

        // 2. All 5 valid statuses must succeed
        var validStatuses = new[] { "proposed", "approved", "active", "completed", "cancelled" };

        foreach (var status in validStatuses)
        {
            var validAct = async () =>
            {
                await ExecuteAsync(insertSql, new
                {
                    Id = Guid.NewGuid(),
                    SubNumber = $"SUB-OK-{Guid.NewGuid():N}"[..20],
                    AssignmentId = assignmentId,
                    OrigTeacherId = origTeacherId,
                    SubTeacherId = subTeacherId,
                    Status = status
                });
            };
            await validAct.Should().NotThrowAsync($"status '{status}' must be accepted by CK_teaching_sub_status");
        }
    }

    [SkippableFact]
    public async Task TeacherAvailabilities_FilteredUniqueIndex_Should_RejectDuplicateActive_And_AllowInactiveDuplicates()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var periodId = Guid.NewGuid();

        const string insertSql = """
            INSERT INTO teaching.teacher_availabilities (
                id, teacher_profile_id, academic_period_id, max_available_hours,
                campus_scope, available_delivery_modes, effective_start_date, effective_end_date,
                status, created_at, created_by
            ) VALUES (
                @Id, @TeacherId, @AcademicPeriodId, 24.00,
                'All', 'All', '2026-08-01', '2026-12-15',
                @Status, SYSUTCDATETIME(), 'test'
            );
            """;

        // 1. Insert first active availability: succeeds
        await ExecuteAsync(insertSql, new
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            AcademicPeriodId = periodId,
            Status = "active"
        });

        // 2. Insert second active availability with same (teacher_profile_id, academic_period_id):
        // must fail with unique index violation (error 2601 or 2627)
        var duplicateActiveAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                AcademicPeriodId = periodId,
                Status = "active"
            });
        };
        var ex = await duplicateActiveAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate active availability for same teacher and period must trigger unique index violation");

        // 3. Insert inactive availability ('revoked') for same teacher and period: must succeed
        var revokedAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                AcademicPeriodId = periodId,
                Status = "revoked"
            });
        };
        await revokedAct.Should().NotThrowAsync("UQ_teaching_teacher_availabilities_active filtered index must permit multiple inactive records");

        // 4. Insert another inactive availability ('superseded') for same teacher and period: must succeed
        var supersededAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                AcademicPeriodId = periodId,
                Status = "superseded"
            });
        };
        await supersededAct.Should().NotThrowAsync("UQ_teaching_teacher_availabilities_active filtered index must permit superseded records");
    }

    [SkippableFact]
    public async Task WorkloadCommitments_UniqueConstraint_Should_RejectDuplicateTeacherAndPeriod()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var policyId = await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);
        var periodId = Guid.NewGuid();

        const string insertSql = """
            INSERT INTO teaching.workload_commitments (
                id, teacher_profile_id, academic_period_id, workload_policy_id,
                planned_workload_hours, committed_workload_hours, max_capacity_limit_hours, remaining_capacity_hours,
                status, active_assignment_count, approved_exception_count, created_at, created_by
            ) VALUES (
                @Id, @TeacherId, @AcademicPeriodId, @PolicyId,
                0.00, 16.00, 24.00, 8.00,
                'normal', 2, 0, SYSUTCDATETIME(), 'test'
            );
            """;

        // 1. Insert first record: succeeds
        await ExecuteAsync(insertSql, new
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            AcademicPeriodId = periodId,
            PolicyId = policyId
        });

        // 2. Insert second record with same teacher and period: must fail with unique constraint violation
        var duplicateAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                AcademicPeriodId = periodId,
                PolicyId = policyId
            });
        };

        var ex = await duplicateAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "UQ_teaching_workload_commitments_teacher_period must reject duplicate (teacher_profile_id, academic_period_id)");
    }

    [SkippableFact]
    public async Task AssignmentCompletions_UniqueConstraint_Should_RejectDuplicateCompletionPerAssignment()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, roleId);
        var assignmentId = await TeachingIntegrationTestHelpers.EnsureTeachingAssignmentAsync(connection, demandId, teacherId, roleId);

        const string insertSql = """
            INSERT INTO teaching.assignment_completions (
                id, teaching_assignment_id, completion_date, academic_confirmation_received,
                handover_confirmed, unresolved_items_status, capacity_released_date, completed_by,
                status, created_at, created_by
            ) VALUES (
                @Id, @AssignmentId, '2026-12-15', 1,
                1, 'none', '2026-12-15', 'Dean of Academics',
                'completed', SYSUTCDATETIME(), 'test'
            );
            """;

        // 1. Insert first completion: succeeds
        await ExecuteAsync(insertSql, new
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignmentId
        });

        // 2. Insert second completion for same assignment: must fail with unique constraint violation
        var duplicateAct = async () =>
        {
            await ExecuteAsync(insertSql, new
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId
            });
        };

        var ex = await duplicateAct.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "UQ_teaching_assignment_completions_asn must reject duplicate completion record for same assignment");
    }

    [SkippableFact]
    public async Task IntraSchemaForeignKeys_Should_SpecifyConfiguredReferentialActions()
    {
        SkipIfDatabaseUnavailable();

        const string fkQuery = """
            SELECT fk.name AS Name, fk.delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys fk
            JOIN sys.schemas s ON fk.schema_id = s.schema_id
            WHERE s.name = 'teaching';
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkQuery)).ToDictionary(f => f.Name, f => f.DeleteReferentialAction);

        // Verify CASCADE actions (delete_referential_action = 1)
        var expectedCascadeFks = new[]
        {
            "FK_teaching_tee_demand",
            "FK_teaching_tee_profile",
            "FK_teaching_cnf_teacher",
            "FK_teaching_tav_teacher",
            "FK_teaching_unav_teacher",
            "FK_teaching_wcom_teacher",
            "FK_teaching_wex_assignment",
            "FK_teaching_acmp_assignment",
            "FK_teaching_hist_teacher"
        };

        foreach (var fkName in expectedCascadeFks)
        {
            fks.Should().ContainKey(fkName);
            fks[fkName].Should().Be(1, $"{fkName} must be configured with ON DELETE CASCADE (action = 1)");
        }

        // Verify SET NULL actions (delete_referential_action = 2)
        var expectedSetNullFks = new[]
        {
            "FK_teaching_cnf_asn",
            "FK_teaching_cnf_dem"
        };

        foreach (var fkName in expectedSetNullFks)
        {
            fks.Should().ContainKey(fkName);
            fks[fkName].Should().Be(2, $"{fkName} must be configured with ON DELETE SET NULL (action = 2)");
        }

        // Verify NO ACTION actions (delete_referential_action = 0)
        var expectedNoActionFks = new[]
        {
            "FK_teaching_dem_role",
            "FK_teaching_asn_demand",
            "FK_teaching_asn_teacher",
            "FK_teaching_asn_role",
            "FK_teaching_asn_predecessor",
            "FK_teaching_wcom_policy",
            "FK_teaching_wex_teacher",
            "FK_teaching_wex_policy",
            "FK_teaching_sub_assignment",
            "FK_teaching_sub_orig_teacher",
            "FK_teaching_sub_sub_teacher",
            "FK_teaching_rea_orig_assignment",
            "FK_teaching_rea_pred_teacher",
            "FK_teaching_rea_new_teacher",
            "FK_teaching_rea_repl_assignment"
        };

        foreach (var fkName in expectedNoActionFks)
        {
            fks.Should().ContainKey(fkName);
            fks[fkName].Should().Be(0, $"{fkName} must be configured with ON DELETE NO ACTION (action = 0)");
        }
    }

    [SkippableFact]
    public async Task CascadeDeletes_DemandDeletion_Should_CascadeToEligibilityEvaluations()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, roleId);

        var evalId = Guid.NewGuid();
        const string insertEvalSql = """
            INSERT INTO teaching.teacher_eligibility_evaluations (
                id, teaching_demand_id, teacher_profile_id, evaluation_date, evaluated_by,
                is_eligible, eligibility_status, profile_active_check, authorization_check_passed,
                qualification_check_passed, availability_check_passed, capacity_check_passed,
                explanation, created_at, created_by
            ) VALUES (
                @Id, @DemandId, @TeacherId, '2026-08-01', 'system',
                1, 'eligible', 1, 1, 1, 1, 1,
                'Candidate satisfies all requirements', SYSUTCDATETIME(), 'test'
            );
            """;

        await ExecuteAsync(insertEvalSql, new { Id = evalId, DemandId = demandId, TeacherId = teacherId });

        // Verify evaluation exists
        var evalExistsBefore = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.teacher_eligibility_evaluations WHERE id = @Id;",
            new { Id = evalId });
        evalExistsBefore.Should().Be(1);

        // Delete parent demand
        await ExecuteAsync("DELETE FROM teaching.teaching_demands WHERE id = @Id;", new { Id = demandId });

        // Verify evaluation was cascade deleted
        var evalExistsAfter = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.teacher_eligibility_evaluations WHERE id = @Id;",
            new { Id = evalId });
        evalExistsAfter.Should().Be(0, "deleting demand must cascade delete associated eligibility evaluations");
    }

    [SkippableFact]
    public async Task CascadeDeletes_AssignmentDeletion_Should_CascadeToExceptionsAndCompletions()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, roleId);
        var assignmentId = await TeachingIntegrationTestHelpers.EnsureTeachingAssignmentAsync(connection, demandId, teacherId, roleId);
        var policyId = await TeachingIntegrationTestHelpers.EnsureWorkloadPolicyAsync(connection);

        // Insert workload exception referencing assignment
        var wexId = Guid.NewGuid();
        const string insertWexSql = """
            INSERT INTO teaching.workload_exceptions (
                id, exception_number, teaching_assignment_id, teacher_profile_id, workload_policy_id,
                exceeded_hours, justification, recommended_by, recommended_date,
                effective_start_date, effective_end_date, status, created_at, created_by
            ) VALUES (
                @Id, @ExceptionNumber, @AssignmentId, @TeacherId, @PolicyId,
                4.00, 'Overload justification', 'Coordinator', '2026-08-01',
                '2026-08-01', '2026-12-15', 'requested', SYSUTCDATETIME(), 'test'
            );
            """;

        await ExecuteAsync(insertWexSql, new
        {
            Id = wexId,
            ExceptionNumber = $"WEX-CAS-{Guid.NewGuid():N}"[..20],
            AssignmentId = assignmentId,
            TeacherId = teacherId,
            PolicyId = policyId
        });

        // Insert assignment completion referencing assignment
        var compId = Guid.NewGuid();
        const string insertCompSql = """
            INSERT INTO teaching.assignment_completions (
                id, teaching_assignment_id, completion_date, academic_confirmation_received,
                handover_confirmed, unresolved_items_status, capacity_released_date, completed_by,
                status, created_at, created_by
            ) VALUES (
                @Id, @AssignmentId, '2026-12-15', 1,
                1, 'none', '2026-12-15', 'Coordinator',
                'completed', SYSUTCDATETIME(), 'test'
            );
            """;

        await ExecuteAsync(insertCompSql, new { Id = compId, AssignmentId = assignmentId });

        // Delete parent assignment
        await ExecuteAsync("DELETE FROM teaching.teaching_assignments WHERE id = @Id;", new { Id = assignmentId });

        // Verify both exception and completion were cascade deleted
        var wexCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.workload_exceptions WHERE id = @Id;",
            new { Id = wexId });
        wexCount.Should().Be(0, "deleting assignment must cascade delete associated workload exception");

        var compCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.assignment_completions WHERE id = @Id;",
            new { Id = compId });
        compCount.Should().Be(0, "deleting assignment must cascade delete associated assignment completion");
    }

    [SkippableFact]
    public async Task CascadeDeletes_AssignmentAndDemandDeletion_Should_SetNullOnCapacityConflicts()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureAssignmentRoleAsync(connection);
        var teacherId = await TeachingIntegrationTestHelpers.EnsureTeacherProfileAsync(connection);
        var demandId = await TeachingIntegrationTestHelpers.EnsureTeachingDemandAsync(connection, roleId);
        var assignmentId = await TeachingIntegrationTestHelpers.EnsureTeachingAssignmentAsync(connection, demandId, teacherId, roleId);

        var conflictId = Guid.NewGuid();
        const string insertConflictSql = """
            INSERT INTO teaching.capacity_conflicts (
                id, teaching_assignment_id, teaching_demand_id, teacher_profile_id,
                conflict_type, conflict_classification, severity, description,
                detected_date, status, created_at, created_by
            ) VALUES (
                @Id, @AssignmentId, @DemandId, @TeacherId,
                'overload', 'capacity_ceiling_exceeded', 'blocking', 'Conflict detected',
                '2026-08-01', 'unresolved', SYSUTCDATETIME(), 'test'
            );
            """;

        await ExecuteAsync(insertConflictSql, new
        {
            Id = conflictId,
            AssignmentId = assignmentId,
            DemandId = demandId,
            TeacherId = teacherId
        });

        // Delete assignment
        await ExecuteAsync("DELETE FROM teaching.teaching_assignments WHERE id = @Id;", new { Id = assignmentId });

        // Delete demand
        await ExecuteAsync("DELETE FROM teaching.teaching_demands WHERE id = @Id;", new { Id = demandId });

        // Conflict should still exist, but with assignment and demand IDs set to NULL
        const string checkConflictSql = """
            SELECT teaching_assignment_id AS AssignmentId, teaching_demand_id AS DemandId
            FROM teaching.capacity_conflicts
            WHERE id = @Id;
            """;

        var (resolvedAssignmentId, resolvedDemandId) = await ExecuteScalarAsync<(Guid?, Guid?)>(
            checkConflictSql, new { Id = conflictId });

        resolvedAssignmentId.Should().BeNull("deleting assignment must set teaching_assignment_id to NULL");
        resolvedDemandId.Should().BeNull("deleting demand must set teaching_demand_id to NULL");
    }
}
