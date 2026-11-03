---
Title: Feasibility Assessment - Stage 1 Placement & Status Core Submodules
Code: STU-PLACEMENT-STATUS
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-04
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Feature being assessed: Stage 1 "Placement & Status — core submodules only: Administrative Placement, Student Status Management, Administrative History" under the Student domain, as formally captured in change request intake `CR-STU-PLACEMENT-STATUS` (`docs/issues/IS005 - STU-PLACEMENT-STATUS-ISSUE.md`).

Referenced artifacts:
- **ISSUE:** `docs/issues/IS005 - STU-PLACEMENT-STATUS-ISSUE.md`
- **DOMAIN BLUEPRINT:** `docs/domains/student-domain.md` (Sections: Administrative Placement lines 151–171; Student Status lines 173–196; Administrative History lines 320–330, 1590; Cohort Placement line 180)
- **ROADMAP & CRITERIA:** `docs/sub-modul-and-development-phases.md` (Stage 1 Module: Placement & Status; Criteria 3, 5, 10; Section E Domain Boundary Issues - Homeroom ownership resolution)
- **WORKFLOWS & USE CASES:** `docs/operational-workflow-discovery.md` (`STD-WF-05: Change Administrative Placement`; Criteria 2–5 Traceability), `docs/use-cases.md` (`UC-STD-06: Change Administrative Placement`), `docs/actors.md`
- **DATA SPECIFICATIONS:** `docs/entities/student-entities.md` (Submodule 3 — Placement & Status; Decisions 5, 6, 9; Assumptions), `docs/entities/student-entities-tables.md` (Submodule 3: `administrative_placements`, `student_statuses`, `administrative_history_entries`, lines 418–485, 742–770)
- **SCREEN INVENTORY:** `docs/screen-inventory.md` (`SCR-STU-02: Student Records Desk` - Administrative placement history, current placement card, status history, and administrative history audit trail)
- **ARCHITECTURE CONSTRAINTS:** `docs/architecture/adr.md` (ADR-001 through ADR-010)

## Objective

Assess the technical, architectural, and operational feasibility, system impact, persistence schema requirements, invariant enforcement mechanisms, query projection requirements, and implementation readiness for delivering the Stage 1 Placement & Status core submodules.

This assessment strictly embodies a **Pragmatic**, **Minimalist**, and **Explicit** approach to reconcile the existing baseline codebase with authoritative specifications, resolving all architectural design decisions through a structured collaborative interview.

---

# 2. Current State

## Existing Behavior

1. **Backend Implementation (`Elsasa.Sis.Students`):**
   - The Students domain implements Submodule 1 Admission (`Features/Admission`) and Submodule 2 Enrollment & Identity (`Features/Enrollment`, `Features/Records`).
   - `EnrollAcceptedApplicantHandler` executes the official enrollment of an accepted applicant: assigns student number, writes `students.students` (with `current_status = 'enrolled'`), writes `students.enrollments` (capturing initial placement fields: `initial_campus`, `initial_level`, `initial_academic_year`, `initial_program`, `initial_cohort`), links guardians, and promotes verified credentials to `students.student_documents`.
   - However, `EnrollAcceptedApplicantHandler` does **not** provision a dedicated record in `students.administrative_placements`, does **not** transition student status to `active` via `students.student_statuses`, and does **not** log intake events into `students.administrative_history_entries`.
   - `GetStudentByIdHandler` projects the 360-degree student dossier (`StudentDetailResponse`) containing identity, guardian matrix, certified documents, and enrollment history, but does **not** project current administrative placement, historical placement periods, status periods, or administrative history audit logs.
   - No vertical slices exist to change administrative placement (`ChangeAdministrativePlacement` for `STD-WF-05`, `UC-STD-06`), adjust student status (`ChangeStudentStatus`), or query chronological administrative history (`GetStudentAdministrativeHistory`).

2. **Database Persistence (`Elsasa.Sis.Database`):**
   - Migrations up to `20261103_0301_Students_EnrollmentIdentity_Tables.sql` provide tables for Admission and Enrollment & Identity:
     - `students.applicants`, `students.admission_applications`, `students.admission_decisions`, `students.applicant_documents`, `students.entrance_assessment_evidence`
     - `students.students`, `students.enrollments`, `students.guardians`, `students.student_guardian_relationships`, `students.student_documents`
     - Sequence `students.student_number_seq`
   - Core tables for Submodule 3 (Placement & Status) do **not** exist in the database:
     - `students.administrative_placements`
     - `students.student_statuses`
     - `students.administrative_history_entries`

3. **Frontend Implementation (`src/frontend`):**
   - `StudentRecordsDeskView.vue` (`SCR-STU-02`) provides student search, metrics counters, dossier overview, and four functional tabs: `Certified Identity`, `Guardians & Household`, `Official Documents`, and `Enrollment History`.
   - `SCR-STU-02` lacks UI components to inspect current administrative placement (campus, school level, grade, academic year, homeroom, cohort, program, stream), view historical placement periods, trigger a "Change Placement" modal (`UC-STD-06`), inspect status history, or browse the immutable chronological administrative history audit log.

## Existing Constraints

1. **Modular Monolith Architecture (ADR-001):** `Elsasa.Sis.Students` cannot reference other domain assemblies (`Operations`, `Finance`). All cross-domain collaboration occurs via `Elsasa.Sis.Contracts`.
2. **Database Schema Isolation (ADR-002):** Schema isolation between `students`, `operations`, and `finance` is mandatory. Logical references (e.g. `academic_year`, `campus`) are stored as explicit strings/UUIDs; physical cross-schema foreign keys are strictly prohibited. Intra-schema foreign keys within `students` must be fully enforced.
3. **Cross-Domain Communication (ADR-003):** Domain events (e.g. `PlacementChangedEvent`) published via MediatR must be triggered strictly **after** primary database transactions commit.
4. **Data Access & Endpoint Patterns (ADR-004, ADR-005):** Explicit Dapper queries using C# raw string literals, `Result<T>` domain outcomes, and native Minimal APIs with FluentValidation endpoint filters.
5. **Stage 1 Scope Boundary:** Advanced lifecycle transitions (Leave of Absence, Re-enrollment, Promotion/Retention Execution, Internal Transfer, External Transfer Out/In, Withdrawal, Graduation Execution, Alumni) belong to Stage 2 and Stage 3 and are strictly deferred.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | **Missing Persistence Schema for Placement & Status:** Tables `students.administrative_placements`, `students.student_statuses`, and `students.administrative_history_entries` do not exist in the database. |
| GAP-002 | CRITICAL | **Missing Single Active Placement & Status Database Invariants:** No database-level partial unique indexes exist to enforce that a student has at most one active placement (`is_current = 1`) across all campuses and at most one active status (`is_current = 1`). |
| GAP-003 | CRITICAL | **Incomplete Enrollment Intake Sequence Provisioning (`STD-WF-03`, Criteria 3, 5, 10):** `EnrollAcceptedApplicantHandler` does not insert an initial `administrative_placements` record, does not record the authoritative status transition (`enrolled` -> `active`), and does not emit intake audit entries (`application_submitted`, `application_accepted`, `enrollment_completed`, `student_activated`) into `administrative_history_entries`. |
| GAP-004 | CRITICAL | **Missing Change Administrative Placement Vertical Slice (`STD-WF-05`, `UC-STD-06`):** No backend command handler, validator, or endpoint exists to reassign an active student's campus, school level, grade, academic year, homeroom cohort, program, or stream with effective dating, atomic record closure, and history logging. |
| GAP-005 | MAJOR | **Missing Student Status Adjustment Slice:** No backend command handler exists to adjust student administrative status and record authoritative status periods in `student_statuses` with synchronized denormalized status on `students.students`. |
| GAP-006 | MAJOR | **Missing Administrative History & Placement Queries:** `GetStudentByIdHandler` lacks projection of current placement and status history; no dedicated query slice exists to fetch chronological administrative history (`GetStudentAdministrativeHistory`). |
| GAP-007 | MAJOR | **Missing Placement & History UI Tabs in `SCR-STU-02`:** `StudentRecordsDeskView.vue` has no tabs or modals for viewing current placement, inspecting placement history, executing placement changes (`UC-STD-06`), or inspecting the chronological administrative audit trail. |
| GAP-008 | MINOR | **Unmigrated Historical Enrolled Students:** Existing student records created prior to Submodule 3 lack rows in `administrative_placements`, `student_statuses`, and `administrative_history_entries`, causing potential null references on inspection. |

---

# 4. Open Questions

| ID | Question | Impact | Status |
|---|---|---|---|
| OQ-001 | Which database schema and persistence model should be used for Stage 1 Placement & Status relative to `lifecycle_transitions` and `administrative_decisions`? | Governs database migration scope, table definitions, foreign key relationships, and complexity. | CLOSED |
| OQ-002 | How should the student status transition to 'Active' and initial placement be orchestrated during Enrollment Execution? | Governs `EnrollAcceptedApplicantHandler` transaction logic, status history creation, and initial audit trail. | CLOSED |
| OQ-003 | How should effective dating and placement record transitions be handled in `ChangeAdministrativePlacement`? | Governs concurrency, status flipping, effective dating validation, and avoids background daemon requirements. | CLOSED |
| OQ-004 | Should the database migration include an idempotent data backfill for existing enrolled students? | Governs data continuity for existing development/test database records and query safety. | CLOSED |
| OQ-005 | How should the API vertical slices and query projections for Placement, Status, and Administrative History be organized? | Governs API endpoint structure, DTO contracts, and frontend data-fetching efficiency. | CLOSED |
| OQ-006 | How should Placement, Status, and Administrative History be surfaced in the `SCR-STU-02` (Student Records Desk) UI? | Governs frontend view component breakdown, tab layout, modal workflows, and screen inventory alignment. | CLOSED |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | In Stage 1, a student's administrative placement represents their official organizational location (campus, level, grade, academic year, homeroom cohort); classroom physical allocation and teacher advisory assignment are maintained by Operations and Teaching in Stage 2. |
| ASM-002 | The active academic year context is provided by `operations.academic_years` or specified explicitly as a valid academic year identifier string. |
| ASM-003 | Student Administration Officers and School Leadership have authorized permissions (`Permissions.Students.Records`) to execute placement changes and status adjustments. |
| ASM-004 | A student's current operational status denormalized on `students.students.current_status` must always stay strictly synchronized with the latest active row in `students.student_statuses`. |
| ASM-005 | Corrections to administrative history do not overwrite existing entries; they append a new entry marked with `is_correction = 1` referencing `corrects_entry_id`. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Concurrent placement changes creating multiple active placements for the same student. | High | Enforce SQL Server partial unique index `CREATE UNIQUE INDEX ux_administrative_placements_student_active ON students.administrative_placements (student_id) WHERE (is_current = 1)` and execute updates and inserts in a serializable/atomic transaction. |
| RISK-002 | Status desynchronization between `students.students.current_status` and `students.student_statuses`. | High | Wrap all status updates in atomic database transactions updating both the authoritative period row and the denormalized student column simultaneously. |
| RISK-003 | Silent overwriting or omission of administrative lifecycle audit trails. | High | Make `students.administrative_history_entries` append-only via strict command handler discipline, preventing SQL UPDATE or DELETE on historical rows. |
| RISK-004 | Existing enrolled students in development/test environments failing queries or displaying blank placement screens. | Medium | Include idempotent SQL backfill in migration `20261104_0101` sourcing initial placement from `students.enrollments` and active status from `students.students`. |
| RISK-005 | Overcomplicating Stage 1 with complex background scheduled workers for future effective dates. | Medium | Adopt Pragmatic Immediate Activation: placement change immediately closes the prior record and activates the new record with its stated effective date. |

---

# 7. Recommendations

## Evaluated Architectural Approaches

### Approach 1: Pragmatic, Minimalist, and Explicit Domain Alignment (Selected)
- **Database Schema:** Provision exactly 3 focused, explicit domain tables in migration `20261104_0101_Students_PlacementStatus_Tables.sql`:
  1. `students.administrative_placements` (core mandatory: `campus`, `school_level`, `grade`, `academic_year`, `effective_date`; optional extensions: `program`, `stream`, `cohort`, `homeroom`, `reason`, `authorized_by`, `decision_reference`; partial unique index `(student_id) WHERE (is_current = 1)`).
  2. `students.student_statuses` (authoritative status periods: `status`, `effective_date`, `end_date`, `is_current`, `reason`, `authorized_by`, `decision_reference`; partial unique index `(student_id) WHERE (is_current = 1)`).
  3. `students.administrative_history_entries` (immutable chronological event log: `event_type`, `event_date`, `detail`, `authorized_by`, `decision_reference`, `is_correction`, `corrects_entry_id`).
  - Store authorizer (`authorized_by`) and decision reference (`decision_reference`) directly as explicit columns, deferring separate `lifecycle_transitions` and `administrative_decisions` tables to Stage 2.
- **Intake Orchestration:** Enhance `EnrollAcceptedApplicantHandler` to atomically:
  - Create the initial `administrative_placements` record (`is_current = 1`).
  - Record the sequential status transition (`enrolled` followed immediately by `active` with `is_current = 1` and `students.students.current_status = 'active'`).
  - Append the full intake audit trail (`application_submitted`, `application_accepted`, `enrollment_completed`, `student_activated`) into `administrative_history_entries`.
- **Placement & Status Mutation Slices:**
  - `ChangeAdministrativePlacementCommand` (`POST /api/students/{id}/placements`): Atomically closes existing active placement (`is_current = 0, end_date = @EffectiveDate`), inserts new active placement (`is_current = 1, effective_date = @EffectiveDate`), and appends `placement_changed` to history.
  - `ChangeStudentStatusCommand` (`POST /api/students/{id}/status`): Atomically closes existing active status, inserts new active status, synchronizes `students.students.current_status`, and appends status event to history.
- **Query Projection:**
  - Extend `StudentDetailResponse` (`GET /api/students/{id}`) to include `CurrentPlacement`, `PlacementHistory`, and `StatusHistory`.
  - Provide dedicated query slice `GetStudentAdministrativeHistory` (`GET /api/students/{id}/history`) for the chronological audit log.
- **Frontend Architecture (`SCR-STU-02`):**
  - Add `Placement` tab (`StudentPlacementTab.vue`) displaying current placement card, historical placements timeline, and "Change Placement" modal (`ChangePlacementModal.vue`).
  - Add `Administrative History` tab (`StudentAdministrativeHistoryTab.vue`) rendering the chronological audit timeline with event filters.
  - Surface authoritative status badge in dossier header with a "Change Status" quick action modal (`ChangeStatusModal.vue`).
- **Data Continuity:** Safe idempotent `INSERT ... SELECT` backfill in migration `20261104_0101` for existing student records.

### Approach 2: Full Enterprise 5-Table Normalized Model (Rejected)
- Provision all 5 tables (`administrative_placements`, `student_statuses`, `administrative_history_entries`, `lifecycle_transitions`, `administrative_decisions`) immediately in Stage 1.
- Every placement change requires creating an `administrative_decision` row, a `lifecycle_transition` row, a `student_statuses` or `administrative_placements` row, and an `administrative_history_entries` row.
- **Verdict:** Rejected. High operational and development overhead; violates minimalist principles when formal multi-committee decision entities belong to Stage 2/3.

### Approach 3: Background Worker Daemon for Deferred Effective Dating (Rejected)
- Placements with future effective dates (`effective_date > TODAY`) are inserted with `is_current = 0`, requiring a background scheduled job to monitor and activate records on their effective date.
- **Verdict:** Rejected. Unnecessary infrastructure complexity for Stage 1. Immediate activation with explicit business effective date satisfies all operational needs cleanly.

---

# 8. Gap Closure

## GAP-001: Missing Persistence Schema for Placement & Status

### Decision
Provision 3 focused tables in `src/backend/Elsasa.Sis.Database/Scripts/20261104_0101_Students_PlacementStatus_Tables.sql`:
1. `students.administrative_placements`
2. `students.student_statuses`
3. `students.administrative_history_entries`

### Rationale
Provides explicit storage for placement periods, status periods, and immutable audit logs without premature normalization of Stage 2 decision entities. Fully satisfies criteria 3, 5, and 10.

### Impact
Defines the schema baseline for all Submodule 3 operations.

### Architecture Impact
Requires new migration script in `Elsasa.Sis.Database`.

### Resolved By
Drury & Antigravity (Collaborative Alignment)

### Resolved Date
2026-11-04

---

## GAP-002: Missing Single Active Placement & Status Database Invariants

### Decision
Enforce single active record invariants directly at the database engine level using SQL Server partial unique indexes:
- `CREATE UNIQUE INDEX [ux_administrative_placements_student_active] ON [students].[administrative_placements] ([student_id]) WHERE ([is_current] = 1);`
- `CREATE UNIQUE INDEX [ux_student_statuses_student_active] ON [students].[student_statuses] ([student_id]) WHERE ([is_current] = 1);`

### Rationale
Guarantees domain integrity (Decision 6) at the storage engine level, preventing race conditions and concurrent mutation anomalies.

### Impact
Command handlers must update prior active rows (`is_current = 0`) before inserting new active rows (`is_current = 1`) within the same transaction.

### Architecture Impact
Included in migration `20261104_0101`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-003: Incomplete Enrollment Intake Sequence Provisioning (`STD-WF-03`, Criteria 3, 5, 10)

### Decision
Update `EnrollAcceptedApplicantHandler` to execute the complete intake sequence within its primary database transaction:
1. Insert initial `students.administrative_placements` row (`campus = initial_campus`, `school_level = initial_level`, `grade = grade/level`, `academic_year = initial_academic_year`, `program = initial_program`, `cohort = initial_cohort`, `effective_date = enrollment_date`, `is_current = 1`, `reason = 'Initial enrollment placement'`).
2. Insert sequential status rows in `students.student_statuses`:
   - Row 1: `status = 'enrolled'`, `effective_date = enrollment_date`, `end_date = enrollment_date`, `is_current = 0`, `reason = 'Initial enrollment completed'`.
   - Row 2: `status = 'active'`, `effective_date = enrollment_date`, `end_date = NULL`, `is_current = 1`, `reason = 'Activated upon enrollment completion'`.
3. Update denormalized `students.students.current_status = 'active'`.
4. Append intake audit entries to `students.administrative_history_entries`:
   - `application_submitted`
   - `application_accepted`
   - `enrollment_completed`
   - `student_activated`

### Rationale
Satisfies Criterion 3 and Criterion 5 end-to-end upon enrollment completion while creating a rich, traceable intake history.

### Impact
Ensures newly enrolled students are immediately active and placed in the school's organizational hierarchy.

### Architecture Impact
Enhancement of `EnrollAcceptedApplicantHandler.cs`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-004: Missing Change Administrative Placement Vertical Slice (`STD-WF-05`, `UC-STD-06`)

### Decision
Implement vertical slice `ChangeAdministrativePlacement` in `Features/Records/Placements/ChangeAdministrativePlacement`:
- Command: `ChangeAdministrativePlacementCommand` (`studentId`, `campus`, `schoolLevel`, `grade`, `academicYear`, `effectiveDate`, `program`, `stream`, `cohort`, `homeroom`, `reason`, `authorizedBy`, `decisionReference`).
- Validator: Enforces mandatory fields and validates that `effectiveDate` is not prior to the current placement's `effectiveDate`.
- Handler: Updates existing active placement (`is_current = 0, end_date = @effectiveDate`), inserts new active placement (`is_current = 1, effective_date = @effectiveDate`), and appends `placement_changed` event to `students.administrative_history_entries`.
- Endpoint: `POST /api/students/{id}/placements`.

### Rationale
Delivers `STD-WF-05` and `UC-STD-06` cleanly with strict audit preservation.

### Impact
Allows Student Administration Officers to reassign cohort, grade, campus, or homeroom with effective dating.

### Architecture Impact
New command vertical slice in `Elsasa.Sis.Students`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-005: Missing Student Status Adjustment Slice

### Decision
Implement vertical slice `ChangeStudentStatus` in `Features/Records/Status/ChangeStudentStatus`:
- Command: `ChangeStudentStatusCommand` (`studentId`, `status`, `effectiveDate`, `reason`, `authorizedBy`, `decisionReference`).
- Validator: Validates allowed Stage 1 statuses (`active`, `enrolled`).
- Handler: Closes current status (`is_current = 0, end_date = @effectiveDate`), inserts new status row (`is_current = 1`), updates `students.students.current_status`, and appends status event to `students.administrative_history_entries`.
- Endpoint: `POST /api/students/{id}/status`.

### Rationale
Gives Student Administration Officers explicit authority over student status management while preserving complete status period history.

### Impact
Enables manual status changes and corrections.

### Architecture Impact
New command vertical slice in `Elsasa.Sis.Students`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-006: Missing Administrative History & Placement Queries

### Decision
1. Extend `StudentDetailResponse` (`GET /api/students/{id}`) to include:
   - `CurrentPlacement` (`AdministrativePlacementDto`)
   - `PlacementHistory` (`IReadOnlyList<AdministrativePlacementDto>`)
   - `StatusHistory` (`IReadOnlyList<StudentStatusDto>`)
2. Implement dedicated query slice `GetStudentAdministrativeHistory` in `Features/Records/History/GetStudentAdministrativeHistory` (`GET /api/students/{id}/history`) returning `IReadOnlyList<AdministrativeHistoryEntryDto>`.

### Rationale
Supports immediate dossier rendering on `SCR-STU-02` while keeping the full immutable audit trail queryable with minimal payload overhead.

### Impact
Enriches the 360-degree student dossier projection.

### Architecture Impact
Updated `GetStudentByIdHandler.cs` and new query vertical slice.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-007: Missing Placement & History UI Tabs in `SCR-STU-02`

### Decision
Update `StudentRecordsDeskView.vue` in `src/frontend`:
1. Add `Placement` tab (`StudentPlacementTab.vue`) showing current placement card, historical placements timeline, and "Change Placement" trigger button.
2. Add `Administrative History` tab (`StudentAdministrativeHistoryTab.vue`) showing chronological event timeline with event type badge, date, authorizer, and details.
3. Add modals: `ChangePlacementModal.vue` (`UC-STD-06`) and `ChangeStatusModal.vue`.
4. Surface current authoritative status in dossier header with a quick action to trigger `ChangeStatusModal.vue`.

### Rationale
Fulfills `SCR-STU-02` screen inventory requirements for administrative placement and history inspection.

### Impact
Provides complete user interface for Student Administration Officers.

### Architecture Impact
New Vue components, composables, and Zod schemas in `features/students/records/`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-008: Unmigrated Historical Enrolled Students

### Decision
Include idempotent SQL backfill scripts in migration `20261104_0101_Students_PlacementStatus_Tables.sql`:
- Backfill active `students.administrative_placements` from `students.enrollments` for any student lacking a placement record.
- Backfill active `students.student_statuses` from `students.students` for any student lacking a status record.
- Backfill foundational intake entries into `students.administrative_history_entries`.

### Rationale
Prevents null reference exceptions and data gaps in existing development and test databases.

### Impact
Existing enrolled students immediately render seamlessly in `SCR-STU-02`.

### Architecture Impact
Part of migration script.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## OQ-001 through OQ-006: Design Decisions Summary

| ID | Decision Summary |
|---|---|
| OQ-001 | Provision 3-table model (`administrative_placements`, `student_statuses`, `administrative_history_entries`) storing authorization and decision reference directly on rows. Defer `lifecycle_transitions` and `administrative_decisions` tables to Stage 2. |
| OQ-002 | Automated complete intake sequence in `EnrollAcceptedApplicantHandler`: provisions initial placement, records sequential status (`enrolled` -> `active`), and appends intake audit trail. Provide explicit `ChangeStudentStatus` command for subsequent adjustments. |
| OQ-003 | Pragmatic immediate activation with effective dating: placement changes immediately close prior placement and activate new placement with recorded business effective date, without requiring background scheduler daemons in Stage 1. |
| OQ-004 | Idempotent data backfill included in migration `20261104_0101` for existing enrolled students. |
| OQ-005 | Hybrid query design: extend `StudentDetailResponse` with current placement, placement history, and status history; provide dedicated `GetStudentAdministrativeHistory` query slice for the audit log. |
| OQ-006 | Dedicated `Placement` and `Administrative History` tabs in `SCR-STU-02` with dedicated modals (`ChangePlacementModal.vue`, `ChangeStatusModal.vue`). |

---

# 9. Architecture Applicability

## Decision
**ARCHITECTURE-REQUIRED**

## Rationale
The Stage 1 Placement & Status core submodules introduce:
- A new database migration (`20261104_0101_Students_PlacementStatus_Tables.sql`) establishing 3 core tables, multiple partial unique indexes, and an idempotent data backfill.
- Critical domain invariants: Single active placement per student across all campuses (`WHERE is_current = 1`) and single active status per student (`WHERE is_current = 1`).
- Multiple new backend vertical slices (Commands: `ChangeAdministrativePlacement`, `ChangeStudentStatus`; Queries: `GetStudentAdministrativeHistory`; Handler updates: `EnrollAcceptedApplicantHandler`, `GetStudentByIdHandler`).
- Expansion of `SCR-STU-02` (`StudentRecordsDeskView.vue`) with two new tabs (`StudentPlacementTab.vue`, `StudentAdministrativeHistoryTab.vue`) and two new modal workflows (`ChangePlacementModal.vue`, `ChangeStatusModal.vue`).
- Domain event publication (`PlacementChangedEvent`).

A formal ARCHITECTURE artifact (`docs/issues/IS005 - STU-PLACEMENT-STATUS-ARCHITECTURE.md`) is required to define technical component boundaries, DTO contracts, Dapper SQL statements, and frontend component contracts before implementation planning begins.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated

## Status
**READY-FOR-PLANNING**

## Notes
Feasibility assessment is complete. All architectural decisions adhere to the Pragmatic, Minimalist, and Explicit principles. The system is ready to proceed to formal ARCHITECTURE artifact definition and subsequent IMPLEMENTATION-PLAN creation.

---

# 11. References

Referenced artifacts:
- `docs/issues/IS005 - STU-PLACEMENT-STATUS-ISSUE.md`
- `docs/domains/student-domain.md`
- `docs/sub-modul-and-development-phases.md`
- `docs/entities/student-entities.md`
- `docs/entities/student-entities-tables.md`
- `docs/operational-workflow-discovery.md`
- `docs/use-cases.md`
- `docs/screen-inventory.md`
- `docs/actors.md`
- `docs/architecture/adr.md`

Referenced codebase locations:
- `src/backend/Elsasa.Sis.Database/Scripts/`
- `src/backend/Elsasa.Sis.Students/Features/Enrollment/EnrollAcceptedApplicant/EnrollAcceptedApplicantHandler.cs`
- `src/backend/Elsasa.Sis.Students/Features/Records/GetStudentById/`
- `src/frontend/src/features/students/records/`
