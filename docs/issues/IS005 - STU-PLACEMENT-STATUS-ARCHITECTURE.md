---
Title: Target Architecture — Stage 1 Placement & Status Core Submodules
Code: STU-PLACEMENT-STATUS
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-04
---

# 1. Overview

This target architecture defines the technical realization for Stage 1 "Placement & Status — core submodules only: Administrative Placement, Student Status Management, Administrative History" under the Student domain, fulfilling change request intake `CR-STU-PLACEMENT-STATUS` ([`IS005 - STU-PLACEMENT-STATUS-ISSUE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS005%20-%20STU-PLACEMENT-STATUS-ISSUE.md)) and realizing the approved analysis decisions documented in [`IS005 - STU-PLACEMENT-STATUS-FEASIBILITY-ASSESSMENT.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS005%20-%20STU-PLACEMENT-STATUS-FEASIBILITY-ASSESSMENT.md).

The architecture establishes the operational placement, authoritative administrative status, and immutable lifecycle history foundations for the Elsasa School Information System (SIS) V3:

1. **Authoritative Administrative Placement Model:** Provisioning `students.administrative_placements` based on the approved core mandatory (`campus`, `school_level`, `grade`, `academic_year`, `effective_date`) and optional extension (`program`, `stream`, `cohort`, `homeroom`, `reason`, `authorized_by`, `decision_reference`) specification, preserving historical placement periods as an append-only timeline.
2. **Database Engine Invariant Enforcement:** Guaranteeing the single active campus placement invariant (Decision 6) and single active status invariant via Microsoft SQL Server partial unique indexes (`(student_id) WHERE (is_current = 1)`), eliminating concurrency anomalies at the storage tier.
3. **Authoritative Status Periods & Denormalization Governance:** Capturing every operational status period in `students.student_statuses` with strict transactional synchronization of the convenience status column on `students.students.current_status`.
4. **Immutable Chronological Administrative History:** Establishing `students.administrative_history_entries` as an append-only, tamper-resistant administrative audit trail recording every significant event (`enrollment_completed`, `student_activated`, `placement_changed`, etc.) and supporting non-destructive corrections via explicit self-referential links (`is_correction`, `corrects_entry_id`).
5. **Intake Sequence Orchestration in Enrollment:** Automating the complete initial placement creation, sequential status transition (`enrolled` &rarr; `active`), and initial intake audit trail logging directly inside `EnrollAcceptedApplicantHandler` upon official enrollment completion (`STD-WF-03`, Criteria 3, 5, 10).
6. **Effective-Dated Placement & Status Mutation Slices:** Delivering dedicated vertical slices for `ChangeAdministrativePlacement` (`STD-WF-05`, `UC-STD-06`) and `ChangeStudentStatus` adhering to the Pragmatic Immediate Activation approach with business effective dating.
7. **360-Degree Dossier Projection & Audit Exploration:** Extending `GetStudentById` to project current placement and historical periods, and providing a dedicated query slice `GetStudentAdministrativeHistory` for chronological audit inspection.
8. **Operational Desk Alignment (`SCR-STU-02`):** Extending the Student Records Desk at `/students/records` with dedicated `Placement` and `Administrative History` tabs, authoritative header status management, and modal dialogs for placement and status changes.

---

# 2. Architectural Basis

## Business Context

This architecture realizes the business outcomes, operational workflows, and domain rules specified in:

- **DOMAIN BLUEPRINT:** [`student-domain.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/student-domain.md)
  - Section: Administrative Placement (lines 151–171)
  - Section: Student Status (lines 173–196)
  - Section: Administrative History (lines 320–330, 1590)
  - Section: Cohort Placement (line 180)
- **ROADMAP & CRITERIA:** [`sub-modul-and-development-phases.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)
  - Stage 1 Module: Placement & Status (Core Submodules Only)
  - Criterion 3: Initial administrative placement upon official enrollment.
  - Criterion 5: Authoritative active student status maintenance.
  - Criterion 10: Traceable administrative history trail.
  - Section E Domain Boundary Issues: Homeroom administrative ownership resolution.
- **WORKFLOWS & USE CASES:**
  - [`operational-workflow-discovery.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md): `STD-WF-05: Change Administrative Placement`.
  - [`use-cases.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md): `UC-STD-06: Change Administrative Placement`.
  - [`actors.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md): Student Administration Officer (`ACT-02`).
- **DATA SPECIFICATIONS:**
  - [`student-entities.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/student-entities.md): Submodule 3 (Placement & Status) Decisions 5, 6, 9.
  - [`student-entities-tables.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/student-entities-tables.md): Tables 3.1 (`administrative_placements`), 3.2 (`student_statuses`), and 3.11 (`administrative_history_entries`).
- **SCREEN INVENTORY:** [`screen-inventory.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md)
  - `SCR-STU-02`: Student Records Desk (Information Lookup pattern — placement history, current placement card, status timeline, and chronological audit trail).

## Analysis Input

This architecture directly realizes the approved gap closures, risk mitigations, and technical decisions recorded in:

- **FEASIBILITY ASSESSMENT:** [`IS005 - STU-PLACEMENT-STATUS-FEASIBILITY-ASSESSMENT.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS005%20-%20STU-PLACEMENT-STATUS-FEASIBILITY-ASSESSMENT.md)
  - Gap closures: GAP-001 through GAP-008
  - Resolved design decisions: OQ-001 through OQ-006
  - Architecture Applicability: ARCHITECTURE-REQUIRED
  - Planning Readiness: READY-FOR-PLANNING
  - Evaluated Approach: Approach 1 (Pragmatic, Minimalist, and Explicit Domain Alignment)

```text
DOMAIN (student-domain.md) + FEATURE (use-cases.md, screen-inventory.md)
                            +
        FEASIBILITY-ASSESSMENT (STU-PLACEMENT-STATUS)
                            ↓
         TARGET ARCHITECTURE (STU-PLACEMENT-STATUS)
```

---

# 3. Scope

## Included

1. **Database Schema & Migrations (`Elsasa.Sis.Database`):**
   - Migration script `20261104_0101_Students_PlacementStatus_Tables.sql` targeting Microsoft SQL Server 2022 ([`ADR-002`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L50-L75), [`ADR-006`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L152-L176)).
   - Creation of 3 domain tables in schema `students`:
     - `students.administrative_placements`: Historical and active placement periods.
     - `students.student_statuses`: Authoritative operational status history.
     - `students.administrative_history_entries`: Immutable chronological lifecycle audit trail.
   - Partial unique indexes enforcing single active placement and single active status invariants (`WHERE is_current = 1`).
   - Intra-schema foreign keys with `ON DELETE NO ACTION` referencing `students.students(id)` and self-referencing correction chains.
   - Idempotent SQL backfill for existing enrolled students in development and test environments (`GAP-008`).

2. **Backend Vertical Slices (`Elsasa.Sis.Students`):**
   - **Enrollment Intake Orchestration Enhancement:** Update `EnrollAcceptedApplicantHandler.cs` to atomically write initial placement, sequential status (`enrolled` &rarr; `active`), and 4 intake history entries in its primary transaction (`GAP-003`).
   - **Placement Reassignment Command:** Vertical slice `ChangeAdministrativePlacement` in `Features/Records/Placements/ChangeAdministrativePlacement` (`POST /api/students/{id}/placements`) executing effective-dated placement changes (`STD-WF-05`, `UC-STD-06`, `GAP-004`).
   - **Status Adjustment Command:** Vertical slice `ChangeStudentStatus` in `Features/Records/Status/ChangeStudentStatus` (`POST /api/students/{id}/status`) updating status periods and synchronizing denormalized student status (`GAP-005`).
   - **Chronological History Query:** Vertical slice `GetStudentAdministrativeHistory` in `Features/Records/History/GetStudentAdministrativeHistory` (`GET /api/students/{id}/history`) projecting the immutable audit timeline (`GAP-006`).
   - **360-Degree Dossier Projection Enrichment:** Update `StudentDetailResponse` and `GetStudentByIdHandler` to project current placement, placement history, and status history (`GAP-006`).

3. **Cross-Domain Contracts & Domain Events (`Elsasa.Sis.Contracts`):**
   - Introduction of `PlacementChangedEvent` in `Elsasa.Sis.Contracts.Students.Events`, published post-commit via MediatR `IPublisher` ([`ADR-003`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L76-L98)).

4. **Frontend Workspaces (`src/frontend`):**
   - **Extension of `SCR-STU-02` (`StudentRecordsDeskView.vue`):**
     - Addition of `Placement` tab (`StudentPlacementTab.vue`) displaying current placement summary, historical placements timeline, and "Change Placement" trigger button.
     - Addition of `Administrative History` tab (`StudentAdministrativeHistoryTab.vue`) rendering the chronological audit log with event filtering.
     - Status header badge displaying current operational status with quick-action trigger for `ChangeStatusModal.vue`.
   - **Modal Dialogs:**
     - `ChangePlacementModal.vue` (`UC-STD-06` form with VeeValidate and Zod validation).
     - `ChangeStatusModal.vue` (status adjustment dialog with reason and effective date).
   - **Composables, Types, and Schemas:**
     - Typed API functions in `records.api.ts`.
     - TanStack Query hooks in `useStudentRecordsQueries.ts` and `useStudentRecordsMutations.ts`.
     - Zod schemas in `records.schema.ts` and TypeScript interfaces in `records.types.ts`.

## Excluded

1. **Continuing Lifecycle Workflows (Stage 2):**
   - Leave of Absence (`UC-STD-07`, `UC-STD-08`), Promotion/Retention execution (`UC-STD-09`), Internal Transfers (`UC-STD-10`), and Annual Re-enrollment (`UC-STD-17`) belong to Stage 2 (`SCR-STU-03: Student Lifecycle Workbench`).
2. **Terminating Exits & Guardian Dispute Holds (Stage 3):**
   - External Transfer Out (`UC-STD-11`), Formal Withdrawal (`UC-STD-12`), Expulsion (`UC-STD-14`), and Graduation/Alumni (`UC-STD-15`, `UC-STD-16`) belong to Stage 3 (`SCR-STU-04: Student Exit Desk`).
3. **Premature Multi-Entity Normalization:**
   - Dedicated `students.lifecycle_transitions` and `students.administrative_decisions` tables are deferred to Stage 2. Authorizer identity (`authorized_by`) and decision reference strings (`decision_reference`) are captured directly on rows.
4. **Physical Scheduling & Teaching Workload:**
   - Physical classroom space scheduling and teacher homeroom allocation belong to Operations and Teaching domains and are decoupled from administrative placement.

---

# 4. Technical Decisions

## TD-01: Three-Table Relational Schema & Storage Layout

- **Decision:** Provision exactly 3 focused, explicit domain tables in migration `20261104_0101_Students_PlacementStatus_Tables.sql` targeting Microsoft SQL Server 2022 ([`ADR-002`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L50-L75), [`ADR-006`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L152-L176), GAP-001, OQ-001).
- **Realization:**
  - **Table 1: `students.administrative_placements`**
    ```sql
    CREATE TABLE [students].[administrative_placements] (
        [id] UNIQUEIDENTIFIER NOT NULL,
        [student_id] UNIQUEIDENTIFIER NOT NULL,
        [campus] NVARCHAR(100) NOT NULL,
        [school_level] NVARCHAR(64) NOT NULL,
        [grade] NVARCHAR(64) NOT NULL,
        [academic_year] NVARCHAR(32) NOT NULL,
        [effective_date] DATE NOT NULL,
        [program] NVARCHAR(100) NULL,
        [stream] NVARCHAR(100) NULL,
        [cohort] NVARCHAR(50) NULL,
        [homeroom] NVARCHAR(100) NULL,
        [end_date] DATE NULL,
        [is_current] BIT NOT NULL CONSTRAINT [df_students_placements_is_current] DEFAULT (1),
        [reason] NVARCHAR(500) NULL,
        [authorized_by] NVARCHAR(100) NULL,
        [decision_reference] NVARCHAR(100) NULL,
        [created_at] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_students_placements_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by] NVARCHAR(100) NULL,
        [updated_at] DATETIMEOFFSET(7) NULL,
        [updated_by] NVARCHAR(100) NULL,
        CONSTRAINT [pk_students_administrative_placements] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [fk_students_placements_student] FOREIGN KEY ([student_id])
            REFERENCES [students].[students] ([id]) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX [ix_students_placements_student]
        ON [students].[administrative_placements] ([student_id], [effective_date] DESC);
    ```
  - **Table 2: `students.student_statuses`**
    ```sql
    CREATE TABLE [students].[student_statuses] (
        [id] UNIQUEIDENTIFIER NOT NULL,
        [student_id] UNIQUEIDENTIFIER NOT NULL,
        [status] NVARCHAR(32) NOT NULL,
        [effective_date] DATE NOT NULL,
        [end_date] DATE NULL,
        [is_current] BIT NOT NULL CONSTRAINT [df_students_statuses_is_current] DEFAULT (1),
        [reason] NVARCHAR(MAX) NULL,
        [authorized_by] NVARCHAR(100) NULL,
        [decision_reference] NVARCHAR(100) NULL,
        [created_at] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_students_statuses_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by] NVARCHAR(100) NULL,
        [updated_at] DATETIMEOFFSET(7) NULL,
        [updated_by] NVARCHAR(100) NULL,
        CONSTRAINT [pk_students_student_statuses] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [fk_students_statuses_student] FOREIGN KEY ([student_id])
            REFERENCES [students].[students] ([id]) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX [ix_students_statuses_student]
        ON [students].[student_statuses] ([student_id], [effective_date] DESC);
    ```
  - **Table 3: `students.administrative_history_entries`**
    ```sql
    CREATE TABLE [students].[administrative_history_entries] (
        [id] UNIQUEIDENTIFIER NOT NULL,
        [student_id] UNIQUEIDENTIFIER NOT NULL,
        [event_type] NVARCHAR(50) NOT NULL,
        [event_date] DATE NOT NULL,
        [detail] NVARCHAR(MAX) NULL,
        [authorized_by] NVARCHAR(100) NULL,
        [decision_reference] NVARCHAR(100) NULL,
        [is_correction] BIT NOT NULL CONSTRAINT [df_students_history_is_correction] DEFAULT (0),
        [corrects_entry_id] UNIQUEIDENTIFIER NULL,
        [created_at] DATETIMEOFFSET(7) NOT NULL CONSTRAINT [df_students_history_created_at] DEFAULT (SYSUTCDATETIME()),
        [created_by] NVARCHAR(100) NULL,
        CONSTRAINT [pk_students_administrative_history_entries] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [fk_students_history_student] FOREIGN KEY ([student_id])
            REFERENCES [students].[students] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [fk_students_history_corrects] FOREIGN KEY ([corrects_entry_id])
            REFERENCES [students].[administrative_history_entries] ([id]) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX [ix_students_history_student]
        ON [students].[administrative_history_entries] ([student_id], [event_date] DESC, [created_at] DESC);
    ```

## TD-02: Engine-Level Single Active Invariant Enforcement

- **Decision:** Enforce the single active campus placement invariant (Decision 6) and single active status invariant directly via SQL Server partial unique indexes (GAP-002, RISK-001, RISK-002).
- **Realization:**
  - Partial unique indexes in migration `20261104_0101`:
    ```sql
    CREATE UNIQUE INDEX [ux_administrative_placements_student_active]
        ON [students].[administrative_placements] ([student_id])
        WHERE ([is_current] = 1);

    CREATE UNIQUE INDEX [ux_student_statuses_student_active]
        ON [students].[student_statuses] ([student_id])
        WHERE ([is_current] = 1);
    ```
  - **Invariant Transaction Sequence:** All mutation command handlers must enforce the following atomic execution sequence within their Dapper database transaction:
    1. Demote/close existing active record:
       ```sql
       UPDATE students.administrative_placements
       SET is_current = 0,
           end_date = @EffectiveDate,
           updated_at = SYSUTCDATETIME(),
           updated_by = @UpdatedBy
       WHERE student_id = @StudentId AND is_current = 1;
       ```
    2. Insert new active record with `is_current = 1` and `end_date = NULL`.
  - Violations of the single active invariant will result in a SQL unique constraint violation (`2601` / `2627`), ensuring zero database-level concurrency corruption.

## TD-03: Complete Intake Sequence Orchestration in Enrollment (`GAP-003`)

- **Decision:** Enhance `EnrollAcceptedApplicantHandler.cs` to orchestrate the complete initial placement creation, status activation, and intake audit trail within its existing atomic unit of work (GAP-003, OQ-002).
- **Realization:**
  - Inside `EnrollAcceptedApplicantHandler.cs`, within the single `SqlTransaction`:
    1. **Prerequisite & Guard Checks:** Validate accepted status, non-enrollment, and payment/waiver gates (existing behavior).
    2. **Student Identity Insertion:** Insert into `students.students` with `current_status = 'active'` (updated from `'enrolled'`).
    3. **Enrollment Snapshot Insertion:** Insert into `students.enrollments` capturing initial placement parameters (existing behavior).
    4. **Initial Administrative Placement:**
       ```sql
       INSERT INTO students.administrative_placements (
           id, student_id, campus, school_level, grade, academic_year,
           effective_date, program, stream, cohort, homeroom,
           end_date, is_current, reason, authorized_by, decision_reference,
           created_at, created_by
       ) VALUES (
           @PlacementId, @StudentId, @InitialCampus, @InitialLevel, @InitialLevel, @InitialAcademicYear,
           @EnrollmentDate, @InitialProgram, NULL, @InitialCohort, NULL,
           NULL, 1, 'Initial enrollment placement', @ApprovedBy, @EnrollmentNumber,
           SYSUTCDATETIME(), @CreatedBy
       );
       ```
    5. **Sequential Status Logging:**
       ```sql
       -- Closed enrollment status period
       INSERT INTO students.student_statuses (
           id, student_id, status, effective_date, end_date, is_current,
           reason, authorized_by, decision_reference, created_at, created_by
       ) VALUES (
           @StatusEnrolledId, @StudentId, 'enrolled', @EnrollmentDate, @EnrollmentDate, 0,
           'Initial enrollment completed', @ApprovedBy, @EnrollmentNumber,
           SYSUTCDATETIME(), @CreatedBy
       );

       -- Active current status period
       INSERT INTO students.student_statuses (
           id, student_id, status, effective_date, end_date, is_current,
           reason, authorized_by, decision_reference, created_at, created_by
       ) VALUES (
           @StatusActiveId, @StudentId, 'active', @EnrollmentDate, NULL, 1,
           'Activated upon enrollment completion', @ApprovedBy, @EnrollmentNumber,
           SYSUTCDATETIME(), @CreatedBy
       );
       ```
    6. **Document Promotion & Guardian Matrix:** Promote verified documents and link guardians (existing behavior).
    7. **Intake History Audit Trail:** Insert 4 chronological events into `students.administrative_history_entries`:
       - `application_submitted`: Sourced from application registration date.
       - `application_accepted`: Sourced from admission decision date.
       - `enrollment_completed`: Detail "Enrolled at {InitialCampus} {InitialLevel}".
       - `student_activated`: Detail "Official student status activated".
    8. **Commit & Event Publishing:** Commit transaction, then publish `StudentEnrolledEvent` strictly post-commit.

## TD-04: Change Administrative Placement Vertical Slice (`GAP-004`, `STD-WF-05`, `UC-STD-06`)

- **Decision:** Implement vertical slice `ChangeAdministrativePlacement` in `Features/Records/Placements/ChangeAdministrativePlacement` adhering to [`coding-conventions.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/coding-conventions.md) and [`backend-architecture.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/backend-architecture.md).
- **Realization:**
  - **Endpoint:** `POST /api/students/{id}/placements`
  - **Authorization:** `Permissions.Students.Records.Edit`
  - **Request Model:**
    ```csharp
    public sealed record ChangeAdministrativePlacementRequest(
        string Campus,
        string SchoolLevel,
        string Grade,
        string AcademicYear,
        DateOnly EffectiveDate,
        string? Program,
        string? Stream,
        string? Cohort,
        string? Homeroom,
        string? Reason,
        string? AuthorizedBy,
        string? DecisionReference);
    ```
  - **Validator Rules (`FluentValidation`):**
    - `Campus`, `SchoolLevel`, `Grade`, `AcademicYear` must not be empty.
    - `EffectiveDate` must not be default.
    - `EffectiveDate` must not precede the current active placement's `EffectiveDate`.
  - **Handler Execution Logic:**
    1. Query active placement:
       ```sql
       SELECT id, effective_date
       FROM students.administrative_placements
       WHERE student_id = @StudentId AND is_current = 1;
       ```
    2. If no active placement exists, return `DomainError.NotFound("Students.Placements.ActivePlacementNotFound", "No active placement found for this student.")`.
    3. If `request.EffectiveDate < currentPlacement.EffectiveDate`, return `DomainError.Validation("Students.Placements.EffectiveDatePriorToCurrent", "Effective date cannot precede current placement effective date.")`.
    4. Open atomic database transaction:
       - Update existing active row (`is_current = 0, end_date = @EffectiveDate, updated_at = SYSUTCDATETIME(), updated_by = @User`).
       - Insert new active row (`is_current = 1, effective_date = @EffectiveDate, end_date = NULL`).
       - Insert history entry: `event_type = 'placement_changed'`, `event_date = @EffectiveDate`, `detail = $"Reassigned to {request.Campus}, {request.SchoolLevel}, {request.Grade}, Cohort: {request.Cohort ?? "-"}, Homeroom: {request.Homeroom ?? "-"}"`, `authorized_by = @AuthorizedBy`, `decision_reference = @DecisionReference`.
       - Commit transaction.
    5. Post-commit: publish `PlacementChangedEvent` via MediatR `IPublisher`.
    6. Return `ChangeAdministrativePlacementResponse(placementId, studentId, isCurrent: true)`.

## TD-05: Change Student Status Vertical Slice (`GAP-005`)

- **Decision:** Implement vertical slice `ChangeStudentStatus` in `Features/Records/Status/ChangeStudentStatus` (`POST /api/students/{id}/status`) to manage authoritative operational status transitions (GAP-005, ASM-004).
- **Realization:**
  - **Endpoint:** `POST /api/students/{id}/status`
  - **Authorization:** `Permissions.Students.Records.Edit`
  - **Request Model:**
    ```csharp
    public sealed record ChangeStudentStatusRequest(
        string Status,
        DateOnly EffectiveDate,
        string? Reason,
        string? AuthorizedBy,
        string? DecisionReference);
    ```
  - **Validator Rules:**
    - `Status` must be one of allowed Stage 1 statuses: `'active'`, `'enrolled'`.
    - `EffectiveDate` must not be default.
  - **Handler Execution Logic:**
    1. Verify student exists in `students.students`.
    2. In an atomic transaction:
       - Close current active status in `students.student_statuses`:
         ```sql
         UPDATE students.student_statuses
         SET is_current = 0,
             end_date = @EffectiveDate,
             updated_at = SYSUTCDATETIME(),
             updated_by = @User
         WHERE student_id = @StudentId AND is_current = 1;
         ```
       - Insert new active status row in `students.student_statuses`:
         ```sql
         INSERT INTO students.student_statuses (
             id, student_id, status, effective_date, end_date, is_current,
             reason, authorized_by, decision_reference, created_at, created_by
         ) VALUES (
             @Id, @StudentId, @Status, @EffectiveDate, NULL, 1,
             @Reason, @AuthorizedBy, @DecisionReference, SYSUTCDATETIME(), @User
         );
         ```
       - Synchronize denormalized student column:
         ```sql
         UPDATE students.students
         SET current_status = @Status,
             updated_at = SYSUTCDATETIME(),
             updated_by = @User
         WHERE id = @StudentId;
         ```
       - Append history entry into `students.administrative_history_entries`:
         - `event_type`: `'student_activated'` (if status == 'active') or `'status_adjusted'`.
         - `detail`: `$"Status adjusted to '{request.Status}'. Reason: {request.Reason}"`.
       - Commit transaction.
    3. Return `ChangeStudentStatusResponse(statusId, studentId, request.Status, isCurrent: true)`.

## TD-06: Hybrid Query Projection & Chronological History Slice (`GAP-006`, `OQ-005`)

- **Decision:** Employ a hybrid query strategy: extend `StudentDetailResponse` in `GetStudentById` for immediate dossier presentation, and provide a dedicated query slice `GetStudentAdministrativeHistory` for in-depth chronological audit inspection (GAP-006, OQ-005).
- **Realization:**
  - **Extension of `StudentDetailResponse`:**
    ```csharp
    public sealed record StudentDetailResponse(
        // Existing identity, notes, audit, enrollment, guardian, document fields...
        IReadOnlyList<EnrollmentHistoryDto> EnrollmentHistory,
        IReadOnlyList<StudentGuardianRelationshipDto> GuardianRelationships,
        IReadOnlyList<StudentDocumentDto> Documents,
        AdministrativePlacementDto? CurrentPlacement,
        IReadOnlyList<AdministrativePlacementDto> PlacementHistory,
        IReadOnlyList<StudentStatusDto> StatusHistory);

    public sealed record AdministrativePlacementDto(
        Guid Id,
        string Campus,
        string SchoolLevel,
        string Grade,
        string AcademicYear,
        DateOnly EffectiveDate,
        DateOnly? EndDate,
        bool IsCurrent,
        string? Program,
        string? Stream,
        string? Cohort,
        string? Homeroom,
        string? Reason,
        string? AuthorizedBy,
        string? DecisionReference);

    public sealed record StudentStatusDto(
        Guid Id,
        string Status,
        DateOnly EffectiveDate,
        DateOnly? EndDate,
        bool IsCurrent,
        string? Reason,
        string? AuthorizedBy,
        string? DecisionReference);
    ```
  - **`GetStudentByIdHandler` Multi-Query Update:**
    - Extend `_selectDossierMultiSql` to query `students.administrative_placements` ordered by `effective_date DESC` and `students.student_statuses` ordered by `effective_date DESC`.
    - Derive `CurrentPlacement` from the first row with `is_current = true`.
  - **Dedicated Query Slice `GetStudentAdministrativeHistory`:**
    - Route: `GET /api/students/{id}/history`
    - Directory: `src/backend/Elsasa.Sis.Students/Features/Records/History/GetStudentAdministrativeHistory/`
    - Query:
      ```sql
      SELECT
          h.id AS Id,
          h.event_type AS EventType,
          h.event_date AS EventDate,
          h.detail AS Detail,
          h.authorized_by AS AuthorizedBy,
          h.decision_reference AS DecisionReference,
          h.is_correction AS IsCorrection,
          h.corrects_entry_id AS CorrectsEntryId,
          h.created_at AS CreatedAt,
          h.created_by AS CreatedBy
      FROM students.administrative_history_entries h
      WHERE h.student_id = @StudentId
      ORDER BY h.event_date DESC, h.created_at DESC;
      ```

## TD-07: Idempotent Historical Data Backfill Strategy (`GAP-008`, `OQ-004`)

- **Decision:** Include idempotent SQL backfill scripts in migration `20261104_0101_Students_PlacementStatus_Tables.sql` sourcing active placements from `students.enrollments` and active statuses from `students.students` (GAP-008, OQ-004, RISK-004).
- **Realization:**
  - In `20261104_0101_Students_PlacementStatus_Tables.sql`:
    ```sql
    -- 1. Backfill administrative placements for existing students lacking one
    INSERT INTO [students].[administrative_placements] (
        [id], [student_id], [campus], [school_level], [grade], [academic_year],
        [effective_date], [program], [stream], [cohort], [homeroom],
        [end_date], [is_current], [reason], [authorized_by], [decision_reference],
        [created_at], [created_by]
    )
    SELECT
        NEWID(),
        s.id,
        COALESCE(latest_enr.initial_campus, N'Main Campus'),
        COALESCE(latest_enr.initial_level, N'Default Level'),
        COALESCE(latest_enr.initial_level, N'Default Level'),
        COALESCE(latest_enr.initial_academic_year, N'2026/2027'),
        s.enrollment_date,
        latest_enr.initial_program,
        NULL,
        latest_enr.initial_cohort,
        NULL,
        NULL,
        1,
        N'Historical enrollment data backfill',
        COALESCE(latest_enr.approved_by, N'SYSTEM_MIGRATION'),
        N'MIGRATION_20261104',
        SYSUTCDATETIME(),
        N'SYSTEM_MIGRATION'
    FROM [students].[students] s
    OUTER APPLY (
        SELECT TOP 1 *
        FROM [students].[enrollments] e
        WHERE e.student_id = s.id
        ORDER BY e.enrollment_date DESC, e.created_at DESC
    ) latest_enr
    WHERE NOT EXISTS (
        SELECT 1 FROM [students].[administrative_placements] ap WHERE ap.student_id = s.id
    );

    -- 2. Backfill student statuses for existing students lacking one
    INSERT INTO [students].[student_statuses] (
        [id], [student_id], [status], [effective_date], [end_date], [is_current],
        [reason], [authorized_by], [decision_reference], [created_at], [created_by]
    )
    SELECT
        NEWID(),
        s.id,
        COALESCE(s.current_status, N'active'),
        s.enrollment_date,
        NULL,
        1,
        N'Historical status backfill',
        N'SYSTEM_MIGRATION',
        N'MIGRATION_20261104',
        SYSUTCDATETIME(),
        N'SYSTEM_MIGRATION'
    FROM [students].[students] s
    WHERE NOT EXISTS (
        SELECT 1 FROM [students].[student_statuses] st WHERE st.student_id = s.id
    );

    -- 3. Backfill baseline intake administrative history for existing students lacking history
    INSERT INTO [students].[administrative_history_entries] (
        [id], [student_id], [event_type], [event_date], [detail],
        [authorized_by], [decision_reference], [is_correction], [corrects_entry_id],
        [created_at], [created_by]
    )
    SELECT
        NEWID(),
        s.id,
        N'enrollment_completed',
        s.enrollment_date,
        CONCAT(N'Officially enrolled with student number ', s.student_number),
        N'SYSTEM_MIGRATION',
        N'MIGRATION_20261104',
        0,
        NULL,
        SYSUTCDATETIME(),
        N'SYSTEM_MIGRATION'
    FROM [students].[students] s
    WHERE NOT EXISTS (
        SELECT 1 FROM [students].[administrative_history_entries] h WHERE h.student_id = s.id
    );
    ```

## TD-08: Frontend Architecture & Workspace Modularization in `SCR-STU-02` (`GAP-007`)

- **Decision:** Expand `src/frontend/src/features/students/records/` with dedicated placement and history subcomponents adhering strictly to [`frontend-architecture.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/frontend-architecture.md) and [`screen-inventory.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md).
- **Realization:**
  - **Component Structure:**
    ```text
    src/frontend/src/features/students/records/
    ├── components/
    │   ├── StudentPlacementTab.vue              # Current placement card + historical placement list
    │   ├── StudentAdministrativeHistoryTab.vue  # Chronological audit timeline with event badges
    │   ├── ChangePlacementModal.vue             # Modal dialog executing UC-STD-06
    │   └── ChangeStatusModal.vue                # Modal dialog executing status adjustment
    ├── api/
    │   └── records.api.ts                       # changePlacement, changeStatus, getAdministrativeHistory
    ├── composables/
    │   ├── queryKeys.ts                         # queryKeys.students.history(id)
    │   ├── useStudentRecordsQueries.ts          # useStudentHistoryQuery
    │   └── useStudentRecordsMutations.ts        # useChangePlacementMutation, useChangeStatusMutation
    ├── schemas/
    │   └── records.schema.ts                    # changePlacementSchema, changeStatusSchema (Zod)
    └── types/
        └── records.types.ts                     # TypeScript DTOs and Request interfaces
    ```
  - **View Layout (`StudentRecordsDeskView.vue`):**
    - Retain existing tabs: `Certified Identity`, `Guardians & Household`, `Official Documents`, `Enrollment History`.
    - Add new tabs:
      - `Placement` (`StudentPlacementTab.vue`)
      - `Administrative History` (`StudentAdministrativeHistoryTab.vue`)
    - Surface current operational status badge in the dossier header with a "Change Status" quick-action button triggering `ChangeStatusModal.vue`.
  - **State Management & Invalidation:**
    - Successful placement mutation invalidates `queryKeys.students.detail(studentId)` and `queryKeys.students.history(studentId)`.
    - Successful status mutation invalidates `queryKeys.students.detail(studentId)`, `queryKeys.students.history(studentId)`, and `queryKeys.students.list()`.

---

# 5. Component Responsibilities

| Component | Assembly / Directory | Responsibility |
|---|---|---|
| `20261104_0101_Students_PlacementStatus_Tables.sql` | `Elsasa.Sis.Database/Scripts/` | Database migration creating placement, status, history tables, partial unique indexes, and data backfill. |
| `PlacementChangedEvent` | `Elsasa.Sis.Contracts/Students/Events/` | In-memory domain event notified post-commit when a student's administrative placement changes. |
| `EnrollAcceptedApplicantHandler` | `Elsasa.Sis.Students/Features/Enrollment/EnrollAcceptedApplicant/` | Enhanced handler provisioning initial placement, sequential statuses (`enrolled` &rarr; `active`), and intake history. |
| `ChangeAdministrativePlacementEndpoint` | `Elsasa.Sis.Students/Features/Records/Placements/ChangeAdministrativePlacement/` | Minimal API endpoint mapping `POST /api/students/{id}/placements`. |
| `ChangeAdministrativePlacementValidator` | `Elsasa.Sis.Students/Features/Records/Placements/ChangeAdministrativePlacement/` | FluentValidation rules for placement change command inputs and effective date ordering. |
| `ChangeAdministrativePlacementHandler` | `Elsasa.Sis.Students/Features/Records/Placements/ChangeAdministrativePlacement/` | Handler executing atomic placement update, history insertion, and `PlacementChangedEvent` dispatch. |
| `ChangeStudentStatusEndpoint` | `Elsasa.Sis.Students/Features/Records/Status/ChangeStudentStatus/` | Minimal API endpoint mapping `POST /api/students/{id}/status`. |
| `ChangeStudentStatusValidator` | `Elsasa.Sis.Students/Features/Records/Status/ChangeStudentStatus/` | FluentValidation rules for student status adjustment command. |
| `ChangeStudentStatusHandler` | `Elsasa.Sis.Students/Features/Records/Status/ChangeStudentStatus/` | Handler executing atomic status update, student denormalized sync, and history insertion. |
| `GetStudentAdministrativeHistoryEndpoint` | `Elsasa.Sis.Students/Features/Records/History/GetStudentAdministrativeHistory/` | Minimal API endpoint mapping `GET /api/students/{id}/history`. |
| `GetStudentAdministrativeHistoryHandler` | `Elsasa.Sis.Students/Features/Records/History/GetStudentAdministrativeHistory/` | Dapper query handler projecting chronological history timeline. |
| `GetStudentByIdHandler` | `Elsasa.Sis.Students/Features/Records/GetStudentById/` | Enhanced multi-query handler projecting current placement, placement history, and status history. |
| `StudentPlacementTab.vue` | `src/frontend/.../records/components/` | Vue tab rendering current placement summary card, historical placements timeline, and action trigger. |
| `StudentAdministrativeHistoryTab.vue` | `src/frontend/.../records/components/` | Vue tab rendering chronological lifecycle audit trail with event badges and details. |
| `ChangePlacementModal.vue` | `src/frontend/.../records/components/` | PrimeVue dialog form for executing `UC-STD-06` placement reassignment. |
| `ChangeStatusModal.vue` | `src/frontend/.../records/components/` | PrimeVue dialog form for executing operational status adjustment. |
| `StudentRecordsDeskView.vue` | `src/frontend/.../records/views/` | Main records desk workspace (`SCR-STU-02`) hosting tabs, metrics, search, and header status badge. |

---

# 6. Integration Design

| Source | Target | Purpose | Protocol / Mechanism |
|---|---|---|---|
| `ChangePlacementModal.vue` | `records.api.ts` | Submit placement reassignment payload | TypeScript function invocation |
| `records.api.ts` | `ChangeAdministrativePlacementEndpoint` | Execute placement reassignment | HTTP `POST /api/students/{id}/placements` (Axios) |
| `ChangeAdministrativePlacementEndpoint` | `ChangeAdministrativePlacementHandler` | Invoke placement command | Direct C# method call (`HandleAsync`) |
| `ChangeAdministrativePlacementHandler` | `students.administrative_placements` | Close prior placement (`is_current = 0`) & insert new active placement | Dapper T-SQL via `ISqlConnectionFactory` |
| `ChangeAdministrativePlacementHandler` | `students.administrative_history_entries` | Append `placement_changed` event | Dapper T-SQL via `ISqlConnectionFactory` |
| `ChangeAdministrativePlacementHandler` | `PlacementChangedEvent` | Notify downstream domains post-commit | MediatR `IPublisher.Publish` |
| `ChangeStatusModal.vue` | `records.api.ts` | Submit status change payload | TypeScript function invocation |
| `records.api.ts` | `ChangeStudentStatusEndpoint` | Execute status transition | HTTP `POST /api/students/{id}/status` (Axios) |
| `ChangeStudentStatusHandler` | `students.student_statuses` | Close prior status & insert new status row | Dapper T-SQL via `ISqlConnectionFactory` |
| `ChangeStudentStatusHandler` | `students.students` | Synchronize denormalized `current_status` | Dapper T-SQL via `ISqlConnectionFactory` |
| `ChangeStudentStatusHandler` | `students.administrative_history_entries` | Append status transition event | Dapper T-SQL via `ISqlConnectionFactory` |
| `StudentRecordsDeskView.vue` | `GetStudentByIdEndpoint` | Fetch 360° dossier including placements & statuses | HTTP `GET /api/students/{id}` (TanStack Query) |
| `StudentAdministrativeHistoryTab.vue`| `GetStudentAdministrativeHistoryEndpoint` | Fetch chronological audit timeline | HTTP `GET /api/students/{id}/history` (TanStack Query) |

---

# 7. Data Ownership

| Data Entity | Authoritative Owner | Access Mode |
|---|---|---|
| `students.students` | `Elsasa.Sis.Students` | Read/Write (maintains `current_status` synchronized with active status) |
| `students.enrollments` | `Elsasa.Sis.Students` | Append-only (records initial placement & enrollment snapshot) |
| `students.administrative_placements` | `Elsasa.Sis.Students` | Read/Write (append new rows, update `is_current`/`end_date` on close) |
| `students.student_statuses` | `Elsasa.Sis.Students` | Read/Write (append new rows, update `is_current`/`end_date` on close) |
| `students.administrative_history_entries` | `Elsasa.Sis.Students` | Append-only (immutable audit log; corrections link via `corrects_entry_id`) |
| `operations.academic_years` | `Elsasa.Sis.Operations` | Read-only reference via logical string identifier |
| `operations.campuses` | `Elsasa.Sis.Operations` | Read-only reference via logical string identifier |

---

# 8. Database Design

## New Tables

| Table | Purpose |
|---|---|
| `students.administrative_placements` | Records historical and active organizational placements (campus, level, grade, academic year, homeroom, cohort). |
| `students.student_statuses` | Authoritative operational status history periods (`enrolled`, `active`). |
| `students.administrative_history_entries` | Immutable, append-only chronological lifecycle event audit trail. |

## Modified Tables

| Table | Change |
|---|---|
| `students.students` | Denormalized column `current_status` is updated to `'active'` upon enrollment completion and maintained in strict lockstep with `students.student_statuses.is_current = 1`. |

## Relationships

- `students.administrative_placements.student_id` &rarr; `students.students(id)` (Foreign Key, `ON DELETE NO ACTION`).
- `students.student_statuses.student_id` &rarr; `students.students(id)` (Foreign Key, `ON DELETE NO ACTION`).
- `students.administrative_history_entries.student_id` &rarr; `students.students(id)` (Foreign Key, `ON DELETE NO ACTION`).
- `students.administrative_history_entries.corrects_entry_id` &rarr; `students.administrative_history_entries(id)` (Foreign Key, `ON DELETE NO ACTION`).
- Logical references (zero physical cross-schema foreign keys, [`ADR-002`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L50-L75)):
  - `administrative_placements.academic_year` logically maps to `operations.academic_years`.
  - `administrative_placements.campus` logically maps to operational campuses.

## Migration Considerations

- Migration script: `src/backend/Elsasa.Sis.Database/Scripts/20261104_0101_Students_PlacementStatus_Tables.sql`.
- Migration runner: DbUp SqlServer execution engine.
- Transactional integrity: The script runs within a database transaction; rollback occurs automatically on syntax or execution failure.
- Idempotency & Backfill: The migration contains explicit `WHERE NOT EXISTS` checks and backfills active placement and status rows for all existing students in `students.students` using their corresponding `students.enrollments` records.

---

# 9. Cross-Cutting Concerns

- **Security & Authorization ([`ADR-009`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L226-L250)):**
  - Read queries (`GetStudentById`, `GetStudentAdministrativeHistory`) require `Permissions.Students.Records.View`.
  - Mutation commands (`ChangeAdministrativePlacement`, `ChangeStudentStatus`) require `Permissions.Students.Records.Edit`.
- **Audit Trail Immutability ([`ADR-008`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L202-L225)):**
  - `students.administrative_history_entries` is strictly append-only.
  - No `UPDATE` or `DELETE` SQL queries exist for history entries.
  - Corrections are recorded by inserting a new row with `is_correction = 1` and `corrects_entry_id = @PriorEntryId`.
- **Concurrency & Invariant Safety:**
  - Database partial unique indexes `ux_administrative_placements_student_active` and `ux_student_statuses_student_active` prevent race conditions and concurrent mutation anomalies.
  - Transactions ensure atomicity across demotion and promotion of records.
- **Performance & Batching:**
  - `GetStudentByIdHandler` retrieves the full dossier in a single database round-trip via Dapper `QueryMultipleAsync`.
  - Filtered indexes on `(student_id, effective_date DESC)` ensure low query latencies.

---

# 10. Implementation Constraints

1. **Framework & Language Standards:** Must use .NET 8, C# 12 file-scoped namespaces, primary constructors for dependency injection, and `public sealed record` for DTOs ([`coding-conventions.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/coding-conventions.md)).
2. **Data Access Integrity:** Explicit T-SQL parameterized queries via Dapper and `ISqlConnectionFactory`. No Entity Framework Core or generic repositories allowed ([`backend-architecture.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/backend-architecture.md)).
3. **Domain Outcome Discipline:** Handlers must return `Result<T>` or `Result` domain outcomes; throwing business exceptions is strictly prohibited.
4. **Compile-Time Assembly Isolation:** `Elsasa.Sis.Students` must not reference `Elsasa.Sis.Operations` or `Elsasa.Sis.Finance`. Cross-domain integration uses `Elsasa.Sis.Contracts`.
5. **Decoupled Event Publication:** `PlacementChangedEvent` and `StudentEnrolledEvent` must be published strictly post-commit using MediatR `IPublisher` ([`ADR-003`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L76-L98)).
6. **Frontend Technology Discipline:** Vue 3 `<script setup lang="ts">`, Tailwind CSS 4, PrimeVue primitives, VeeValidate with Zod schemas, and TanStack Query for server state management ([`frontend-architecture.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/frontend-architecture.md)).
7. **Zero Pinia Persistence of Server Data:** Pinia stores only client-session context; all student dossier, placement, and history collections must be stored and cached exclusively in TanStack Query.

---

# 11. Acceptance Conditions

The implementation will be considered architecturally complete when:

1. **Database Migration Verified:** Script `20261104_0101_Students_PlacementStatus_Tables.sql` runs cleanly via DbUp, successfully creating the 3 tables, partial unique indexes, foreign keys, and backfilling existing student records.
2. **Active Invariants Enforced:** Attempting to insert two active records (`is_current = 1`) for the same student in `administrative_placements` or `student_statuses` raises a database unique constraint violation error.
3. **Enrollment Intake Flow Operational:** Enrolling an accepted applicant via `EnrollAcceptedApplicantHandler` atomically creates the student with `current_status = 'active'`, inserts an initial `administrative_placements` row, records sequential `enrolled` and `active` status rows in `student_statuses`, and appends 4 intake audit entries to `administrative_history_entries`.
4. **Placement Reassignment Operational:** Invoking `POST /api/students/{id}/placements` atomically closes the prior active placement, creates the new active placement, writes `placement_changed` to history, and publishes `PlacementChangedEvent`.
5. **Status Adjustment Operational:** Invoking `POST /api/students/{id}/status` atomically closes the prior active status, inserts the new active status, updates `students.students.current_status`, and appends a status transition event to history.
6. **Query Endpoints Verified:** `GET /api/students/{id}` returns the student dossier populated with `CurrentPlacement`, `PlacementHistory`, and `StatusHistory`; `GET /api/students/{id}/history` returns the full chronological audit trail.
7. **Frontend Desk Verified:** `SCR-STU-02` at `/students/records` displays the `Placement` tab with current placement and history, the `Administrative History` tab with audit log, the active status header badge, and functional modals for `Change Placement` (`UC-STD-06`) and `Change Status`.
8. **Automated Verification:** All backend unit/integration tests and frontend Vitest component tests pass without errors.
