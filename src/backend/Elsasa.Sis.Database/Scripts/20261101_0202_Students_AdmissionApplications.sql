-- =============================================================================
-- 20261101_0202_Students_AdmissionApplications.sql
-- students.admission_applications: applications submitted by applicants.
-- applicant_id is an intra-schema foreign key (enforced).
-- academic_year_id is a logical UUID reference to operations.academic_years with
-- NO physical cross-schema foreign key (ADR-002).
-- =============================================================================

IF OBJECT_ID(N'students.admission_applications', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[admission_applications]
    (
        [id]                 UNIQUEIDENTIFIER NOT NULL,
        [applicant_id]       UNIQUEIDENTIFIER NOT NULL,
        [academic_year_id]   UNIQUEIDENTIFIER NOT NULL,
        [application_number] NVARCHAR(32)     NOT NULL,
        [status]             NVARCHAR(32)     NOT NULL,
        [submitted_at]       DATETIMEOFFSET(7) NULL,
        [created_at]         DATETIMEOFFSET(7) NOT NULL,
        [created_by]         NVARCHAR(100)    NULL,
        CONSTRAINT [pk_students_admission_applications] PRIMARY KEY ([id]),
        CONSTRAINT [fk_students_admission_applications_applicants]
            FOREIGN KEY ([applicant_id]) REFERENCES [students].[applicants] ([id])
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ux_students_admission_applications_application_number'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    CREATE UNIQUE INDEX [ux_students_admission_applications_application_number]
        ON [students].[admission_applications] ([application_number]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_admission_applications_applicant_id'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    CREATE INDEX [ix_students_admission_applications_applicant_id]
        ON [students].[admission_applications] ([applicant_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_admission_applications_academic_year_id'
      AND [object_id] = OBJECT_ID(N'students.admission_applications')
)
BEGIN
    CREATE INDEX [ix_students_admission_applications_academic_year_id]
        ON [students].[admission_applications] ([academic_year_id]);
END;
GO
