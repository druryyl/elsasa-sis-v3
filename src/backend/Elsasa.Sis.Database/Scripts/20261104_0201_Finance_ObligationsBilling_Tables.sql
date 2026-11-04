-- =============================================================================
-- 20261104_0201_Finance_ObligationsBilling_Tables.sql
-- Finance domain Stage 1 Obligations & Billing schema evolution (IS006 / CR-FIN-OBLIGATIONS-BILLING).
-- 1. Drops obsolete stub table finance.policies.
-- 2. Provisions 7 authoritative domain tables in schema finance:
--    - finance.financial_policies
--    - finance.charge_rules
--    - finance.student_financial_accounts
--    - finance.financial_obligations
--    - finance.obligation_due_schedules
--    - finance.billing_documents
--    - finance.billing_document_items
-- 3. Provisions indexes, unique invariants, and intra-schema foreign key constraints.
-- 4. Executes idempotent student account backfill from students.students (GAP-002, TD-03).
-- 5. Seeds GAP-06 core Financial Policy FP-2026-001 and 3 charge rules (GAP-007, TD-06).
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 0. Drop obsolete stub table finance.policies
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.policies', N'U') IS NOT NULL
BEGIN
    DROP TABLE [finance].[policies];
END;
GO

-- -----------------------------------------------------------------------------
-- 1. Table: finance.financial_policies
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.financial_policies', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[financial_policies] (
        [id]                   UNIQUEIDENTIFIER  NOT NULL,
        [policy_code]          NVARCHAR(64)      NOT NULL,
        [name]                 NVARCHAR(200)     NOT NULL,
        [category]             NVARCHAR(64)      NOT NULL,
        [business_purpose]     NVARCHAR(MAX)     NOT NULL,
        [applicable_context]   NVARCHAR(200)     NULL,
        [effective_date]       DATE              NOT NULL,
        [end_date]             DATE              NULL,
        [policy_version]       NVARCHAR(32)      NOT NULL CONSTRAINT [df_financial_policies_version] DEFAULT ('1.0'),
        [status]               NVARCHAR(32)      NOT NULL CONSTRAINT [df_financial_policies_status] DEFAULT ('draft'),
        [approving_authority]  NVARCHAR(100)     NULL,
        [approved_date]        DATE              NULL,
        [supersedes_policy_id] UNIQUEIDENTIFIER  NULL,
        [notes]                NVARCHAR(MAX)     NULL,
        [created_at]           DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_financial_policies_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]           NVARCHAR(100)     NULL,
        [updated_at]           DATETIMEOFFSET(7) NULL,
        [updated_by]           NVARCHAR(100)     NULL,
        CONSTRAINT [pk_finance_financial_policies] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [uq_finance_financial_policies_code] UNIQUE NONCLUSTERED ([policy_code]),
        CONSTRAINT [fk_finance_financial_policies_supersedes] FOREIGN KEY ([supersedes_policy_id])
            REFERENCES [finance].[financial_policies] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_financial_policies_category_status'
      AND [object_id] = OBJECT_ID(N'finance.financial_policies')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_financial_policies_category_status]
        ON [finance].[financial_policies] ([category], [status], [effective_date] DESC);
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Table: finance.charge_rules
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.charge_rules', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[charge_rules] (
        [id]                   UNIQUEIDENTIFIER  NOT NULL,
        [rule_code]            NVARCHAR(64)      NOT NULL,
        [policy_id]            UNIQUEIDENTIFIER  NOT NULL,
        [name]                 NVARCHAR(200)     NOT NULL,
        [charge_type]          NVARCHAR(64)      NOT NULL,
        [is_mandatory]         BIT               NOT NULL CONSTRAINT [df_charge_rules_is_mandatory] DEFAULT (1),
        [determination_method] NVARCHAR(64)      NOT NULL CONSTRAINT [df_charge_rules_method] DEFAULT ('fixed_amount'),
        [base_amount]          BIGINT            NOT NULL,
        [currency_code]        VARCHAR(3)        NOT NULL CONSTRAINT [df_charge_rules_currency] DEFAULT ('IDR'),
        [recurrence_pattern]   NVARCHAR(32)      NOT NULL,
        [due_rule_type]        NVARCHAR(32)      NOT NULL,
        [due_rule_value]       NVARCHAR(64)      NOT NULL,
        [applicable_grade]     NVARCHAR(64)      NULL,
        [applicable_program]   NVARCHAR(100)     NULL,
        [status]               NVARCHAR(32)      NOT NULL CONSTRAINT [df_charge_rules_status] DEFAULT ('active'),
        [notes]                NVARCHAR(MAX)     NULL,
        [created_at]           DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_charge_rules_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]           NVARCHAR(100)     NULL,
        [updated_at]           DATETIMEOFFSET(7) NULL,
        [updated_by]           NVARCHAR(100)     NULL,
        CONSTRAINT [pk_finance_charge_rules] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [uq_finance_charge_rules_code] UNIQUE NONCLUSTERED ([rule_code]),
        CONSTRAINT [fk_finance_charge_rules_policy] FOREIGN KEY ([policy_id])
            REFERENCES [finance].[financial_policies] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_charge_rules_policy'
      AND [object_id] = OBJECT_ID(N'finance.charge_rules')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_charge_rules_policy]
        ON [finance].[charge_rules] ([policy_id], [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: finance.student_financial_accounts
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.student_financial_accounts', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[student_financial_accounts] (
        [id]             UNIQUEIDENTIFIER  NOT NULL,
        [account_number] NVARCHAR(64)      NOT NULL,
        [student_id]     UNIQUEIDENTIFIER  NOT NULL,
        [currency_code]  VARCHAR(3)        NOT NULL CONSTRAINT [df_financial_accounts_currency] DEFAULT ('IDR'),
        [account_status] NVARCHAR(32)      NOT NULL CONSTRAINT [df_financial_accounts_status] DEFAULT ('active'),
        [opened_date]    DATE              NOT NULL,
        [closed_date]    DATE              NULL,
        [notes]          NVARCHAR(MAX)     NULL,
        [created_at]     DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_financial_accounts_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]     NVARCHAR(100)     NULL,
        [updated_at]     DATETIMEOFFSET(7) NULL,
        [updated_by]     NVARCHAR(100)     NULL,
        CONSTRAINT [pk_finance_student_financial_accounts] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [uq_finance_student_financial_accounts_number] UNIQUE NONCLUSTERED ([account_number]),
        CONSTRAINT [uq_finance_student_financial_accounts_student] UNIQUE NONCLUSTERED ([student_id])
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_student_financial_accounts_status'
      AND [object_id] = OBJECT_ID(N'finance.student_financial_accounts')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_student_financial_accounts_status]
        ON [finance].[student_financial_accounts] ([account_status], [opened_date] DESC);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Table: finance.financial_obligations
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.financial_obligations', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[financial_obligations] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL,
        [obligation_number]     NVARCHAR(64)      NOT NULL,
        [student_account_id]    UNIQUEIDENTIFIER  NOT NULL,
        [student_id]            UNIQUEIDENTIFIER  NOT NULL,
        [charge_rule_id]        UNIQUEIDENTIFIER  NULL,
        [policy_id]             UNIQUEIDENTIFIER  NOT NULL,
        [description]           NVARCHAR(255)     NOT NULL,
        [charge_type]           NVARCHAR(64)      NOT NULL,
        [is_mandatory]          BIT               NOT NULL CONSTRAINT [df_obligations_is_mandatory] DEFAULT (1),
        [original_amount]       BIGINT            NOT NULL,
        [currency_code]         VARCHAR(3)        NOT NULL CONSTRAINT [df_obligations_currency] DEFAULT ('IDR'),
        [issue_date]            DATE              NOT NULL,
        [due_date]              DATE              NOT NULL,
        [academic_period_id]    UNIQUEIDENTIFIER  NOT NULL,
        [academic_period_label] NVARCHAR(100)     NOT NULL,
        [status]                NVARCHAR(32)      NOT NULL CONSTRAINT [df_obligations_status] DEFAULT ('assessed'),
        [authorized_by]         NVARCHAR(100)     NOT NULL,
        [notes]                 NVARCHAR(MAX)     NULL,
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_obligations_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(100)     NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(100)     NULL,
        CONSTRAINT [pk_finance_financial_obligations] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [uq_finance_financial_obligations_number] UNIQUE NONCLUSTERED ([obligation_number]),
        CONSTRAINT [fk_finance_obligations_account] FOREIGN KEY ([student_account_id])
            REFERENCES [finance].[student_financial_accounts] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_finance_obligations_charge_rule] FOREIGN KEY ([charge_rule_id])
            REFERENCES [finance].[charge_rules] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_finance_obligations_policy] FOREIGN KEY ([policy_id])
            REFERENCES [finance].[financial_policies] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
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

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_obligations_account_period'
      AND [object_id] = OBJECT_ID(N'finance.financial_obligations')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_obligations_account_period]
        ON [finance].[financial_obligations] ([student_account_id], [academic_period_id], [charge_rule_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 5. Table: finance.obligation_due_schedules
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.obligation_due_schedules', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[obligation_due_schedules] (
        [id]                   UNIQUEIDENTIFIER  NOT NULL,
        [obligation_id]        UNIQUEIDENTIFIER  NOT NULL,
        [installment_sequence] INT               NOT NULL,
        [due_date]             DATE              NOT NULL,
        [scheduled_amount]     BIGINT            NOT NULL,
        [currency_code]        VARCHAR(3)        NOT NULL CONSTRAINT [df_due_schedules_currency] DEFAULT ('IDR'),
        [status]               NVARCHAR(32)      NOT NULL CONSTRAINT [df_due_schedules_status] DEFAULT ('pending'),
        [notes]                NVARCHAR(MAX)     NULL,
        [created_at]           DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_due_schedules_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]           NVARCHAR(100)     NULL,
        [updated_at]           DATETIMEOFFSET(7) NULL,
        [updated_by]           NVARCHAR(100)     NULL,
        CONSTRAINT [pk_finance_obligation_due_schedules] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [uq_finance_due_schedules_sequence] UNIQUE NONCLUSTERED ([obligation_id], [installment_sequence]),
        CONSTRAINT [fk_finance_due_schedules_obligation] FOREIGN KEY ([obligation_id])
            REFERENCES [finance].[financial_obligations] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_due_schedules_due_date_status'
      AND [object_id] = OBJECT_ID(N'finance.obligation_due_schedules')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_due_schedules_due_date_status]
        ON [finance].[obligation_due_schedules] ([due_date] ASC, [status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 6. Table: finance.billing_documents
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.billing_documents', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[billing_documents] (
        [id]                     UNIQUEIDENTIFIER  NOT NULL,
        [document_number]        NVARCHAR(64)      NOT NULL,
        [student_account_id]     UNIQUEIDENTIFIER  NOT NULL,
        [student_id]             UNIQUEIDENTIFIER  NOT NULL,
        [document_type]          NVARCHAR(32)      NOT NULL, -- 'bill' | 'statement'
        [recipient_name]         NVARCHAR(200)     NOT NULL,
        [recipient_relationship] NVARCHAR(64)      NOT NULL,
        [recipient_contact]      NVARCHAR(200)     NOT NULL,
        [billing_period_start]   DATE              NULL,
        [billing_period_end]     DATE              NULL,
        [issue_date]             DATE              NOT NULL,
        [payment_due_date]       DATE              NULL,
        [total_billed_amount]    BIGINT            NOT NULL,
        [currency_code]          VARCHAR(3)        NOT NULL CONSTRAINT [df_billing_docs_currency] DEFAULT ('IDR'),
        [delivery_channel]       NVARCHAR(32)      NOT NULL CONSTRAINT [df_billing_docs_channel] DEFAULT ('portal'),
        [delivery_status]        NVARCHAR(32)      NOT NULL CONSTRAINT [df_billing_docs_status] DEFAULT ('issued'),
        [sent_at]                DATETIMEOFFSET(7) NULL,
        [notes]                  NVARCHAR(MAX)     NULL,
        [created_at]             DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_billing_docs_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]             NVARCHAR(100)     NULL,
        [updated_at]             DATETIMEOFFSET(7) NULL,
        [updated_by]             NVARCHAR(100)     NULL,
        CONSTRAINT [pk_finance_billing_documents] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [uq_finance_billing_documents_number] UNIQUE NONCLUSTERED ([document_number]),
        CONSTRAINT [fk_finance_billing_documents_account] FOREIGN KEY ([student_account_id])
            REFERENCES [finance].[student_financial_accounts] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_billing_documents_student'
      AND [object_id] = OBJECT_ID(N'finance.billing_documents')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_billing_documents_student]
        ON [finance].[billing_documents] ([student_id], [issue_date] DESC);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_billing_documents_status'
      AND [object_id] = OBJECT_ID(N'finance.billing_documents')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_billing_documents_status]
        ON [finance].[billing_documents] ([delivery_status], [delivery_channel]);
END;
GO

-- -----------------------------------------------------------------------------
-- 7. Table: finance.billing_document_items
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.billing_document_items', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[billing_document_items] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL,
        [billing_document_id]   UNIQUEIDENTIFIER  NOT NULL,
        [line_sequence]         INT               NOT NULL,
        [item_type]             NVARCHAR(64)      NOT NULL, -- 'obligation', 'benefit_reduction', 'payment_credit', 'adjustment'
        [reference_entity_type] NVARCHAR(64)      NOT NULL, -- 'financial_obligations', etc.
        [reference_entity_id]   UNIQUEIDENTIFIER  NOT NULL,
        [description]           NVARCHAR(255)     NOT NULL,
        [amount]                BIGINT            NOT NULL,
        [currency_code]         VARCHAR(3)        NOT NULL CONSTRAINT [df_billing_items_currency] DEFAULT ('IDR'),
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_billing_items_created_at] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [pk_finance_billing_document_items] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [uq_finance_billing_items_sequence] UNIQUE NONCLUSTERED ([billing_document_id], [line_sequence]),
        CONSTRAINT [fk_finance_billing_items_document] FOREIGN KEY ([billing_document_id])
            REFERENCES [finance].[billing_documents] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_finance_billing_items_reference'
      AND [object_id] = OBJECT_ID(N'finance.billing_document_items')
)
BEGIN
    CREATE NONCLUSTERED INDEX [ix_finance_billing_items_reference]
        ON [finance].[billing_document_items] ([reference_entity_type], [reference_entity_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 8. Idempotent Student Account Backfill (GAP-002, TD-03)
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'students.students', N'U') IS NOT NULL
BEGIN
    INSERT INTO [finance].[student_financial_accounts] (
        [id], [account_number], [student_id], [currency_code], [account_status], [opened_date], [created_at], [created_by]
    )
    SELECT 
        NEWID(),
        CONCAT('ACC-', FORMAT(s.enrollment_date, 'yyyy'), '-', UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), s.id), '-', ''), 8))),
        s.id,
        'IDR',
        'active',
        s.enrollment_date,
        SYSUTCDATETIME(),
        'MIGRATION_BACKFILL'
    FROM [students].[students] s
    WHERE NOT EXISTS (
        SELECT 1 FROM [finance].[student_financial_accounts] sfa WHERE sfa.student_id = s.id
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 9. Seed Core Financial Policy & Charge Rules (GAP-007, TD-06)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM [finance].[financial_policies]
    WHERE [policy_code] = N'FP-2026-001'
)
BEGIN
    INSERT INTO [finance].[financial_policies] (
        [id],
        [policy_code],
        [name],
        [category],
        [business_purpose],
        [applicable_context],
        [effective_date],
        [end_date],
        [policy_version],
        [status],
        [approving_authority],
        [approved_date],
        [supersedes_policy_id],
        [notes],
        [created_at],
        [created_by]
    ) VALUES (
        '00000000-0000-0000-0002-000000000001',
        N'FP-2026-001',
        N'Kebijakan Biaya Pendidikan TP 2026/2027',
        N'tuition',
        N'Menetapkan struktur biaya pendaftaran, uang pangkal, dan SPP bulanan untuk seluruh jenjang TP 2026/2027.',
        N'Tahun Pelajaran 2026/2027',
        '2026-01-15',
        NULL,
        N'1.0',
        N'active',
        N'Direktur Keuangan',
        '2026-01-15',
        NULL,
        N'GAP-06 Core financial policy seed',
        SYSUTCDATETIME(),
        N'SYSTEM_SEED'
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM [finance].[charge_rules]
    WHERE [rule_code] = N'CR-2026-APP'
)
BEGIN
    INSERT INTO [finance].[charge_rules] (
        [id],
        [rule_code],
        [policy_id],
        [name],
        [charge_type],
        [is_mandatory],
        [determination_method],
        [base_amount],
        [currency_code],
        [recurrence_pattern],
        [due_rule_type],
        [due_rule_value],
        [applicable_grade],
        [applicable_program],
        [status],
        [notes],
        [created_at],
        [created_by]
    ) VALUES (
        '00000000-0000-0000-0002-000000000002',
        N'CR-2026-APP',
        '00000000-0000-0000-0002-000000000001',
        N'Biaya Formulir Pendaftaran Siswa Baru',
        N'admission_fee',
        1,
        N'fixed_amount',
        500000,
        'IDR',
        N'one_time',
        N'days_after_issuance',
        N'14',
        NULL,
        NULL,
        N'active',
        N'GAP-06 Application Fee core seed',
        SYSUTCDATETIME(),
        N'SYSTEM_SEED'
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM [finance].[charge_rules]
    WHERE [rule_code] = N'CR-2026-ENR'
)
BEGIN
    INSERT INTO [finance].[charge_rules] (
        [id],
        [rule_code],
        [policy_id],
        [name],
        [charge_type],
        [is_mandatory],
        [determination_method],
        [base_amount],
        [currency_code],
        [recurrence_pattern],
        [due_rule_type],
        [due_rule_value],
        [applicable_grade],
        [applicable_program],
        [status],
        [notes],
        [created_at],
        [created_by]
    ) VALUES (
        '00000000-0000-0000-0002-000000000003',
        N'CR-2026-ENR',
        '00000000-0000-0000-0002-000000000001',
        N'Uang Pangkal Masuk Siswa Baru',
        N'enrollment_fee',
        1,
        N'fixed_amount',
        15000000,
        'IDR',
        N'one_time',
        N'days_after_issuance',
        N'14',
        NULL,
        NULL,
        N'active',
        N'GAP-06 Enrollment Fee core seed',
        SYSUTCDATETIME(),
        N'SYSTEM_SEED'
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM [finance].[charge_rules]
    WHERE [rule_code] = N'CR-2026-SPP-SD'
)
BEGIN
    INSERT INTO [finance].[charge_rules] (
        [id],
        [rule_code],
        [policy_id],
        [name],
        [charge_type],
        [is_mandatory],
        [determination_method],
        [base_amount],
        [currency_code],
        [recurrence_pattern],
        [due_rule_type],
        [due_rule_value],
        [applicable_grade],
        [applicable_program],
        [status],
        [notes],
        [created_at],
        [created_by]
    ) VALUES (
        '00000000-0000-0000-0002-000000000004',
        N'CR-2026-SPP-SD',
        '00000000-0000-0000-0002-000000000001',
        N'SPP Bulanan SD Reguler',
        N'tuition',
        1,
        N'fixed_amount',
        750000,
        'IDR',
        N'monthly',
        N'fixed_day_of_month',
        N'10',
        NULL,
        N'SD Reguler',
        N'active',
        N'GAP-06 Tuition Fee core seed',
        SYSUTCDATETIME(),
        N'SYSTEM_SEED'
    );
END;
GO
