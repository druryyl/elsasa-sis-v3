---
Title: Feasibility Assessment — Stage 2 Academic Participation & Learning Activities Full Scope
Code: CR-ACD-PARTICIPATION-LEARNING-ACTIVITIES
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-05
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Implementation of the full scope of Stage 2 "Participation & Learning Activities" under the Academic domain as specified in [IS009 - ACD-PARTICIPATION-LEARNING-ACTIVITIES-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS009%20-%20ACD-PARTICIPATION-LEARNING-ACTIVITIES-ISSUE.md) and the SIS implementation roadmap ([sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)).

Referenced artifacts:
- **DOMAIN:** [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md)
- **ENTITIES:** [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md), [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md) (`ACD-WF-03`, `ACD-WF-04`, `ACD-WF-05`, `ACD-WF-06`, `ACD-WF-11`, `ACD-WF-18`, `GAP-03`, `GAP-08`)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md) (`UC-ACD-01`, `UC-ACD-02`, `UC-ACD-03`, `UC-ACD-04`, `UC-ACD-10`, `UC-ACD-12`, `UC-ACD-15`)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md) (Academic Coordinator, Teacher, Student, Academic Authority)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md) (`SCR-ACD-05`, `SCR-ACD-01`, `SCR-ACD-02`, `SCR-SELF-03`)

## Objective

Evaluate technical feasibility, architectural consistency, codebase readiness, and structural impacts of implementing Stage 2 Participation & Learning Activities across four cohesive submodules:
1. **Academic Participation (`ACD-WF-03`, `ACD-WF-11`, `UC-ACD-10`, `UC-ACD-12`, `SCR-ACD-05`):**
   - Establish formal learning relationships between eligible active students and approved learning units (`academic_participations`).
   - Manage participation lifecycle states (`in_progress`, `paused`, `withdrawn`, `completed`) with start, end, and completion dates.
   - Support participation types (`standard`, `repeat`, `transferred_in`, `recognized_prior`), retake counters (`attempt_number`), and origin tracking (`source`: `assigned`, `student_selected`, `learning_plan`).
   - Partition student cohorts flexibly via `section_label` (e.g. "4A") without requiring an artificial standalone class table.
   - Record credit recognition for transferred-in or prior recognized learning (`source_institution`, `external_grade_or_credit`, `recognition_basis`, `approved_by`) per `GAP-08`.
   - Enforce the explicit domain boundary rule: *administrative enrollment does not automatically create academic participation*.
2. **Learning Activity Conduct (`ACD-WF-04`, `UC-ACD-02`, `SCR-ACD-01`):**
   - Manage instructional occurrences and learning containers (`learning_activities`) tied to active learning units.
   - Support activity types (`lesson`, `tutorial`, `laboratory`, `independent_work`, `project`, `discussion`, `practice`, `examination`, `remediation`, `homeroom_session`, `other`).
   - Track scheduling context (`activity_date`, `start_time`, `end_time`), cohort section (`section_label`), instructional delivery notes, and lifecycle status (`planned`, `conducted`, `cancelled`).
   - Support provisional teacher references (`teacher_assignment_id`, `teacher_name`) unblocking concurrent Stage 2 rollout before the Teaching domain is deployed.
   - Conduct statutory daily homeroom roll-call sessions (`activity_type = 'homeroom_session'`, per `GAP-03`) to produce statutory daily attendance totals without conflating with physical gate presence.
3. **Student Attendance & Audit Corrections (`ACD-WF-05`, `ACD-WF-18`, `UC-ACD-03`, `UC-ACD-15`, `SCR-ACD-01`):**
   - Record session attendance for required activities (`attendance_records`) across standard statuses: `present`, `absent`, `late`, `excused_absence`, `partial`, capturing recorder identity, date, and supporting evidence.
   - Enforce an immutable audit trail for attendance corrections (`attendance_corrections`), recording previous status, new status, mandatory justification reason, authorizer identity, and timestamp.
   - Support the standard 24-hour same-day teacher grace window, after which direct modification is locked and changes require formal justified correction.
4. **Assignment & Submission Management (`ACD-WF-06`, `ACD-WF-18`, `UC-ACD-04`, `UC-ACD-01`, `SCR-ACD-02`, `SCR-SELF-03`):**
   - Publish and manage academic coursework tasks (`assignments`) strictly originating from a learning activity (`Learning Activity → Assignment → Submission`), with title, instructions, release date, due date, and lifecycle status (`draft`, `released`, `closed`).
   - Record and track student submission responses (`submissions`) with delivery timestamp, submission reference/attachment, resubmission tracking (`is_resubmission`, `previous_submission_id`), and submission statuses (`submitted`, `late`, `missing`, `excused`, `exempted`).
   - Maintain strict separation between submission status and academic grading (formal evaluation belongs to Assessment & Results, `IS010`).

Deliver the assessment strictly adhering to **Pragmatic**, **Minimalist**, and **Explicit** architectural principles, preserving domain boundaries, compile-time assembly isolation, and the established vertical slice conventions of Elsasa SIS V3.

---

# 2. Current State

## Existing Behavior

1. **Database Schema:**
   - The database contains provisioned tables for Operations (`operations`), Students (`students`), and Finance (`finance`).
   - The `academics` schema exists and contains tables from Stage 2 Submodule 1 (`IS008`): `academics.learning_structures`, `academics.learning_units`, and `academics.academic_requirements`.
   - The tables specified for Submodule 2 in [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md) lines 150–380 (`academics.academic_participations`, `academics.learning_activities`, `academics.attendance_records`, `academics.attendance_corrections`, `academics.assignments`, `academics.submissions`) are **not yet provisioned**.
2. **Backend Domain & Services:**
   - `src/backend/Elsasa.Sis.Academics` exists and is integrated into `Elsasa.Sis.sln` and `Elsasa.Sis.Api`.
   - Existing feature slices in `Elsasa.Sis.Academics` cover Learning Structures, Learning Units, and Academic Requirements.
   - There are **no feature slices, domain handlers, or Dapper repositories** for Academic Participations, Learning Activities, Attendance, Attendance Corrections, Assignments, or Submissions.
   - Upstream contracts:
     - `IStudentsModule.IsStudentEnrolledAsync(Guid studentId)` is available in `Elsasa.Sis.Contracts/Students`, enabling synchronous validation of student enrollment eligibility.
     - `IOperationsCalendarQueryService` is available in `Elsasa.Sis.Contracts/Operations`, enabling validation of active academic periods and instructional calendar dates.
     - `IAcademicsModule` exposes synchronous queries for learning structures, learning units, and requirements.
     - The **Teaching domain does not exist yet** in `src/backend/` (no `Elsasa.Sis.Teaching` assembly or `ITeachingModule` contract).
3. **Frontend Implementation:**
   - `src/frontend/src/features/academics/curriculum` exists for `SCR-ACD-03` (Curriculum & Learning Structure Workspace).
   - There are **no implementations** for:
     - `SCR-ACD-05` (Participation & Roster Workbench)
     - `SCR-ACD-01` (Class Session Workbench)
     - `SCR-ACD-02` (Coursework & Assessment Workbench)
     - `SCR-SELF-03` (Student Learning Workspace)
   - Navigation links and Vue routes for these four screens are not yet registered.

## Existing Constraints

1. **Domain Isolation & ADR Compliance (ADR-001, ADR-002, ADR-010):**
   - `Elsasa.Sis.Academics` references only `Elsasa.Sis.Shared` and `Elsasa.Sis.Contracts`. Direct assembly references to `Students`, `Finance`, `Operations`, or `Teaching` are prohibited.
   - Schema isolation: `academics` schema owns all 6 tables. Physical foreign keys across schemas are prohibited; external keys (`student_id`, `academic_period_id`, `teacher_assignment_id`) are stored as logical UUIDs (`uniqueidentifier`).
2. **Curriculum Hierarchy Invariant (`academic-domain.md`):**
   - The domain enforces the explicit relationship hierarchy:
     `Learning Structure → Learning Unit → Learning Activity → Assignment → Submission`
   - Assignments must **never** exist independently of a Learning Activity (`learning_activity_id` NOT NULL).
   - Submissions must **never** exist independently of an Assignment (`assignment_id` NOT NULL).
3. **Participation Boundary Rule (`academic-domain.md`):**
   - Administrative enrollment in Students does not automatically create Academic Participation. Learning participation must be established explicitly within Academics.
   - A student may be enrolled but have zero active participations (e.g., new intake awaiting placement, leave of absence, or delayed start).
4. **Attendance Architecture & GAP-03 Compliance (`operational-workflow-discovery.md`):**
   - Academics owns all instructional attendance.
   - Statutory daily attendance is captured via scheduled homeroom learning activities (`activity_type = 'homeroom_session'`).
   - Physical gate/turnstile presence belongs to Operations (Stage 3) and must not overwrite academic attendance.
5. **Auditable Attendance Immutability (`academic-entities.md` Q8):**
   - Same-day edits by the recording teacher are permitted within a 24-hour grace window.
   - After grace window closure, historical attendance records cannot be silently overwritten; modifications require an explicit justification reason and authorization, logged immutably in `attendance_corrections`.
6. **Separation of Submission Tracking and Grading (`academic-domain.md`):**
   - Missing submissions do not automatically become zero scores.
   - Submissions track receipt and timeliness (`submitted`, `late`, `missing`, `excused`, `exempted`).
   - Formal evaluation and scoring belong to Assessment & Results (`IS010`).

---

# 3. Gap Analysis

| ID | Severity | Gap | Status | Resolution |
|------|------|------|:---:|------|
| `GAP-001` | CRITICAL | **Missing Database Tables:** Database lacks tables `academic_participations`, `learning_activities`, `attendance_records`, `attendance_corrections`, `assignments`, and `submissions` in schema `academics`. | **CLOSED** | Author DbUp migration script `20261105_0401_Academics_ParticipationActivities_Tables.sql` with full table DDL, intra-schema foreign keys, indexes, and check constraints matching `academic-entities-tables.md`. |
| `GAP-002` | CRITICAL | **Missing Backend Vertical Slices in `Elsasa.Sis.Academics`:** No domain models, endpoint mappings, validators, or Dapper handlers exist for the 4 submodules. | **CLOSED** | Implement vertical slices under `Features/AcademicParticipations/`, `Features/LearningActivities/`, `Features/Attendance/`, and `Features/Assignments/` using Minimal APIs and Dapper raw string queries per ADR-004 and ADR-005. |
| `GAP-003` | CRITICAL | **Dual Participation Registration Mechanics (Cohort vs Individual):** Academic Coordinators require both bulk cohort enrollment and individual student registration without violating the boundary rule that enrollment != participation. | **CLOSED** | Adopted **Dual Registration Modes (Batch Cohort + Individual Registration)** with synchronous enrollment verification via `IStudentsModule.IsStudentEnrolledAsync`. |
| `GAP-004` | MAJOR | **Provisional Teacher Reference During Concurrent Stage 2 Rollout:** The Teaching domain is not yet built, but learning activities require teacher context for class conduct and timetable reconciliation. | **CLOSED** | Adopted **Nullable Logical UUID with Optional Display Reference** (`teacher_assignment_id` UUID NULL, `teacher_name` VARCHAR NULL), unblocking activity logging without waiting for the Teaching domain. |
| `GAP-005` | MAJOR | **Attendance Grace Window & Correction Enforcement:** Need an explicit server-side mechanism to allow same-day roll call edits while enforcing mandatory justification reasons and audit logging for historical changes. | **CLOSED** | Adopted **Two Distinct Endpoints with Server-Side Temporal Grace Window**: `RecordSessionAttendance` (same calendar day) and `CorrectAttendance` (`UC-ACD-15`, requires reason, logs to `attendance_corrections`). |
| `GAP-006` | MAJOR | **Coursework Submission File & Storage Model:** System must support student coursework submissions without incurring heavy external cloud LMS storage dependencies or database bloat. | **CLOSED** | Adopted **Lightweight Hybrid Reference**: store external URLs, plain text, or local uploaded file path in `submission_reference` via a minimalist multipart upload endpoint. |
| `GAP-007` | MAJOR | **Missing Submission Tracking Without Background Worker Overhead:** Avoid running a heavy periodic daemon/cron job to generate thousands of physical `missing` rows for past-due assignments. | **CLOSED** | Adopted **On-the-Fly Query Projection for Missing Work**: calculate `missing` dynamically when past `due_date` without submission; store explicit rows only for actual student submissions or teacher exemptions (`excused`/`exempted`). |
| `GAP-008` | MAJOR | **Missing Frontend Workbenches (`SCR-ACD-05`, `SCR-ACD-01`, `SCR-ACD-02`, `SCR-SELF-03`):** No UI screens exist for participation rosters, daily instructional sessions, coursework assignments, or student self-service. | **CLOSED** | Adopted **Role-Aligned Screen Slices**: scaffold 4 dedicated features in `src/frontend/src/features/` aligning with Screen Inventory and actor boundaries. |
| `GAP-009` | MINOR | **Cross-Domain Contract & Event Definition:** Downstream modules (`Assessment & Results`, `Teaching`, `Standing & Progression`) require contract queries and domain events for participation, activities, attendance, and submissions. | **CLOSED** | Extend `IAcademicsModule` and define domain events (`AcademicParticipationCreatedEvent`, `LearningActivityConductedEvent`, `AttendanceRecordedEvent`, `AssignmentReleasedEvent`, `SubmissionReceivedEvent`) in `Elsasa.Sis.Contracts/Academics`. |

---

# 4. Open Questions

| ID | Question | Impact | Status | Decision Summary |
|------|------|------|:---:|------|
| `OQ-001` | How should Academic Participation registration (`academic_participations`, `SCR-ACD-05`) be designed for registering students into learning units? | Directly dictates coordinator operational efficiency, API contract signatures, and transactional boundaries. | **CLOSED** | **Dual Registration Modes with Synchronous Enrollment Check:** Provide both `BatchRegisterCohortParticipation` (enrolling all active students in a section/placement) and `RegisterParticipation` (individual student). Synchronously verify enrollment via `IStudentsModule`. |
| `OQ-002` | How should teacher assignments (`teacher_assignment_id` in `learning_activities`) be handled given that the Teaching domain is planned for concurrent rollout and not yet implemented? | Dictates module dependencies, schema constraints, and unblocks daily session logging. | **CLOSED** | **Nullable Logical UUID with Optional Display Reference:** Follow table spec (`teacher_assignment_id` nullable UUID) and add `teacher_name` string context on `learning_activities`, enabling immediate operation without hard compile or runtime blocks. |
| `OQ-003` | How should the 24-hour same-day teacher grace window and attendance correction audit trail (`attendance_records` vs `attendance_corrections`) be enforced in the backend? | Dictates API slice structure, security guards, and regulatory audit compliance. | **CLOSED** | **Two Distinct Endpoints with Server-Side Temporal Grace Window:** `RecordSessionAttendance` allows direct roll call within same calendar day. `CorrectAttendance` (`UC-ACD-15`) requires mandatory justification reason and authorizer, writing immutable audit log to `attendance_corrections`. |
| `OQ-004` | How should student coursework submission files/materials (`submission_reference` in `submissions`, `UC-ACD-01`) be stored and referenced? | Dictates backend storage infrastructure, I/O performance, and deployment footprint. | **CLOSED** | **Lightweight Hybrid Reference:** `submission_reference` stores external URLs (Google Docs, Drive links), inline text, or local relative file paths uploaded via a minimalist multipart upload endpoint. No heavy LMS cloud dependencies or database blob storage. |
| `OQ-005` | How should 'missing' submission statuses be generated when students do not submit assignments before the due date? | Dictates database write volume, background service scheduling, and query logic. | **CLOSED** | **On-the-Fly Query Projection for Missing Work:** Read queries dynamically project `missing` for active participants when `due_date` has elapsed with no submission. Physical rows are inserted only upon actual submission or explicit teacher override (`excused`/`exempted`). |
| `OQ-006` | How should frontend screens (`SCR-ACD-05`, `SCR-ACD-01`, `SCR-ACD-02`, `SCR-SELF-03`) be structured and routed in the frontend application? | Dictates frontend modularity, route configuration, and role-based ergonomics. | **CLOSED** | **Role-Aligned Screen Slices:** Implement 4 distinct, purpose-built feature slices: `features/academics/participation/` (`SCR-ACD-05`, Coordinator), `features/academics/sessions/` (`SCR-ACD-01`, Teacher), `features/academics/coursework/` (`SCR-ACD-02`, Teacher), and `features/portal/learning/` (`SCR-SELF-03`, Student). |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| `ASM-001` | The database engine is SQL Server 2022+ with the `academics` schema already established by `IS008`. |
| `ASM-002` | Active academic periods (`academic_period_id`) and operational calendar dates are validated via `IOperationsCalendarQueryService`. |
| `ASM-003` | Student enrollment eligibility is validated synchronously via `IStudentsModule.IsStudentEnrolledAsync(studentId)`. |
| `ASM-004` | A student may hold only one active participation (`status = 'in_progress'`) per `learning_unit_id` within a given `academic_period_id`. Repeated learning requires the prior record to be `completed` or `withdrawn` before a new row with `participation_type = 'repeat'` and incremented `attempt_number` is created. |
| `ASM-005` | Statutory daily homeroom roll-call activities are created with `activity_type = 'homeroom_session'` and `is_attendance_required = true` per `GAP-03`, allowing statutory daily presence summaries to be derived directly from `attendance_records`. |
| `ASM-006` | All user identity tracking (`recorded_by`, `corrected_by`, `approved_by`) utilizes authenticated username/email strings from `ICurrentUser`. |
| `ASM-007` | Coursework grading, scoring rubrics, and result moderation belong exclusively to `Assessment & Results` (`IS010`) and are not implemented in this issue. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| `RISK-001` | Silent overwriting of historical attendance records destroying auditability and regulatory compliance. | High | Enforce server-side date validation in command handlers; block direct updates past same-day grace window; require `CorrectAttendanceCommand` with mandatory `reason` logging to `attendance_corrections`. |
| `RISK-002` | Orphan assignments or submissions violating the domain hierarchy. | High | Enforce database `NOT NULL` foreign keys: `assignments.learning_activity_id` and `submissions.assignment_id`. Reject creation if parent container does not exist. |
| `RISK-003` | Database write churn and locking caused by background jobs generating thousands of synthetic 'missing' submission rows. | Medium | Adopt **On-the-Fly Query Projection**: calculate missing status dynamically during read queries against `due_date` and participant roster without physical synthetic rows. |
| `RISK-004` | Coupling Academics to unreleased Teaching domain, blocking class activity logging. | High | Implement `teacher_assignment_id` as a nullable logical UUID with companion `teacher_name` string context, allowing session conducting during concurrent development. |
| `RISK-005` | Massive coordinator manual data-entry fatigue when setting up term rosters for entire grade levels. | Medium | Provide `BatchRegisterCohortParticipation` allowing one-click registration of all students in a section into all mandatory learning units. |

---

# 7. Recommendations

## Academic Participation Registration Architecture (GAP-003 / OQ-001)

### Option A: Dual Registration Modes (Batch Cohort + Individual) — Recommended
- Provide `POST /api/v1/academics/participations/batch-cohort`:
  - Accepts `academic_period_id`, `learning_unit_ids` array, `section_label` (e.g. "4A"), and optional list of `student_ids`.
  - Atomically creates participation records for all active students in that section/cohort.
- Provide `POST /api/v1/academics/participations`:
  - Accepts individual student participation payload for electives, retakes, transfers, or individualized adjustments.
- Both endpoints synchronously check student active enrollment via `IStudentsModule.IsStudentEnrolledAsync`.

#### Advantages
- **Pragmatic & Highly Efficient:** Eliminates hundreds of repetitive registration clicks for school coordinators at the start of each semester.
- **Explicit Domain Boundary:** Preserves the core principle that administrative enrollment does not automatically create academic participation; participation is explicitly initiated by Academics.
- **Minimalist:** Reuses existing section labels and student IDs without introducing complex workflow orchestration daemons.

#### Disadvantages
- Requires handling partial duplicate failures in batch registration (resolved cleanly via SQL `WHERE NOT EXISTS` or transaction rollback).

---

### Option B: Individual-Only Registration
- Provide only single-student participation registration endpoints.

#### Advantages
- Smallest initial code footprint.

#### Disadvantages
- Extreme operational friction: an elementary school with 200 students taking 8 subjects requires 1,600 manual data entry actions.

---

## Attendance Grace Window & Audit Enforcement (GAP-005 / OQ-003)

### Option A: Two Dedicated Endpoints with Server-Side Temporal Guard — Recommended
- `POST /api/v1/academics/learning-activities/{id}/attendance`:
  - Roll-call recording for an activity.
  - Permitted only if `activity_date == today` (or within 24-hour grace window from initial recording). Directly updates/inserts `attendance_records`.
- `POST /api/v1/academics/attendance-records/{id}/correct`:
  - Formal correction slice (`UC-ACD-15`).
  - Required once grace window has expired.
  - Requires `new_status`, mandatory `reason` (string, min length 5), and optional `evidence`.
  - In a single SQL transaction: updates `attendance_records.status` and inserts an immutable row into `attendance_corrections`.

#### Advantages
- **Pragmatic for Teachers:** Teachers can freely correct live roll-call misclicks during class without typing justification memos.
- **Explicit & Audit Compliant:** Historical alterations are permanently locked and auditable with authorizer identity, prior status, new status, and documented reason.
- **Clean CQRS Slices:** Clear separation of operational roll call from formal regulatory record correction.

#### Disadvantages
- Minor logic branch on server side to evaluate the grace window timestamp.

---

### Option B: Uniform Audit on Any Mutation
- Every change after initial submission requires a justification reason and creates an `attendance_corrections` entry.

#### Advantages
- Simplest code (single audit path for all modifications).

#### Disadvantages
- Severe teacher friction during live classroom sessions.

---

## Coursework Submission File Reference Model (GAP-006 / OQ-004)

### Option A: Lightweight Hybrid Reference (URL, Text, or Local Stored File) — Recommended
- `submission_reference` column in `submissions` stores a string representing:
  - An external URL (e.g. `https://docs.google.com/...` or GitHub link)
  - Plain-text student response
  - A relative storage path for uploaded files (e.g. `/uploads/submissions/2026/std-108/math-hw1.pdf`)
- A dedicated minimalist endpoint `POST /api/v1/academics/submissions/upload` handles multipart file uploads to a designated application directory with safe file naming and MIME-type validation.

#### Advantages
- **Pragmatic & Flexible:** Accommodates both file-based submissions (PDF, images, Word docs) and cloud link submissions.
- **Minimalist:** Zero external cloud storage (AWS S3, Azure Blob) dependencies required for self-hosted school operation.
- **Database Cleanliness:** Keeps binary data out of SQL Server, avoiding database bloat.

#### Disadvantages
- Local filesystem requires standard directory backup alongside SQL Server backups.

---

## Missing Submission Tracking Architecture (GAP-007 / OQ-005)

### Option A: On-the-Fly Query Projection for Missing Work — Recommended
- When querying student submissions for an assignment or student workspace:
  - Query joins `academic_participations` with `assignments`.
  - If a row exists in `submissions`, return its actual status (`submitted`, `late`, `excused`, `exempted`).
  - If NO row exists in `submissions`:
    - If `CURRENT_TIMESTAMP > assignments.due_date`, project status as `missing`.
    - If `CURRENT_TIMESTAMP <= assignments.due_date`, project status as `pending`.
- Physical rows in `submissions` are created only when:
  1. Student submits work (`submitted` or `late`).
  2. Teacher explicitly grants an exception (`excused` or `exempted`).

#### Advantages
- **Minimalist & Zero Maintenance:** Eliminates background cron jobs, scheduler daemons, and database locking.
- **Zero Database Bloat:** Avoids inserting thousands of synthetic `missing` rows that must later be deleted or overwritten if assignments are extended.
- **Real-Time Accuracy:** Due date changes immediately reflect across all student statuses without batch data repair.

#### Disadvantages
- Query projection requires SQL `CASE` expression in Dapper query slices (trivial to implement and test).

---

# 8. Gap Closure

## GAP-001: Missing Database Tables
### Decision
Author DbUp migration script `20261105_0401_Academics_ParticipationActivities_Tables.sql` provisioning the 6 Submodule 2 tables in schema `academics`: `academic_participations`, `learning_activities`, `attendance_records`, `attendance_corrections`, `assignments`, and `submissions`.
### Rationale
Provides complete relational persistence matching `academic-entities-tables.md` lines 150–380, with explicit constraints, UUID PKs, intra-schema FKs, and performance indexes.
### Impact
Adds 6 tables to SQL Server database under schema `academics`.
### Architecture Impact
Strictly preserves ADR-002: zero physical foreign keys across schema boundaries; intra-schema foreign keys fully enforced.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-002: Missing Backend Vertical Slices in `Elsasa.Sis.Academics`
### Decision
Implement vertical slice feature folders under `src/backend/Elsasa.Sis.Academics/Features/`:
- `AcademicParticipations/` (`BatchRegisterCohort`, `RegisterParticipation`, `UpdateParticipationStatus`, `RecognizePriorCredit`, `ConcludeParticipation`, `GetParticipationsByUnit`, `GetStudentParticipations`)
- `LearningActivities/` (`PlanLearningActivity`, `ConductLearningActivity`, `CancelLearningActivity`, `GetActivitiesByUnit`, `GetActivityById`)
- `Attendance/` (`RecordSessionAttendance`, `CorrectAttendance`, `GetActivityAttendance`, `GetStudentAttendanceSummary`)
- `Assignments/` (`CreateAssignment`, `ReleaseAssignment`, `CloseAssignment`, `GetAssignmentsByActivity`, `SubmitCoursework`, `RecordSubmissionException`, `GetAssignmentSubmissions`, `GetStudentCourseworkWorkspace`, `UploadSubmissionFile`)
### Rationale
Follows ADR-004 (Dapper + raw SQL literals) and ADR-005 (Native Minimal APIs + direct slice handlers).
### Impact
Exposes REST endpoints under `/api/v1/academics/...` and `/api/v1/portal/learning/...`.
### Architecture Impact
Zero controller overhead; direct F12 slice navigation; high performance.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-003: Dual Participation Registration Mechanics
### Decision
Implement both batch cohort registration and single student registration with synchronous enrollment verification via `IStudentsModule.IsStudentEnrolledAsync`.
### Rationale
Supports real-world school operations where 90% of students are enrolled by class/section, while preserving individual elective/transfer handling and explicit domain boundaries.
### Impact
Coordinators can enroll an entire section with one click in `SCR-ACD-05`.
### Architecture Impact
Enforces ADR-003 synchronous contract call without physical database coupling.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-004: Provisional Teacher Reference During Concurrent Stage 2 Rollout
### Decision
Model `learning_activities.teacher_assignment_id` as a nullable `uniqueidentifier` with an accompanying `teacher_name` string context on `learning_activities`.
### Rationale
Allows teachers to conduct activities, log curriculum milestones, and take roll call immediately without waiting for the Teaching domain to be implemented.
### Impact
Unblocks Stage 2 academic conduction workflows. When Teaching is deployed later, `teacher_assignment_id` will be populated from official assignments without schema breaking changes.
### Architecture Impact
Follows ADR-002 logical UUID reference pattern.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-005: Attendance Grace Window & Correction Enforcement
### Decision
Provide two distinct endpoints: `RecordSessionAttendance` for same-day roll call, and `CorrectAttendance` (`UC-ACD-15`) requiring mandatory `reason` and inserting an immutable audit entry in `attendance_corrections`.
### Rationale
Balances live classroom operational speed with legal audit requirements for historical attendance changes.
### Impact
Guarantees historical attendance records are never silently altered while keeping live roll call frictionless.
### Architecture Impact
Explicit command handlers with date-based authorization guard.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-006: Coursework Submission File & Storage Model
### Decision
Store URL, text, or local relative file path in `submission_reference`, supported by a lightweight multipart file upload endpoint `/api/v1/academics/submissions/upload`.
### Rationale
Provides reliable file and link submission without heavy cloud S3 SDKs or SQL Server binary blob bloat.
### Impact
Students can submit either files or links; teachers can inspect submitted work directly.
### Architecture Impact
Keeps file I/O decoupled from core database transactions.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-007: Missing Submission Tracking Without Background Worker Overhead
### Decision
Project `missing` submission status dynamically in read queries when `CURRENT_TIMESTAMP > due_date` and no submission exists, creating physical rows only for actual submissions or explicit teacher exemptions (`excused`/`exempted`).
### Rationale
Eliminates background cron daemons, lock contention, and synthetic row pollution in the database.
### Impact
Zero background daemon infrastructure required; instant real-time accuracy.
### Architecture Impact
Clean CQRS read query projection using SQL `CASE`.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-008: Missing Frontend Workbenches
### Decision
Implement 4 dedicated feature slices in `src/frontend/src/features/`:
- `features/academics/participation/` (`SCR-ACD-05`, Academic Coordinator)
- `features/academics/sessions/` (`SCR-ACD-01`, Teacher Sessions & Attendance)
- `features/academics/coursework/` (`SCR-ACD-02`, Teacher Coursework & Submissions)
- `features/portal/learning/` (`SCR-SELF-03`, Student Learning Workspace)
### Rationale
Aligns strictly with Screen Inventory (`screen-inventory.md`) and actor role boundaries.
### Impact
Delivers full Stage 2 UI capabilities for coordinators, teachers, and students.
### Architecture Impact
Preserves modular component architecture and role-based route guards.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-009: Cross-Domain Contract & Event Definition
### Decision
Extend `IAcademicsModule` with synchronous query methods for downstream consumers (e.g. checking student participation and attendance totals) and define domain event records in `Elsasa.Sis.Contracts/Academics/Events/`.
### Rationale
Provides clean synchronous contracts and post-commit side-effect events per ADR-003.
### Impact
Enables `Assessment & Results`, `Teaching`, and `Standing & Progression` to consume Stage 2 learning evidence cleanly.
### Architecture Impact
Preserves compile-time assembly isolation across domains.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

# 9. Architecture Applicability

## Decision
**ARCHITECTURE-REQUIRED**

## Rationale
Implementing Stage 2 Participation & Learning Activities introduces:
1. **6 new core persistence tables** in SQL Server (`academic_participations`, `learning_activities`, `attendance_records`, `attendance_corrections`, `assignments`, `submissions`).
2. **Cross-domain integration boundaries** with `Students` (enrollment eligibility validation), `Operations` (calendar periods and instructional day validation), and provisional `Teaching` context.
3. **Auditable regulatory immutability rules** for attendance corrections (`attendance_corrections`) and submission resubmission chains.
4. **4 distinct frontend screens** across 3 different actor personas (Academic Coordinator, Teacher, Student).
5. **Significant architectural decisions** on dynamic missing status projection, temporal grace windows, and hybrid file submission storage.

These structural changes require a formal `ARCHITECTURE` artifact before implementation planning can proceed.

---

# 10. Planning Readiness

## Readiness Checklist
- [x] All critical gaps resolved (`GAP-001` through `GAP-009` CLOSED)
- [x] All required decisions recorded (`OQ-001` through `OQ-006` CLOSED)
- [x] All blocking open questions resolved
- [x] Architecture Applicability decided (`ARCHITECTURE-REQUIRED`)
- [x] Architecture can be finalized or updated based on approved decisions

## Status
**READY-FOR-PLANNING**

## Notes
Feasibility assessment is complete and fully resolved. All decisions reflect the **Pragmatic**, **Minimalist**, and **Explicit** architectural principles. The next step is to create or update the formal **ARCHITECTURE** artifact for `CR-ACD-PARTICIPATION-LEARNING-ACTIVITIES`, followed by the executable **IMPLEMENTATION-PLAN**.

---

# 11. References

Referenced artifacts:
- **DOMAIN:** [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md)
- **ENTITIES:** [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md), [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md) (`ACD-WF-03`, `ACD-WF-04`, `ACD-WF-05`, `ACD-WF-06`, `ACD-WF-11`, `ACD-WF-18`, `GAP-03`, `GAP-08`)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md) (`UC-ACD-01`, `UC-ACD-02`, `UC-ACD-03`, `UC-ACD-04`, `UC-ACD-10`, `UC-ACD-12`, `UC-ACD-15`)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md) (`SCR-ACD-05`, `SCR-ACD-01`, `SCR-ACD-02`, `SCR-SELF-03`)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md) (ADR-001 through ADR-010)
- **ISSUE:** [IS009 - ACD-PARTICIPATION-LEARNING-ACTIVITIES-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS009%20-%20ACD-PARTICIPATION-LEARNING-ACTIVITIES-ISSUE.md)

Referenced codebase locations:
- `src/backend/Elsasa.Sis.Academics/`
- `src/backend/Elsasa.Sis.Contracts/Academics/`
- `src/backend/Elsasa.Sis.Contracts/Students/IStudentsModule.cs`
- `src/backend/Elsasa.Sis.Contracts/Operations/IOperationsCalendarQueryService.cs`
- `src/backend/Elsasa.Sis.Database/Scripts/`
- `src/frontend/src/features/academics/`
- `src/frontend/src/features/portal/`
