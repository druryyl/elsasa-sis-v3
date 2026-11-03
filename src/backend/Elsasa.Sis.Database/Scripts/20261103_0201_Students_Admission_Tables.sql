-- =============================================================================
-- 20261103_0201_Students_Admission_Tables.sql
-- Students domain Stage 1 Admission persistence evolution.
-- 1. Non-destructively alters students.applicants to add full_name (with backfill),
--    biographical details, government identity, address, prior school, and audit fields.
-- 2. Non-destructively alters students.admission_applications to add admission_period_id,
--    application_type, requested_entry_level, placement preferences, registration_date,
--    and audit fields.
-- 3. Provisions sequence students.admission_application_number_seq.
-- 4. Provisions 4 domain tables:
--    - students.admission_decisions
--    - students.applicant_documents
--    - students.entrance_assessment_evidence
--    - students.admission_application_contacts
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Alter students.applicants
-- -----------------------------------------------------------------------------

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'full_name'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [full_name] NVARCHAR(200) NOT NULL CONSTRAINT [df_students_applicants_full_name] DEFAULT (N'');

    EXEC(N'UPDATE [students].[applicants]
          SET [full_name] = LTRIM(RTRIM(CONCAT(ISNULL([first_name], ''''), '' '', ISNULL([last_name], ''''))))
          WHERE [full_name] = '''';');

    ALTER TABLE [students].[applicants]
        DROP CONSTRAINT [df_students_applicants_full_name];
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'preferred_name'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [preferred_name] NVARCHAR(100) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'place_of_birth'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [place_of_birth] NVARCHAR(100) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'nationality'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [nationality] NVARCHAR(100) NULL CONSTRAINT [df_students_applicants_nationality] DEFAULT (N'Indonesian');
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'government_id_type'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [government_id_type] NVARCHAR(32) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'government_id_number'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [government_id_number] NVARCHAR(64) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'address'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [address] NVARCHAR(MAX) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'previous_school_name'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [previous_school_name] NVARCHAR(200) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'previous_school_address'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [previous_school_address] NVARCHAR(MAX) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'notes'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [notes] NVARCHAR(MAX) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'updated_at'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [updated_at] DATETIMEOFFSET(7) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'updated_by'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    ALTER TABLE [students].[applicants]
        ADD [updated_by] NVARCHAR(100) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_students_applicants_gov_id'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    CREATE UNIQUE INDEX [ux_students_applicants_gov_id]
        ON [students].[applicants] ([government_id_number])
        WHERE [government_id_number] IS NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_applicants_name_dob'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    CREATE INDEX [ix_students_applicants_name_dob]
        ON [students].[applicants] ([full_name], [date_of_birth]);
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Alter students.admission_applications
-- -----------------------------------------------------------------------------

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'admission_period_id'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    ALTER TABLE [students].[admission_applications]
        ADD [admission_period_id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [df_students_admission_applications_admission_period_id] DEFAULT ('00000000-0000-0000-0000-000000000000');

    ALTER TABLE [students].[admission_applications]
        DROP CONSTRAINT [df_students_admission_applications_admission_period_id];
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'application_type'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    ALTER TABLE [students].[admission_applications]
        ADD [application_type] NVARCHAR(32) NOT NULL CONSTRAINT [df_students_admission_applications_application_type] DEFAULT (N'new');
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'requested_entry_level'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    ALTER TABLE [students].[admission_applications]
        ADD [requested_entry_level] NVARCHAR(64) NOT NULL CONSTRAINT [df_students_admission_applications_requested_entry_level] DEFAULT (N'');

    ALTER TABLE [students].[admission_applications]
        DROP CONSTRAINT [df_students_admission_applications_requested_entry_level];
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'requested_campus'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    ALTER TABLE [students].[admission_applications]
        ADD [requested_campus] NVARCHAR(100) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'requested_program'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    ALTER TABLE [students].[admission_applications]
        ADD [requested_program] NVARCHAR(100) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'registration_date'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    ALTER TABLE [students].[admission_applications]
        ADD [registration_date] DATE NOT NULL CONSTRAINT [df_students_admission_applications_registration_date] DEFAULT (CAST(GETUTCDATE() AS DATE));
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'notes'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    ALTER TABLE [students].[admission_applications]
        ADD [notes] NVARCHAR(MAX) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'updated_at'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    ALTER TABLE [students].[admission_applications]
        ADD [updated_at] DATETIMEOFFSET(7) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE [name] = N'updated_by'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    ALTER TABLE [students].[admission_applications]
        ADD [updated_by] NVARCHAR(100) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    JOIN sys.columns c
      ON dc.[parent_object_id] = c.[object_id]
     AND dc.[parent_column_id] = c.[column_id]
    WHERE dc.[parent_object_id] = OBJECT_ID(N'students.admission_applications')
      AND c.[name] = N'status'
)
BEGIN
    ALTER TABLE [students].[admission_applications]
        ADD CONSTRAINT [df_students_admission_applications_status] DEFAULT (N'registered') FOR [status];
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_students_admission_applications_active'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    CREATE UNIQUE INDEX [ux_students_admission_applications_active]
        ON [students].[admission_applications] ([applicant_id], [admission_period_id])
        WHERE [status] <> N'withdrawn' AND [status] <> N'decided';
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_admission_applications_period_status'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    CREATE INDEX [ix_students_admission_applications_period_status]
        ON [students].[admission_applications] ([admission_period_id], [status]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_admission_applications_reg_date'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    CREATE INDEX [ix_students_admission_applications_reg_date]
        ON [students].[admission_applications] ([registration_date] DESC);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Sequence: students.admission_application_number_seq
-- -----------------------------------------------------------------------------

IF NOT EXISTS
(
    SELECT 1
    FROM sys.sequences
    WHERE [name] = N'admission_application_number_seq'
      AND [schema_id] = SCHEMA_ID(N'students')
)
BEGIN
    CREATE SEQUENCE [students].[admission_application_number_seq]
        AS INT
        START WITH 1
        INCREMENT BY 1
        NO CYCLE;
END;
GO

-- -----------------------------------------------------------------------------
-- 4. students.admission_decisions
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.admission_decisions', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[admission_decisions]
    (
        [id]                           UNIQUEIDENTIFIER  NOT NULL,
        [application_id]               UNIQUEIDENTIFIER  NOT NULL,
        [decision_type]                NVARCHAR(32)      NOT NULL,
        [decision_date]                DATE              NOT NULL,
        [effective_date]               DATE              NOT NULL,
        [decided_by]                   NVARCHAR(100)     NOT NULL,
        [reason]                       NVARCHAR(MAX)     NULL,
        [supporting_reference]         NVARCHAR(200)     NULL,
        [deferred_admission_period_id] UNIQUEIDENTIFIER  NULL,
        [created_at]                   DATETIMEOFFSET(7) NOT NULL,
        [created_by]                   NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_admission_decisions] PRIMARY KEY ([id]),
        CONSTRAINT [ux_students_admission_decisions_application_id] UNIQUE ([application_id]),
        CONSTRAINT [fk_students_admission_decisions_applications]
            FOREIGN KEY ([application_id]) REFERENCES [students].[admission_applications] ([id])
            ON DELETE NO ACTION
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 5. students.applicant_documents
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.applicant_documents', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[applicant_documents]
    (
        [id]                UNIQUEIDENTIFIER  NOT NULL,
        [application_id]    UNIQUEIDENTIFIER  NOT NULL,
        [document_type]     NVARCHAR(100)     NOT NULL,
        [submission_status] NVARCHAR(32)      NOT NULL CONSTRAINT [df_students_applicant_documents_submission_status] DEFAULT (N'not_submitted'),
        [submitted_date]    DATE              NULL,
        [verified_date]     DATE              NULL,
        [verified_by]       NVARCHAR(100)     NULL,
        [file_reference]    NVARCHAR(500)     NULL,
        [file_name]         NVARCHAR(255)     NULL,
        [file_size_bytes]   BIGINT            NULL,
        [content_type]      NVARCHAR(100)     NULL,
        [notes]             NVARCHAR(MAX)     NULL,
        [created_at]        DATETIMEOFFSET(7) NOT NULL,
        [created_by]        NVARCHAR(100)     NULL,
        [updated_at]        DATETIMEOFFSET(7) NULL,
        [updated_by]        NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_applicant_documents] PRIMARY KEY ([id]),
        CONSTRAINT [fk_students_applicant_documents_applications]
            FOREIGN KEY ([application_id]) REFERENCES [students].[admission_applications] ([id])
            ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_applicant_documents_app_status'
      AND [object_id] = OBJECT_ID(N'students.applicant_documents')
)
BEGIN
    CREATE INDEX [ix_students_applicant_documents_app_status]
        ON [students].[applicant_documents] ([application_id], [submission_status]);
END;
GO

-- -----------------------------------------------------------------------------
-- 6. students.entrance_assessment_evidence
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.entrance_assessment_evidence', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[entrance_assessment_evidence]
    (
        [id]              UNIQUEIDENTIFIER  NOT NULL,
        [application_id]  UNIQUEIDENTIFIER  NOT NULL,
        [assessment_type] NVARCHAR(100)     NOT NULL,
        [score]           NVARCHAR(50)      NULL,
        [source]          NVARCHAR(100)     NOT NULL CONSTRAINT [df_students_assessment_evidence_source] DEFAULT (N'External Assessment Center'),
        [assessment_date] DATE              NULL,
        [notes]           NVARCHAR(MAX)     NULL,
        [created_at]      DATETIMEOFFSET(7) NOT NULL,
        [created_by]      NVARCHAR(100)     NULL,
        [updated_at]      DATETIMEOFFSET(7) NULL,
        [updated_by]      NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_entrance_assessment_evidence] PRIMARY KEY ([id]),
        CONSTRAINT [fk_students_assessment_evidence_applications]
            FOREIGN KEY ([application_id]) REFERENCES [students].[admission_applications] ([id])
            ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_assessment_evidence_app'
      AND [object_id] = OBJECT_ID(N'students.entrance_assessment_evidence')
)
BEGIN
    CREATE INDEX [ix_students_assessment_evidence_app]
        ON [students].[entrance_assessment_evidence] ([application_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 7. students.admission_application_contacts
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.admission_application_contacts', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[admission_application_contacts]
    (
        [id]             UNIQUEIDENTIFIER  NOT NULL,
        [application_id] UNIQUEIDENTIFIER  NOT NULL,
        [full_name]      NVARCHAR(200)     NOT NULL,
        [relationship]   NVARCHAR(50)      NOT NULL,
        [phone]          NVARCHAR(32)      NULL,
        [email]          NVARCHAR(254)     NULL,
        [address]        NVARCHAR(MAX)     NULL,
        [is_primary]     BIT               NOT NULL CONSTRAINT [df_students_application_contacts_is_primary] DEFAULT (1),
        [created_at]     DATETIMEOFFSET(7) NOT NULL,
        [created_by]     NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_admission_application_contacts] PRIMARY KEY ([id]),
        CONSTRAINT [fk_students_application_contacts_applications]
            FOREIGN KEY ([application_id]) REFERENCES [students].[admission_applications] ([id])
            ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_application_contacts_app'
      AND [object_id] = OBJECT_ID(N'students.admission_application_contacts')
)
BEGIN
    CREATE INDEX [ix_students_application_contacts_app]
        ON [students].[admission_application_contacts] ([application_id]);
END;
GO
