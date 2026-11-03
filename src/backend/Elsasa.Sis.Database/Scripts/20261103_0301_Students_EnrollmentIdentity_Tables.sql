-- =============================================================================
-- 20261103_0301_Students_EnrollmentIdentity_Tables.sql
-- Students domain Stage 1 Enrollment & Identity persistence evolution.
-- 1. Provisions sequence students.student_number_seq starting at 1.
-- 2. Provisions 5 domain tables:
--    - students.students (Permanent student identity continuity, 1:1 applicant link)
--    - students.enrollments (Administrative enrollment placement & payment audit log)
--    - students.guardians (Master directory of parents and legal guardians)
--    - students.student_guardian_relationships (Association with responsibility flags & single active primary contact constraint)
--    - students.student_documents (Certified official student documents lifecycle)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Sequence: students.student_number_seq
-- -----------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE object_id = OBJECT_ID(N'students.student_number_seq'))
BEGIN
    CREATE SEQUENCE [students].[student_number_seq]
        AS INT
        START WITH 1
        INCREMENT BY 1
        NO CYCLE;
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Table: students.students
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.students', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[students]
    (
        [id]                   UNIQUEIDENTIFIER  NOT NULL,
        [student_number]       NVARCHAR(32)      NOT NULL,
        [applicant_id]         UNIQUEIDENTIFIER  NOT NULL,
        [full_name]            NVARCHAR(200)     NOT NULL,
        [preferred_name]       NVARCHAR(100)     NULL,
        [date_of_birth]        DATE              NULL,
        [place_of_birth]       NVARCHAR(100)     NULL,
        [gender]               NVARCHAR(20)      NULL,
        [nationality]          NVARCHAR(100)     NULL CONSTRAINT [df_students_students_nationality] DEFAULT (N'Indonesian'),
        [government_id_type]   NVARCHAR(32)      NULL,
        [government_id_number] NVARCHAR(64)      NULL,
        [address]              NVARCHAR(MAX)     NULL,
        [phone]                NVARCHAR(32)      NULL,
        [email]                NVARCHAR(254)     NULL,
        [is_independent]       BIT               NOT NULL CONSTRAINT [df_students_students_is_independent] DEFAULT (0),
        [current_status]       NVARCHAR(32)      NOT NULL CONSTRAINT [df_students_students_current_status] DEFAULT (N'enrolled'),
        [enrollment_date]      DATE              NOT NULL,
        [notes]                NVARCHAR(MAX)     NULL,
        [created_at]           DATETIMEOFFSET(7) NOT NULL,
        [created_by]           NVARCHAR(100)     NULL,
        [updated_at]           DATETIMEOFFSET(7) NULL,
        [updated_by]           NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_students] PRIMARY KEY ([id]),
        CONSTRAINT [ux_students_students_student_number] UNIQUE ([student_number]),
        CONSTRAINT [ux_students_students_applicant_id] UNIQUE ([applicant_id]),
        CONSTRAINT [fk_students_students_applicants]
            FOREIGN KEY ([applicant_id]) REFERENCES [students].[applicants] ([id])
            ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_students_students_gov_id'
      AND [object_id] = OBJECT_ID(N'students.students')
)
BEGIN
    CREATE UNIQUE INDEX [ux_students_students_gov_id]
        ON [students].[students] ([government_id_number])
        WHERE [government_id_number] IS NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_students_status_name'
      AND [object_id] = OBJECT_ID(N'students.students')
)
BEGIN
    CREATE INDEX [ix_students_students_status_name]
        ON [students].[students] ([current_status], [full_name]);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: students.enrollments
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.enrollments', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[enrollments]
    (
        [id]                          UNIQUEIDENTIFIER  NOT NULL,
        [student_id]                  UNIQUEIDENTIFIER  NOT NULL,
        [application_id]              UNIQUEIDENTIFIER  NOT NULL,
        [enrollment_type]             NVARCHAR(32)      NOT NULL CONSTRAINT [df_students_enrollments_type] DEFAULT (N'initial'),
        [enrollment_date]             DATE              NOT NULL,
        [initial_campus]              NVARCHAR(100)     NOT NULL,
        [initial_level]               NVARCHAR(64)      NOT NULL,
        [initial_academic_year]       NVARCHAR(32)      NOT NULL,
        [initial_program]             NVARCHAR(100)     NULL,
        [initial_cohort]              NVARCHAR(50)      NULL,
        [approved_by]                 NVARCHAR(100)     NOT NULL,
        [enrollment_payment_confirmed] BIT              NULL,
        [payment_reference]           NVARCHAR(100)     NULL,
        [payment_waived]              BIT               NOT NULL CONSTRAINT [df_students_enrollments_payment_waived] DEFAULT (0),
        [waiver_reference]            NVARCHAR(100)     NULL,
        [notes]                       NVARCHAR(MAX)     NULL,
        [created_at]                  DATETIMEOFFSET(7) NOT NULL,
        [created_by]                  NVARCHAR(100)     NULL,
        [updated_at]                  DATETIMEOFFSET(7) NULL,
        [updated_by]                  NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_enrollments] PRIMARY KEY ([id]),
        CONSTRAINT [fk_students_enrollments_student]
            FOREIGN KEY ([student_id]) REFERENCES [students].[students] ([id])
            ON DELETE NO ACTION,
        CONSTRAINT [fk_students_enrollments_application]
            FOREIGN KEY ([application_id]) REFERENCES [students].[admission_applications] ([id])
            ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_enrollments_student_date'
      AND [object_id] = OBJECT_ID(N'students.enrollments')
)
BEGIN
    CREATE INDEX [ix_students_enrollments_student_date]
        ON [students].[enrollments] ([student_id], [enrollment_date] DESC);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_enrollments_application'
      AND [object_id] = OBJECT_ID(N'students.enrollments')
)
BEGIN
    CREATE INDEX [ix_students_enrollments_application]
        ON [students].[enrollments] ([application_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Table: students.guardians
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.guardians', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[guardians]
    (
        [id]                   UNIQUEIDENTIFIER  NOT NULL,
        [full_name]            NVARCHAR(200)     NOT NULL,
        [government_id_type]   NVARCHAR(32)      NULL,
        [government_id_number] NVARCHAR(64)      NULL,
        [phone]                NVARCHAR(32)      NULL,
        [email]                NVARCHAR(254)     NULL,
        [address]              NVARCHAR(MAX)     NULL,
        [occupation]           NVARCHAR(100)     NULL,
        [notes]                NVARCHAR(MAX)     NULL,
        [created_at]           DATETIMEOFFSET(7) NOT NULL,
        [created_by]           NVARCHAR(100)     NULL,
        [updated_at]           DATETIMEOFFSET(7) NULL,
        [updated_by]           NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_guardians] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_students_guardians_gov_id'
      AND [object_id] = OBJECT_ID(N'students.guardians')
)
BEGIN
    CREATE UNIQUE INDEX [ux_students_guardians_gov_id]
        ON [students].[guardians] ([government_id_number])
        WHERE [government_id_number] IS NOT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_guardians_name'
      AND [object_id] = OBJECT_ID(N'students.guardians')
)
BEGIN
    CREATE INDEX [ix_students_guardians_name]
        ON [students].[guardians] ([full_name]);
END;
GO

-- -----------------------------------------------------------------------------
-- 5. Table: students.student_guardian_relationships
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.student_guardian_relationships', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[student_guardian_relationships]
    (
        [id]                    UNIQUEIDENTIFIER  NOT NULL,
        [student_id]            UNIQUEIDENTIFIER  NOT NULL,
        [guardian_id]           UNIQUEIDENTIFIER  NOT NULL,
        [relationship_type]     NVARCHAR(50)      NOT NULL,
        [is_primary_contact]    BIT               NOT NULL CONSTRAINT [df_students_rel_is_primary] DEFAULT (0),
        [is_emergency_contact]  BIT               NOT NULL CONSTRAINT [df_students_rel_is_emergency] DEFAULT (0),
        [is_financial_contact]  BIT               NOT NULL CONSTRAINT [df_students_rel_is_financial] DEFAULT (0),
        [is_authorized_pickup]  BIT               NOT NULL CONSTRAINT [df_students_rel_is_pickup] DEFAULT (0),
        [is_active]             BIT               NOT NULL CONSTRAINT [df_students_rel_is_active] DEFAULT (1),
        [created_at]            DATETIMEOFFSET(7) NOT NULL,
        [created_by]            NVARCHAR(100)     NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_student_guardian_relationships] PRIMARY KEY ([id]),
        CONSTRAINT [fk_students_rel_student]
            FOREIGN KEY ([student_id]) REFERENCES [students].[students] ([id])
            ON DELETE NO ACTION,
        CONSTRAINT [fk_students_rel_guardian]
            FOREIGN KEY ([guardian_id]) REFERENCES [students].[guardians] ([id])
            ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_students_rel_primary_active'
      AND [object_id] = OBJECT_ID(N'students.student_guardian_relationships')
)
BEGIN
    CREATE UNIQUE INDEX [ux_students_rel_primary_active]
        ON [students].[student_guardian_relationships] ([student_id])
        WHERE ([is_primary_contact] = 1 AND [is_active] = 1);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_rel_student_guardian'
      AND [object_id] = OBJECT_ID(N'students.student_guardian_relationships')
)
BEGIN
    CREATE INDEX [ix_students_rel_student_guardian]
        ON [students].[student_guardian_relationships] ([student_id], [guardian_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_rel_guardian_active'
      AND [object_id] = OBJECT_ID(N'students.student_guardian_relationships')
)
BEGIN
    CREATE INDEX [ix_students_rel_guardian_active]
        ON [students].[student_guardian_relationships] ([guardian_id], [is_active]);
END;
GO

-- -----------------------------------------------------------------------------
-- 6. Table: students.student_documents
-- -----------------------------------------------------------------------------

IF OBJECT_ID(N'students.student_documents', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[student_documents]
    (
        [id]                UNIQUEIDENTIFIER  NOT NULL,
        [student_id]        UNIQUEIDENTIFIER  NOT NULL,
        [document_type]     NVARCHAR(100)     NOT NULL,
        [submission_status] NVARCHAR(32)      NOT NULL CONSTRAINT [df_students_docs_status] DEFAULT (N'submitted'),
        [submitted_date]    DATE              NULL,
        [verified_date]     DATE              NULL,
        [verified_by]       NVARCHAR(100)     NULL,
        [file_reference]    NVARCHAR(500)     NULL,
        [file_name]         NVARCHAR(255)     NULL,
        [file_size_bytes]   BIGINT            NULL,
        [content_type]      NVARCHAR(100)     NULL,
        [valid_until]       DATE              NULL,
        [notes]             NVARCHAR(MAX)     NULL,
        [created_at]        DATETIMEOFFSET(7) NOT NULL,
        [created_by]        NVARCHAR(100)     NULL,
        [updated_at]        DATETIMEOFFSET(7) NULL,
        [updated_by]        NVARCHAR(100)     NULL,
        CONSTRAINT [pk_students_student_documents] PRIMARY KEY ([id]),
        CONSTRAINT [fk_students_student_documents_student]
            FOREIGN KEY ([student_id]) REFERENCES [students].[students] ([id])
            ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_student_documents_student_status'
      AND [object_id] = OBJECT_ID(N'students.student_documents')
)
BEGIN
    CREATE INDEX [ix_students_student_documents_student_status]
        ON [students].[student_documents] ([student_id], [submission_status]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_docs_student_type'
      AND [object_id] = OBJECT_ID(N'students.student_documents')
)
BEGIN
    CREATE INDEX [ix_students_docs_student_type]
        ON [students].[student_documents] ([student_id], [document_type]);
END;
GO
