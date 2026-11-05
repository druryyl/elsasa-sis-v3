-- =============================================================================
-- 20261105_0402_Academics_AssessmentResults_Tables.sql
-- Academics domain Stage 2 Assessment & Results persistence schema
-- (IS010 / CR-ACD-ASSESSMENT-RESULTS, ADR-002, ADR-006, P1-S01).
--
-- 1. Idempotently ensures isolated schema [academics] exists.
-- 2. Provisions 7 authoritative domain tables in schema [academics]:
--    - academics.assessments: Planned and conducted evaluation events, assessment types,
--      methods, scoring specs (max_score, passing_score), reassessment policies, and status.
--    - academics.assessment_evidence: Raw learning evidence, student/applicant dual reference,
--      evidence types, submission references, and timestamps.
--    - academics.academic_results: Primary academic evaluation records, multi-attempt counters,
--      previous attempt links, operative flags (is_final), scoring models, and lifecycle statuses.
--    - academics.academic_record_corrections: Unified immutable correction audit log capturing
--      pre/post-mutation states, mandatory reason text, authorizer identity, and materiality.
--    - academics.report_cards: Periodic academic reports for active periods (mid_term, end_of_term),
--      template styles, statuses, teacher narrative comments, and immutable JSON content_snapshot.
--    - academics.report_card_entries: Linking report cards to constituent approved academic results
--      and subject-specific teacher remarks.
--    - academics.transcripts: Cumulative academic history documents, covered date ranges,
--      issuance purpose, approval metadata, and frozen JSON content_snapshot.
-- 3. Provisions primary keys, intra-schema foreign keys (with zero cross-schema physical FKs),
--    composite check constraints, candidate CHECK constraints, and performance indexes.
-- 4. Preserves zero physical foreign keys across domain schemas (ADR-001, ADR-002).
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 0. Schema: academics
-- -----------------------------------------------------------------------------
IF SCHEMA_ID(N'academics') IS NULL
BEGIN
    EXEC (N'CREATE SCHEMA [academics] AUTHORIZATION [dbo];');
END;
GO

-- -----------------------------------------------------------------------------
-- 1. Table: academics.assessments
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.assessments', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[assessments] (
        [id]                    UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_assessments_id] DEFAULT (NEWID()),
        [learning_unit_id]      UNIQUEIDENTIFIER  NOT NULL,
        [learning_activity_id]  UNIQUEIDENTIFIER  NULL,
        [title]                 NVARCHAR(200)     NOT NULL,
        [assessment_type]       NVARCHAR(50)      NOT NULL,
        [assessment_method]     NVARCHAR(50)      NOT NULL,
        [max_score]             DECIMAL(5, 2)     NULL,
        [passing_score]         DECIMAL(5, 2)     NULL,     -- Minimum passing score / mastery threshold (KKM)
        [reassessment_policy]   NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_assessments_reassessment_policy] DEFAULT ('highest_score'),
        [criteria]              NVARCHAR(MAX)     NULL,     -- Assessment criteria or rubric descriptor JSON
        [assessment_date]       DATE              NULL,
        [assessor_reference]    NVARCHAR(200)     NULL,     -- Provisional assessor / teacher context
        [status]                NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_assessments_status] DEFAULT ('planned'),
        [notes]                 NVARCHAR(MAX)     NULL,
        [created_at]            DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_assessments_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]            NVARCHAR(256)     NOT NULL,
        [updated_at]            DATETIMEOFFSET(7) NULL,
        [updated_by]            NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_assessments] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_assessments_unit] FOREIGN KEY ([learning_unit_id])
            REFERENCES [academics].[learning_units] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_academics_assessments_activity] FOREIGN KEY ([learning_activity_id])
            REFERENCES [academics].[learning_activities] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_assessments_type]
            CHECK ([assessment_type] IN ('formative', 'summative', 'diagnostic', 'portfolio', 'project', 'other')),
        CONSTRAINT [CK_academics_assessments_method]
            CHECK ([assessment_method] IN ('written_test', 'oral', 'practical', 'project', 'portfolio', 'observation', 'other')),
        CONSTRAINT [CK_academics_assessments_reassessment_policy]
            CHECK ([reassessment_policy] IN ('capped_at_passing', 'highest_score', 'latest_attempt', 'average', 'none')),
        CONSTRAINT [CK_academics_assessments_status]
            CHECK ([status] IN ('planned', 'conducted', 'cancelled'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_assessments_unit_status'
      AND [object_id] = OBJECT_ID(N'academics.assessments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_assessments_unit_status]
        ON [academics].[assessments] ([learning_unit_id], [status]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_assessments_activity'
      AND [object_id] = OBJECT_ID(N'academics.assessments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_assessments_activity]
        ON [academics].[assessments] ([learning_activity_id])
        WHERE ([learning_activity_id] IS NOT NULL);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_assessments_date'
      AND [object_id] = OBJECT_ID(N'academics.assessments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_assessments_date]
        ON [academics].[assessments] ([assessment_date]);
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Table: academics.assessment_evidence
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.assessment_evidence', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[assessment_evidence] (
        [id]                  UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_assessment_evidence_id] DEFAULT (NEWID()),
        [assessment_id]       UNIQUEIDENTIFIER  NOT NULL,
        [student_id]          UNIQUEIDENTIFIER  NULL,     -- Enrolled student (logical reference to students.students)
        [applicant_id]        UNIQUEIDENTIFIER  NULL,     -- Admissions applicant (logical reference to students.applicants)
        [submission_id]       UNIQUEIDENTIFIER  NULL,     -- Optional reference to coursework submission
        [evidence_type]       NVARCHAR(50)      NOT NULL,
        [collected_date]      DATE              NOT NULL,
        [evidence_reference]  NVARCHAR(500)     NULL,     -- URL, file path, or observation note
        [notes]               NVARCHAR(MAX)     NULL,
        [created_at]          DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_assessment_evidence_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]          NVARCHAR(256)     NOT NULL,
        [updated_at]          DATETIMEOFFSET(7) NULL,
        [updated_by]          NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_assessment_evidence] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_assessment_evidence_assessment] FOREIGN KEY ([assessment_id])
            REFERENCES [academics].[assessments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_academics_assessment_evidence_submission] FOREIGN KEY ([submission_id])
            REFERENCES [academics].[submissions] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_assessment_evidence_candidate]
            CHECK (([student_id] IS NOT NULL AND [applicant_id] IS NULL) OR
                   ([student_id] IS NULL AND [applicant_id] IS NOT NULL)),
        CONSTRAINT [CK_academics_assessment_evidence_type]
            CHECK ([evidence_type] IN ('test_paper', 'oral_response', 'project_artifact', 'portfolio_item', 'observation', 'practical_demonstration', 'other'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_assessment_evidence_assessment'
      AND [object_id] = OBJECT_ID(N'academics.assessment_evidence')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_assessment_evidence_assessment]
        ON [academics].[assessment_evidence] ([assessment_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_assessment_evidence_student'
      AND [object_id] = OBJECT_ID(N'academics.assessment_evidence')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_assessment_evidence_student]
        ON [academics].[assessment_evidence] ([student_id], [collected_date])
        WHERE ([student_id] IS NOT NULL);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_assessment_evidence_applicant'
      AND [object_id] = OBJECT_ID(N'academics.assessment_evidence')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_assessment_evidence_applicant]
        ON [academics].[assessment_evidence] ([applicant_id], [collected_date])
        WHERE ([applicant_id] IS NOT NULL);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_assessment_evidence_submission'
      AND [object_id] = OBJECT_ID(N'academics.assessment_evidence')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_assessment_evidence_submission]
        ON [academics].[assessment_evidence] ([submission_id])
        WHERE ([submission_id] IS NOT NULL);
END;
GO

-- -----------------------------------------------------------------------------
-- 3. Table: academics.academic_results
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.academic_results', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[academic_results] (
        [id]                  UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_academic_results_id] DEFAULT (NEWID()),
        [assessment_id]       UNIQUEIDENTIFIER  NOT NULL,
        [student_id]          UNIQUEIDENTIFIER  NULL,     -- Enrolled student (logical reference to students.students)
        [applicant_id]        UNIQUEIDENTIFIER  NULL,     -- Admissions applicant (logical reference to students.applicants)
        [attempt_number]      INT               NOT NULL CONSTRAINT [DF_academics_academic_results_attempt] DEFAULT (1),
        [replaces_result_id]  UNIQUEIDENTIFIER  NULL,     -- Superceded attempt record
        [is_final]            BIT               NOT NULL CONSTRAINT [DF_academics_academic_results_is_final] DEFAULT (1),
        [score_numeric]       DECIMAL(5, 2)     NULL,
        [score_max]           DECIMAL(5, 2)     NULL,
        [grade_letter]        NVARCHAR(10)      NULL,     -- E.g. "A", "B+", "C"
        [grade_label]         NVARCHAR(100)     NULL,     -- E.g. "Kompeten", "Sangat Mahir"
        [narrative_feedback]  NVARCHAR(MAX)     NULL,
        [evaluation_model]    NVARCHAR(50)      NOT NULL,
        [evaluated_by]        NVARCHAR(256)     NOT NULL,
        [evaluated_date]      DATE              NOT NULL,
        [result_status]       NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_academic_results_status] DEFAULT ('provisional'),
        [approved_by]         NVARCHAR(256)     NULL,     -- Academic Authority
        [approved_date]       DATE              NULL,
        [published_date]      DATE              NULL,
        [notes]               NVARCHAR(MAX)     NULL,
        [created_at]          DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_academic_results_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]          NVARCHAR(256)     NOT NULL,
        [updated_at]          DATETIMEOFFSET(7) NULL,
        [updated_by]          NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_academic_results] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_academic_results_assessment] FOREIGN KEY ([assessment_id])
            REFERENCES [academics].[assessments] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_academics_academic_results_replaces] FOREIGN KEY ([replaces_result_id])
            REFERENCES [academics].[academic_results] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_academics_academic_results_candidate]
            CHECK (([student_id] IS NOT NULL AND [applicant_id] IS NULL) OR
                   ([student_id] IS NULL AND [applicant_id] IS NOT NULL)),
        CONSTRAINT [CK_academics_academic_results_model]
            CHECK ([evaluation_model] IN ('numeric', 'letter_grade', 'pass_fail', 'competency', 'rubric', 'narrative', 'completion')),
        CONSTRAINT [CK_academics_academic_results_status]
            CHECK ([result_status] IN ('provisional', 'approved', 'published'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_results_assessment_student'
      AND [object_id] = OBJECT_ID(N'academics.academic_results')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_results_assessment_student]
        ON [academics].[academic_results] ([assessment_id], [student_id], [attempt_number])
        WHERE ([student_id] IS NOT NULL);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_results_assessment_applicant'
      AND [object_id] = OBJECT_ID(N'academics.academic_results')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_results_assessment_applicant]
        ON [academics].[academic_results] ([assessment_id], [applicant_id], [attempt_number])
        WHERE ([applicant_id] IS NOT NULL);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_results_student_final'
      AND [object_id] = OBJECT_ID(N'academics.academic_results')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_results_student_final]
        ON [academics].[academic_results] ([student_id], [is_final], [result_status])
        INCLUDE ([assessment_id], [score_numeric], [grade_letter], [grade_label])
        WHERE ([student_id] IS NOT NULL);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_results_replaces'
      AND [object_id] = OBJECT_ID(N'academics.academic_results')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_results_replaces]
        ON [academics].[academic_results] ([replaces_result_id])
        WHERE ([replaces_result_id] IS NOT NULL);
END;
GO

-- -----------------------------------------------------------------------------
-- 4. Table: academics.academic_record_corrections
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.academic_record_corrections', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[academic_record_corrections] (
        [id]               UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_academic_record_corrections_id] DEFAULT (NEWID()),
        [record_type]      NVARCHAR(50)      NOT NULL,
        [record_id]        UNIQUEIDENTIFIER  NOT NULL,
        [original_value]   NVARCHAR(MAX)     NOT NULL, -- Serialized JSON of pre-mutation values
        [corrected_value]  NVARCHAR(MAX)     NOT NULL, -- Serialized JSON of corrected values
        [reason]           NVARCHAR(MAX)     NOT NULL,
        [evidence]         NVARCHAR(MAX)     NULL,
        [corrected_by]     NVARCHAR(256)     NOT NULL,
        [corrected_date]   DATE              NOT NULL,
        [is_material]      BIT               NOT NULL CONSTRAINT [DF_academics_academic_record_corrections_material] DEFAULT (0),
        [notes]            NVARCHAR(MAX)     NULL,
        [created_at]       DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_academic_record_corrections_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]       NVARCHAR(256)     NOT NULL,
        CONSTRAINT [PK_academics_academic_record_corrections] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [CK_academics_academic_record_corrections_type]
            CHECK ([record_type] IN ('academic_result', 'assignment', 'submission', 'academic_standing', 'promotion_recommendation', 'graduation_eligibility', 'academic_clearance'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_academic_record_corrections_record'
      AND [object_id] = OBJECT_ID(N'academics.academic_record_corrections')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_academic_record_corrections_record]
        ON [academics].[academic_record_corrections] ([record_type], [record_id], [corrected_date]);
END;
GO

-- -----------------------------------------------------------------------------
-- 5. Table: academics.report_cards
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.report_cards', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[report_cards] (
        [id]                     UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_report_cards_id] DEFAULT (NEWID()),
        [student_id]             UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [academic_period_id]     UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to operations.operational_periods(id)
        [academic_period_label]  NVARCHAR(100)     NOT NULL,
        [report_type]            NVARCHAR(50)      NOT NULL,
        [template_name]          NVARCHAR(100)     NOT NULL, -- E.g. "Buku Rapor Merdeka", "Mid-Term Progress"
        [status]                 NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_report_cards_status] DEFAULT ('draft'),
        [approved_by]            NVARCHAR(256)     NULL,
        [approved_date]          DATE              NULL,
        [published_date]         DATE              NULL,
        [narrative_comments]     NVARCHAR(MAX)     NULL,     -- Homeroom teacher summary comment
        [content_snapshot]       NVARCHAR(MAX)     NULL,     -- Immutable frozen JSON snapshot for 100% reproducibility
        [notes]                  NVARCHAR(MAX)     NULL,
        [created_at]             DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_report_cards_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]             NVARCHAR(256)     NOT NULL,
        [updated_at]             DATETIMEOFFSET(7) NULL,
        [updated_by]             NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_report_cards] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_academics_report_cards_student_period_type]
            UNIQUE ([student_id], [academic_period_id], [report_type]),
        CONSTRAINT [CK_academics_report_cards_type]
            CHECK ([report_type] IN ('mid_term', 'end_of_term')),
        CONSTRAINT [CK_academics_report_cards_status]
            CHECK ([status] IN ('draft', 'approved', 'published'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_report_cards_period_status'
      AND [object_id] = OBJECT_ID(N'academics.report_cards')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_report_cards_period_status]
        ON [academics].[report_cards] ([academic_period_id], [status]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_report_cards_student'
      AND [object_id] = OBJECT_ID(N'academics.report_cards')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_report_cards_student]
        ON [academics].[report_cards] ([student_id]);
END;
GO

-- -----------------------------------------------------------------------------
-- 6. Table: academics.report_card_entries
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.report_card_entries', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[report_card_entries] (
        [id]                  UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_report_card_entries_id] DEFAULT (NEWID()),
        [report_card_id]      UNIQUEIDENTIFIER  NOT NULL,
        [learning_unit_id]    UNIQUEIDENTIFIER  NOT NULL,
        [academic_result_id]  UNIQUEIDENTIFIER  NULL,     -- Approved operative academic result
        [display_order]       INT               NOT NULL,
        [teacher_comment]     NVARCHAR(MAX)     NULL,     -- Subject teacher remarks
        [created_at]          DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_report_card_entries_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]          NVARCHAR(256)     NOT NULL,
        CONSTRAINT [PK_academics_report_card_entries] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [FK_academics_report_card_entries_card] FOREIGN KEY ([report_card_id])
            REFERENCES [academics].[report_cards] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_academics_report_card_entries_unit] FOREIGN KEY ([learning_unit_id])
            REFERENCES [academics].[learning_units] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_academics_report_card_entries_result] FOREIGN KEY ([academic_result_id])
            REFERENCES [academics].[academic_results] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_report_card_entries_card'
      AND [object_id] = OBJECT_ID(N'academics.report_card_entries')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_report_card_entries_card]
        ON [academics].[report_card_entries] ([report_card_id], [display_order]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_report_card_entries_unit'
      AND [object_id] = OBJECT_ID(N'academics.report_card_entries')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_report_card_entries_unit]
        ON [academics].[report_card_entries] ([learning_unit_id]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_report_card_entries_result'
      AND [object_id] = OBJECT_ID(N'academics.report_card_entries')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_report_card_entries_result]
        ON [academics].[report_card_entries] ([academic_result_id])
        WHERE ([academic_result_id] IS NOT NULL);
END;
GO

-- -----------------------------------------------------------------------------
-- 7. Table: academics.transcripts
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'academics.transcripts', N'U') IS NULL
BEGIN
    CREATE TABLE [academics].[transcripts] (
        [id]                UNIQUEIDENTIFIER  NOT NULL CONSTRAINT [DF_academics_transcripts_id] DEFAULT (NEWID()),
        [student_id]        UNIQUEIDENTIFIER  NOT NULL, -- Logical reference to students.students(id)
        [transcript_type]   NVARCHAR(50)      NOT NULL,
        [period_start]      DATE              NOT NULL,
        [period_end]        DATE              NOT NULL,
        [status]            NVARCHAR(50)      NOT NULL CONSTRAINT [DF_academics_transcripts_status] DEFAULT ('draft'),
        [approved_by]       NVARCHAR(256)     NULL,
        [approved_date]     DATE              NULL,
        [issued_date]       DATE              NULL,
        [purpose]           NVARCHAR(200)     NULL,     -- E.g. "Advising", "Interim Transfer", "Evaluation"
        [content_snapshot]  NVARCHAR(MAX)     NULL,     -- Immutable frozen JSON snapshot
        [notes]             NVARCHAR(MAX)     NULL,
        [created_at]        DATETIMEOFFSET(7) NOT NULL CONSTRAINT [DF_academics_transcripts_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by]        NVARCHAR(256)     NOT NULL,
        [updated_at]        DATETIMEOFFSET(7) NULL,
        [updated_by]        NVARCHAR(256)     NULL,
        CONSTRAINT [PK_academics_transcripts] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [CK_academics_transcripts_type]
            CHECK ([transcript_type] IN ('official', 'unofficial')),
        CONSTRAINT [CK_academics_transcripts_status]
            CHECK ([status] IN ('draft', 'approved', 'issued'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_transcripts_student_type'
      AND [object_id] = OBJECT_ID(N'academics.transcripts')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_transcripts_student_type]
        ON [academics].[transcripts] ([student_id], [transcript_type]);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_academics_transcripts_status'
      AND [object_id] = OBJECT_ID(N'academics.transcripts')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_academics_transcripts_status]
        ON [academics].[transcripts] ([status], [issued_date]);
END;
GO
