-- =============================================================================
-- 20261107_0402_Academics_StudentLearningPlan_Tables.sql
-- Academics domain Stage 2 Student Learning Plan persistence schema
-- (IS013 / CR-ACD-STUDENT-LEARNING-PLAN, ADR-001, ADR-002, ADR-006, P1-S01).
--
-- 1. Idempotently ensures isolated schema [academics] exists.
-- 2. Provisions 4 authoritative domain tables in schema [academics]:
--    - academics.student_learning_plans: Core plan record tracking student, academic period,
--      status (draft, active, revised, completed, archived), linear version, notes, and audit timestamps.
--    - academics.slp_learning_units: Supporting table linking constituent learning units with
--      sequence ordering, required flag, and adaptation/level notes.
--    - academics.academic_plan_adjustments: Core table recording accommodations, substitutions,
--      and exemptions with description, rationale, effective dates, and approving authority.
--    - academics.slp_approvals: Supporting audit table recording immutable initial approval and
--      revision approval decisions.
-- 3. Provisions SQL Server partial unique index [UQ_academics_slp_active] guaranteeing at most
--    one active plan per student per academic period.
-- 4. Provisions intra-schema foreign key [FK_academics_academic_participations_slp] from
--    academics.academic_participations(student_learning_plan_id) to academics.student_learning_plans(id)
--    with ON DELETE SET NULL.
-- 5. Preserves zero physical foreign keys across domain schemas (ADR-001, ADR-002).
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 0. Schema: academics
-- -----------------------------------------------------------------------------
IF SCHEMA_ID(N'academics') IS NULL
BEGIN
    EXEC (N'CREATE SCHEMA [academics] AUTHORIZATION [dbo];');
END;
GO

-- -----------------------------------------------------------------------------
-- 1. Table: academics.student_learning_plans
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.student_learning_plans', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[student_learning_plans] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_slp_id] DEFAULT (NEWID()),
        [student_id]            UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [academic_period_id]    UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to operations.operational_periods(id)
        [academic_period_label] NVARCHAR(100)     NOT NULL,
        [status]                NVARCHAR(30)      NOT NULL CONSTRAINT [DF_academics_slp_status] DEFAULT ('draft'),
        [version]               INT               NOT NULL CONSTRAINT [DF_academics_slp_version] DEFAULT (1),
        [notes]                 NVARCHAR(MAX)     NULL,
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_slp_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(256)     NOT NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_student_learning_plans] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [CK_academics_slp_status] CHECK ([status] IN ('draft', 'active', 'revised', 'completed', 'archived')),
        CONSTRAINT [CK_academics_slp_version] CHECK ([version] >= 1)
    );
END;
GO

-- Concurrency Invariant: At most ONE active SLP per student per academic period
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UQ_academics_slp_active'
      AND [object_id] = OBJECT_ID(N'academics.student_learning_plans')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_academics_slp_active]
        ON [academics].[student_learning_plans] ([student_id], [academic_period_id])
        WHERE [status] = 'active';
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_slp_student_period'
      AND [object_id] = OBJECT_ID(N'academics.student_learning_plans')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_slp_student_period]
        ON [academics].[student_learning_plans] ([student_id], [academic_period_id], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Table: academics.slp_learning_units
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.slp_learning_units', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[slp_learning_units] (
        [id]                       UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_slp_units_id] DEFAULT (NEWID()),
        [student_learning_plan_id] UNIQUEIDENTIFIER  NOT NULL,
        [learning_unit_id]         UNIQUEIDENTIFIER  NOT NULL,
        [sequence_order]           INT               NULL,
        [is_required]              BIT               NOT NULL CONSTRAINT [DF_academics_slp_units_required] DEFAULT (1),
        [notes]                    NVARCHAR(MAX)     NULL,
        [created_at]               DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_slp_units_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]               NVARCHAR(256)     NOT NULL,
        CONSTRAINT [PK_academics_slp_learning_units] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_slp_units_plan] FOREIGN KEY ([student_learning_plan_id])
            REFERENCES [academics].[student_learning_plans] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_academics_slp_units_unit] FOREIGN KEY ([learning_unit_id])
            REFERENCES [academics].[learning_units] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [UQ_academics_slp_units_plan_unit] UNIQUE NONCLUSTERED ([student_learning_plan_id], [learning_unit_id])
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_slp_units_learning_unit'
      AND [object_id] = OBJECT_ID(N'academics.slp_learning_units')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_slp_units_learning_unit]
        ON [academics].[slp_learning_units] ([learning_unit_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: academics.academic_plan_adjustments
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.academic_plan_adjustments', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[academic_plan_adjustments] (
        [id]                       UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_adjustments_id] DEFAULT (NEWID()),
        [student_learning_plan_id] UNIQUEIDENTIFIER  NOT NULL,
        [adjustment_type]          NVARCHAR(30)      NOT NULL,
        [description]              NVARCHAR(MAX)     NOT NULL,
        [rationale]                NVARCHAR(MAX)     NOT NULL,
        [effective_date]           DATE              NOT NULL,
        [end_date]                 DATE              NULL,
        [approved_by]              NVARCHAR(256)     NOT NULL,
        [approved_date]            DATE              NOT NULL,
        [notes]                    NVARCHAR(MAX)     NULL,
        [created_at]               DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_adjustments_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]               NVARCHAR(256)     NOT NULL,
        [updated_at]               DATETIMEOFFSET(7) NULL,
        [updated_by]               NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_academic_plan_adjustments] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_adjustments_plan] FOREIGN KEY ([student_learning_plan_id])
            REFERENCES [academics].[student_learning_plans] ([id]) ON DELETE CASCADE,
        CONSTRAINT [CK_academics_adjustments_type] CHECK ([adjustment_type] IN ('accommodation', 'substitution', 'exemption'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_adjustments_plan'
      AND [object_id] = OBJECT_ID(N'academics.academic_plan_adjustments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_adjustments_plan]
        ON [academics].[academic_plan_adjustments] ([student_learning_plan_id], [adjustment_type]);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Table: academics.slp_approvals
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.slp_approvals', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[slp_approvals] (
        [id]                       UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_slp_approvals_id] DEFAULT (NEWID()),
        [student_learning_plan_id] UNIQUEIDENTIFIER  NOT NULL,
        [plan_version]             INT               NOT NULL,
        [approved_by]              NVARCHAR(256)     NOT NULL,
        [approved_date]            DATE              NOT NULL,
        [approval_type]            NVARCHAR(30)      NOT NULL,
        [notes]                    NVARCHAR(MAX)     NULL,
        [created_at]               DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_slp_approvals_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]               NVARCHAR(256)     NOT NULL,
        CONSTRAINT [PK_academics_slp_approvals] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_slp_approvals_plan] FOREIGN KEY ([student_learning_plan_id])
            REFERENCES [academics].[student_learning_plans] ([id]) ON DELETE CASCADE,
        CONSTRAINT [CK_academics_slp_approvals_type] CHECK ([approval_type] IN ('initial_approval', 'revision_approval')),
        CONSTRAINT [CK_academics_slp_approvals_version] CHECK ([plan_version] >= 1)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_slp_approvals_plan'
      AND [object_id] = OBJECT_ID(N'academics.slp_approvals')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_slp_approvals_plan]
        ON [academics].[slp_approvals] ([student_learning_plan_id], [plan_version]);
END;
GO

-- -----------------------------------------------------------------------------
-- 5. Foreign Key: academics.academic_participations -> student_learning_plans
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.academic_participations', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE [name] = N'FK_academics_academic_participations_slp'
          AND [parent_object_id] = OBJECT_ID(N'academics.academic_participations')
    )
    BEGIN
        ALTER TABLE [academics].[academic_participations]
        ADD CONSTRAINT [FK_academics_academic_participations_slp]
            FOREIGN KEY ([student_learning_plan_id])
            REFERENCES [academics].[student_learning_plans] ([id])
            ON DELETE SET NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_academics_participations_slp'
          AND [object_id] = OBJECT_ID(N'academics.academic_participations')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_academics_participations_slp]
            ON [academics].[academic_participations] ([student_learning_plan_id])
            WHERE ([student_learning_plan_id] IS NOT NULL);
    END;
END;
GO
