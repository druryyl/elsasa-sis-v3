-- =============================================================================
-- 20261102_0101_Operations_CalendarPeriods_Tables.sql
-- Operations domain Stage 1 Calendar & Operational Periods persistence.
-- 1. Non-destructively alters operations.academic_years to introduce the 5-status
--    lifecycle (draft, approved, active, completed, archived) with audit columns,
--    migrates legacy is_active = 1 rows to 'active', and removes is_active.
-- 2. Provisions domain tables:
--    - operations.operational_periods
--    - operations.school_calendars
--    - operations.school_closures
--    - operations.calendar_entries
--    - operations.calendar_amendments
-- 3. Provisions auditable record corrections table:
--    - operations.operations_record_corrections
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Alter operations.academic_years
-- -----------------------------------------------------------------------------

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'status'
      AND [object_id] = OBJECT_ID(N'operations.academic_years')
)
BEGIN
    ALTER TABLE [operations].[academic_years]
        ADD [status] NVARCHAR(32) NOT NULL CONSTRAINT [df_operations_academic_years_status] DEFAULT (N'draft');
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'approved_by'
      AND [object_id] = OBJECT_ID(N'operations.academic_years')
)
BEGIN
    ALTER TABLE [operations].[academic_years]
        ADD [approved_by] NVARCHAR(100) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'approved_date'
      AND [object_id] = OBJECT_ID(N'operations.academic_years')
)
BEGIN
    ALTER TABLE [operations].[academic_years]
        ADD [approved_date] DATE NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'notes'
      AND [object_id] = OBJECT_ID(N'operations.academic_years')
)
BEGIN
    ALTER TABLE [operations].[academic_years]
        ADD [notes] NVARCHAR(MAX) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'updated_at'
      AND [object_id] = OBJECT_ID(N'operations.academic_years')
)
BEGIN
    ALTER TABLE [operations].[academic_years]
        ADD [updated_at] DATETIMEOFFSET(7) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'updated_by'
      AND [object_id] = OBJECT_ID(N'operations.academic_years')
)
BEGIN
    ALTER TABLE [operations].[academic_years]
        ADD [updated_by] NVARCHAR(100) NULL;
END;
GO

-- Migrate data and remove legacy is_active column
IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'is_active'
      AND [object_id] = OBJECT_ID(N'operations.academic_years')
)
BEGIN
    EXEC(N'UPDATE [operations].[academic_years] SET [status] = N''active'' WHERE [is_active] = 1;');

    DECLARE @df_academic_years_is_active NVARCHAR(256);
    SELECT @df_academic_years_is_active = dc.[name]
    FROM sys.default_constraints dc
    JOIN sys.columns c
      ON dc.[parent_object_id] = c.[object_id]
     AND dc.[parent_column_id] = c.[column_id]
    WHERE dc.[parent_object_id] = OBJECT_ID(N'operations.academic_years')
      AND c.[name] = N'is_active';

    IF @df_academic_years_is_active IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE [operations].[academic_years] DROP CONSTRAINT ' + QUOTENAME(@df_academic_years_is_active));
    END;

    ALTER TABLE [operations].[academic_years] DROP COLUMN [is_active];
END;
GO

-- -----------------------------------------------------------------------------
-- 2. operations.operational_periods
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'operations.operational_periods', N'U') IS NULL
BEGIN
    CREATE TABLE [operations].[operational_periods]
    (
        [id]               UNIQUEIDENTIFIER  NOT NULL,
        [academic_year_id] UNIQUEIDENTIFIER  NOT NULL,
        [period_code]      NVARCHAR(64)      NOT NULL,
        [name]             NVARCHAR(200)     NOT NULL,
        [period_type]      NVARCHAR(32)      NOT NULL,
        [start_date]       DATE              NOT NULL,
        [end_date]         DATE              NOT NULL,
        [parent_period_id] UNIQUEIDENTIFIER  NULL,
        [campus_context]   NVARCHAR(100)     NOT NULL CONSTRAINT [df_operations_operational_periods_campus_context] DEFAULT (N'All'),
        [status]           NVARCHAR(32)      NOT NULL CONSTRAINT [df_operations_operational_periods_status] DEFAULT (N'draft'),
        [notes]            NVARCHAR(MAX)     NULL,
        [created_at]       DATETIMEOFFSET(7) NOT NULL,
        [created_by]       NVARCHAR(100)     NULL,
        [updated_at]       DATETIMEOFFSET(7) NULL,
        [updated_by]       NVARCHAR(100)     NULL,
        CONSTRAINT [pk_operations_operational_periods] PRIMARY KEY ([id]),
        CONSTRAINT [fk_operations_operational_periods_academic_year]
            FOREIGN KEY ([academic_year_id]) REFERENCES [operations].[academic_years] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_operations_operational_periods_parent]
            FOREIGN KEY ([parent_period_id]) REFERENCES [operations].[operational_periods] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_operations_operational_periods_code'
      AND [object_id] = OBJECT_ID(N'operations.operational_periods')
)
BEGIN
    CREATE UNIQUE INDEX [ux_operations_operational_periods_code]
        ON [operations].[operational_periods] ([academic_year_id], [period_code]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_operations_operational_periods_dates'
      AND [object_id] = OBJECT_ID(N'operations.operational_periods')
)
BEGIN
    CREATE INDEX [ix_operations_operational_periods_dates]
        ON [operations].[operational_periods] ([start_date], [end_date], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. operations.school_calendars
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'operations.school_calendars', N'U') IS NULL
BEGIN
    CREATE TABLE [operations].[school_calendars]
    (
        [id]                 UNIQUEIDENTIFIER  NOT NULL,
        [academic_year_id]   UNIQUEIDENTIFIER  NOT NULL,
        [name]               NVARCHAR(200)     NOT NULL,
        [scope_type]         NVARCHAR(32)      NOT NULL CONSTRAINT [df_operations_school_calendars_scope_type] DEFAULT (N'institution'),
        [scope_reference]    NVARCHAR(100)     NOT NULL CONSTRAINT [df_operations_school_calendars_scope_reference] DEFAULT (N'All'),
        [version]            NVARCHAR(16)      NOT NULL CONSTRAINT [df_operations_school_calendars_version] DEFAULT (N'1.0'),
        [status]             NVARCHAR(32)      NOT NULL CONSTRAINT [df_operations_school_calendars_status] DEFAULT (N'draft'),
        [publication_status] NVARCHAR(32)      NOT NULL CONSTRAINT [df_operations_school_calendars_publication_status] DEFAULT (N'draft'),
        [published_at]       DATETIMEOFFSET(7) NULL,
        [published_by]       NVARCHAR(100)     NULL,
        [approved_by]        NVARCHAR(100)     NULL,
        [approved_date]      DATE              NULL,
        [notes]              NVARCHAR(MAX)     NULL,
        [created_at]         DATETIMEOFFSET(7) NOT NULL,
        [created_by]         NVARCHAR(100)     NULL,
        [updated_at]         DATETIMEOFFSET(7) NULL,
        [updated_by]         NVARCHAR(100)     NULL,
        CONSTRAINT [pk_operations_school_calendars] PRIMARY KEY ([id]),
        CONSTRAINT [fk_operations_school_calendars_academic_year]
            FOREIGN KEY ([academic_year_id]) REFERENCES [operations].[academic_years] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_operations_school_calendars_ay'
      AND [object_id] = OBJECT_ID(N'operations.school_calendars')
)
BEGIN
    CREATE INDEX [ix_operations_school_calendars_ay]
        ON [operations].[school_calendars] ([academic_year_id], [publication_status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. operations.school_closures
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'operations.school_closures', N'U') IS NULL
BEGIN
    CREATE TABLE [operations].[school_closures]
    (
        [id]                   UNIQUEIDENTIFIER  NOT NULL,
        [academic_year_id]     UNIQUEIDENTIFIER  NOT NULL,
        [closure_number]       NVARCHAR(64)      NOT NULL,
        [reason_category]      NVARCHAR(32)      NOT NULL,
        [reason_details]       NVARCHAR(MAX)     NOT NULL,
        [start_date]           DATE              NOT NULL,
        [end_date]             DATE              NOT NULL,
        [affected_campus]      NVARCHAR(100)     NOT NULL CONSTRAINT [df_operations_school_closures_affected_campus] DEFAULT (N'All'),
        [affected_population]  NVARCHAR(32)      NOT NULL CONSTRAINT [df_operations_school_closures_affected_population] DEFAULT (N'all'),
        [status]               NVARCHAR(32)      NOT NULL CONSTRAINT [df_operations_school_closures_status] DEFAULT (N'authorized'),
        [authorized_by]        NVARCHAR(100)     NOT NULL,
        [authorized_date]      DATE              NOT NULL,
        [communication_status] NVARCHAR(32)      NOT NULL CONSTRAINT [df_operations_school_closures_communication_status] DEFAULT (N'pending'),
        [recovery_plan_notes]  NVARCHAR(MAX)     NULL,
        [created_at]           DATETIMEOFFSET(7) NOT NULL,
        [created_by]           NVARCHAR(100)     NULL,
        [updated_at]           DATETIMEOFFSET(7) NULL,
        [updated_by]           NVARCHAR(100)     NULL,
        CONSTRAINT [pk_operations_school_closures] PRIMARY KEY ([id]),
        CONSTRAINT [fk_operations_school_closures_academic_year]
            FOREIGN KEY ([academic_year_id]) REFERENCES [operations].[academic_years] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_school_closures_number'
      AND [object_id] = OBJECT_ID(N'operations.school_closures')
)
BEGIN
    CREATE UNIQUE INDEX [ux_school_closures_number]
        ON [operations].[school_closures] ([closure_number]);
END;
GO

-- -----------------------------------------------------------------------------
-- 5. operations.calendar_entries
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'operations.calendar_entries', N'U') IS NULL
BEGIN
    CREATE TABLE [operations].[calendar_entries]
    (
        [id]               UNIQUEIDENTIFIER  NOT NULL,
        [calendar_id]      UNIQUEIDENTIFIER  NOT NULL,
        [entry_date]       DATE              NOT NULL,
        [end_date]         DATE              NULL,
        [designation]      NVARCHAR(32)      NOT NULL,
        [name]             NVARCHAR(200)     NOT NULL,
        [description]      NVARCHAR(MAX)     NULL,
        [is_instructional] BIT               NOT NULL CONSTRAINT [df_operations_calendar_entries_is_instructional] DEFAULT (0),
        [closure_id]       UNIQUEIDENTIFIER  NULL,
        [applies_to]       NVARCHAR(100)     NOT NULL CONSTRAINT [df_operations_calendar_entries_applies_to] DEFAULT (N'All'),
        [created_at]       DATETIMEOFFSET(7) NOT NULL,
        [created_by]       NVARCHAR(100)     NULL,
        CONSTRAINT [pk_operations_calendar_entries] PRIMARY KEY ([id]),
        CONSTRAINT [fk_operations_calendar_entries_calendar]
            FOREIGN KEY ([calendar_id]) REFERENCES [operations].[school_calendars] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_operations_calendar_entries_closure]
            FOREIGN KEY ([closure_id]) REFERENCES [operations].[school_closures] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_operations_calendar_entries_date'
      AND [object_id] = OBJECT_ID(N'operations.calendar_entries')
)
BEGIN
    CREATE INDEX [ix_operations_calendar_entries_date]
        ON [operations].[calendar_entries] ([calendar_id], [entry_date], [end_date]);
END;
GO

-- -----------------------------------------------------------------------------
-- 6. operations.calendar_amendments
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'operations.calendar_amendments', N'U') IS NULL
BEGIN
    CREATE TABLE [operations].[calendar_amendments]
    (
        [id]                         UNIQUEIDENTIFIER  NOT NULL,
        [calendar_id]                UNIQUEIDENTIFIER  NOT NULL,
        [amendment_number]           NVARCHAR(64)      NOT NULL,
        [from_version]               NVARCHAR(16)      NOT NULL,
        [to_version]                 NVARCHAR(16)      NOT NULL,
        [amendment_type]             NVARCHAR(32)      NOT NULL,
        [affected_entry_id]          UNIQUEIDENTIFIER  NULL,
        [change_summary]             NVARCHAR(MAX)     NOT NULL,
        [previous_value]             NVARCHAR(MAX)     NULL,
        [new_value]                  NVARCHAR(MAX)     NULL,
        [reason]                     NVARCHAR(MAX)     NOT NULL,
        [effective_date]             DATE              NOT NULL,
        [authorized_by]              NVARCHAR(100)     NOT NULL,
        [authorized_date]            DATE              NOT NULL,
        [downstream_impact_reviewed] BIT               NOT NULL CONSTRAINT [df_operations_calendar_amendments_downstream_impact_reviewed] DEFAULT (0),
        [created_at]                 DATETIMEOFFSET(7) NOT NULL,
        [created_by]                 NVARCHAR(100)     NULL,
        CONSTRAINT [pk_operations_calendar_amendments] PRIMARY KEY ([id]),
        CONSTRAINT [fk_operations_calendar_amendments_calendar]
            FOREIGN KEY ([calendar_id]) REFERENCES [operations].[school_calendars] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_operations_calendar_amendments_entry]
            FOREIGN KEY ([affected_entry_id]) REFERENCES [operations].[calendar_entries] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_calendar_amendments_number'
      AND [object_id] = OBJECT_ID(N'operations.calendar_amendments')
)
BEGIN
    CREATE UNIQUE INDEX [ux_calendar_amendments_number]
        ON [operations].[calendar_amendments] ([amendment_number]);
END;
GO

-- -----------------------------------------------------------------------------
-- 7. operations.operations_record_corrections
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'operations.operations_record_corrections', N'U') IS NULL
BEGIN
    CREATE TABLE [operations].[operations_record_corrections]
    (
        [id]                   UNIQUEIDENTIFIER  NOT NULL,
        [academic_year_id]     UNIQUEIDENTIFIER  NOT NULL,
        [target_table_name]    NVARCHAR(64)      NOT NULL,
        [target_record_id]     UNIQUEIDENTIFIER  NOT NULL,
        [field_name]           NVARCHAR(64)      NOT NULL,
        [old_value]            NVARCHAR(MAX)     NULL,
        [new_value]            NVARCHAR(MAX)     NULL,
        [justification_reason] NVARCHAR(MAX)     NOT NULL,
        [authorized_by]        NVARCHAR(100)     NOT NULL,
        [authorized_at]        DATETIMEOFFSET(7) NOT NULL,
        [created_at]           DATETIMEOFFSET(7) NOT NULL,
        [created_by]           NVARCHAR(100)     NULL,
        CONSTRAINT [pk_operations_operations_record_corrections] PRIMARY KEY ([id]),
        CONSTRAINT [fk_operations_operations_record_corrections_academic_year]
            FOREIGN KEY ([academic_year_id]) REFERENCES [operations].[academic_years] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_operations_operations_record_corrections_target'
      AND [object_id] = OBJECT_ID(N'operations.operations_record_corrections')
)
BEGIN
    CREATE INDEX [ix_operations_operations_record_corrections_target]
        ON [operations].[operations_record_corrections] ([target_table_name], [target_record_id]);
END;
GO
