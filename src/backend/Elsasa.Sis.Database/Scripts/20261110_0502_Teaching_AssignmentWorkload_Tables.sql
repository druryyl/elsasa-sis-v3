-- =============================================================================
-- 20261110_0502_Teaching_AssignmentWorkload_Tables.sql
-- Teaching domain Stage 2 Teaching Assignment & Workload persistence schema
-- (CR-TCH-ASSIGNMENT-WORKLOAD, ADR-001, ADR-002, ADR-006, TD-01, P1-S01).
--
-- 1. Idempotently provisions isolated schema [teaching].
-- 2. Provisions 14 authoritative domain tables in schema [teaching]:
--    1.  teaching.assignment_roles: Configurable teaching staffing roles.
--    2.  teaching.workload_policies: Workload policy limits, thresholds and rules.
--    3.  teaching.teaching_demands: Cross-domain formal capacity requests.
--    4.  teaching.teacher_eligibility_evaluations: Explainable 5-factor candidate evaluations.
--    5.  teaching.teaching_assignments: Operational assignment of teachers to scopes.
--    6.  teaching.capacity_conflicts: Staffing and capacity conflict tracking.
--    7.  teaching.teacher_availabilities: Baseline weekly recurring availability.
--    8.  teaching.teacher_unavailability_windows: Leaves and blackout date windows.
--    9.  teaching.workload_commitments: Materialized period capacity snapshots.
--    10. teaching.workload_exceptions: Authorized overload departures governance.
--    11. teaching.teaching_substitutions: Emergency temporary coverage (Res 3.8).
--    12. teaching.teaching_reassignments: Permanent transfers with handover tracking.
--    13. teaching.assignment_completions: Conclusion and capacity release tracking.
--    14. teaching.teaching_history_entries: Append-only audit history log.
-- 3. Provisions indexes, unique invariants, and intra-schema foreign key constraints.
-- 4. Preserves zero physical foreign keys across domain schemas (ADR-001, ADR-002).
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 0. Schema: teaching
-- -----------------------------------------------------------------------------
IF SCHEMA_ID(N'teaching') IS NULL
BEGIN
    EXEC (N'CREATE SCHEMA [teaching] AUTHORIZATION [dbo];');
END;
GO

-- -----------------------------------------------------------------------------
-- 1. Table: teaching.assignment_roles
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.assignment_roles', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[assignment_roles] (
        [id]                     UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_asnr_id] DEFAULT (NEWID()),
        [code]                   VARCHAR(50)       NOT NULL,
        [name]                   NVARCHAR(100)     NOT NULL,
        [description]            NVARCHAR(MAX)     NULL,
        [is_lead_role]           BIT               NOT NULL CONSTRAINT [DF_teaching_asnr_lead] DEFAULT (0),
        [counts_toward_workload] BIT               NOT NULL CONSTRAINT [DF_teaching_asnr_counts] DEFAULT (1),
        [is_active]              BIT               NOT NULL CONSTRAINT [DF_teaching_asnr_active] DEFAULT (1),
        [created_at]             DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_asnr_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]             NVARCHAR(256)     NOT NULL,
        [updated_at]             DATETIMEOFFSET(7) NULL,
        [updated_by]             NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_assignment_roles] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_assignment_roles_code] UNIQUE NONCLUSTERED ([code])
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Table: teaching.workload_policies
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.workload_policies', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[workload_policies] (
        [id]                         UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_wpol_id] DEFAULT (NEWID()),
        [code]                       VARCHAR(50)       NOT NULL,
        [name]                       NVARCHAR(150)     NOT NULL,
        [academic_year_id]           UNIQUEIDENTIFIER  NOT NULL,
        [version]                    VARCHAR(20)       NOT NULL,
        [workload_unit]              VARCHAR(50)       NOT NULL CONSTRAINT [DF_teaching_wpol_unit] DEFAULT ('Teaching Hours'),
        [max_standard_capacity]      DECIMAL(5,2)      NOT NULL,
        [warning_threshold]          DECIMAL(5,2)      NOT NULL,
        [prohibited_threshold]       DECIMAL(5,2)      NOT NULL,
        [includes_assessment_effort] BIT               NOT NULL CONSTRAINT [DF_teaching_wpol_assess] DEFAULT (1),
        [includes_advising_effort]   BIT               NOT NULL CONSTRAINT [DF_teaching_wpol_advise] DEFAULT (1),
        [includes_preparation_time]  BIT               NOT NULL CONSTRAINT [DF_teaching_wpol_prep] DEFAULT (0),
        [calculation_rules_summary]  NVARCHAR(MAX)     NOT NULL,
        [effective_start_date]       DATE              NOT NULL,
        [effective_end_date]         DATE              NULL,
        [status]                     VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_wpol_status] DEFAULT ('active'),
        [approved_by]                NVARCHAR(256)     NOT NULL,
        [approved_date]              DATE              NOT NULL,
        [created_at]                 DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_wpol_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                 NVARCHAR(256)     NOT NULL,
        [updated_at]                 DATETIMEOFFSET(7) NULL,
        [updated_by]                 NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_workload_policies] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_workload_policies_code] UNIQUE NONCLUSTERED ([code]),
        CONSTRAINT [CK_teaching_wpol_status] CHECK ([status] IN ('draft', 'active', 'superseded', 'archived'))
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: teaching.teaching_demands
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teaching_demands', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teaching_demands] (
        [id]                            UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_dem_id] DEFAULT (NEWID()),
        [demand_code]                   VARCHAR(50)       NOT NULL,
        [source_domain]                 VARCHAR(50)       NOT NULL,
        [source_work_reference]         UNIQUEIDENTIFIER  NOT NULL,
        [source_work_label]             NVARCHAR(255)     NOT NULL,
        [academic_period_id]            UNIQUEIDENTIFIER  NOT NULL,
        [required_capability_code]      VARCHAR(100)      NOT NULL,
        [required_assignment_role_id]   UNIQUEIDENTIFIER  NOT NULL,
        [expected_workload_hours]       DECIMAL(5,2)      NOT NULL,
        [workload_unit]                 VARCHAR(50)       NOT NULL CONSTRAINT [DF_teaching_dem_unit] DEFAULT ('Teaching Hours'),
        [requires_assessment_authority] BIT               NOT NULL CONSTRAINT [DF_teaching_dem_auth] DEFAULT (1),
        [requested_start_date]          DATE              NOT NULL,
        [requested_end_date]            DATE              NOT NULL,
        [campus_scope]                  NVARCHAR(100)     NOT NULL CONSTRAINT [DF_teaching_dem_campus] DEFAULT ('Kampus Utama'),
        [required_teachers_count]       INT               NOT NULL CONSTRAINT [DF_teaching_dem_count] DEFAULT (1),
        [status]                        VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_dem_status] DEFAULT ('received'),
        [requested_by]                  NVARCHAR(256)     NOT NULL,
        [requested_at]                  DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_dem_requested_at] DEFAULT (SYSUTCDATETIME()),
        [decline_reason]                NVARCHAR(MAX)     NULL,
        [clarification_notes]           NVARCHAR(MAX)     NULL,
        [created_at]                    DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_dem_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                    NVARCHAR(256)     NOT NULL,
        [updated_at]                    DATETIMEOFFSET(7) NULL,
        [updated_by]                    NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_teaching_demands] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_demands_code] UNIQUE NONCLUSTERED ([demand_code]),
        CONSTRAINT [FK_teaching_dem_role] FOREIGN KEY ([required_assignment_role_id])
            REFERENCES [teaching].[assignment_roles] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_dem_status] CHECK (
            [status] IN ('received', 'accepted_for_staffing', 'returned_for_clarification', 'changed', 'cancelled', 'declined', 'staffed')
        )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_demands_period_status'
      AND [object_id] = OBJECT_ID(N'teaching.teaching_demands')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_demands_period_status]
        ON [teaching].[teaching_demands] ([academic_period_id], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Table: teaching.teacher_eligibility_evaluations
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teacher_eligibility_evaluations', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teacher_eligibility_evaluations] (
        [id]                         UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_tee_id] DEFAULT (NEWID()),
        [teaching_demand_id]         UNIQUEIDENTIFIER  NOT NULL,
        [teacher_profile_id]         UNIQUEIDENTIFIER  NOT NULL,
        [evaluation_date]            DATE              NOT NULL,
        [evaluated_by]               NVARCHAR(256)     NOT NULL,
        [is_eligible]                BIT               NOT NULL,
        [eligibility_status]         VARCHAR(50)       NOT NULL,
        [profile_active_check]       BIT               NOT NULL,
        [authorization_check_passed] BIT               NOT NULL,
        [qualification_check_passed] BIT               NOT NULL,
        [availability_check_passed]  BIT               NOT NULL,
        [capacity_check_passed]      BIT               NOT NULL,
        [explanation]                NVARCHAR(MAX)     NOT NULL,
        [notes]                      NVARCHAR(MAX)     NULL,
        [created_at]                 DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_tee_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                 NVARCHAR(256)     NOT NULL,
        CONSTRAINT [PK_teaching_teacher_eligibility_evaluations] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_teaching_tee_demand] FOREIGN KEY ([teaching_demand_id])
            REFERENCES [teaching].[teaching_demands] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_teaching_tee_profile] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE CASCADE,
        CONSTRAINT [CK_teaching_eval_status] CHECK (
            [eligibility_status] IN ('eligible', 'eligible_under_supervision', 'ineligible_unqualified', 'ineligible_conflicted', 'ineligible_capacity_exceeded')
        )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_tee_demand_teacher'
      AND [object_id] = OBJECT_ID(N'teaching.teacher_eligibility_evaluations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_tee_demand_teacher]
        ON [teaching].[teacher_eligibility_evaluations] ([teaching_demand_id], [teacher_profile_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 5. Table: teaching.teaching_assignments
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teaching_assignments', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teaching_assignments] (
        [id]                        UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_asn_id] DEFAULT (NEWID()),
        [assignment_number]         VARCHAR(50)       NOT NULL,
        [teaching_demand_id]        UNIQUEIDENTIFIER  NOT NULL,
        [teacher_profile_id]        UNIQUEIDENTIFIER  NOT NULL,
        [assignment_role_id]        UNIQUEIDENTIFIER  NOT NULL,
        [assignment_scope]          VARCHAR(50)       NOT NULL,
        [scope_reference_id]        UNIQUEIDENTIFIER  NOT NULL,
        [scope_label]               NVARCHAR(255)     NOT NULL,
        [academic_period_id]        UNIQUEIDENTIFIER  NOT NULL,
        [effective_start_date]      DATE              NOT NULL,
        [effective_end_date]        DATE              NOT NULL,
        [workload_commitment_value] DECIMAL(5,2)      NOT NULL,
        [workload_unit]             VARCHAR(50)       NOT NULL CONSTRAINT [DF_teaching_asn_unit] DEFAULT ('Teaching Hours'),
        [status]                    VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_asn_status] DEFAULT ('proposed'),
        [approved_by]               NVARCHAR(256)     NULL,
        [approved_date]             DATE              NULL,
        [teacher_acknowledged]      BIT               NOT NULL CONSTRAINT [DF_teaching_asn_ack] DEFAULT (0),
        [teacher_acknowledged_date] DATE              NULL,
        [predecessor_assignment_id] UNIQUEIDENTIFIER  NULL,
        [notes]                     NVARCHAR(MAX)     NULL,
        [created_at]                DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_asn_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                NVARCHAR(256)     NOT NULL,
        [updated_at]                DATETIMEOFFSET(7) NULL,
        [updated_by]                NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_teaching_assignments] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_assignments_number] UNIQUE NONCLUSTERED ([assignment_number]),
        CONSTRAINT [FK_teaching_asn_demand] FOREIGN KEY ([teaching_demand_id])
            REFERENCES [teaching].[teaching_demands] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_teaching_asn_teacher] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_teaching_asn_role] FOREIGN KEY ([assignment_role_id])
            REFERENCES [teaching].[assignment_roles] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_teaching_asn_predecessor] FOREIGN KEY ([predecessor_assignment_id])
            REFERENCES [teaching].[teaching_assignments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_asn_status] CHECK (
            [status] IN ('proposed', 'approved', 'rejected', 'withdrawn', 'returned_for_revision', 'suspended', 'cancelled', 'completed')
        ),
        CONSTRAINT [CK_teaching_asn_scope] CHECK (
            [assignment_scope] IN ('learning_unit', 'cohort_group', 'individual_student', 'extracurricular_group')
        )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_assignments_teacher_period'
      AND [object_id] = OBJECT_ID(N'teaching.teaching_assignments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_assignments_teacher_period]
        ON [teaching].[teaching_assignments] ([teacher_profile_id], [academic_period_id], [status]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_assignments_scope'
      AND [object_id] = OBJECT_ID(N'teaching.teaching_assignments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_assignments_scope]
        ON [teaching].[teaching_assignments] ([assignment_scope], [scope_reference_id], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 6. Table: teaching.capacity_conflicts
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.capacity_conflicts', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[capacity_conflicts] (
        [id]                      UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_cnf_id] DEFAULT (NEWID()),
        [teaching_assignment_id]  UNIQUEIDENTIFIER  NULL,
        [teaching_demand_id]      UNIQUEIDENTIFIER  NULL,
        [teacher_profile_id]      UNIQUEIDENTIFIER  NOT NULL,
        [conflict_type]           VARCHAR(50)       NOT NULL,
        [conflict_classification] VARCHAR(30)       NOT NULL,
        [severity]                VARCHAR(20)       NOT NULL,
        [description]             NVARCHAR(MAX)     NOT NULL,
        [detected_date]           DATE              NOT NULL,
        [status]                  VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_cnf_status] DEFAULT ('unresolved'),
        [resolution_action]       VARCHAR(50)       NULL,
        [resolved_by]             NVARCHAR(256)     NULL,
        [resolved_date]           DATE              NULL,
        [resolution_notes]        NVARCHAR(MAX)     NULL,
        [created_at]              DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_cnf_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]              NVARCHAR(256)     NOT NULL,
        [updated_at]              DATETIMEOFFSET(7) NULL,
        [updated_by]              NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_capacity_conflicts] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_teaching_cnf_asn] FOREIGN KEY ([teaching_assignment_id])
            REFERENCES [teaching].[teaching_assignments] ([id]) ON DELETE SET NULL,
        CONSTRAINT [FK_teaching_cnf_dem] FOREIGN KEY ([teaching_demand_id])
            REFERENCES [teaching].[teaching_demands] ([id]) ON DELETE SET NULL,
        CONSTRAINT [FK_teaching_cnf_teacher] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE CASCADE,
        CONSTRAINT [CK_teaching_conf_sev] CHECK ([severity] IN ('advisory', 'blocking')),
        CONSTRAINT [CK_teaching_conf_status] CHECK ([status] IN ('unresolved', 'resolved', 'waived_with_exception'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_conflicts_teacher_status'
      AND [object_id] = OBJECT_ID(N'teaching.capacity_conflicts')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_conflicts_teacher_status]
        ON [teaching].[capacity_conflicts] ([teacher_profile_id], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 7. Table: teaching.teacher_availabilities
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teacher_availabilities', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teacher_availabilities] (
        [id]                           UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_tav_id] DEFAULT (NEWID()),
        [teacher_profile_id]           UNIQUEIDENTIFIER  NOT NULL,
        [academic_period_id]           UNIQUEIDENTIFIER  NOT NULL,
        [max_available_hours]          DECIMAL(5,2)      NOT NULL,
        [campus_scope]                 NVARCHAR(100)     NOT NULL CONSTRAINT [DF_teaching_tav_campus] DEFAULT ('All'),
        [available_delivery_modes]     VARCHAR(100)      NOT NULL CONSTRAINT [DF_teaching_tav_modes] DEFAULT ('All'),
        [recurring_available_schedule] NVARCHAR(MAX)     NULL,
        [effective_start_date]         DATE              NOT NULL,
        [effective_end_date]           DATE              NOT NULL,
        [status]                       VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_tav_status] DEFAULT ('active'),
        [notes]                        NVARCHAR(MAX)     NULL,
        [created_at]                   DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_tav_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                   NVARCHAR(256)     NOT NULL,
        [updated_at]                   DATETIMEOFFSET(7) NULL,
        [updated_by]                   NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_teacher_availabilities] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_teaching_tav_teacher] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE CASCADE,
        CONSTRAINT [CK_teaching_tav_status] CHECK ([status] IN ('active', 'revoked', 'superseded'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UQ_teaching_teacher_availabilities_active'
      AND [object_id] = OBJECT_ID(N'teaching.teacher_availabilities')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_teaching_teacher_availabilities_active]
        ON [teaching].[teacher_availabilities] ([teacher_profile_id], [academic_period_id])
        WHERE [status] = 'active';
END;
GO

-- -----------------------------------------------------------------------------
-- 8. Table: teaching.teacher_unavailability_windows
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teacher_unavailability_windows', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teacher_unavailability_windows] (
        [id]                        UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_unav_id] DEFAULT (NEWID()),
        [teacher_profile_id]        UNIQUEIDENTIFIER  NOT NULL,
        [start_date]                DATE              NOT NULL,
        [end_date]                  DATE              NOT NULL,
        [start_time]                TIME              NULL,
        [end_time]                  TIME              NULL,
        [unavailability_type]       VARCHAR(50)       NOT NULL,
        [external_source_reference] VARCHAR(100)      NULL,
        [reason]                    NVARCHAR(MAX)     NOT NULL,
        [status]                    VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_unav_status] DEFAULT ('active'),
        [notes]                     NVARCHAR(MAX)     NULL,
        [created_at]                DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_unav_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                NVARCHAR(256)     NOT NULL,
        [updated_at]                DATETIMEOFFSET(7) NULL,
        [updated_by]                NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_teacher_unavailability_windows] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_teaching_unav_teacher] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE CASCADE,
        CONSTRAINT [CK_teaching_unav_status] CHECK ([status] IN ('active', 'cancelled', 'expired')),
        CONSTRAINT [CK_teaching_unav_type] CHECK (
            [unavailability_type] IN ('medical_leave', 'official_training', 'personal_leave', 'blackout')
        )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_unavailability_teacher_dates'
      AND [object_id] = OBJECT_ID(N'teaching.teacher_unavailability_windows')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_unavailability_teacher_dates]
        ON [teaching].[teacher_unavailability_windows] ([teacher_profile_id], [start_date], [end_date], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 9. Table: teaching.workload_commitments
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.workload_commitments', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[workload_commitments] (
        [id]                       UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_wcom_id] DEFAULT (NEWID()),
        [teacher_profile_id]       UNIQUEIDENTIFIER  NOT NULL,
        [academic_period_id]       UNIQUEIDENTIFIER  NOT NULL,
        [workload_policy_id]       UNIQUEIDENTIFIER  NOT NULL,
        [planned_workload_hours]   DECIMAL(5,2)      NOT NULL CONSTRAINT [DF_teaching_wcom_planned] DEFAULT (0.00),
        [committed_workload_hours] DECIMAL(5,2)      NOT NULL CONSTRAINT [DF_teaching_wcom_committed] DEFAULT (0.00),
        [max_capacity_limit_hours] DECIMAL(5,2)      NOT NULL,
        [remaining_capacity_hours] DECIMAL(5,2)      NOT NULL,
        [status]                   VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_wcom_status] DEFAULT ('normal'),
        [active_assignment_count]  INT               NOT NULL CONSTRAINT [DF_teaching_wcom_actcnt] DEFAULT (0),
        [approved_exception_count] INT               NOT NULL CONSTRAINT [DF_teaching_wcom_exccnt] DEFAULT (0),
        [as_of_timestamp]          DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_wcom_asof] DEFAULT (SYSUTCDATETIME()),
        [created_at]               DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_wcom_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]               NVARCHAR(256)     NOT NULL,
        [updated_at]               DATETIMEOFFSET(7) NULL,
        [updated_by]               NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_workload_commitments] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_workload_commitments_teacher_period] UNIQUE NONCLUSTERED ([teacher_profile_id], [academic_period_id]),
        CONSTRAINT [FK_teaching_wcom_teacher] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_teaching_wcom_policy] FOREIGN KEY ([workload_policy_id])
            REFERENCES [teaching].[workload_policies] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_wcom_status] CHECK (
            [status] IN ('normal', 'under_allocated', 'approaching_limit', 'overloaded_with_exception', 'breached')
        )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_wcom_period_status'
      AND [object_id] = OBJECT_ID(N'teaching.workload_commitments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_wcom_period_status]
        ON [teaching].[workload_commitments] ([academic_period_id], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 10. Table: teaching.workload_exceptions
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.workload_exceptions', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[workload_exceptions] (
        [id]                     UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_wex_id] DEFAULT (NEWID()),
        [exception_number]       VARCHAR(50)       NOT NULL,
        [teaching_assignment_id] UNIQUEIDENTIFIER  NOT NULL,
        [teacher_profile_id]     UNIQUEIDENTIFIER  NOT NULL,
        [workload_policy_id]     UNIQUEIDENTIFIER  NOT NULL,
        [exceeded_hours]         DECIMAL(5,2)      NOT NULL,
        [justification]          NVARCHAR(MAX)     NOT NULL,
        [recommended_by]         NVARCHAR(256)     NOT NULL,
        [recommended_date]       DATE              NOT NULL,
        [approved_by]            NVARCHAR(256)     NULL,
        [approved_date]          DATE              NULL,
        [effective_start_date]   DATE              NOT NULL,
        [effective_end_date]     DATE              NOT NULL,
        [status]                 VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_wex_status] DEFAULT ('requested'),
        [decision_remarks]       NVARCHAR(MAX)     NULL,
        [created_at]             DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_wex_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]             NVARCHAR(256)     NOT NULL,
        [updated_at]             DATETIMEOFFSET(7) NULL,
        [updated_by]             NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_workload_exceptions] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_workload_exceptions_number] UNIQUE NONCLUSTERED ([exception_number]),
        CONSTRAINT [FK_teaching_wex_assignment] FOREIGN KEY ([teaching_assignment_id])
            REFERENCES [teaching].[teaching_assignments] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_teaching_wex_teacher] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_teaching_wex_policy] FOREIGN KEY ([workload_policy_id])
            REFERENCES [teaching].[workload_policies] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_wexc_status] CHECK ([status] IN ('requested', 'recommended', 'approved', 'rejected'))
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 11. Table: teaching.teaching_substitutions
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teaching_substitutions', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teaching_substitutions] (
        [id]                     UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_sub_id] DEFAULT (NEWID()),
        [substitution_number]    VARCHAR(50)       NOT NULL,
        [original_assignment_id] UNIQUEIDENTIFIER  NOT NULL,
        [original_teacher_id]    UNIQUEIDENTIFIER  NOT NULL,
        [substitute_teacher_id]  UNIQUEIDENTIFIER  NOT NULL,
        [coverage_start_date]    DATE              NOT NULL,
        [coverage_end_date]      DATE              NOT NULL,
        [coverage_scope_type]    VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_sub_scope] DEFAULT ('full'),
        [partial_scope_details]  NVARCHAR(MAX)     NULL,
        [reason]                 VARCHAR(50)       NOT NULL,
        [workload_impact_hours]  DECIMAL(5,2)      NOT NULL,
        [status]                 VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_sub_status] DEFAULT ('proposed'),
        [handover_notes]         NVARCHAR(MAX)     NULL,
        [approved_by]            NVARCHAR(256)     NOT NULL,
        [approved_date]          DATE              NOT NULL,
        [created_at]             DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_sub_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]             NVARCHAR(256)     NOT NULL,
        [updated_at]             DATETIMEOFFSET(7) NULL,
        [updated_by]             NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_teaching_substitutions] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_substitutions_number] UNIQUE NONCLUSTERED ([substitution_number]),
        CONSTRAINT [FK_teaching_sub_assignment] FOREIGN KEY ([original_assignment_id])
            REFERENCES [teaching].[teaching_assignments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_teaching_sub_orig_teacher] FOREIGN KEY ([original_teacher_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_teaching_sub_sub_teacher] FOREIGN KEY ([substitute_teacher_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_sub_status] CHECK ([status] IN ('proposed', 'approved', 'active', 'completed', 'cancelled')),
        CONSTRAINT [CK_teaching_sub_scope] CHECK ([coverage_scope_type] IN ('full', 'partial'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_substitutions_assignment'
      AND [object_id] = OBJECT_ID(N'teaching.teaching_substitutions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_substitutions_assignment]
        ON [teaching].[teaching_substitutions] ([original_assignment_id], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 12. Table: teaching.teaching_reassignments
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teaching_reassignments', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teaching_reassignments] (
        [id]                        UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_rea_id] DEFAULT (NEWID()),
        [reassignment_number]       VARCHAR(50)       NOT NULL,
        [original_assignment_id]    UNIQUEIDENTIFIER  NOT NULL,
        [predecessor_teacher_id]    UNIQUEIDENTIFIER  NOT NULL,
        [new_teacher_id]            UNIQUEIDENTIFIER  NOT NULL,
        [replacement_assignment_id] UNIQUEIDENTIFIER  NULL,
        [effective_date]            DATE              NOT NULL,
        [reassignment_type]         VARCHAR(50)       NOT NULL,
        [reason]                    NVARCHAR(MAX)     NOT NULL,
        [requires_handover]         BIT               NOT NULL CONSTRAINT [DF_teaching_rea_req_ho] DEFAULT (1),
        [handover_status]           VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_rea_ho_status] DEFAULT ('pending'),
        [handover_completed_date]   DATE              NULL,
        [handover_notes]            NVARCHAR(MAX)     NULL,
        [status]                    VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_rea_status] DEFAULT ('proposed'),
        [approved_by]               NVARCHAR(256)     NOT NULL,
        [approved_date]             DATE              NOT NULL,
        [created_at]                DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_rea_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                NVARCHAR(256)     NOT NULL,
        [updated_at]                DATETIMEOFFSET(7) NULL,
        [updated_by]                NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_teaching_reassignments] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_reassignments_number] UNIQUE NONCLUSTERED ([reassignment_number]),
        CONSTRAINT [FK_teaching_rea_orig_assignment] FOREIGN KEY ([original_assignment_id])
            REFERENCES [teaching].[teaching_assignments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_teaching_rea_pred_teacher] FOREIGN KEY ([predecessor_teacher_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_teaching_rea_new_teacher] FOREIGN KEY ([new_teacher_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_teaching_rea_repl_assignment] FOREIGN KEY ([replacement_assignment_id])
            REFERENCES [teaching].[teaching_assignments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_rea_status] CHECK ([status] IN ('proposed', 'approved', 'executed', 'cancelled')),
        CONSTRAINT [CK_teaching_rea_handover] CHECK ([handover_status] IN ('pending', 'completed', 'waived'))
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 13. Table: teaching.assignment_completions
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.assignment_completions', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[assignment_completions] (
        [id]                             UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_acmp_id] DEFAULT (NEWID()),
        [teaching_assignment_id]         UNIQUEIDENTIFIER  NOT NULL,
        [completion_date]                DATE              NOT NULL,
        [academic_confirmation_received] BIT               NOT NULL CONSTRAINT [DF_teaching_acmp_acad_conf] DEFAULT (1),
        [academic_confirmation_source]   NVARCHAR(256)     NULL,
        [handover_confirmed]             BIT               NOT NULL CONSTRAINT [DF_teaching_acmp_ho_conf] DEFAULT (1),
        [unresolved_items_status]        VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_acmp_unres] DEFAULT ('none'),
        [unresolved_items_description]   NVARCHAR(MAX)     NULL,
        [capacity_released_date]         DATE              NOT NULL,
        [completed_by]                   NVARCHAR(256)     NOT NULL,
        [status]                         VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_acmp_status] DEFAULT ('completed'),
        [notes]                          NVARCHAR(MAX)     NULL,
        [created_at]                     DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_acmp_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                     NVARCHAR(256)     NOT NULL,
        [updated_at]                     DATETIMEOFFSET(7) NULL,
        [updated_by]                     NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_assignment_completions] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_assignment_completions_asn] UNIQUE NONCLUSTERED ([teaching_assignment_id]),
        CONSTRAINT [FK_teaching_acmp_assignment] FOREIGN KEY ([teaching_assignment_id])
            REFERENCES [teaching].[teaching_assignments] ([id]) ON DELETE CASCADE,
        CONSTRAINT [CK_teaching_acmp_status] CHECK ([status] IN ('completed', 'completed_with_exceptions')),
        CONSTRAINT [CK_teaching_acmp_unres] CHECK ([unresolved_items_status] IN ('none', 'resolved', 'escalated'))
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 14. Table: teaching.teaching_history_entries
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teaching_history_entries', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teaching_history_entries] (
        [id]                 UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_hist_id] DEFAULT (NEWID()),
        [teacher_profile_id] UNIQUEIDENTIFIER  NOT NULL,
        [event_type]         VARCHAR(100)      NOT NULL,
        [entity_type]        VARCHAR(100)      NOT NULL,
        [entity_id]          UNIQUEIDENTIFIER  NOT NULL,
        [effective_date]     DATE              NOT NULL,
        [event_timestamp]    DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_hist_ts] DEFAULT (SYSUTCDATETIME()),
        [actor_role]         VARCHAR(100)      NOT NULL,
        [actor_id]           NVARCHAR(256)     NOT NULL,
        [event_summary]      NVARCHAR(MAX)     NOT NULL,
        [change_payload]     NVARCHAR(MAX)     NULL,
        [notes]              NVARCHAR(MAX)     NULL,
        [created_at]         DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_hist_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]         NVARCHAR(256)     NOT NULL,
        CONSTRAINT [PK_teaching_teaching_history_entries] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_teaching_hist_teacher] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_history_teacher_timestamp'
      AND [object_id] = OBJECT_ID(N'teaching.teaching_history_entries')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_history_teacher_timestamp]
        ON [teaching].[teaching_history_entries] ([teacher_profile_id], [event_timestamp] DESC);
END;
GO
