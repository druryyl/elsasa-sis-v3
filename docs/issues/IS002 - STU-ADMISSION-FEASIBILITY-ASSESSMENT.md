---
Title: Feasibility Assessment - Stage 1 Admission Full Scope
Code: STU-ADMISSION
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-03
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Feature being assessed: Stage 1 "Admission — full scope (entrance assessment evidence entered as manual/external input; full Academics integration deferred to Stage 2)" under the Student domain, as captured in change request intake `CR-STU-ADMISSION` (`docs/issues/IS002 - STU-ADMISSION-ISSUE.md`).

Referenced artifacts:
- **DOMAIN:** `docs/domains/student-domain.md`
- **FEATURE:** `docs/sub-modul-and-development-phases.md` (Stage 1), `docs/operational-workflow-discovery.md` (`STD-WF-01`, `STD-WF-02`), `docs/use-cases.md` (`UC-STD-01`, `UC-STD-03`), `docs/actors.md`, `docs/screen-inventory.md` (`SCR-STU-01`, `SCR-SELF-01`)
- **DATA SPECIFICATIONS:** `docs/entities/student-entities.md`, `docs/entities/student-entities-tables.md` (Submodule 1 — Admission)
- **ARCHITECTURE CONSTRAINTS:** `docs/architecture/adr.md` (ADR-001 through ADR-010)

## Objective

Assess the technical and architectural feasibility, system impact, cross-domain dependencies, data model requirements, and implementation readiness for delivering the full Stage 1 Admission module. This assessment reconciles the existing baseline codebase with authoritative domain specifications using a **Pragmatic**, **Minimalist**, and **Explicit** approach.

---

# 2. Current State

## Existing Behavior

1. **Backend Implementation:**
   - `src/backend/Elsasa.Sis.Students` exists only as a project skeleton with an assembly marker (`StudentsAssemblyMarker.cs`).
   - No vertical slice commands, queries, Dapper handlers, or endpoint mappings exist for the Student domain.
   - Cross-domain contract `IOperationsCalendarQueryService` exists in `Elsasa.Sis.Contracts.Operations` (delivered in `CR-OPS-CALENDAR-PERIODS`) and exposes `GetActiveAcademicYearAsync`, `GetOperationalPeriodByIdAsync`, and `GetActiveAdmissionWindowsAsync`.
   - Domain event `ApplicantAcceptedEvent` is declared in `Elsasa.Sis.Contracts.Students.Events.ApplicantAcceptedEvent`.

2. **Database Migrations:**
   - Early migration scripts `20261101_0201_Students_Applicants.sql` and `20261101_0202_Students_AdmissionApplications.sql` create initial basic tables:
     - `students.applicants` with columns: `id`, `first_name`, `last_name`, `date_of_birth`, `gender`, `email`, `phone`, `created_at`, `created_by`.
     - `students.admission_applications` with columns: `id`, `applicant_id`, `academic_year_id`, `application_number`, `status`, `submitted_at`, `created_at`, `created_by`.
   - Missing tables from `docs/entities/student-entities-tables.md`:
     - `students.admission_decisions`
     - `students.applicant_documents`
     - `students.entrance_assessment_evidence`
     - `students.admission_application_contacts`
   - Database sequence for application numbering does not exist.

3. **Frontend Implementation:**
   - Feature views `src/frontend/src/features/students/admission/views/AdmissionQueueView.vue` and `ApplicationDetailView.vue` are simple stub components displaying static placeholder text.
   - No typed API clients, validation schemas, or reactive state management exist for Admission.
   - Public self-service admission portal route (`SCR-SELF-01`) is not yet wired in the router.

4. **Storage Infrastructure:**
   - No local or cloud file storage abstraction exists for uploading and managing applicant documents (`applicant_documents.file_reference`).

## Existing Constraints

1. **Modular Monolith Architecture (ADR-001):** `Elsasa.Sis.Students` cannot reference other domain assemblies (`Operations`, `Finance`). All cross-domain collaboration must occur via `Elsasa.Sis.Contracts`.
2. **Database Schema Isolation (ADR-002):** Schema isolation between `students`, `operations`, and `finance` is mandatory. Foreign keys across schemas are logical UUIDs only; physical cross-schema foreign keys are strictly prohibited.
3. **Cross-Domain Communication (ADR-003):** Synchronous inquiries to Operations (e.g. validating admission windows) must use `IOperationsCalendarQueryService`. Asynchronous side-effects (e.g. notifying downstream consumers upon acceptance) must publish in-memory domain events (`ApplicantAcceptedEvent`) after the primary transaction commits.
4. **Data Access & Endpoint Patterns (ADR-004, ADR-005):** Explicit Dapper queries using C# raw string literals, `Result<T>` domain outcomes, and native Minimal APIs with FluentValidation filters.
5. **Stage 1 Functional Boundary:** Entrance assessment evidence intake in Stage 1 is strictly manual/external evidence entry. Direct digital integration with Academics assessment workflows (`ACD-WF-20`) is deferred to Stage 2. Enrollment execution and student number assignment are deferred to the subsequent `Enrollment & Identity` module.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | **Schema Inconsistency in Applicants & Applications:** The existing `students.applicants` and `students.admission_applications` tables lack core authoritative fields from `docs/entities/student-entities-tables.md` (e.g. `full_name`, `government_id_number`, `admission_period_id`, `application_type`, `requested_entry_level`, `requested_campus`, `requested_program`). |
| GAP-002 | CRITICAL | **Missing Admission Supporting Tables:** Four essential domain tables (`admission_decisions`, `applicant_documents`, `entrance_assessment_evidence`, `admission_application_contacts`) and the application numbering sequence do not exist in the database. |
| GAP-003 | CRITICAL | **Missing Application Registration & Period Validation Logic (`STD-WF-01`, `UC-STD-01`):** No backend commands exist to register an application, validate its target admission window against `IOperationsCalendarQueryService`, generate sequential application numbers, or deduplicate applicant identity. |
| GAP-004 | CRITICAL | **Missing Admission Review & Decision Lifecycle Logic (`STD-WF-02`, `UC-STD-03`):** No backend handlers exist to transition applications (`registered` -> `under_review` -> `decided`), record immutable admission decisions, validate deferrals, or publish `ApplicantAcceptedEvent`. |
| GAP-005 | MAJOR | **Missing Document Verification & File Storage Infrastructure:** No endpoints exist to upload applicant credentials, save files, track document submission/verification status, or serve files for review. |
| GAP-006 | MAJOR | **Missing Manual Entrance Assessment Evidence Intake:** No CRUD endpoints or domain handlers exist to attach manual/external assessment scores (`entrance_assessment_evidence`) to an application during review. |
| GAP-007 | MAJOR | **Incomplete Administrative Workbench UI (`SCR-STU-01`):** `AdmissionQueueView.vue` and `ApplicationDetailView.vue` are stubs lacking work queue filtering, detail panels, document verification actions, assessment evidence intake, and decision recording modals. |
| GAP-008 | MAJOR | **Missing Self-Service Admission Portal UI (`SCR-SELF-01`):** No public/family self-service registration form and application status tracking view exist in the Vue frontend. |

---

# 4. Open Questions

| ID | Question | Impact | Status |
|---|---|---|---|
| OQ-001 | How should existing database tables (`students.applicants`, `students.admission_applications`) be reconciled with `student-entities-tables.md`? | Governs database migration strategy and column definitions. | CLOSED |
| OQ-002 | How should the Students domain validate the target admission period (`admission_period_id`) and link its academic year? | Governs cross-domain service consumption and registration validation rules. | CLOSED |
| OQ-003 | How should `application_number` be formatted and duplicate active applications constrained? | Governs sequence generation and database uniqueness indexing. | CLOSED |
| OQ-004 | How should applicant identity deduplication be handled during application registration? | Governs applicant search/matching and conflict handling logic. | CLOSED |
| OQ-005 | How should document file storage and `file_reference` in `applicant_documents` be handled in Stage 1? | Governs file storage architecture and multipart upload endpoints. | CLOSED |
| OQ-006 | How should manual entrance assessment evidence intake be implemented in the backend and UI during Stage 1? | Governs assessment evidence data handling and staff intake workflows. | CLOSED |
| OQ-007 | How should the admission decision lifecycle and state transitions be governed? | Governs status state machine, immutability, and domain event publishing. | CLOSED |
| OQ-008 | How should the user interface for Stage 1 Admission (`SCR-STU-01` vs `SCR-SELF-01`) be structured? | Governs frontend view architecture, component breakdown, and routing. | CLOSED |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | The Operations domain has at least one active Academic Year and at least one Operational Period of type `admission_window` available via `IOperationsCalendarQueryService`. |
| ASM-002 | Entrance assessment scores entered manually in Stage 1 originate from external diagnostic tests or paper exams and do not require prior Academics domain assessment setup. |
| ASM-003 | Document uploads during Stage 1 can be securely stored on local server storage under a structured directory path, with MIME-type and size validation. |
| ASM-004 | Admission Officers and School Leadership have authorized permissions to record and review admission decisions; unauthenticated or applicant users access only self-service submission and status tracking. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Modifying `students.applicants` and `students.admission_applications` breaks existing code or migrations. | Low | Existing codebase has only assembly markers and no active queries against these tables. A forward DbUp migration safely alters and adds columns. |
| RISK-002 | Duplicate applications registered by the same applicant for the same admission period. | High | Enforce partial unique index on `(applicant_id, admission_period_id)` WHERE `status NOT IN ('withdrawn', 'decided')` and validate in command handlers. |
| RISK-003 | Incomplete or unverified documents submitted for an application being mistakenly approved. | Medium | Provide explicit verification status tracking (`not_submitted`, `submitted`, `verified`, `rejected`) in `applicant_documents` and surface warning indicators in `SCR-STU-01`. |
| RISK-004 | Arbitrary file upload vulnerabilities (malicious file types or directory traversal). | High | Enforce strict file extension/MIME-type whitelisting, file size limits (e.g. max 5MB), and generate sanitized unique storage paths. |

---

# 7. Recommendations

## Evaluated Architectural Options

### Approach 1: Pragmatic, Minimalist, and Explicit Domain Alignment (Selected)
- **Database:** Full alignment via forward DbUp migration (`20261103_0201_Students_Admission_Tables.sql`). Evolve `applicants` and `admission_applications`, create 4 missing tables, and introduce sequence `students.admission_application_number_seq`.
- **Validation:** Synchronous verification against `IOperationsCalendarQueryService.GetOperationalPeriodByIdAsync` ensuring the period exists and registration dates are respected.
- **Deduplication:** Automatic matching by `government_id_number` or normalized `(full_name, date_of_birth)`.
- **Storage:** Local file storage service implementing `IFileStorageService` with endpoints for multipart document upload and secure file serving.
- **Assessment Evidence:** Dedicated CRUD endpoints and workbench card for manual external assessment scores.
- **Lifecycle:** Explicit state transitions (`registered` -> `under_review` -> `decided` / `withdrawn`) publishing `ApplicantAcceptedEvent` upon acceptance.
- **UI:** Dual-view Vue implementation providing `SCR-STU-01` (Student Intake Workbench) for staff and `SCR-SELF-01` (Admission Portal) for self-service applicants.

### Approach 2: Monolithic Schema Shortcut
- Keep tables unchanged, pack documents, contacts, and assessment scores into JSON columns.
- Skip cross-domain period validation.
- Build only staff view without self-service.
- **Verdict:** Rejected. Violates explicit domain specifications, breaks auditability, and undermines downstream Stage 1 and Stage 2 handshakes.

---

# 8. Gap Closure

## GAP-001 & GAP-002 / OQ-001: Schema Reconciliation & Supporting Tables
### Decision
Execute a new DbUp migration (`20261103_0201_Students_Admission_Tables.sql`) that:
1. Alters `students.applicants` to match `student-entities-tables.md`: add `full_name`, `preferred_name`, `place_of_birth`, `nationality`, `government_id_type`, `government_id_number` (with unique index), `address`, `previous_school_name`, `previous_school_address`, `notes`.
2. Alters `students.admission_applications`: add `admission_period_id` (UUID logical FK to Operations), `application_type` (`new`, `transfer_in`), `requested_entry_level`, `requested_campus`, `requested_program`, `registration_date`, `notes`; retain `academic_year_id` as denormalized reference.
3. Creates sequence `students.admission_application_number_seq` starting at 1.
4. Creates table `students.admission_decisions` (`id`, `application_id`, `decision_type`, `decision_date`, `effective_date`, `decided_by`, `reason`, `supporting_reference`, `deferred_admission_period_id`, `created_at`).
5. Creates table `students.applicant_documents` (`id`, `application_id`, `document_type`, `submission_status`, `submitted_date`, `verified_date`, `verified_by`, `file_reference`, `notes`, `created_at`).
6. Creates table `students.entrance_assessment_evidence` (`id`, `application_id`, `assessment_type`, `score`, `source`, `assessment_date`, `notes`, `created_at`).
7. Creates table `students.admission_application_contacts` (`id`, `application_id`, `full_name`, `relationship`, `phone`, `email`, `address`, `created_at`).
### Rationale
Ensures 100% adherence to authoritative data specifications and relational integrity within the Student domain.
### Impact
Provides complete persistence infrastructure required by all Stage 1 admission features.
### Architecture Impact
Strictly conforms to ADR-002 (intra-schema physical FKs, logical UUID cross-schema references).

---

## GAP-003 / OQ-002: Cross-Domain Period Validation
### Decision
Inject `IOperationsCalendarQueryService` into the application registration command handler. Synchronously query `GetOperationalPeriodByIdAsync(admissionPeriodId)`:
- Verify the period exists and has `status = 'active'` or `'approved'`.
- Extract and store its `AcademicYearId`.
- Enforce that `registration_date` falls between `start_date` and `end_date` (unless staff-assisted registration flag bypasses the date check).
### Rationale
Guarantees that admission applications are firmly grounded in an authoritative operational window established by Operations.
### Impact
Prevents orphan applications or applications attached to non-existent periods.
### Architecture Impact
Conforms to ADR-003 synchronous contract inquiry pattern.

---

## GAP-003 / OQ-003: Sequential Application Numbering & Active Application Uniqueness
### Decision
1. Format application numbers deterministically as `ADM-{AcademicYearCode}-{Sequence:D4}` (e.g. `ADM-2026-0001`) using `NEXT VALUE FOR students.admission_application_number_seq`.
2. Add a partial unique index on `students.admission_applications (applicant_id, admission_period_id) WHERE status NOT IN ('withdrawn', 'decided')`.
### Rationale
Provides human-readable traceable references and prevents applicants from submitting concurrent duplicate active applications in the same window.
### Impact
Guarantees data integrity and predictable operational tracking.
### Architecture Impact
Native SQL Server sequencing and indexing aligned with Dapper command handlers.

---

## GAP-003 / OQ-004: Applicant Identity Deduplication
### Decision
During registration:
1. If `government_id_number` is provided, query `students.applicants` by government ID. If found, link the application to the existing applicant and update contact/biographical details if provided.
2. If `government_id_number` is omitted, query `students.applicants` by normalized `full_name` and `date_of_birth`.
3. If no match is found, create a new `students.applicants` record.
4. Enforce that the linked applicant has no other active application in the target period.
### Rationale
Respects `student-domain.md` rule that duplicate applicant identities should be detected, while accommodating both first-time and returning applicants.
### Impact
Eliminates fragmented applicant identity rows.
### Architecture Impact
Encapsulated within the `RegisterAdmissionApplication` slice transaction.

---

## GAP-005 / OQ-005: Document Storage & Verification
### Decision
1. Introduce lightweight `IFileStorageService` in `Elsasa.Sis.Shared.Storage` (implemented by `PhysicalFileStorageService`) that stores uploaded files in `storage/documents/{applicationId}/{documentType}_{fileName}`.
2. Implement endpoints:
   - `POST /api/students/applications/{id}/documents/{docId}/upload` (multipart/form-data)
   - `GET /api/students/applications/{id}/documents/{docId}/file` (file streaming)
   - `PUT /api/students/applications/{id}/documents/{docId}/verify` (updates `submission_status`, `verified_by`, `verified_date`, `notes`)
### Rationale
Delivers a reliable, local-first file handling mechanism for Stage 1 without external cloud vendor dependencies.
### Impact
Enables end-to-end credential submission, storage, inspection, and verification.
### Architecture Impact
Isolated infrastructure service adhering to clean architecture.

---

## GAP-006 / OQ-006: Manual Entrance Assessment Evidence Intake
### Decision
Implement dedicated vertical slice endpoints:
- `POST /api/students/applications/{id}/assessment-evidence`: Add manual assessment score (`assessment_type`, `score`, `source = 'External Assessment Center'`, `assessment_date`, `notes`).
- `PUT /api/students/applications/{id}/assessment-evidence/{evidenceId}`: Update assessment score or notes.
- `DELETE /api/students/applications/{id}/assessment-evidence/{evidenceId}`: Remove assessment record.
In `SCR-STU-01`, render a dedicated Assessment Evidence Card allowing Admission Officers to enter and review these scores prior to recording an admission decision.
### Rationale
Satisfies Stage 1 roadmap scope ("entrance assessment evidence entered as manual/external input; full Academics integration deferred to Stage 2").
### Impact
Admission Officers have full visibility into entrance assessment evidence during application review.
### Architecture Impact
Granular CQRS slices within `Elsasa.Sis.Students.Features.Admission`.

---

## GAP-004 / OQ-007: Admission Decision Lifecycle & Event Publishing
### Decision
Implement explicit state transitions:
1. `StartReview`: Transitions application from `registered` to `under_review`.
2. `RecordDecision`: Inserts row into `students.admission_decisions`, updates `students.admission_applications.status = 'decided'`.
   - If `decision_type == 'accepted'`: Publishes `ApplicantAcceptedEvent(applicantId, applicationId, academicYearId)` via `IPublisher` after transaction commit.
   - If `decision_type == 'deferred'`: Validates `deferred_admission_period_id` against `IOperationsCalendarQueryService` and records the future period.
   - Once decided, decisions are immutable.
3. `WithdrawApplication`: Transitions application to `withdrawn`.
### Rationale
Enforces business rules from `student-domain.md` and fulfills ADR-003 for cross-domain event notifications.
### Impact
Downstream domains (Finance, Enrollment) receive the necessary event triggers when applicants are accepted.
### Architecture Impact
Conforms to ADR-003, ADR-005, and ADR-007.

---

## GAP-007 & GAP-008 / OQ-008: Frontend Workspaces & Portals
### Decision
Deliver two views in the Vue SPA:
1. **`SCR-STU-01` Student Intake Workbench (`/students/intake`):**
   - Work queue table with filters (Period, Status, Entry Level, Search).
   - Application Detail drawer/view:
     - Header summary (Applicant, Target Period, Grade, Status badge).
     - Tabs: Application Info & Guardian Contacts, Document Verification, Entrance Assessment Evidence, Decision & Review History.
     - Action buttons: "Start Review", "Verify Document", "Add Assessment Score", "Record Decision", "Withdraw".
2. **`SCR-SELF-01` Admission Application Portal (`/admission/apply` and `/admission/status`):**
   - Clean public self-service wizard: Period & Level Selection -> Applicant Details -> Guardian Contact -> Document Upload -> Review & Submit.
   - Tracking desk: Look up application by `application_number` and applicant date of birth to view current status and decision notice.
### Rationale
Directly fulfills screen inventory definitions while maintaining high usability for both prospective families and school staff.
### Impact
Complete UI coverage for Stage 1 Admission intake.
### Architecture Impact
Uses TanStack Query, Pinia, Zod schemas, and Tailwind/Lucide component library.

---

# 9. Architecture Applicability

## Decision
**ARCHITECTURE-REQUIRED**

## Rationale
The Stage 1 Admission module introduces:
- Significant database persistence updates (2 modified tables, 4 new tables, 1 sequence).
- Cross-domain synchronous consumption of `IOperationsCalendarQueryService` and publication of `ApplicantAcceptedEvent`.
- New file storage infrastructure (`IFileStorageService`) for multipart uploads.
- Multiple vertical slice command/query handlers and native Minimal API route groups.
- Two distinct frontend applications (`SCR-STU-01` and `SCR-SELF-01`).

A formal ARCHITECTURE artifact (`IS002 - STU-ADMISSION-ARCHITECTURE.md`) is required to define technical component boundaries, DTO schemas, SQL scripts, and API contracts prior to implementation planning.

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
Feasibility analysis is complete. All architectural decisions adhere to the **Pragmatic**, **Minimalist**, and **Explicit** principles and respect all source domain and entity specifications. The next stage is **Architecture Finalization** (`ica-architect`).

---

# 11. References

### Referenced Artifacts
- DOMAIN: `docs/domains/student-domain.md`
- FEATURE: `docs/sub-modul-and-development-phases.md`, `docs/operational-workflow-discovery.md`, `docs/use-cases.md`, `docs/actors.md`, `docs/screen-inventory.md`
- INTAKE ISSUE: `docs/issues/IS002 - STU-ADMISSION-ISSUE.md`

### Referenced Codebase Locations
- Class Libraries: `src/backend/Elsasa.Sis.Students/`, `src/backend/Elsasa.Sis.Contracts/`, `src/backend/Elsasa.Sis.Shared/`, `src/backend/Elsasa.Sis.Database/`
- Frontend Views: `src/frontend/src/features/students/admission/views/`
- Existing Migrations: `src/backend/Elsasa.Sis.Database/Scripts/20261101_0201_Students_Applicants.sql`, `src/backend/Elsasa.Sis.Database/Scripts/20261101_0202_Students_AdmissionApplications.sql`
- Cross-Domain Service: `src/backend/Elsasa.Sis.Contracts/Operations/IOperationsCalendarQueryService.cs`
- In-Memory Domain Event: `src/backend/Elsasa.Sis.Contracts/Students/Events/ApplicantAcceptedEvent.cs`
