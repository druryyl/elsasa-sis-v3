---
Title: Feasibility Assessment - Stage 1 Enrollment & Identity Full Scope
Code: STU-ENROLLMENT-IDENTITY
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-03
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Feature being assessed: Stage 1 "Enrollment & Identity — full scope" under the Student domain, as formally captured in change request intake `CR-STU-ENROLLMENT-IDENTITY` (`docs/issues/IS004 - STU-ENROLLMENT-IDENTITY-ISSUE.md`).

Referenced artifacts:
- **ISSUE:** `docs/issues/IS004 - STU-ENROLLMENT-IDENTITY-ISSUE.md`
- **DOMAIN BLUEPRINT:** `docs/domains/student-domain.md` (Sections: Enrollment, Parent and Guardian Information, Student Number, Student)
- **ROADMAP & CRITERIA:** `docs/sub-modul-and-development-phases.md` (Stage 1 Completion Criteria 3, 4, 9, 10; Cross-Domain Dependencies)
- **WORKFLOWS & USE CASES:** `docs/operational-workflow-discovery.md` (`STD-WF-03: Enroll Accepted Applicant`, `STD-WF-04: Maintain Student Identity`, `GAP-01`), `docs/use-cases.md` (`UC-STD-04`, `UC-STD-05`, Section 8.3 Independence, Section 8.4 Multi-Guardian Authority), `docs/actors.md`
- **DATA SPECIFICATIONS:** `docs/entities/student-entities.md`, `docs/entities/student-entities-tables.md` (Submodule 2 — Enrollment & Identity)
- **SCREEN INVENTORY:** `docs/screen-inventory.md` (`SCR-STU-01: Student Intake Workbench`, `SCR-STU-02: Student Records Desk`)
- **ARCHITECTURE CONSTRAINTS:** `docs/architecture/adr.md` (ADR-001 through ADR-010)

## Objective

Assess the technical, architectural, and operational feasibility, system impact, cross-domain dependencies, persistence schema requirements, and implementation readiness for delivering the full Stage 1 Enrollment & Identity scope. 

This assessment adopts a **Pragmatic**, **Minimalist**, and **Explicit** approach to reconcile the existing baseline codebase with authoritative specifications, resolving all architectural design decisions through a structured interview process.

---

# 2. Current State

## Existing Behavior

1. **Backend Implementation (`Elsasa.Sis.Students`):**
   - The Students domain currently implements the Admission submodule (`Features/Admission`), covering application registration, document upload/verification, external entrance assessment evidence intake, and admission review/decisions (`accepted`, `rejected`, `waitlisted`, `returned`).
   - No commands, queries, validators, Dapper repositories, or Minimal API endpoints exist for Enrollment Execution, Student Number assignment, Student Identity maintenance, Guardian management, or Student Documents.
   - Domain event `StudentEnrolledEvent` is declared in `Elsasa.Sis.Contracts.Students.Events.StudentEnrolledEvent`, ready for cross-domain publication.
   - Synchronous query interface `IFinanceModule.IsEnrollmentFeePaidAsync` is declared in `Elsasa.Sis.Contracts.Finance.IFinanceModule`, but `Elsasa.Sis.Finance` currently exists only as an empty assembly skeleton without database tables.

2. **Database Persistence (`Elsasa.Sis.Database`):**
   - Migrations up to `20261103_0201_Students_Admission_Tables.sql` provide tables for applicants and admission lifecycle:
     - `students.applicants`
     - `students.admission_applications`
     - `students.admission_decisions`
     - `students.applicant_documents`
     - `students.entrance_assessment_evidence`
     - `students.admission_application_contacts`
     - Sequence `students.admission_application_number_seq`
   - Core tables for Submodule 2 (Enrollment & Identity) do **not** exist:
     - `students.students`
     - `students.enrollments`
     - `students.guardians`
     - `students.student_guardian_relationships`
     - `students.student_documents`
   - Sequence for official student numbering (`students.student_number_seq`) does not exist.

3. **Frontend Implementation (`src/frontend`):**
   - `StudentIntakeWorkbenchView.vue` (`SCR-STU-01`) provides admission intake, document verification, assessment scoring, and decision recording, but lacks an "Enroll" action and modal for accepted applicants.
   - `EnrollmentDeskView.vue` exists only as a placeholder 145-byte view stub.
   - `StudentRecordsDeskView.vue` (`SCR-STU-02`) has not yet been built.
   - No typed API clients, query composables, or Zod schemas exist for enrollment execution or student records maintenance.

## Existing Constraints

1. **Modular Monolith Architecture (ADR-001):** `Elsasa.Sis.Students` cannot reference other domain assemblies (`Operations`, `Finance`). All cross-domain collaboration must occur via `Elsasa.Sis.Contracts`.
2. **Database Schema Isolation (ADR-002):** Schema isolation between `students`, `operations`, and `finance` is mandatory. Cross-schema references are logical UUIDs only; physical cross-schema foreign keys are strictly prohibited. Intra-schema foreign keys within `students` must be fully enforced.
3. **Cross-Domain Communication (ADR-003):** Synchronous inquiries to Finance (e.g. checking enrollment fee payment) use `IFinanceModule`. Asynchronous side-effects (e.g. notifying downstream consumers upon enrollment) must publish in-memory domain events (`StudentEnrolledEvent`) via MediatR strictly **after** the primary database transaction commits.
4. **Data Access & Endpoint Patterns (ADR-004, ADR-005):** Explicit Dapper queries using C# raw string literals, `Result<T>` domain outcomes, and native Minimal APIs with FluentValidation endpoint filters.
5. **Stage 1 Roadmap Boundary:** Stage 1 Submodule 2 focuses on Enrollment & Identity. Submodule 3 (Placement & Status) multi-record history tables (`administrative_placements`, `student_statuses`, `lifecycle_transitions`) and Stage 2 lifecycle features (Leave, Promotion, Internal Transfer, Re-enrollment) are deferred to subsequent development phases.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | **Missing Persistence Schema for Enrollment & Identity:** Tables `students.students`, `students.enrollments`, `students.guardians`, `students.student_guardian_relationships`, and `students.student_documents` do not exist in the database. |
| GAP-002 | CRITICAL | **Missing Authoritative Student Number Sequence:** Sequence `students.student_number_seq` and student number formatting logic (`STD-YYYY-NNNN`) do not exist. |
| GAP-003 | CRITICAL | **Missing Enrollment Execution Logic (`STD-WF-03`, `UC-STD-04`):** No backend command handler, validator, or endpoint exists to transform an accepted applicant into an official student, enforce payment/waiver gates (`GAP-01`), capture initial placement context, and publish `StudentEnrolledEvent`. |
| GAP-004 | CRITICAL | **Missing Student Records & Identity Maintenance Slices (`STD-WF-04`, `UC-STD-05`):** No backend queries or commands exist to search students, retrieve student dossiers, update certified biographical details, manage `is_independent` status, or audit modifications. |
| GAP-005 | MAJOR | **Missing Guardian Management & Primary Contact Enforcement:** No backend logic exists to link guardians, record designated responsibilities (`is_emergency_contact`, `is_financial_contact`, `is_authorized_pickup`), or enforce the single active primary contact constraint (`is_primary_contact = true`). |
| GAP-006 | MAJOR | **Missing Document Promotion and Student Document Management:** No logic exists to promote verified `applicant_documents` into `student_documents` upon enrollment or manage ongoing official student documents. |
| GAP-007 | MAJOR | **Incomplete Student Intake Workbench UI (`SCR-STU-01`):** `StudentIntakeWorkbenchView.vue` lacks an "Enroll" trigger, enrollment parameter dialog (placement, fee payment confirmation/waiver, guardian review), and mutation wiring for accepted applications. |
| GAP-008 | MAJOR | **Missing Student Records Desk UI (`SCR-STU-02`):** `EnrollmentDeskView.vue` is a static stub; `StudentRecordsDeskView.vue` is not implemented for student search, dossier inspection, guardian editing, and certified document tracking. |

---

# 4. Open Questions

| ID | Question | Impact | Status |
|---|---|---|---|
| OQ-001 | How should the database schema and entity boundaries for Stage 1 Enrollment & Identity be scoped relative to Submodule 3 (Placement & Status)? | Governs database migration scope, table definitions, and initial placement storage. | CLOSED |
| OQ-002 | How should authoritative, unique student numbers (`STD-YYYY-NNNN`) be generated and guaranteed unique in the database? | Governs sequence generation, formatting service, concurrency safety, and indexing. | CLOSED |
| OQ-003 | Since full Finance automation follows Enrollment in Stage 1, how should the enrollment payment confirmation gate (`GAP-01`) be enforced during enrollment execution? | Governs cross-domain service consumption, payment reference recording, and fee waiver override validation. | CLOSED |
| OQ-004 | When an accepted applicant is enrolled, how should applicant biographical data, uploaded applicant documents, and admission contacts transition into the official Student record? | Governs data continuity, zero re-entry, document promotion mechanism, and guardian pre-population. | CLOSED |
| OQ-005 | How should the Primary Guardian rule (`UC-STD-05`, Section 8.4) and student independence flag (Section 8.3) be modeled and enforced? | Governs database constraints, guardian deduplication, and single active primary contact indexing. | CLOSED |
| OQ-006 | How should the frontend user interface be structured between enrollment execution (`SCR-STU-01`) and ongoing student records maintenance (`SCR-STU-02`)? | Governs UI architecture, screen inventory adherence, view component breakdown, and routing. | CLOSED |
| OQ-007 | How should the transactional boundary and domain event publishing be managed for enrollment execution? | Governs database transaction scope, consistency guarantees, and ADR-003 event publishing timing. | CLOSED |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | An applicant must have an accepted admission decision (`admission_decisions.decision_type = 'accepted'`) in `students.admission_applications` before enrollment can be executed. |
| ASM-002 | Target Academic Year, Campus, School Level, and Program exist and are administratively valid prior to enrollment. |
| ASM-003 | Local file storage infrastructure (`IFileStorageService`) established in Admission can be reused directly for storing and retrieving student documents in `student_documents`. |
| ASM-004 | In Stage 1, explicit manual payment receipt reference or authorized fee waiver entry provides full operational compliance with institutional policy before automated Finance invoicing is deployed. |
| ASM-005 | Student Administration Officers and School Leadership have authorized permissions to execute enrollment and maintain certified student records. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Concurrent enrollment attempts for the same accepted applicant causing duplicate student records. | High | Enforce unique constraint on `students.applicant_id` and validate in command handler before transaction begins. |
| RISK-002 | Violation of single primary contact rule when multiple guardians are registered. | High | Enforce SQL Server partial unique index `(student_id) WHERE (is_primary_contact = 1 AND is_active = 1)` and validate in command validator. |
| RISK-003 | Premature enrollment execution without payment compliance under strict institutional policies (`GAP-01`). | High | Command validation strictly rejects enrollment if payment is neither confirmed (`payment_reference` provided) nor waived (`payment_waived = true` with `waiver_reference`). |
| RISK-004 | Duplicate guardian records created across multiple students or enrollments. | Medium | Enforce partial unique index on `guardians.government_id_number WHERE government_id_number IS NOT NULL` and support linking existing guardians. |
| RISK-005 | Orphaned file storage references when document promotion or enrollment fails. | Medium | Promote documents within the single database transaction reusing existing `file_reference` pointers; do not duplicate physical disk files. |

---

# 7. Recommendations

## Evaluated Architectural Options

### Approach 1: Pragmatic, Minimalist, and Explicit Domain Alignment (Selected)
- **Database Schema:** Provision exactly the 5 authoritative Submodule 2 tables (`students`, `enrollments`, `guardians`, `student_guardian_relationships`, `student_documents`) plus sequence `students.student_number_seq`. Capture initial placement directly in `enrollments` (`initial_campus`, `initial_level`, `initial_academic_year`, `initial_program`, `initial_cohort`) and initial status on `students.current_status`. Multi-record placement and status history tables (`administrative_placements`, `student_statuses`, `lifecycle_transitions`) are deferred to Submodule 3.
- **Student Number Generation:** SQL Server sequence `students.student_number_seq` starting at 1, formatted via `IStudentNumberGenerator` as `STD-{EnrollmentYear}-{Seq:D4}`, backed by a strict `UNIQUE` constraint on `students.student_number`.
- **Payment Gate:** Explicit dual-mode gate. Evaluates `IFinanceModule.IsEnrollmentFeePaidAsync` if available, but enforces mandatory payment reference or fee waiver override with logged justification directly on the enrollment command, satisfying `GAP-01`.
- **Data Continuity:** Atomic snapshot promotion in the enrollment transaction: copy verified biographical info from `applicants` to `students` (1:1 `applicant_id` link), promote verified `applicant_documents` into `student_documents` sharing `file_reference`, and link/create guardians from `admission_application_contacts`.
- **Primary Contact & Independence:** Column `is_independent` (BOOLEAN DEFAULT false) on `students.students`. Database partial unique index on `(student_id) WHERE (is_primary_contact = 1 AND is_active = 1)` to guarantee exactly one active primary guardian per student.
- **Frontend Architecture:** Two-screen alignment respecting the 36-screen inventory:
  - `SCR-STU-01` (`StudentIntakeWorkbenchView.vue`): Add "Enroll Applicant" modal for accepted applications.
  - `SCR-STU-02` (`StudentRecordsDeskView.vue`): Replace stub `EnrollmentDeskView.vue` with dedicated student search, dossier inspection, certified identity updates, and guardian/document management.
- **Transaction & Events:** Single atomic database transaction for enrollment; publish `StudentEnrolledEvent` strictly post-commit via MediatR (ADR-003).

### Approach 2: Pre-emptive Submodule 3 Front-Loading (Rejected)
- Provision all 8 tables now (including `administrative_placements`, `student_statuses`, `lifecycle_transitions`).
- **Verdict:** Rejected. Violates roadmap modularity, introduces unnecessary complexity and testing surface into Submodule 2, and increases risk of migration rework when implementing Submodule 3.

### Approach 3: Unconstrained Application-Layer Soft Validation (Rejected)
- Rely only on application code for primary guardian uniqueness and student numbering without database constraints or sequences.
- **Verdict:** Rejected. Vulnerable to race conditions, concurrent enrollment conflicts, and data corruption. Violates explicit domain constraints.

---

# 8. Gap Closure

## GAP-001 & GAP-002 / OQ-001: Scope & Persistence Schema
### Decision
Execute a forward DbUp migration (`20261103_0301_Students_EnrollmentIdentity_Tables.sql`) that:
1. Creates sequence `students.student_number_seq` starting at 1.
2. Creates table `students.students`:
   - `id` (UUID PK)
   - `student_number` (NVARCHAR(32) NOT NULL, UNIQUE)
   - `applicant_id` (UUID NOT NULL, UNIQUE, FK → `students.applicants.id`)
   - `full_name` (NVARCHAR(200) NOT NULL)
   - `preferred_name` (NVARCHAR(100) NULL)
   - `date_of_birth` (DATE NULL)
   - `place_of_birth` (NVARCHAR(100) NULL)
   - `gender` (NVARCHAR(20) NULL)
   - `nationality` (NVARCHAR(100) NULL DEFAULT 'Indonesian')
   - `government_id_type` (NVARCHAR(32) NULL)
   - `government_id_number` (NVARCHAR(50) NULL)
   - `address` (NVARCHAR(500) NULL)
   - `phone` (NVARCHAR(50) NULL)
   - `email` (NVARCHAR(100) NULL)
   - `is_independent` (BIT NOT NULL DEFAULT 0)
   - `current_status` (NVARCHAR(32) NOT NULL DEFAULT 'enrolled')
   - `enrollment_date` (DATE NOT NULL)
   - `notes` (NVARCHAR(MAX) NULL)
   - Standard audit columns (`created_at`, `created_by`, `updated_at`, `updated_by`).
3. Creates table `students.enrollments`:
   - `id` (UUID PK)
   - `student_id` (UUID NOT NULL, FK → `students.students.id`)
   - `application_id` (UUID NOT NULL, FK → `students.admission_applications.id`)
   - `enrollment_type` (NVARCHAR(32) NOT NULL DEFAULT 'initial')
   - `enrollment_date` (DATE NOT NULL)
   - `initial_campus` (NVARCHAR(100) NOT NULL)
   - `initial_level` (NVARCHAR(50) NOT NULL)
   - `initial_academic_year` (NVARCHAR(20) NOT NULL)
   - `initial_program` (NVARCHAR(100) NULL)
   - `initial_cohort` (NVARCHAR(50) NULL)
   - `approved_by` (NVARCHAR(100) NOT NULL)
   - `enrollment_payment_confirmed` (BIT NULL)
   - `payment_reference` (NVARCHAR(100) NULL)
   - `payment_waived` (BIT NOT NULL DEFAULT 0)
   - `waiver_reference` (NVARCHAR(100) NULL)
   - `notes` (NVARCHAR(MAX) NULL)
   - Standard audit columns.
4. Creates table `students.guardians`:
   - `id` (UUID PK)
   - `full_name` (NVARCHAR(200) NOT NULL)
   - `government_id_type` (NVARCHAR(32) NULL)
   - `government_id_number` (NVARCHAR(50) NULL)
   - `phone` (NVARCHAR(50) NULL)
   - `email` (NVARCHAR(100) NULL)
   - `address` (NVARCHAR(500) NULL)
   - `occupation` (NVARCHAR(100) NULL)
   - `notes` (NVARCHAR(MAX) NULL)
   - Partial unique index on `government_id_number WHERE government_id_number IS NOT NULL`.
   - Standard audit columns.
5. Creates table `students.student_guardian_relationships`:
   - `id` (UUID PK)
   - `student_id` (UUID NOT NULL, FK → `students.students.id`)
   - `guardian_id` (UUID NOT NULL, FK → `students.guardians.id`)
   - `relationship_type` (NVARCHAR(50) NOT NULL)
   - `is_primary_contact` (BIT NOT NULL DEFAULT 0)
   - `is_emergency_contact` (BIT NOT NULL DEFAULT 0)
   - `is_financial_contact` (BIT NOT NULL DEFAULT 0)
   - `is_authorized_pickup` (BIT NOT NULL DEFAULT 0)
   - `is_active` (BIT NOT NULL DEFAULT 1)
   - Partial unique index on `(student_id) WHERE (is_primary_contact = 1 AND is_active = 1)`.
   - Standard audit columns.
6. Creates table `students.student_documents`:
   - `id` (UUID PK)
   - `student_id` (UUID NOT NULL, FK → `students.students.id`)
   - `document_type` (NVARCHAR(100) NOT NULL)
   - `submission_status` (NVARCHAR(32) NOT NULL DEFAULT 'submitted')
   - `submitted_date` (DATE NULL)
   - `verified_date` (DATE NULL)
   - `verified_by` (NVARCHAR(100) NULL)
   - `file_reference` (NVARCHAR(500) NULL)
   - `valid_until` (DATE NULL)
   - `notes` (NVARCHAR(MAX) NULL)
   - Standard audit columns.
### Rationale
Strictly delivers the authoritative entity specification for Submodule 2 while keeping Submodule 3 (Placement & Status) cleanly demarcated. Eliminates premature schema complexity while ensuring immediate operational completeness.
### Impact
Clean schema migration; provides foundational data models required by all downstream modules.
### Architecture Impact
Persistence layer target state defined; zero foreign key leaks across external schemas.
### Resolved By
Drury & Antigravity (Grill-Me Session)
### Resolved Date
2026-11-03

---

## OQ-002: Student Number Generation Strategy
### Decision
Implement `IStudentNumberGenerator` using SQL Server sequence `students.student_number_seq`.
Formula: `STD-{EnrollmentYear}-{Seq:D4}` (e.g. `STD-2026-0001`).
Enforce non-reusability via a strict unique constraint on `students.students.student_number`.
### Rationale
Database sequences provide non-blocking, concurrency-safe, gap-free, atomic number generation without table locking. The format is explicit, universal across campuses, and permanent.
### Impact
Reliable, race-condition-free enrollment execution under concurrent administrative requests.
### Architecture Impact
Introduces `IStudentNumberGenerator` service in `Elsasa.Sis.Students/Features/Enrollment/Services`.
### Resolved By
Drury & Antigravity (Grill-Me Session)
### Resolved Date
2026-11-03

---

## GAP-003 / OQ-003: Payment Gate & Policy Enforcement (GAP-01)
### Decision
Enforce the payment gate in `EnrollAcceptedApplicantValidator` and Handler:
1. If `payment_waived == true`, require non-empty `waiver_reference` (or executive override reason).
2. If `payment_waived == false`, require non-empty `payment_reference` OR confirm `IFinanceModule.IsEnrollmentFeePaidAsync` returns `true`.
3. If neither condition is satisfied, reject command with error code `Students.Enrollment.PaymentRequired`.
4. Record payment parameters (`enrollment_payment_confirmed`, `payment_reference`, `payment_waived`, `waiver_reference`) in `students.enrollments`.
### Rationale
Satisfies `GAP-01` ("Hard block by default with explicit administrative override") without requiring full Finance automation in Stage 1, while providing a seamless transition when automated Finance is activated.
### Impact
Enables immediate administrative enrollment with full auditability for payment verification and fee waivers.
### Architecture Impact
Enforces cross-domain handshake protocol cleanly.
### Resolved By
Drury & Antigravity (Grill-Me Session)
### Resolved Date
2026-11-03

---

## GAP-006 / OQ-004: Data & Document Continuity
### Decision
During `EnrollAcceptedApplicantCommand`:
1. Copy verified biographical fields from `students.applicants` into `students.students`.
2. Query verified `students.applicant_documents` for the application, and insert corresponding records into `students.student_documents` with `submission_status = 'verified'` and identical `file_reference`.
3. If admission contacts exist in `students.admission_application_contacts` and are confirmed in the enrollment request, match or insert `students.guardians` and create `students.student_guardian_relationships`.
### Rationale
Provides zero-re-entry continuity. Eliminates duplicate physical file storage and avoids administrative re-typing.
### Impact
Seamless transition from accepted applicant to active student dossier.
### Architecture Impact
Single-transaction copy/promote pattern within the Students domain.
### Resolved By
Drury & Antigravity (Grill-Me Session)
### Resolved Date
2026-11-03

---

## GAP-005 / OQ-005: Guardian Responsibilities & Independence Modeling
### Decision
1. Add `is_independent` (BIT NOT NULL DEFAULT 0) to `students.students`.
2. Enforce at most one active primary contact per student via partial unique index `(student_id) WHERE (is_primary_contact = 1 AND is_active = 1)` in `students.student_guardian_relationships`.
3. In `AddOrUpdateGuardianRelationshipCommand`, if `is_primary_contact == true`, automatically demote any existing primary contact for that student or require explicit confirmation.
4. Add partial unique index on `guardians.government_id_number` to prevent duplicate guardian records.
### Rationale
Directly satisfies `use-cases.md` Section 8.4 (Primary Guardian Rule) and Section 8.3 (Independent student self-service flag), guaranteeing referential and operational consistency at both database and domain layers.
### Impact
Eliminates ambiguous guardian communication; enforces clear primary authority.
### Architecture Impact
Robust relational constraint design.
### Resolved By
Drury & Antigravity (Grill-Me Session)
### Resolved Date
2026-11-03

---

## GAP-007, GAP-008 / OQ-006: Frontend Screen Architecture (SCR-STU-01 & SCR-STU-02)
### Decision
1. **Intake Workbench (`SCR-STU-01`):** In `StudentIntakeWorkbenchView.vue`, add an "Enroll" button visible only for applications in status `decided` with decision `accepted`. Clicking opens `EnrollApplicantModal.vue` allowing the officer to specify placement, enter/confirm payment reference or waiver override, verify guardians, and execute enrollment.
2. **Student Records Desk (`SCR-STU-02`):** Replace `EnrollmentDeskView.vue` with `StudentRecordsDeskView.vue` (under `/students/records`). Provides search by student number/name/campus/status, summary metrics, student detail drawer/panel, biographical editor (including `is_independent`), guardian management card, and student documents list.
### Rationale
Strictly adheres to the 36-screen inventory in `docs/screen-inventory.md`. `SCR-STU-01` handles the intake-to-enrollment handoff; `SCR-STU-02` handles ongoing student records maintenance.
### Impact
Intuitive operational UX without introducing redundant or out-of-scope screens.
### Architecture Impact
Frontend features divided cleanly into `admission` (SCR-STU-01) and `records` (SCR-STU-02).
### Resolved By
Drury & Antigravity (Grill-Me Session)
### Resolved Date
2026-11-03

---

## OQ-007: Transaction Boundary & Event Publishing
### Decision
Execute `EnrollAcceptedApplicantCommand` inside a single Dapper transaction:
1. Verify prerequisites (`applicant_id` uniqueness, application decision `accepted`).
2. Generate next `student_number` from sequence.
3. Insert `students.students`.
4. Insert `students.enrollments`.
5. Promote documents to `students.student_documents`.
6. Insert/link `guardians` and `student_guardian_relationships`.
7. Commit transaction.
8. Strictly **post-commit**, publish `StudentEnrolledEvent(studentId, applicantId, academicYearId, OccurredOn)` via MediatR (ADR-003).
### Rationale
Guarantees atomicity: no student record exists without its corresponding enrollment record. Post-commit event publishing prevents ghost event reactions if the transaction aborts.
### Impact
Rock-solid data integrity and compliance with ADR-003.
### Architecture Impact
Clean MediatR pipeline and Dapper transaction management.
### Resolved By
Drury & Antigravity (Grill-Me Session)
### Resolved Date
2026-11-03

---

# 9. Architecture Applicability

## Decision
**ARCHITECTURE-REQUIRED**

## Rationale
The Stage 1 Enrollment & Identity module introduces:
- A new persistence migration containing 5 core domain tables, multiple partial unique indexes, and 1 database sequence.
- Cross-domain integration patterns (payment gate handshake with Finance, post-commit publishing of `StudentEnrolledEvent`).
- Multiple new vertical slices (Commands: `EnrollAcceptedApplicant`, `UpdateStudentIdentity`, `AddGuardianRelationship`, `UpdateGuardianRelationship`, `UploadStudentDocument`; Queries: `SearchStudents`, `GetStudentById`, `GetStudentDocuments`).
- Implementation of a major Stage 1 screen (`SCR-STU-02: Student Records Desk`) and expansion of `SCR-STU-01`.
- New permission definitions (`Permissions.Students.Enrollment`, `Permissions.Students.Records`).

A formal ARCHITECTURE artifact (`docs/issues/IS004 - STU-ENROLLMENT-IDENTITY-ARCHITECTURE.md`) is required to define technical component boundaries, DTO contracts, Dapper SQL statements, and frontend component contracts before implementation planning begins.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated based on approved decisions

## Status

**READY-FOR-PLANNING**

## Notes

All architectural branches were thoroughly evaluated and resolved in alignment with **Pragmatic**, **Minimalist**, and **Explicit** values. Feasibility is complete. The next step is creating `docs/issues/IS004 - STU-ENROLLMENT-IDENTITY-ARCHITECTURE.md`.

---

# 11. References

Referenced artifacts:
- `docs/issues/IS004 - STU-ENROLLMENT-IDENTITY-ISSUE.md`
- `docs/domains/student-domain.md`
- `docs/sub-modul-and-development-phases.md`
- `docs/operational-workflow-discovery.md`
- `docs/use-cases.md`
- `docs/actors.md`
- `docs/screen-inventory.md`
- `docs/entities/student-entities.md`
- `docs/entities/student-entities-tables.md`
- `docs/architecture/adr.md`

Referenced codebase locations:
- `src/backend/Elsasa.Sis.Students/Features/Admission`
- `src/backend/Elsasa.Sis.Contracts/Students/Events/StudentEnrolledEvent.cs`
- `src/backend/Elsasa.Sis.Contracts/Finance/IFinanceModule.cs`
- `src/backend/Elsasa.Sis.Database/Scripts`
- `src/frontend/src/features/students/admission/views/StudentIntakeWorkbenchView.vue`
- `src/frontend/src/features/students/enrollment/views/EnrollmentDeskView.vue`
