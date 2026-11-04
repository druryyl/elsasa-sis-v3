-- =============================================================================
-- 20261104_0401_Academics_LearningStructure_Tables.sql
-- Academics domain Stage 2 Learning Structure & Requirements persistence schema
-- (IS008 / CR-ACD-LEARNING-STRUCTURE-REQUIREMENTS, ADR-002, ADR-006, P1-S01).
--
-- 1. Idempotently provisions isolated schema [academics].
-- 2. Provisions 3 authoritative domain tables in schema [academics]:
--    - academics.learning_structures: Framework headers, period linkage, versioning,
--      effective dates, approval record, and status lifecycle.
--    - academics.learning_units: Curriculum-neutral abstraction, self-referencing
--      2-level parent-child hierarchy, credit weights, and scoped unique codes.
--    - academics.academic_requirements: Policy rules for completion, prerequisites,
--      credit quotas, and progression across program, level, and unit scopes.
-- 3. Provisions indexes, unique invariants, self-referencing relationships,
--    and intra-schema foreign key constraints with ON DELETE NO ACTION.
-- 4. Preserves zero physical foreign keys across domain schemas (ADR-001, ADR-002).
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Schema: academics
-- -----------------------------------------------------------------------------
IF SCHEMA_ID(N'academics') IS NULL
BEGIN
    EXEC (N'CREATE SCHEMA [academics] AUTHORIZATION [dbo];');
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Table: academics.learning_structures
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.learning_structures', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[learning_structures] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_learning_structures_id] DEFAULT (NEWID()),
        [name]                  NVARCHAR(200)     NOT NULL,
        [description]           NVARCHAR(MAX)     NULL,
        [academic_period_id]    UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to operations.operational_periods(id)
        [academic_period_label] NVARCHAR(100)     NOT NULL,
        [version]               NVARCHAR(50)      NOT NULL,
        [effective_date]        DATE              NOT NULL,
        [end_date]              DATE              NULL,
        [status]                NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_learning_structures_status] DEFAULT ('draft'),
        [approved_by]           NVARCHAR(256)     NULL,
        [approved_date]         DATE              NULL,
        [supersedes_id]         UNIQUEIDENTIFIER  NULL,
        [notes]                 NVARCHAR(MAX)     NULL,
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_learning_structures_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(256)     NOT NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_learning_structures] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_learning_structures_supersedes] FOREIGN KEY ([supersedes_id])
            REFERENCES [academics].[learning_structures] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_learning_structures_status]
            CHECK ([status] IN ('draft', 'submitted', 'active', 'superseded', 'archived'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_learning_structures_period_status'
      AND [object_id] = OBJECT_ID(N'academics.learning_structures')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_learning_structures_period_status]
        ON [academics].[learning_structures] ([academic_period_id], [status]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_learning_structures_supersedes'
      AND [object_id] = OBJECT_ID(N'academics.learning_structures')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_learning_structures_supersedes]
        ON [academics].[learning_structures] ([supersedes_id])
        WHERE ([supersedes_id] IS NOT NULL);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: academics.learning_units
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.learning_units', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[learning_units] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_learning_units_id] DEFAULT (NEWID()),
        [learning_structure_id] UNIQUEIDENTIFIER  NOT NULL,
        [parent_unit_id]        UNIQUEIDENTIFIER  NULL,
        [code]                  NVARCHAR(50)      NOT NULL,
        [name]                  NVARCHAR(200)     NOT NULL,
        [unit_type]             NVARCHAR(50)      NOT NULL,
        [level]                 NVARCHAR(50)      NULL,
        [description]           NVARCHAR(MAX)     NULL,
        [credit_weight]         DECIMAL(5,2)      NULL,
        [status]                NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_learning_units_status] DEFAULT ('draft'),
        [notes]                 NVARCHAR(MAX)     NULL,
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_learning_units_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(256)     NOT NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_learning_units] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_learning_units_structure] FOREIGN KEY ([learning_structure_id])
            REFERENCES [academics].[learning_structures] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_academics_learning_units_parent] FOREIGN KEY ([parent_unit_id])
            REFERENCES [academics].[learning_units] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [UQ_academics_learning_units_structure_code] UNIQUE NONCLUSTERED ([learning_structure_id], [code]),
        CONSTRAINT [CK_academics_learning_units_unit_type]
            CHECK ([unit_type] IN ('subject', 'course', 'module', 'pace', 'project', 'competency', 'unit', 'other')),
        CONSTRAINT [CK_academics_learning_units_status]
            CHECK ([status] IN ('draft', 'active', 'archived')),
        CONSTRAINT [CK_academics_learning_units_self_parent]
            CHECK ([parent_unit_id] <> [id])
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_learning_units_structure_parent'
      AND [object_id] = OBJECT_ID(N'academics.learning_units')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_learning_units_structure_parent]
        ON [academics].[learning_units] ([learning_structure_id], [parent_unit_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Table: academics.academic_requirements
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.academic_requirements', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[academic_requirements] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_academic_requirements_id] DEFAULT (NEWID()),
        [name]                  NVARCHAR(200)     NOT NULL,
        [requirement_scope]     NVARCHAR(50)      NOT NULL,
        [learning_structure_id] UNIQUEIDENTIFIER  NOT NULL,
        [learning_unit_id]      UNIQUEIDENTIFIER  NULL,
        [level]                 NVARCHAR(50)      NULL,
        [requirement_type]      NVARCHAR(50)      NOT NULL,
        [description]           NVARCHAR(MAX)     NOT NULL,
        [version]               NVARCHAR(50)      NOT NULL,
        [effective_date]        DATE              NOT NULL,
        [end_date]              DATE              NULL,
        [status]                NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_academic_requirements_status] DEFAULT ('draft'),
        [approved_by]           NVARCHAR(256)     NULL,
        [approved_date]         DATE              NULL,
        [supersedes_id]         UNIQUEIDENTIFIER  NULL,
        [notes]                 NVARCHAR(MAX)     NULL,
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_academic_requirements_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(256)     NOT NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_academic_requirements] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_academic_requirements_structure] FOREIGN KEY ([learning_structure_id])
            REFERENCES [academics].[learning_structures] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_academics_academic_requirements_unit] FOREIGN KEY ([learning_unit_id])
            REFERENCES [academics].[learning_units] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_academics_academic_requirements_supersedes] FOREIGN KEY ([supersedes_id])
            REFERENCES [academics].[academic_requirements] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_academic_requirements_scope]
            CHECK ([requirement_scope] IN ('program', 'level', 'learning_unit')),
        CONSTRAINT [CK_academics_academic_requirements_type]
            CHECK ([requirement_type] IN ('completion', 'prerequisite', 'credit', 'progression', 'graduation', 'assessment_policy')),
        CONSTRAINT [CK_academics_academic_requirements_status]
            CHECK ([status] IN ('draft', 'active', 'superseded', 'archived'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_requirements_structure_scope'
      AND [object_id] = OBJECT_ID(N'academics.academic_requirements')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_requirements_structure_scope]
        ON [academics].[academic_requirements] ([learning_structure_id], [requirement_scope], [status]);
END;
GO
