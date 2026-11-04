-- =============================================================================
-- 20261104_0101_Students_PlacementStatus_Tables.sql
-- Students domain Stage 1 Placement & Status persistence evolution (IS005 / CR-STU-PLACEMENT-STATUS).
-- 1. Provisions domain tables in students schema:
--    - students.administrative_placements (Historical and active placement periods)
--    - students.student_statuses (Authoritative operational status history)
--    - students.administrative_history_entries (Immutable chronological lifecycle audit trail)
-- 2. Provisions partial unique indexes enforcing single active invariants:
--    - ux_administrative_placements_student_active (WHERE is_current = 1)
--    - ux_student_statuses_student_active (WHERE is_current = 1)
-- 3. Provisions query performance indexes for student timelines.
-- 4. Executes idempotent historical data backfill from enrollments and students.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Table: students.administrative_placements
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.administrative_placements', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[administrative_placements] (
        [id]                 UNIQUEIDENTIFIER  NOT NULL,
        [student_id]         UNIQUEIDENTIFIER  NOT NULL,
        [campus]             NVARCHAR(100)     NOT NULL,
        [school_level]       NVARCHAR(64)      NOT NULL,
        [grade]              NVARCHAR(64)      NOT NULL,
        [academic_year]      NVARCHAR(32)      NOT NULL,
        [effective_date]     DATE              NOT NULL,
        [program]            NVARCHAR(100)     NULL,
        [stream]             NVARCHAR(100)     NULL,
        [cohort]             NVARCHAR(50)      NULL,
        [homeroom]           NVARCHAR(100)     NULL,
        [end_date]           DATE              NULL,
        [is_current]         BIT               NOT NULL CONSTRAINT [df_students_placements_is_current] DEFAULT (1),
        [reason]             NVARCHAR(500)     NULL,
        [authorized_by]      NVARCHAR(100)     NULL,
        [decision_reference] NVARCHAR(100)     NULL,
        [created_at]         DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_students_placements_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]         NVARCHAR(100)     NULL,
        [updated_at]         DATETIMEOFFSET(7) NULL,
        [updated_by]         NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_administrative_placements] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [fk_students_placements_student] FOREIGN KEY ([student_id])
            REFERENCES [students].[students] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_administrative_placements_student_active'
      AND [object_id] = OBJECT_ID(N'students.administrative_placements')
)
BEGIN
    CREATE UNIQUE INDEX [ux_administrative_placements_student_active]
        ON [students].[administrative_placements] ([student_id])
        WHERE ([is_current] = 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_placements_student'
      AND [object_id] = OBJECT_ID(N'students.administrative_placements')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_students_placements_student]
        ON [students].[administrative_placements] ([student_id], [effective_date] DESC);
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Table: students.student_statuses
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.student_statuses', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[student_statuses] (
        [id]                 UNIQUEIDENTIFIER  NOT NULL,
        [student_id]         UNIQUEIDENTIFIER  NOT NULL,
        [status]             NVARCHAR(32)      NOT NULL,
        [effective_date]     DATE              NOT NULL,
        [end_date]           DATE              NULL,
        [is_current]         BIT               NOT NULL CONSTRAINT [df_students_statuses_is_current] DEFAULT (1),
        [reason]             NVARCHAR(MAX)     NULL,
        [authorized_by]      NVARCHAR(100)     NULL,
        [decision_reference] NVARCHAR(100)     NULL,
        [created_at]         DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_students_statuses_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]         NVARCHAR(100)     NULL,
        [updated_at]         DATETIMEOFFSET(7) NULL,
        [updated_by]         NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_student_statuses] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [fk_students_statuses_student] FOREIGN KEY ([student_id])
            REFERENCES [students].[students] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_student_statuses_student_active'
      AND [object_id] = OBJECT_ID(N'students.student_statuses')
)
BEGIN
    CREATE UNIQUE INDEX [ux_student_statuses_student_active]
        ON [students].[student_statuses] ([student_id])
        WHERE ([is_current] = 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_statuses_student'
      AND [object_id] = OBJECT_ID(N'students.student_statuses')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_students_statuses_student]
        ON [students].[student_statuses] ([student_id], [effective_date] DESC);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: students.administrative_history_entries
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.administrative_history_entries', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[administrative_history_entries] (
        [id]                 UNIQUEIDENTIFIER  NOT NULL,
        [student_id]         UNIQUEIDENTIFIER  NOT NULL,
        [event_type]         NVARCHAR(50)      NOT NULL,
        [event_date]         DATE              NOT NULL,
        [detail]             NVARCHAR(MAX)     NULL,
        [authorized_by]      NVARCHAR(100)     NULL,
        [decision_reference] NVARCHAR(100)     NULL,
        [is_correction]      BIT               NOT NULL CONSTRAINT [df_students_history_is_correction] DEFAULT (0),
        [corrects_entry_id]  UNIQUEIDENTIFIER  NULL,
        [created_at]         DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_students_history_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]         NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_administrative_history_entries] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [fk_students_history_student] FOREIGN KEY ([student_id])
            REFERENCES [students].[students] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_students_history_corrects] FOREIGN KEY ([corrects_entry_id])
            REFERENCES [students].[administrative_history_entries] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_history_student'
      AND [object_id] = OBJECT_ID(N'students.administrative_history_entries')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_students_history_student]
        ON [students].[administrative_history_entries] ([student_id], [event_date] DESC, [created_at] DESC);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Idempotent Historical Data Backfill
-- -----------------------------------------------------------------------------

-- 1. Backfill administrative placements for existing students lacking one
INSERT INTO [students].[administrative_placements] (
    [id], [student_id], [campus], [school_level], [grade], [academic_year],
    [effective_date], [program], [stream], [cohort], [homeroom],
    [end_date], [is_current], [reason], [authorized_by], [decision_reference],
    [created_at], [created_by]
)
SELECT
    NEWID(),
    s.id,
    COALESCE(latest_enr.initial_campus, N'Main Campus'),
    COALESCE(latest_enr.initial_level, N'Default Level'),
    COALESCE(latest_enr.initial_level, N'Default Level'),
    COALESCE(latest_enr.initial_academic_year, N'2026/2027'),
    s.enrollment_date,
    latest_enr.initial_program,
    NULL,
    latest_enr.initial_cohort,
    NULL,
    NULL,
    1,
    N'Historical enrollment data backfill',
    COALESCE(latest_enr.approved_by, N'SYSTEM_MIGRATION'),
    N'MIGRATION_20261104',
    SYSUTCDATETIME(),
    N'SYSTEM_MIGRATION'
FROM [students].[students] s
OUTER APPLY (
    SELECT TOP 1 *
    FROM [students].[enrollments] e
    WHERE e.student_id = s.id
    ORDER BY e.enrollment_date DESC, e.created_at DESC
) latest_enr
WHERE NOT EXISTS (
    SELECT 1 FROM [students].[administrative_placements] ap WHERE ap.student_id = s.id
);
GO

-- 2. Backfill student statuses for existing students lacking one
INSERT INTO [students].[student_statuses] (
    [id], [student_id], [status], [effective_date], [end_date], [is_current],
    [reason], [authorized_by], [decision_reference], [created_at], [created_by]
)
SELECT
    NEWID(),
    s.id,
    COALESCE(s.current_status, N'active'),
    s.enrollment_date,
    NULL,
    1,
    N'Historical status backfill',
    N'SYSTEM_MIGRATION',
    N'MIGRATION_20261104',
    SYSUTCDATETIME(),
    N'SYSTEM_MIGRATION'
FROM [students].[students] s
WHERE NOT EXISTS (
    SELECT 1 FROM [students].[student_statuses] st WHERE st.student_id = s.id
);
GO

-- 3. Backfill baseline intake administrative history for existing students lacking history
INSERT INTO [students].[administrative_history_entries] (
    [id], [student_id], [event_type], [event_date], [detail],
    [authorized_by], [decision_reference], [is_correction], [corrects_entry_id],
    [created_at], [created_by]
)
SELECT
    NEWID(),
    s.id,
    N'enrollment_completed',
    s.enrollment_date,
    CONCAT(N'Officially enrolled with student number ', s.student_number),
    N'SYSTEM_MIGRATION',
    N'MIGRATION_20261104',
    0,
    NULL,
    SYSUTCDATETIME(),
    N'SYSTEM_MIGRATION'
FROM [students].[students] s
WHERE NOT EXISTS (
    SELECT 1 FROM [students].[administrative_history_entries] h WHERE h.student_id = s.id
);
GO
