-- =============================================================================
-- 20261101_0301_Finance_Policies.sql
-- finance.policies: financial policies owned by the Finance domain.
-- academic_year_id is a logical UUID reference to operations.academic_years with
-- NO physical cross-schema foreign key (ADR-002).
-- =============================================================================

IF OBJECT_ID(N'finance.policies', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[policies]
    (
        [id]               UNIQUEIDENTIFIER NOT NULL,
        [academic_year_id] UNIQUEIDENTIFIER NOT NULL,
        [policy_code]      NVARCHAR(32)     NOT NULL,
        [name]             NVARCHAR(200)    NOT NULL,
        [is_active]        BIT              NOT NULL CONSTRAINT [df_finance_policies_is_active] DEFAULT (1),
        [created_at]       DATETIMEOFFSET(7) NOT NULL,
        [created_by]       NVARCHAR(100)    NULL,
        CONSTRAINT [pk_finance_policies] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_policies_academic_year_id'
      AND [object_id] = OBJECT_ID(N'finance.policies')
)
BEGIN
    CREATE INDEX [ix_finance_policies_academic_year_id]
        ON [finance].[policies] ([academic_year_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_policies_policy_code'
      AND [object_id] = OBJECT_ID(N'finance.policies')
)
BEGIN
    CREATE INDEX [ix_finance_policies_policy_code]
        ON [finance].[policies] ([policy_code]);
END;
GO
