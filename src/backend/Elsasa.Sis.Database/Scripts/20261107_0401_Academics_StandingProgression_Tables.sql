-- =============================================================================
-- 20261107_0401_Academics_StandingProgression_Tables.sql
-- Academics domain Stage 2 Standing & Progression persistence schema
-- (IS012 / CR-ACD-STANDING-PROGRESSION, ADR-001, ADR-002, ADR-006, P1-S01).
--
-- 1. Idempotently ensures isolated schema [academics] exists.
-- 2. Provisions 5 authoritative domain tables in schema [academics]:
--    - academics.learning_progress: Tracks student advancement toward unit learning expectations,
--      minimal progress states (not_started, in_progress, completed, needs_remediation, exempted),
--      operational basis, out-of-period explicit completion_date, and updater metadata.
--    - academics.academic_standings: Formal period academic condition determination
--      (good_standing, conditional_standing, not_in_good_standing), governing policy reference,
--      mandatory condition text, embedded exception attributes (is_exception, exception_reason,
--      committee_reference e.g. SK Dewan Guru), and authorizing Academic Authority.
--    - academics.promotion_recommendations: Advancement evaluation recommendations
--      (promotion_eligible, retention_recommended, conditional_promotion), source and target levels
--      (from_level, recommended_to_level), mandatory condition text, supporting evidence summary,
--      linked academic_standing_id, and embedded exception attributes.
--    - academics.academic_interventions: Structured learning support actions (remediation,
--      reassessment, tutoring_recommendation, extension_work, recovery_plan, learning_plan_adjustment,
--      academic_warning), dual-trigger source (assessment_below_threshold, missing_work,
--      attendance_concern, standing_concern, teacher_referral), lifecycle status, target unit, and outcomes.
--    - academics.academic_achievements: Formal achievements derived from academic learning or
--      assessment (subject_distinction, academic_honors, mastery_recognition, academic_certification,
--      academic_competition), title, optional learning unit link, criteria, and recognizing authority.
-- 3. Provisions primary keys, intra-schema foreign keys (with zero cross-schema physical FKs),
--    composite check constraints, unique composite indexes, and performance indexes.
-- 4. Preserves zero physical foreign keys across domain schemas (ADR-001, ADR-002).
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
-- 1. Table: academics.learning_progress
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.learning_progress', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[learning_progress] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_learning_progress_id] DEFAULT (NEWID()),
        [student_id]            UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [learning_unit_id]      UNIQUEIDENTIFIER  NOT NULL,
        [academic_period_id]    UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to operations.operational_periods(id)
        [status]                NVARCHAR(30)      NOT NULL CONSTRAINT [DF_academics_learning_progress_status] DEFAULT ('not_started'),
        [basis]                 NVARCHAR(30)      NOT NULL,
        [completion_date]       DATE              NULL,
        [notes]                 NVARCHAR(1000)    NULL,
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_learning_progress_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(256)     NOT NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_learning_progress] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_learning_progress_unit] FOREIGN KEY ([learning_unit_id])
            REFERENCES [academics].[learning_units] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_learning_progress_status]
            CHECK ([status] IN ('not_started', 'in_progress', 'completed', 'needs_remediation', 'exempted')),
        CONSTRAINT [CK_academics_learning_progress_basis]
            CHECK ([basis] IN ('assessment_evidence', 'learning_work', 'authorized_decision'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_learning_progress_student_unit_period'
      AND [object_id] = OBJECT_ID(N'academics.learning_progress')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_academics_learning_progress_student_unit_period]
        ON [academics].[learning_progress] ([student_id], [learning_unit_id], [academic_period_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_learning_progress_unit'
      AND [object_id] = OBJECT_ID(N'academics.learning_progress')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_learning_progress_unit]
        ON [academics].[learning_progress] ([learning_unit_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_learning_progress_period'
      AND [object_id] = OBJECT_ID(N'academics.learning_progress')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_learning_progress_period]
        ON [academics].[learning_progress] ([academic_period_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_learning_progress_status'
      AND [object_id] = OBJECT_ID(N'academics.learning_progress')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_learning_progress_status]
        ON [academics].[learning_progress] ([status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Table: academics.academic_standings
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.academic_standings', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[academic_standings] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_academic_standings_id] DEFAULT (NEWID()),
        [student_id]            UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [academic_period_id]    UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to operations.operational_periods(id)
        [standing]              NVARCHAR(30)      NOT NULL,
        [policy_reference]      NVARCHAR(100)     NOT NULL,
        [conditions]            NVARCHAR(2000)    NULL,
        [is_exception]          BIT               NOT NULL CONSTRAINT [DF_academics_academic_standings_is_exception] DEFAULT (0),
        [exception_reason]      NVARCHAR(1000)    NULL,
        [committee_reference]   NVARCHAR(100)     NULL,     -- e.g. SK Dewan Guru decree reference
        [authorized_by]         NVARCHAR(100)     NOT NULL, -- Academic Authority
        [determined_at]         DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_academic_standings_determined_at] DEFAULT (SYSUTCDATETIME()),
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_academic_standings_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(256)     NOT NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_academic_standings] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [CK_academics_academic_standings_standing]
            CHECK ([standing] IN ('good_standing', 'conditional_standing', 'not_in_good_standing'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_standings_student_period'
      AND [object_id] = OBJECT_ID(N'academics.academic_standings')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_academics_academic_standings_student_period]
        ON [academics].[academic_standings] ([student_id], [academic_period_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_standings_period'
      AND [object_id] = OBJECT_ID(N'academics.academic_standings')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_standings_period]
        ON [academics].[academic_standings] ([academic_period_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_standings_standing'
      AND [object_id] = OBJECT_ID(N'academics.academic_standings')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_standings_standing]
        ON [academics].[academic_standings] ([standing]);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: academics.promotion_recommendations
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.promotion_recommendations', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[promotion_recommendations] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_promotion_recommendations_id] DEFAULT (NEWID()),
        [student_id]            UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [academic_period_id]    UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to operations.operational_periods(id)
        [recommendation]        NVARCHAR(30)      NOT NULL,
        [from_level]            NVARCHAR(50)      NOT NULL,
        [recommended_to_level]  NVARCHAR(50)      NULL,
        [conditions]            NVARCHAR(2000)    NULL,
        [supporting_evidence]   NVARCHAR(2000)    NOT NULL,
        [academic_standing_id]  UNIQUEIDENTIFIER  NULL,
        [is_exception]          BIT               NOT NULL CONSTRAINT [DF_academics_promotion_recommendations_is_exception] DEFAULT (0),
        [exception_reason]      NVARCHAR(1000)    NULL,
        [committee_reference]   NVARCHAR(100)     NULL,     -- e.g. SK Dewan Guru decree reference
        [authorized_by]         NVARCHAR(100)     NOT NULL, -- Academic Authority
        [issued_at]             DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_promotion_recommendations_issued_at] DEFAULT (SYSUTCDATETIME()),
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_promotion_recommendations_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(256)     NOT NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_promotion_recommendations] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_promotion_recommendations_standing] FOREIGN KEY ([academic_standing_id])
            REFERENCES [academics].[academic_standings] ([id]) ON DELETE SET NULL,
        CONSTRAINT [CK_academics_promotion_recommendations_recommendation]
            CHECK ([recommendation] IN ('promotion_eligible', 'retention_recommended', 'conditional_promotion'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_promotion_recommendations_student_period'
      AND [object_id] = OBJECT_ID(N'academics.promotion_recommendations')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_academics_promotion_recommendations_student_period]
        ON [academics].[promotion_recommendations] ([student_id], [academic_period_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_promotion_recommendations_standing'
      AND [object_id] = OBJECT_ID(N'academics.promotion_recommendations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_promotion_recommendations_standing]
        ON [academics].[promotion_recommendations] ([academic_standing_id])
        WHERE ([academic_standing_id] IS NOT NULL);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_promotion_recommendations_period'
      AND [object_id] = OBJECT_ID(N'academics.promotion_recommendations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_promotion_recommendations_period]
        ON [academics].[promotion_recommendations] ([academic_period_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_promotion_recommendations_recommendation'
      AND [object_id] = OBJECT_ID(N'academics.promotion_recommendations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_promotion_recommendations_recommendation]
        ON [academics].[promotion_recommendations] ([recommendation]);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Table: academics.academic_interventions
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.academic_interventions', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[academic_interventions] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_academic_interventions_id] DEFAULT (NEWID()),
        [student_id]            UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [learning_unit_id]      UNIQUEIDENTIFIER  NOT NULL,
        [academic_period_id]    UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to operations.operational_periods(id)
        [intervention_type]     NVARCHAR(50)      NOT NULL,
        [trigger_type]          NVARCHAR(50)      NOT NULL,
        [trigger_description]   NVARCHAR(1000)    NOT NULL,
        [plan_description]      NVARCHAR(2000)    NOT NULL,
        [status]                NVARCHAR(30)      NOT NULL CONSTRAINT [DF_academics_academic_interventions_status] DEFAULT ('planned'),
        [initiated_by]          NVARCHAR(100)     NOT NULL,
        [initiated_at]          DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_academic_interventions_initiated_at] DEFAULT (SYSUTCDATETIME()),
        [concluded_at]          DATETIMEOFFSET(7) NULL,
        [outcome_notes]         NVARCHAR(2000)    NULL,
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_academic_interventions_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(256)     NOT NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_academic_interventions] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_academic_interventions_unit] FOREIGN KEY ([learning_unit_id])
            REFERENCES [academics].[learning_units] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_academic_interventions_type]
            CHECK ([intervention_type] IN ('remediation', 'reassessment', 'tutoring_recommendation', 'extension_work', 'recovery_plan', 'learning_plan_adjustment', 'academic_warning')),
        CONSTRAINT [CK_academics_academic_interventions_trigger]
            CHECK ([trigger_type] IN ('assessment_below_threshold', 'missing_work', 'attendance_concern', 'standing_concern', 'teacher_referral')),
        CONSTRAINT [CK_academics_academic_interventions_status]
            CHECK ([status] IN ('planned', 'in_progress', 'completed', 'cancelled'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_interventions_student'
      AND [object_id] = OBJECT_ID(N'academics.academic_interventions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_interventions_student]
        ON [academics].[academic_interventions] ([student_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_interventions_unit'
      AND [object_id] = OBJECT_ID(N'academics.academic_interventions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_interventions_unit]
        ON [academics].[academic_interventions] ([learning_unit_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_interventions_period'
      AND [object_id] = OBJECT_ID(N'academics.academic_interventions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_interventions_period]
        ON [academics].[academic_interventions] ([academic_period_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_interventions_status'
      AND [object_id] = OBJECT_ID(N'academics.academic_interventions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_interventions_status]
        ON [academics].[academic_interventions] ([status]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_interventions_type'
      AND [object_id] = OBJECT_ID(N'academics.academic_interventions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_interventions_type]
        ON [academics].[academic_interventions] ([intervention_type]);
END;
GO

-- -----------------------------------------------------------------------------
-- 5. Table: academics.academic_achievements
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.academic_achievements', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[academic_achievements] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_academic_achievements_id] DEFAULT (NEWID()),
        [student_id]            UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [learning_unit_id]      UNIQUEIDENTIFIER  NULL,
        [academic_period_id]    UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to operations.operational_periods(id)
        [achievement_type]      NVARCHAR(50)      NOT NULL,
        [title]                 NVARCHAR(200)     NOT NULL,
        [description]           NVARCHAR(1000)    NULL,
        [criteria_met]          NVARCHAR(1000)    NOT NULL,
        [supporting_evidence]   NVARCHAR(2000)    NULL,
        [recognized_by]         NVARCHAR(100)     NOT NULL, -- Academic Authority
        [recognized_at]         DATE              NOT NULL,
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_academic_achievements_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(256)     NOT NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_academic_achievements] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_academic_achievements_unit] FOREIGN KEY ([learning_unit_id])
            REFERENCES [academics].[learning_units] ([id]) ON DELETE SET NULL,
        CONSTRAINT [CK_academics_academic_achievements_type]
            CHECK ([achievement_type] IN ('subject_distinction', 'academic_honors', 'mastery_recognition', 'academic_certification', 'academic_competition'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_achievements_student'
      AND [object_id] = OBJECT_ID(N'academics.academic_achievements')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_achievements_student]
        ON [academics].[academic_achievements] ([student_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_achievements_period'
      AND [object_id] = OBJECT_ID(N'academics.academic_achievements')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_achievements_period]
        ON [academics].[academic_achievements] ([academic_period_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_achievements_unit'
      AND [object_id] = OBJECT_ID(N'academics.academic_achievements')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_achievements_unit]
        ON [academics].[academic_achievements] ([learning_unit_id])
        WHERE ([learning_unit_id] IS NOT NULL);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_achievements_type'
      AND [object_id] = OBJECT_ID(N'academics.academic_achievements')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_achievements_type]
        ON [academics].[academic_achievements] ([achievement_type]);
END;
GO
