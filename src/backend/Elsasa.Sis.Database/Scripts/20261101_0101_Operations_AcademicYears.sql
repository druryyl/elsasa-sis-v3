-- =============================================================================
-- 20261101_0101_Operations_AcademicYears.sql
-- operations.academic_years: academic year catalogue owned by the Operations
-- domain. Referenced by other schemas only as a logical UUID key (ADR-002), so
-- no cross-schema foreign key is declared here.
-- =============================================================================

IF OBJECT_ID(N'operations.academic_years', N'U') IS NULL
BEGIN
    CREATE TABLE [operations].[academic_years]
    (
        [id]         UNIQUEIDENTIFIER NOT NULL,
        [code]       NVARCHAR(32)     NOT NULL,
        [name]       NVARCHAR(200)    NOT NULL,
        [start_date] DATE             NOT NULL,
        [end_date]   DATE             NOT NULL,
        [is_active]  BIT              NOT NULL CONSTRAINT [df_operations_academic_years_is_active] DEFAULT (1),
        [created_at] DATETIMEOFFSET(7) NOT NULL,
        [created_by] NVARCHAR(100)    NULL,
        CONSTRAINT [pk_operations_academic_years] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_operations_academic_years_code'
      AND [object_id] = OBJECT_ID(N'operations.academic_years')
)
BEGIN
    CREATE UNIQUE INDEX [ux_operations_academic_years_code]
        ON [operations].[academic_years] ([code]);
END;
GO
