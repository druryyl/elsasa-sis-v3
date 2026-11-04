-- =============================================================================
-- 20261104_0301_Finance_PaymentsReceivables_Tables.sql
-- Finance domain Stage 1 Payments & Receivables schema evolution (IS007 / CR-FIN-PAYMENTS-RECEIVABLES).
-- 1. Dual Reference Alterations for Pre-Enrollment Applicants (GAP-001, OQ-001):
--    - Alters finance.student_financial_accounts: student_id NULL, adds applicant_id,
--      drops legacy unique constraint uq_finance_student_financial_accounts_student,
--      creates filtered unique indexes ux_finance_student_accounts_student_id and
--      ux_finance_student_accounts_applicant_id, and adds candidate check constraint.
--    - Alters finance.financial_obligations: student_id NULL, adds applicant_id,
--      and adds candidate check constraint ck_finance_obligations_candidate.
-- 2. Provisions 3 authoritative domain tables in schema finance:
--    - finance.payers (Fee payers, sponsors, guardians, embassies)
--    - finance.payments (Gross received funds, payment methods, receipts, confirmation status)
--    - finance.payment_allocations (Settlement of obligations, append-only reallocation lineage)
-- 3. Provisions indexes, unique invariants, and intra-schema foreign key constraints.
-- 4. Defines dynamic projection view finance.v_student_receivables.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Dual Reference Alterations: finance.student_financial_accounts
-- -----------------------------------------------------------------------------

-- 1.1 Add column applicant_id if missing
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'applicant_id'
      AND [object_id] = OBJECT_ID(N'finance.student_financial_accounts')
)
BEGIN
    ALTER TABLE [finance].[student_financial_accounts]
        ADD [applicant_id] UNIQUEIDENTIFIER NULL;
END;
GO

-- 1.2 Drop legacy single-column unique constraint on student_id
IF EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE [name] = N'uq_finance_student_financial_accounts_student'
      AND [parent_object_id] = OBJECT_ID(N'finance.student_financial_accounts')
)
BEGIN
    ALTER TABLE [finance].[student_financial_accounts]
        DROP CONSTRAINT [uq_finance_student_financial_accounts_student];
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'uq_finance_student_financial_accounts_student'
      AND [object_id] = OBJECT_ID(N'finance.student_financial_accounts')
)
BEGIN
    DROP INDEX [uq_finance_student_financial_accounts_student]
        ON [finance].[student_financial_accounts];
END;
GO

-- 1.3 Alter student_id column to nullable
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'student_id'
      AND [object_id] = OBJECT_ID(N'finance.student_financial_accounts')
      AND [is_nullable] = 0
)
BEGIN
    ALTER TABLE [finance].[student_financial_accounts]
        ALTER COLUMN [student_id] UNIQUEIDENTIFIER NULL;
END;
GO

-- 1.4 Filtered unique index on student_id (when student_id is not null)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_finance_student_accounts_student_id'
      AND [object_id] = OBJECT_ID(N'finance.student_financial_accounts')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [ux_finance_student_accounts_student_id]
        ON [finance].[student_financial_accounts] ([student_id])
        WHERE [student_id] IS NOT NULL;
END;
GO

-- 1.5 Filtered unique index on applicant_id (when applicant_id is not null)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_finance_student_accounts_applicant_id'
      AND [object_id] = OBJECT_ID(N'finance.student_financial_accounts')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [ux_finance_student_accounts_applicant_id]
        ON [finance].[student_financial_accounts] ([applicant_id])
        WHERE [applicant_id] IS NOT NULL;
END;
GO

-- 1.6 Candidate check constraint: at least one candidate reference must be populated
IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'ck_finance_student_accounts_candidate'
      AND [parent_object_id] = OBJECT_ID(N'finance.student_financial_accounts')
)
BEGIN
    ALTER TABLE [finance].[student_financial_accounts]
        ADD CONSTRAINT [ck_finance_student_accounts_candidate]
        CHECK ([student_id] IS NOT NULL OR [applicant_id] IS NOT NULL);
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Dual Reference Alterations: finance.financial_obligations
-- -----------------------------------------------------------------------------

-- 2.1 Add column applicant_id if missing
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'applicant_id'
      AND [object_id] = OBJECT_ID(N'finance.financial_obligations')
)
BEGIN
    ALTER TABLE [finance].[financial_obligations]
        ADD [applicant_id] UNIQUEIDENTIFIER NULL;
END;
GO

-- 2.2 Drop dependent index on student_id before altering column nullability
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_obligations_student_status'
      AND [object_id] = OBJECT_ID(N'finance.financial_obligations')
)
BEGIN
    DROP INDEX [ix_finance_obligations_student_status]
        ON [finance].[financial_obligations];
END;
GO

-- 2.3 Alter student_id column to nullable
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'student_id'
      AND [object_id] = OBJECT_ID(N'finance.financial_obligations')
      AND [is_nullable] = 0
)
BEGIN
    ALTER TABLE [finance].[financial_obligations]
        ALTER COLUMN [student_id] UNIQUEIDENTIFIER NULL;
END;
GO

-- 2.4 Recreate index on student_id
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_obligations_student_status'
      AND [object_id] = OBJECT_ID(N'finance.financial_obligations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_obligations_student_status]
        ON [finance].[financial_obligations] ([student_id], [status], [due_date] ASC);
END;
GO

-- 2.5 Index on applicant_id for candidate obligation queries
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_obligations_applicant_status'
      AND [object_id] = OBJECT_ID(N'finance.financial_obligations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_obligations_applicant_status]
        ON [finance].[financial_obligations] ([applicant_id], [status], [due_date] ASC)
        WHERE [applicant_id] IS NOT NULL;
END;
GO

-- 2.6 Candidate check constraint: at least one candidate reference must be populated
IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE [name] = N'ck_finance_obligations_candidate'
      AND [parent_object_id] = OBJECT_ID(N'finance.financial_obligations')
)
BEGIN
    ALTER TABLE [finance].[financial_obligations]
        ADD CONSTRAINT [ck_finance_obligations_candidate]
        CHECK ([student_id] IS NOT NULL OR [applicant_id] IS NOT NULL);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: finance.payers
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'finance.payers', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[payers] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL,
        [payer_number]          NVARCHAR(64)      NOT NULL,
        [payer_name]            NVARCHAR(200)     NOT NULL,
        [payer_type]            NVARCHAR(64)      NOT NULL CONSTRAINT [df_payers_type] DEFAULT ('parent_guardian'),
        [identification_number] NVARCHAR(64)      NULL, -- NIK, NPWP, or corporate registration number
        [email]                 NVARCHAR(200)     NULL,
        [phone_number]          NVARCHAR(50)      NULL,
        [billing_address]       NVARCHAR(MAX)     NULL,
        [bank_account_name]     NVARCHAR(200)     NULL,
        [bank_account_number]   NVARCHAR(100)     NULL,
        [bank_name]             NVARCHAR(100)     NULL,
        [notes]                 NVARCHAR(MAX)     NULL,
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_payers_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(100)     NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(100)     NULL,
        CONSTRAINT [pk_finance_payers] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [uq_finance_payers_number] UNIQUE NONCLUSTERED ([payer_number])
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_payers_identification'
      AND [object_id] = OBJECT_ID(N'finance.payers')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_payers_identification]
        ON [finance].[payers] ([identification_number])
        WHERE [identification_number] IS NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_payers_contact'
      AND [object_id] = OBJECT_ID(N'finance.payers')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_payers_contact]
        ON [finance].[payers] ([email], [phone_number]);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Table: finance.payments
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'finance.payments', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[payments] (
        [id]                  UNIQUEIDENTIFIER  NOT NULL,
        [payment_number]      NVARCHAR(64)      NOT NULL,
        [student_account_id]  UNIQUEIDENTIFIER  NOT NULL,
        [student_id]          UNIQUEIDENTIFIER  NULL,
        [applicant_id]        UNIQUEIDENTIFIER  NULL,
        [payer_id]            UNIQUEIDENTIFIER  NOT NULL,
        [payment_amount]      BIGINT            NOT NULL, -- Gross received amount in IDR
        [currency_code]       VARCHAR(3)        NOT NULL CONSTRAINT [df_payments_currency] DEFAULT ('IDR'),
        [payment_method]      NVARCHAR(64)      NOT NULL, -- 'bank_transfer', 'cash', 'pos_card', 'qris', 'virtual_account', 'cheque', 'other'
        [external_reference]  NVARCHAR(100)     NULL,     -- Bank transaction reference or slip number
        [payment_date]        DATE              NOT NULL,
        [submission_date]     DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_payments_submission_date] DEFAULT (SYSUTCDATETIME()),
        [confirmation_status] NVARCHAR(32)      NOT NULL CONSTRAINT [df_payments_status] DEFAULT ('submitted'), -- 'submitted', 'confirmed', 'rejected', 'reversed'
        [confirmation_date]   DATETIMEOFFSET(7) NULL,
        [confirmed_by]        NVARCHAR(100)     NULL,
        [receipt_number]      NVARCHAR(64)      NULL,     -- Official sequential receipt voucher (e.g. REC-2026-000108)
        [receipt_issued_at]   DATETIMEOFFSET(7) NULL,
        [rejection_reason]    NVARCHAR(MAX)     NULL,
        [notes]               NVARCHAR(MAX)     NULL,
        [created_at]          DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_payments_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]          NVARCHAR(100)     NULL,
        [updated_at]          DATETIMEOFFSET(7) NULL,
        [updated_by]          NVARCHAR(100)     NULL,
        CONSTRAINT [pk_finance_payments] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [uq_finance_payments_number] UNIQUE NONCLUSTERED ([payment_number]),
        CONSTRAINT [fk_finance_payments_account] FOREIGN KEY ([student_account_id])
            REFERENCES [finance].[student_financial_accounts] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_finance_payments_payer] FOREIGN KEY ([payer_id])
            REFERENCES [finance].[payers] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [ck_finance_payments_candidate] CHECK ([student_id] IS NOT NULL OR [applicant_id] IS NOT NULL)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'uq_finance_payments_receipt_number'
      AND [object_id] = OBJECT_ID(N'finance.payments')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [uq_finance_payments_receipt_number]
        ON [finance].[payments] ([receipt_number])
        WHERE [receipt_number] IS NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_payments_queue'
      AND [object_id] = OBJECT_ID(N'finance.payments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_payments_queue]
        ON [finance].[payments] ([confirmation_status], [submission_date] DESC);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_payments_account'
      AND [object_id] = OBJECT_ID(N'finance.payments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_payments_account]
        ON [finance].[payments] ([student_account_id], [confirmation_status]);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_payments_student_applicant'
      AND [object_id] = OBJECT_ID(N'finance.payments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_payments_student_applicant]
        ON [finance].[payments] ([student_id], [applicant_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 5. Table: finance.payment_allocations
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'finance.payment_allocations', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[payment_allocations] (
        [id]                 UNIQUEIDENTIFIER  NOT NULL,
        [allocation_number]  NVARCHAR(64)      NOT NULL,
        [student_account_id] UNIQUEIDENTIFIER  NOT NULL,
        [obligation_id]      UNIQUEIDENTIFIER  NOT NULL,
        [source_type]        NVARCHAR(32)      NOT NULL CONSTRAINT [df_allocations_source] DEFAULT ('payment'), -- 'payment', 'credit_balance'
        [payment_id]         UNIQUEIDENTIFIER  NULL,
        [credit_balance_id]  UNIQUEIDENTIFIER  NULL,
        [allocated_amount]   BIGINT            NOT NULL, -- Amount applied in IDR
        [currency_code]      VARCHAR(3)        NOT NULL CONSTRAINT [df_allocations_currency] DEFAULT ('IDR'),
        [allocation_date]    DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_allocations_date] DEFAULT (SYSUTCDATETIME()),
        [allocated_by]       NVARCHAR(100)     NOT NULL,
        [status]             NVARCHAR(32)      NOT NULL CONSTRAINT [df_allocations_status] DEFAULT ('active'), -- 'active', 'reversed', 'superseded'
        [superseded_by_id]   UNIQUEIDENTIFIER  NULL,
        [reversal_reason]    NVARCHAR(MAX)     NULL,
        [notes]              NVARCHAR(MAX)     NULL,
        [created_at]         DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_allocations_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]         NVARCHAR(100)     NULL,
        [updated_at]         DATETIMEOFFSET(7) NULL,
        [updated_by]         NVARCHAR(100)     NULL,
        CONSTRAINT [pk_finance_payment_allocations] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [uq_finance_allocations_number] UNIQUE NONCLUSTERED ([allocation_number]),
        CONSTRAINT [fk_finance_allocations_account] FOREIGN KEY ([student_account_id])
            REFERENCES [finance].[student_financial_accounts] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_finance_allocations_obligation] FOREIGN KEY ([obligation_id])
            REFERENCES [finance].[financial_obligations] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_finance_allocations_payment] FOREIGN KEY ([payment_id])
            REFERENCES [finance].[payments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_finance_allocations_superseded] FOREIGN KEY ([superseded_by_id])
            REFERENCES [finance].[payment_allocations] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_allocations_obligation_status'
      AND [object_id] = OBJECT_ID(N'finance.payment_allocations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_allocations_obligation_status]
        ON [finance].[payment_allocations] ([obligation_id], [status])
        INCLUDE ([allocated_amount]);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_allocations_payment_status'
      AND [object_id] = OBJECT_ID(N'finance.payment_allocations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_allocations_payment_status]
        ON [finance].[payment_allocations] ([payment_id], [status])
        INCLUDE ([allocated_amount]);
END;
GO

-- -----------------------------------------------------------------------------
-- 6. Dynamic View: finance.v_student_receivables
-- -----------------------------------------------------------------------------

CREATE OR ALTER VIEW [finance].[v_student_receivables]
AS
SELECT 
    fo.[id]                    AS [obligation_id],
    fo.[obligation_number]     AS [obligation_number],
    fo.[student_account_id]    AS [student_account_id],
    fo.[student_id]            AS [student_id],
    fo.[applicant_id]          AS [applicant_id],
    fo.[charge_type]           AS [charge_type],
    fo.[description]           AS [description],
    fo.[is_mandatory]          AS [is_mandatory],
    fo.[due_date]              AS [due_date],
    fo.[original_amount]       AS [original_amount],
    fo.[currency_code]         AS [currency_code],
    fo.[academic_period_id]    AS [academic_period_id],
    fo.[academic_period_label] AS [academic_period_label],
    fo.[status]                AS [obligation_status],
    -- Effective net obligation (Stage 1 without benefits/adjustments)
    fo.[original_amount]       AS [net_obligation],
    -- Confirmed active payments allocated
    ISNULL(alloc.[total_allocated], 0) AS [total_paid],
    -- Dynamic outstanding amount clamped at >= 0
    CASE 
        WHEN fo.[original_amount] - ISNULL(alloc.[total_allocated], 0) > 0 
        THEN fo.[original_amount] - ISNULL(alloc.[total_allocated], 0)
        ELSE 0 
    END AS [outstanding_amount],
    -- Overdue detection
    CASE 
        WHEN CAST(SYSUTCDATETIME() AS DATE) > fo.[due_date] 
             AND (fo.[original_amount] - ISNULL(alloc.[total_allocated], 0)) > 0
        THEN 1 
        ELSE 0 
    END AS [is_overdue],
    -- Days overdue calculation
    CASE 
        WHEN CAST(SYSUTCDATETIME() AS DATE) > fo.[due_date] 
             AND (fo.[original_amount] - ISNULL(alloc.[total_allocated], 0)) > 0
        THEN DATEDIFF(DAY, fo.[due_date], CAST(SYSUTCDATETIME() AS DATE))
        ELSE 0 
    END AS [days_overdue],
    -- Standard SIS aging bracket
    CASE 
        WHEN CAST(SYSUTCDATETIME() AS DATE) <= fo.[due_date] 
             OR (fo.[original_amount] - ISNULL(alloc.[total_allocated], 0)) <= 0 THEN 'Current'
        WHEN DATEDIFF(DAY, fo.[due_date], CAST(SYSUTCDATETIME() AS DATE)) <= 30 THEN '1-30 Days'
        WHEN DATEDIFF(DAY, fo.[due_date], CAST(SYSUTCDATETIME() AS DATE)) <= 60 THEN '31-60 Days'
        WHEN DATEDIFF(DAY, fo.[due_date], CAST(SYSUTCDATETIME() AS DATE)) <= 90 THEN '61-90 Days'
        ELSE '90+ Days'
    END AS [aging_bracket],
    -- Operational classification status
    CASE 
        WHEN (fo.[original_amount] - ISNULL(alloc.[total_allocated], 0)) <= 0 THEN 'settled'
        WHEN CAST(SYSUTCDATETIME() AS DATE) > fo.[due_date] THEN 'overdue'
        ELSE 'current'
    END AS [receivable_status]
FROM [finance].[financial_obligations] fo
LEFT JOIN (
    SELECT 
        pa.[obligation_id],
        SUM(pa.[allocated_amount]) AS [total_allocated]
    FROM [finance].[payment_allocations] pa
    WHERE pa.[status] = 'active'
    GROUP BY pa.[obligation_id]
) alloc ON alloc.[obligation_id] = fo.[id]
WHERE fo.[status] NOT IN ('cancelled');
GO
