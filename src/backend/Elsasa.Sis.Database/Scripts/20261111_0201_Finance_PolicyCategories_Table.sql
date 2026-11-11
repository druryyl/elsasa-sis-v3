-- =============================================================================
-- 20261111_0201_Finance_PolicyCategories_Table.sql
-- Finance domain Financial Policy Category master reference table (IS017 / FIN-POLICY-CATEGORY).
-- 1. Provisions finance.policy_categories with natural key [code] NVARCHAR(64)
--    matching finance.financial_policies.category NVARCHAR(64).
-- 2. Idempotently seeds the 8 canonical policy categories.
-- 3. Normalizes legacy category values in finance.financial_policies before
--    applying the foreign key constraint.
-- 4. Enforces intra-schema foreign key fk_finance_financial_policies_category.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Table: finance.policy_categories
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.policy_categories', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[policy_categories] (
        [code]          NVARCHAR(64)      NOT NULL,
        [name]          NVARCHAR(100)     NOT NULL,
        [description]   NVARCHAR(255)     NULL,
        [display_order] INT               NOT NULL CONSTRAINT [df_policy_categories_display_order] DEFAULT (0),
        [is_active]     BIT               NOT NULL CONSTRAINT [df_policy_categories_active] DEFAULT (1),
        [created_at]    DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_policy_categories_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]    NVARCHAR(100)     NULL,
        [updated_at]    DATETIMEOFFSET(7) NULL,
        [updated_by]    NVARCHAR(100)     NULL,
        CONSTRAINT [pk_finance_policy_categories] PRIMARY KEY CLUSTERED ([code])
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Seed 8 canonical policy categories (idempotent)
-- -----------------------------------------------------------------------------
INSERT INTO [finance].[policy_categories] (
    [code],
    [name],
    [description],
    [display_order],
    [is_active],
    [created_at],
    [created_by]
)
SELECT
    seed.[code],
    seed.[name],
    seed.[description],
    seed.[display_order],
    1,
    SYSUTCDATETIME(),
    N'SYSTEM_SEED'
FROM (
    VALUES
        (N'tuition',               N'Tuition (SPP)',                                N'Regular academic and instructional fees',                                        1),
        (N'admission_fee',         N'Admission Fee (Uang Pangkal)',                N'One-time enrollment and registration fees',                                      2),
        (N'facilities',            N'Facilities (Sarana & Prasarana)',             N'Campus development, building, and facility maintenance fees',                    3),
        (N'activities',            N'Activities (Kegiatan Siswa)',                 N'Co-curricular activities, field trips, and student events',                      4),
        (N'general_fee',           N'General Institutional Fee (Biaya Umum)',      N'Institutional and operational charges including materials and books',            5),
        (N'scholarship_guideline', N'Scholarship Guideline (Panduan Beasiswa)',    N'Scholarship and institutional financial aid policies',                           6),
        (N'refund_rule',           N'Refund Rule (Ketentuan Pengembalian Dana)',   N'Policies governing withdrawal, overpayment, and fee refund calculations',        7),
        (N'clearance_rule',        N'Clearance Rule (Ketentuan Bebas Keuangan)',   N'Financial clearance requirements for examination and graduation eligibility',    8)
) AS seed ([code], [name], [description], [display_order])
WHERE NOT EXISTS (
    SELECT 1
    FROM [finance].[policy_categories] AS target
    WHERE target.[code] = seed.[code]
);
GO

-- -----------------------------------------------------------------------------
-- 3. Pre-FK data normalization on finance.financial_policies
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.financial_policies', N'U') IS NOT NULL
BEGIN
    UPDATE [finance].[financial_policies]
    SET [category] = N'general_fee'
    WHERE [category] = N'general'
       OR [category] COLLATE Latin1_General_CS_AS = N'GENERAL_FEE';

    UPDATE [finance].[financial_policies]
    SET [category] = N'admission_fee'
    WHERE [category] = N'admission';
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Intra-schema Foreign Key: finance.financial_policies -> finance.policy_categories
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'finance.financial_policies', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE [name] = N'fk_finance_financial_policies_category'
          AND [parent_object_id] = OBJECT_ID(N'finance.financial_policies')
    )
    BEGIN
        ALTER TABLE [finance].[financial_policies]
            ADD CONSTRAINT [fk_finance_financial_policies_category]
            FOREIGN KEY ([category])
            REFERENCES [finance].[policy_categories] ([code])
            ON DELETE NO ACTION;
    END;
END;
GO
