-- =============================================================================
-- 20261105_0401_Academics_ParticipationActivities_Tables.sql
-- Academics domain Stage 2 Participation & Learning Activities persistence schema
-- (IS009 / CR-ACD-PARTICIPATION-LEARNING-ACTIVITIES, ADR-002, ADR-006, P1-S01).
--
-- 1. Idempotently ensures isolated schema [academics] exists.
-- 2. Provisions 6 authoritative domain tables in schema [academics]:
--    - academics.academic_participations: Student-learning unit learning relationship,
--      attempt counters, lifecycle status, cohort section label, prior learning recognition.
--    - academics.learning_activities: Scheduled learning occurrences, activity types,
--      provisional teacher display context, homeroom sessions, and delivery notes.
--    - academics.attendance_records: Student session attendance records, statuses,
--      recorder identity, and evidence.
--    - academics.attendance_corrections: Immutable audit log for historical attendance
--      modifications with mandatory reason and authorizer identity.
--    - academics.assignments: Coursework tasks strictly originating from learning
--      activities, release dates, due dates, and lifecycle statuses.
--    - academics.submissions: Student submission responses, delivery timestamps,
--      hybrid references, resubmission lineage, and exception statuses.
-- 3. Provisions indexes, unique invariants, self-referencing relationships,
--    and intra-schema foreign key constraints with ON DELETE NO ACTION.
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
-- 1. Table: academics.academic_participations
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.academic_participations', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[academic_participations] (
        [id]                        UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_academic_participations_id] DEFAULT (NEWID()),
        [student_id]                UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [learning_unit_id]          UNIQUEIDENTIFIER  NOT NULL,
        [academic_period_id]        UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to operations.operational_periods(id)
        [academic_period_label]     NVARCHAR(100)     NOT NULL,
        [participation_type]        NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_academic_participations_type] DEFAULT ('standard'),
        [attempt_number]            INT               NOT NULL CONSTRAINT [DF_academics_academic_participations_attempt] DEFAULT (1),
        [source]                    NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_academic_participations_source] DEFAULT ('assigned'),
        [section_label]             NVARCHAR(50)      NULL,     -- Cohort partition (e.g. "4A")
        [status]                    NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_academic_participations_status] DEFAULT ('in_progress'),
        [start_date]                DATE              NOT NULL,
        [end_date]                  DATE              NULL,
        [completion_date]           DATE              NULL,
        [source_institution]        NVARCHAR(200)     NULL,     -- For transfer/prior credit
        [external_grade_or_credit]  NVARCHAR(100)     NULL,
        [recognition_basis]         NVARCHAR(MAX)     NULL,
        [approved_by]               NVARCHAR(256)     NULL,     -- Academic Authority
        [student_learning_plan_id]  UNIQUEIDENTIFIER  NULL,     -- Optional reference to SLP
        [notes]                     NVARCHAR(MAX)     NULL,
        [created_at]                DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_academic_participations_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                NVARCHAR(256)     NOT NULL,
        [updated_at]                DATETIMEOFFSET(7) NULL,
        [updated_by]                NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_academic_participations] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_academic_participations_unit] FOREIGN KEY ([learning_unit_id])
            REFERENCES [academics].[learning_units] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_academic_participations_type]
            CHECK ([participation_type] IN ('standard', 'repeat', 'transferred_in', 'recognized_prior')),
        CONSTRAINT [CK_academics_academic_participations_source]
            CHECK ([source] IN ('assigned', 'student_selected', 'learning_plan')),
        CONSTRAINT [CK_academics_academic_participations_status]
            CHECK ([status] IN ('in_progress', 'paused', 'withdrawn', 'completed'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UQ_academics_participations_active'
      AND [object_id] = OBJECT_ID(N'academics.academic_participations')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_academics_participations_active]
        ON [academics].[academic_participations] ([student_id], [learning_unit_id], [academic_period_id])
        WHERE ([status] = 'in_progress');
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_participations_unit_period'
      AND [object_id] = OBJECT_ID(N'academics.academic_participations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_participations_unit_period]
        ON [academics].[academic_participations] ([learning_unit_id], [academic_period_id], [section_label]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_participations_student_period'
      AND [object_id] = OBJECT_ID(N'academics.academic_participations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_participations_student_period]
        ON [academics].[academic_participations] ([student_id], [academic_period_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Table: academics.learning_activities
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.learning_activities', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[learning_activities] (
        [id]                        UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_learning_activities_id] DEFAULT (NEWID()),
        [learning_unit_id]          UNIQUEIDENTIFIER  NOT NULL,
        [activity_type]             NVARCHAR(50)      NOT NULL,
        [title]                     NVARCHAR(200)     NOT NULL,
        [description]               NVARCHAR(MAX)     NULL,
        [section_label]             NVARCHAR(50)      NULL,     -- Cohort partition (e.g. "4A")
        [activity_date]             DATE              NOT NULL,
        [start_time]                TIME(0)           NULL,
        [end_time]                  TIME(0)           NULL,
        [teacher_assignment_id]     UNIQUEIDENTIFIER  NULL,     -- Provisional logical reference to Teaching
        [teacher_name]              NVARCHAR(200)     NULL,     -- Display context for concurrent rollout
        [status]                    NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_learning_activities_status] DEFAULT ('planned'),
        [is_attendance_required]    BIT               NOT NULL CONSTRAINT [DF_academics_learning_activities_att_req] DEFAULT (1),
        [delivery_notes]            NVARCHAR(MAX)     NULL,     -- Curriculum milestones / notes
        [notes]                     NVARCHAR(MAX)     NULL,
        [created_at]                DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_learning_activities_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                NVARCHAR(256)     NOT NULL,
        [updated_at]                DATETIMEOFFSET(7) NULL,
        [updated_by]                NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_learning_activities] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_learning_activities_unit] FOREIGN KEY ([learning_unit_id])
            REFERENCES [academics].[learning_units] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_learning_activities_type]
            CHECK ([activity_type] IN ('lesson', 'tutorial', 'laboratory', 'independent_work', 'project', 'discussion', 'practice', 'examination', 'remediation', 'homeroom_session', 'other')),
        CONSTRAINT [CK_academics_learning_activities_status]
            CHECK ([status] IN ('planned', 'conducted', 'cancelled'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_learning_activities_unit_date'
      AND [object_id] = OBJECT_ID(N'academics.learning_activities')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_learning_activities_unit_date]
        ON [academics].[learning_activities] ([learning_unit_id], [activity_date]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_learning_activities_date_status'
      AND [object_id] = OBJECT_ID(N'academics.learning_activities')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_learning_activities_date_status]
        ON [academics].[learning_activities] ([activity_date], [status]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_learning_activities_section_date'
      AND [object_id] = OBJECT_ID(N'academics.learning_activities')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_learning_activities_section_date]
        ON [academics].[learning_activities] ([section_label], [activity_date]);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: academics.attendance_records
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.attendance_records', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[attendance_records] (
        [id]                        UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_attendance_records_id] DEFAULT (NEWID()),
        [learning_activity_id]      UNIQUEIDENTIFIER  NOT NULL,
        [student_id]                UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [status]                    NVARCHAR(50)      NOT NULL,
        [recorded_by]               NVARCHAR(256)     NOT NULL,
        [recorded_date]             DATE              NOT NULL,
        [evidence]                  NVARCHAR(MAX)     NULL,
        [notes]                     NVARCHAR(MAX)     NULL,
        [created_at]                DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_attendance_records_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                NVARCHAR(256)     NOT NULL,
        [updated_at]                DATETIMEOFFSET(7) NULL,
        [updated_by]                NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_attendance_records] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_attendance_records_activity] FOREIGN KEY ([learning_activity_id])
            REFERENCES [academics].[learning_activities] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [UQ_academics_attendance_records_activity_student] UNIQUE ([learning_activity_id], [student_id]),
        CONSTRAINT [CK_academics_attendance_records_status]
            CHECK ([status] IN ('present', 'absent', 'late', 'excused_absence', 'partial'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_attendance_records_student_date'
      AND [object_id] = OBJECT_ID(N'academics.attendance_records')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_attendance_records_student_date]
        ON [academics].[attendance_records] ([student_id], [recorded_date]);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Table: academics.attendance_corrections
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.attendance_corrections', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[attendance_corrections] (
        [id]                        UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_attendance_corrections_id] DEFAULT (NEWID()),
        [attendance_record_id]      UNIQUEIDENTIFIER  NOT NULL,
        [previous_status]           NVARCHAR(50)      NOT NULL,
        [new_status]                NVARCHAR(50)      NOT NULL,
        [reason]                    NVARCHAR(MAX)     NOT NULL,
        [corrected_by]              NVARCHAR(256)     NOT NULL,
        [corrected_date]            DATE              NOT NULL,
        [evidence]                  NVARCHAR(MAX)     NULL,
        [created_at]                DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_attendance_corrections_created_at] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_academics_attendance_corrections] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_attendance_corrections_record] FOREIGN KEY ([attendance_record_id])
            REFERENCES [academics].[attendance_records] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_attendance_corrections_status]
            CHECK (LEN(LTRIM(RTRIM([previous_status]))) > 0 AND LEN(LTRIM(RTRIM([new_status]))) > 0)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_attendance_corrections_record'
      AND [object_id] = OBJECT_ID(N'academics.attendance_corrections')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_attendance_corrections_record]
        ON [academics].[attendance_corrections] ([attendance_record_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 5. Table: academics.assignments
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.assignments', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[assignments] (
        [id]                        UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_assignments_id] DEFAULT (NEWID()),
        [learning_activity_id]      UNIQUEIDENTIFIER  NOT NULL,
        [title]                     NVARCHAR(200)     NOT NULL,
        [description]               NVARCHAR(MAX)     NULL,
        [release_date]              DATE              NOT NULL,
        [due_date]                  DATE              NULL,
        [status]                    NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_assignments_status] DEFAULT ('draft'),
        [notes]                     NVARCHAR(MAX)     NULL,
        [created_at]                DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_assignments_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                NVARCHAR(256)     NOT NULL,
        [updated_at]                DATETIMEOFFSET(7) NULL,
        [updated_by]                NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_assignments] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_assignments_activity] FOREIGN KEY ([learning_activity_id])
            REFERENCES [academics].[learning_activities] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_assignments_status]
            CHECK ([status] IN ('draft', 'released', 'closed'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_assignments_activity'
      AND [object_id] = OBJECT_ID(N'academics.assignments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_assignments_activity]
        ON [academics].[assignments] ([learning_activity_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_assignments_release_status'
      AND [object_id] = OBJECT_ID(N'academics.assignments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_assignments_release_status]
        ON [academics].[assignments] ([release_date], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 6. Table: academics.submissions
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.submissions', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[submissions] (
        [id]                        UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_submissions_id] DEFAULT (NEWID()),
        [assignment_id]             UNIQUEIDENTIFIER  NOT NULL,
        [student_id]                UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [status]                    NVARCHAR(50)      NOT NULL,
        [submitted_date]            DATETIMEOFFSET(7) NULL,
        [submission_reference]      NVARCHAR(1000)    NULL,     -- URL, plain text, or local upload path
        [is_resubmission]           BIT               NOT NULL CONSTRAINT [DF_academics_submissions_is_resub] DEFAULT (0),
        [previous_submission_id]    UNIQUEIDENTIFIER  NULL,
        [notes]                     NVARCHAR(MAX)     NULL,
        [created_at]                DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_submissions_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]                NVARCHAR(256)     NOT NULL,
        [updated_at]                DATETIMEOFFSET(7) NULL,
        [updated_by]                NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_submissions] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_submissions_assignment] FOREIGN KEY ([assignment_id])
            REFERENCES [academics].[assignments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_academics_submissions_previous] FOREIGN KEY ([previous_submission_id])
            REFERENCES [academics].[submissions] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_submissions_status]
            CHECK ([status] IN ('submitted', 'late', 'missing', 'excused', 'exempted'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_submissions_assignment_student'
      AND [object_id] = OBJECT_ID(N'academics.submissions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_submissions_assignment_student]
        ON [academics].[submissions] ([assignment_id], [student_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_submissions_student_status'
      AND [object_id] = OBJECT_ID(N'academics.submissions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_submissions_student_status]
        ON [academics].[submissions] ([student_id], [status]);
END;
GO
