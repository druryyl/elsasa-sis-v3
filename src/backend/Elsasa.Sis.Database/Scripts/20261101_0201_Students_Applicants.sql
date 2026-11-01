-- =============================================================================
-- 20261101_0201_Students_Applicants.sql
-- students.applicants: prospective students owned by the Students domain.
-- Contact details are nullable at intake; first/last name identify the applicant.
-- =============================================================================

IF OBJECT_ID(N'students.applicants', N'U') IS NULL
BEGIN
    CREATE TABLE [students].[applicants]
    (
        [id]            UNIQUEIDENTIFIER NOT NULL,
        [first_name]    NVARCHAR(100)    NOT NULL,
        [last_name]     NVARCHAR(100)    NOT NULL,
        [date_of_birth] DATE             NULL,
        [gender]        NVARCHAR(20)     NULL,
        [email]         NVARCHAR(254)    NULL,
        [phone]         NVARCHAR(32)     NULL,
        [created_at]    DATETIMEOFFSET(7) NOT NULL,
        [created_by]    NVARCHAR(100)    NULL,
        CONSTRAINT [pk_students_applicants] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'ix_students_applicants_last_name_first_name'
      AND [object_id] = OBJECT_ID(N'students.applicants')
)
BEGIN
    CREATE INDEX [ix_students_applicants_last_name_first_name]
        ON [students].[applicants] ([last_name], [first_name]);
END;
GO
