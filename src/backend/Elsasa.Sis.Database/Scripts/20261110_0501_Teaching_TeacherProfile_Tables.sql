-- =============================================================================
-- 20261110_0501_Teaching_TeacherProfile_Tables.sql
-- Teaching domain Stage 2 Teacher Profile & Authorization persistence schema
-- (CR-TCH-TEACHER-PROFILE-AUTHORIZATION, ADR-001, ADR-002, ADR-006, P1-S01).
--
-- 1. Idempotently provisions isolated schema [teaching].
-- 2. Provisions 10 authoritative domain tables in schema [teaching]:
--    - teaching.teaching_roles: Configurable teaching relationships with check mandates.
--    - teaching.teacher_profiles: Core teaching resource identity, standing lifecycle, and HR decoupling.
--    - teaching.capability_types: Taxonomy categories of teaching capabilities.
--    - teaching.teaching_capabilities: Potential pedagogical capabilities across subject, stage, mode, language.
--    - teaching.qualification_types: Qualification types with renewal and validity policies.
--    - teaching.qualification_evidence: Documentary evidence and verification lifecycle tracking.
--    - teaching.teaching_authorizations: Official teaching permissions within defined scopes and effective dates.
--    - teaching.authorization_evidence: M:N junction linking authorizations to supporting qualification evidence.
--    - teaching.supervision_requirements: Mentoring and supervision conditions for conditional authorizations.
--    - teaching.teaching_record_corrections: Auditable record corrections log (Table 2.15) enforcing No Silent Overwrites.
-- 3. Provisions indexes, unique invariants, and intra-schema foreign key constraints.
-- 4. Preserves zero physical foreign keys across domain schemas (ADR-001, ADR-002).
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Schema: teaching
-- -----------------------------------------------------------------------------
IF SCHEMA_ID(N'teaching') IS NULL
BEGIN
    EXEC (N'CREATE SCHEMA [teaching] AUTHORIZATION [dbo];');
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Table: teaching.teaching_roles
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teaching_roles', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teaching_roles] (
        [id]                          UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_roles_id] DEFAULT (NEWID()),
        [code]                        VARCHAR(50)       NOT NULL,
        [name]                        NVARCHAR(100)     NOT NULL,
        [description]                 NVARCHAR(MAX)     NULL,
        [requires_qualification_check] BIT              NOT NULL CONSTRAINT [DF_teaching_roles_req_qual] DEFAULT (1),
        [is_active]                   BIT               NOT NULL CONSTRAINT [DF_teaching_roles_active] DEFAULT (1),
        [created_at]                  DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_roles_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                  NVARCHAR(256)     NOT NULL,
        [updated_at]                  DATETIMEOFFSET(7) NULL,
        [updated_by]                  NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_roles] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_roles_code] UNIQUE NONCLUSTERED ([code])
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: teaching.teacher_profiles
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teacher_profiles', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teacher_profiles] (
        [id]                          UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_profiles_id] DEFAULT (NEWID()),
        [teacher_number]              VARCHAR(50)       NOT NULL,
        [full_name]                   NVARCHAR(200)     NOT NULL,
        [display_name]                NVARCHAR(200)     NULL,
        [employee_ref]                VARCHAR(100)      NULL,
        [national_id_or_external_ref] VARCHAR(100)      NULL,
        [email]                       NVARCHAR(256)     NULL,
        [phone]                       VARCHAR(50)       NULL,
        [teaching_role_id]            UNIQUEIDENTIFIER  NOT NULL,
        [campus_affiliation]          NVARCHAR(100)     NOT NULL CONSTRAINT [DF_teaching_profiles_campus] DEFAULT ('All'),
        [department]                  NVARCHAR(100)     NULL,
        [standing]                    VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_profiles_standing] DEFAULT ('pending_approval'),
        [effective_from]              DATE              NOT NULL,
        [effective_to]                DATE              NULL,
        [standing_reason]             NVARCHAR(MAX)     NULL,
        [approved_by]                 NVARCHAR(256)     NULL,
        [approved_date]               DATE              NULL,
        [notes]                       NVARCHAR(MAX)     NULL,
        [created_at]                  DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_profiles_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                  NVARCHAR(256)     NOT NULL,
        [updated_at]                  DATETIMEOFFSET(7) NULL,
        [updated_by]                  NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_teacher_profiles] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_teacher_profiles_number] UNIQUE NONCLUSTERED ([teacher_number]),
        CONSTRAINT [FK_teaching_profiles_role] FOREIGN KEY ([teaching_role_id])
            REFERENCES [teaching].[teaching_roles] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_profiles_standing] CHECK ([standing] IN ('pending_approval', 'active', 'restricted', 'suspended', 'inactive'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UQ_teaching_teacher_profiles_national_id'
      AND [object_id] = OBJECT_ID(N'teaching.teacher_profiles')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_teaching_teacher_profiles_national_id]
        ON [teaching].[teacher_profiles] ([national_id_or_external_ref])
        WHERE ([national_id_or_external_ref] IS NOT NULL);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_profiles_standing_campus'
      AND [object_id] = OBJECT_ID(N'teaching.teacher_profiles')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_profiles_standing_campus]
        ON [teaching].[teacher_profiles] ([standing], [campus_affiliation]);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Table: teaching.capability_types
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.capability_types', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[capability_types] (
        [id]          UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_captypes_id] DEFAULT (NEWID()),
        [code]        VARCHAR(50)       NOT NULL,
        [name]        NVARCHAR(100)     NOT NULL,
        [description] NVARCHAR(MAX)     NULL,
        [is_active]   BIT               NOT NULL CONSTRAINT [DF_teaching_captypes_active] DEFAULT (1),
        [created_at]  DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_captypes_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]  NVARCHAR(256)     NOT NULL,
        [updated_at]  DATETIMEOFFSET(7) NULL,
        [updated_by]  NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_capability_types] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_capability_types_code] UNIQUE NONCLUSTERED ([code])
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 5. Table: teaching.teaching_capabilities
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teaching_capabilities', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teaching_capabilities] (
        [id]                      UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_caps_id] DEFAULT (NEWID()),
        [teacher_profile_id]      UNIQUEIDENTIFIER  NOT NULL,
        [capability_type_id]      UNIQUEIDENTIFIER  NOT NULL,
        [capability_code]         VARCHAR(100)      NOT NULL,
        [capability_name]         NVARCHAR(200)     NOT NULL,
        [applicable_level]        NVARCHAR(100)     NULL,
        [delivery_mode]           VARCHAR(50)       NULL,
        [language_of_instruction] VARCHAR(50)       NULL,
        [status]                  VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_caps_status] DEFAULT ('active'),
        [notes]                   NVARCHAR(MAX)     NULL,
        [created_at]              DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_caps_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]              NVARCHAR(256)     NOT NULL,
        [updated_at]              DATETIMEOFFSET(7) NULL,
        [updated_by]              NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_teaching_capabilities] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_teaching_caps_profile] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_teaching_caps_type] FOREIGN KEY ([capability_type_id])
            REFERENCES [teaching].[capability_types] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_caps_status] CHECK ([status] IN ('active', 'inactive')),
        CONSTRAINT [UQ_teaching_capabilities_unique] UNIQUE NONCLUSTERED (
            [teacher_profile_id], [capability_code], [applicable_level], [delivery_mode]
        )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_capabilities_teacher'
      AND [object_id] = OBJECT_ID(N'teaching.teaching_capabilities')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_capabilities_teacher]
        ON [teaching].[teaching_capabilities] ([teacher_profile_id], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 6. Table: teaching.qualification_types
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.qualification_types', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[qualification_types] (
        [id]                      UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_qtypes_id] DEFAULT (NEWID()),
        [code]                    VARCHAR(50)       NOT NULL,
        [name]                    NVARCHAR(150)     NOT NULL,
        [description]             NVARCHAR(MAX)     NULL,
        [requires_renewal]        BIT               NOT NULL,
        [default_validity_months] INT               NULL,
        [is_active]               BIT               NOT NULL CONSTRAINT [DF_teaching_qtypes_active] DEFAULT (1),
        [created_at]              DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_qtypes_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]              NVARCHAR(256)     NOT NULL,
        [updated_at]              DATETIMEOFFSET(7) NULL,
        [updated_by]              NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_qualification_types] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_qualification_types_code] UNIQUE NONCLUSTERED ([code])
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 7. Table: teaching.qualification_evidence
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.qualification_evidence', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[qualification_evidence] (
        [id]                          UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_qev_id] DEFAULT (NEWID()),
        [teacher_profile_id]          UNIQUEIDENTIFIER  NOT NULL,
        [qualification_type_id]      UNIQUEIDENTIFIER  NOT NULL,
        [title]                       NVARCHAR(200)     NOT NULL,
        [issuing_authority]           NVARCHAR(200)     NOT NULL,
        [credential_identifier]       VARCHAR(100)      NULL,
        [issue_date]                  DATE              NOT NULL,
        [expiry_date]                 DATE              NULL,
        [document_reference]          NVARCHAR(500)     NULL,
        [verification_state]          VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_qev_state] DEFAULT ('claimed'),
        [verified_by]                 NVARCHAR(256)     NULL,
        [verified_date]               DATE              NULL,
        [verification_notes]          NVARCHAR(MAX)     NULL,
        [rejection_reason]            NVARCHAR(MAX)     NULL,
        [restrictions_or_conditions]  NVARCHAR(MAX)     NULL,
        [created_at]                  DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_qev_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                  NVARCHAR(256)     NOT NULL,
        [updated_at]                  DATETIMEOFFSET(7) NULL,
        [updated_by]                  NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_qualification_evidence] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_teaching_qev_profile] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_teaching_qev_type] FOREIGN KEY ([qualification_type_id])
            REFERENCES [teaching].[qualification_types] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_qev_state] CHECK (
            [verification_state] IN ('claimed', 'submitted', 'verified', 'rejected', 'expired', 'revoked')
        )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_qualification_evidence_teacher'
      AND [object_id] = OBJECT_ID(N'teaching.qualification_evidence')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_qualification_evidence_teacher]
        ON [teaching].[qualification_evidence] ([teacher_profile_id], [verification_state]);
END;
GO

-- -----------------------------------------------------------------------------
-- 8. Table: teaching.teaching_authorizations
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teaching_authorizations', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teaching_authorizations] (
        [id]                             UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_auth_id] DEFAULT (NEWID()),
        [authorization_number]           VARCHAR(50)       NOT NULL,
        [teacher_profile_id]             UNIQUEIDENTIFIER  NOT NULL,
        [authorized_capability_id]       UNIQUEIDENTIFIER  NULL,
        [scope_description]              NVARCHAR(MAX)     NOT NULL,
        [campus_scope]                   NVARCHAR(100)     NOT NULL CONSTRAINT [DF_teaching_auth_campus] DEFAULT ('All'),
        [program_scope]                  NVARCHAR(100)     NULL,
        [level_scope]                    NVARCHAR(100)     NULL,
        [delivery_mode_scope]            VARCHAR(50)       NULL,
        [status]                         VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_auth_status] DEFAULT ('active'),
        [unverified_credential_warning] BIT               NOT NULL CONSTRAINT [DF_teaching_auth_unverified_warn] DEFAULT (0),
        [effective_start_date]           DATE              NOT NULL,
        [effective_end_date]             DATE              NULL,
        [conditions_or_restrictions]     NVARCHAR(MAX)     NULL,
        [decision_authority]             NVARCHAR(256)     NOT NULL,
        [approved_date]                  DATE              NOT NULL,
        [suspension_or_revocation_reason] NVARCHAR(MAX)    NULL,
        [notes]                          NVARCHAR(MAX)     NULL,
        [created_at]                     DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_auth_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                     NVARCHAR(256)     NOT NULL,
        [updated_at]                     DATETIMEOFFSET(7) NULL,
        [updated_by]                     NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_teaching_authorizations] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_authorizations_number] UNIQUE NONCLUSTERED ([authorization_number]),
        CONSTRAINT [FK_teaching_auth_profile] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_teaching_auth_capability] FOREIGN KEY ([authorized_capability_id])
            REFERENCES [teaching].[teaching_capabilities] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_auth_status] CHECK (
            [status] IN ('active', 'authorized_under_supervision', 'restricted', 'suspended', 'revoked', 'expired')
        )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_authorizations_teacher'
      AND [object_id] = OBJECT_ID(N'teaching.teaching_authorizations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_authorizations_teacher]
        ON [teaching].[teaching_authorizations] ([teacher_profile_id], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 9. Table: teaching.authorization_evidence
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.authorization_evidence', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[authorization_evidence] (
        [authorization_id]          UNIQUEIDENTIFIER  NOT NULL,
        [qualification_evidence_id] UNIQUEIDENTIFIER  NOT NULL,
        [notes]                     NVARCHAR(500)     NULL,
        [created_at]                DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_authev_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                NVARCHAR(256)     NOT NULL,
        CONSTRAINT [PK_teaching_authorization_evidence] PRIMARY KEY CLUSTERED ([authorization_id], [qualification_evidence_id]),
        CONSTRAINT [FK_teaching_authev_auth] FOREIGN KEY ([authorization_id])
            REFERENCES [teaching].[teaching_authorizations] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_teaching_authev_evidence] FOREIGN KEY ([qualification_evidence_id])
            REFERENCES [teaching].[qualification_evidence] ([id]) ON DELETE NO ACTION
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 10. Table: teaching.supervision_requirements
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.supervision_requirements', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[supervision_requirements] (
        [id]                     UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_sup_id] DEFAULT (NEWID()),
        [authorization_id]       UNIQUEIDENTIFIER  NOT NULL,
        [supervising_teacher_id] UNIQUEIDENTIFIER  NULL,
        [supervision_type]       VARCHAR(50)       NOT NULL,
        [description]            NVARCHAR(MAX)     NOT NULL,
        [start_date]             DATE              NOT NULL,
        [end_date]               DATE              NULL,
        [status]                 VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_sup_status] DEFAULT ('active'),
        [resolution_notes]       NVARCHAR(MAX)     NULL,
        [created_at]             DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_sup_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]             NVARCHAR(256)     NOT NULL,
        [updated_at]             DATETIMEOFFSET(7) NULL,
        [updated_by]             NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_supervision_requirements] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_teaching_sup_auth] FOREIGN KEY ([authorization_id])
            REFERENCES [teaching].[teaching_authorizations] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_teaching_sup_mentor] FOREIGN KEY ([supervising_teacher_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_teaching_sup_type] CHECK (
            [supervision_type] IN ('co_teaching', 'periodic_observation', 'lesson_plan_review', 'general_mentorship')
        ),
        CONSTRAINT [CK_teaching_sup_status] CHECK (
            [status] IN ('active', 'completed', 'waived', 'escalated')
        )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_supervision_auth'
      AND [object_id] = OBJECT_ID(N'teaching.supervision_requirements')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_supervision_auth]
        ON [teaching].[supervision_requirements] ([authorization_id], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 11. Table: teaching.teaching_record_corrections
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'teaching.teaching_record_corrections', N'U') IS NULL
BEGIN
    CREATE TABLE [teaching].[teaching_record_corrections] (
        [id]                          UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_teaching_trc_id] DEFAULT (NEWID()),
        [correction_number]           VARCHAR(50)       NOT NULL,
        [target_table_name]           VARCHAR(100)      NOT NULL,
        [target_record_id]            UNIQUEIDENTIFIER  NOT NULL,
        [teacher_profile_id]          UNIQUEIDENTIFIER  NULL,
        [field_name]                  VARCHAR(100)      NOT NULL,
        [original_value]              NVARCHAR(MAX)     NOT NULL,
        [corrected_value]             NVARCHAR(MAX)     NOT NULL,
        [reason_for_correction]       NVARCHAR(MAX)     NOT NULL,
        [supporting_evidence]         NVARCHAR(MAX)     NULL,
        [downstream_impact_reviewed]  BIT               NOT NULL CONSTRAINT [DF_teaching_trc_impact] DEFAULT (1),
        [effective_date_implications] NVARCHAR(MAX)     NULL,
        [requested_by]                NVARCHAR(256)     NOT NULL,
        [requested_date]              DATE              NOT NULL,
        [approved_by]                 NVARCHAR(256)     NULL,
        [approved_date]               DATE              NULL,
        [status]                      VARCHAR(30)       NOT NULL CONSTRAINT [DF_teaching_trc_status] DEFAULT ('requested'),
        [created_at]                  DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_teaching_trc_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                  NVARCHAR(256)     NOT NULL,
        [updated_at]                  DATETIMEOFFSET(7) NULL,
        [updated_by]                  NVARCHAR(256)     NULL,
        CONSTRAINT [PK_teaching_teaching_record_corrections] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_teaching_record_corrections_number] UNIQUE NONCLUSTERED ([correction_number]),
        CONSTRAINT [FK_teaching_trc_teacher] FOREIGN KEY ([teacher_profile_id])
            REFERENCES [teaching].[teacher_profiles] ([id]) ON DELETE SET NULL,
        CONSTRAINT [CK_teaching_trc_status] CHECK (
            [status] IN ('requested', 'approved', 'rejected', 'applied')
        )
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_teaching_record_corrections_target'
      AND [object_id] = OBJECT_ID(N'teaching.teaching_record_corrections')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_teaching_record_corrections_target]
        ON [teaching].[teaching_record_corrections] ([target_table_name], [target_record_id]);
END;
GO
