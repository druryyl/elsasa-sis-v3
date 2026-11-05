---
Title: Feasibility Assessment — Stage 2 Academic Assessment & Results Full Scope
Code: CR-ACD-ASSESSMENT-RESULTS
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-05
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Implementation of the full scope of Stage 2 "Assessment & Results" under the Academic domain as specified in [IS010 - ACD-ASSESSMENT-RESULTS-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS010%20-%20ACD-ASSESSMENT-RESULTS-ISSUE.md) and the SIS implementation roadmap ([sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)).

Referenced artifacts:
- **DOMAIN:** [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md) (Assessment, Academic Results and History, Academic Record Correction, Report Cards and Transcripts)
- **ENTITIES:** [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md), [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md) (Submodule 3: lines 390–660)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md) (`ACD-WF-07`, `ACD-WF-08`, `ACD-WF-09`, `ACD-WF-12`, `ACD-WF-15`, `ACD-WF-20`, `GAP-07`, `GAP-08`)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md) (`UC-ACD-05`, `UC-ACD-06`, `UC-ACD-07`, `UC-ACD-13`, `UC-ACD-16`, `UC-ACD-19`)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md) (Teacher, Academic Coordinator, Academic Authority, Student, Parent, Admission Officer)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md) (`SCR-ACD-02`, `SCR-ACD-07`, `SCR-SELF-03`, `SCR-SELF-02`, `SCR-STU-01`)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md) (ADR-001 through ADR-010)

## Objective

Evaluate technical feasibility, architectural consistency, codebase readiness, and structural impacts of implementing Stage 2 Assessment & Results across five cohesive submodules:
1. **Assessment Planning & Conduct (`ACD-WF-07`, `ACD-WF-20`, `UC-ACD-05`, `UC-ACD-07`, `SCR-ACD-02`, `SCR-STU-01`):**
   - Manage planned and conducted evaluation events (`assessments`) linked to approved learning units (`learning_unit_id`) and optionally to specific instructional learning activities (`learning_activity_id`).
   - Support assessment types (`formative`, `summative`, `diagnostic`, `portfolio`, `project`, `other`) and assessment methods (`written_test`, `oral`, `practical`, `project`, `portfolio`, `observation`, `other`).
   - Define scoring specifications: maximum score (`max_score`), minimum passing / mastery threshold (`passing_score` / KKM), evaluation criteria or rubric descriptors (`criteria`), and scheduled conduct date (`assessment_date`).
   - Configure clear reassessment policies (`reassessment_policy`: `capped_at_passing`, `highest_score`, `latest_attempt`, `average`, `none`) to govern how retake attempts are adjudicated.
   - Administer applicant entrance assessments (`ACD-WF-20`, `UC-ACD-07`), linking prospective applicant evaluation evidence directly to Academics scoring workflows and transmitting authorized scored results to Student Admission (`STD-WF-02`, `SCR-STU-01`), resolving `GAP-07`.
2. **Evidence Collection & Evaluation / Scoring (`ACD-WF-08`, `UC-ACD-06`, `SCR-ACD-02`):**
   - Collect and track raw learning evidence (`assessment_evidence`) linked to assessments and students, originating from coursework submissions (`submissions`), examination papers, observations, oral responses, or practical demonstrations.
   - Maintain strict separation between raw evidence, provisional evaluations, approved results, and published results.
   - Support multiple evaluation models (`evaluation_model`): `numeric`, `letter_grade`, `pass_fail`, `competency`, `rubric`, `narrative`, and `completion`.
   - Preserve complete remediation history by recording distinct immutable result records for retakes (`attempt_number = 1, 2, 3...`, referencing `replaces_result_id`), flagging the operative outcome via `is_final = true` in strict accordance with the assessment's configured `reassessment_policy`.
   - Authorize classroom teachers to record provisional evaluations and exercise autonomous authority to release routine formative scores, rubric feedback, and developmental guidance directly to students and parents (`UC-ACD-06`).
3. **Result Moderation, Approval & Publication (`ACD-WF-09`, `UC-ACD-16`, `SCR-ACD-07`):**
   - Enable Academic Authorities and delegated Coordinators to review and moderate provisional summative results across classes and cohorts.
   - Enforce the formal governance lifecycle: Assigned Teacher records provisional evaluations (`provisional`) → Academic Authority formally approves summative results (`approved`) → authorized release to stakeholders (`published`).
   - Enforce the foundational rule that only approved results feed downstream academic progression, standing determination, promotion recommendations, report cards, or transcripts.
   - Publish approved results to student and parent self-service workspaces (`SCR-SELF-03`, `SCR-SELF-02`).
4. **Report Card & Transcript Production (`ACD-WF-12`, `UC-ACD-13`, `SCR-ACD-07`, `SCR-SELF-03`, `SCR-SELF-02`):**
   - Compile periodic academic reports (`report_cards` and `report_card_entries`) for active reporting periods (supporting Mid-Term Progress Reports `mid_term` and End-of-Semester Report Cards `end_of_term` — e.g., *Buku Rapor Merdeka*).
   - Enforce the boundary rule that report cards do not own grades or attendance: report cards compile and present approved academic results and attendance summaries sourced directly from authoritative operational records.
   - Compile and issue interim academic transcripts (`transcripts`: `unofficial` advising transcripts and interim `official` transcripts).
   - Capture immutable frozen document snapshots (`content_snapshot` serialized JSON) upon approval and publication, guaranteeing 100% exact historical reproducibility under historical rules.
5. **Academic Record Correction Protocol (`ACD-WF-15`, `UC-ACD-19`, `SCR-ACD-07`):**
   - Enforce a formal, auditable correction protocol for approved academic results (`academic_record_corrections`).
   - Strictly prohibit silent or in-place overwriting of approved academic records.
   - Capture complete correction audit metadata: original value (`original_value`), new corrected value (`corrected_value`), mandatory justification reason (`reason`), supporting evidence (`evidence`), authorizing actor (`corrected_by`), timestamp (`corrected_date`), and materiality flag (`is_material`).
   - Restrict material grade alterations to Academic Authority authorization.
   - Publish `AcademicResultCorrectedEvent` to trigger downstream alerts for re-evaluating academic standing or progression recommendations.

Deliver the assessment strictly adhering to **Pragmatic**, **Minimalist**, and **Explicit** architectural principles, preserving domain boundaries, compile-time assembly isolation, and the established vertical slice conventions of Elsasa SIS V3.

---

# 2. Current State

## Existing Behavior

1. **Database Schema:**
   - The database contains provisioned tables for Operations (`operations`), Students (`students`), and Finance (`finance`).
   - The `academics` schema exists and contains tables from Stage 2 Submodule 1 (`IS008`: `learning_structures`, `learning_units`, `academic_requirements`) and Submodule 2 (`IS009`: `academic_participations`, `learning_activities`, `attendance_records`, `attendance_corrections`, `assignments`, `submissions`).
   - The tables specified for Submodule 3 in [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md) lines 390–660 (`academics.assessments`, `academics.assessment_evidence`, `academics.academic_results`, `academics.academic_record_corrections`, `academics.report_cards`, `academics.report_card_entries`, `academics.transcripts`) are **not yet provisioned**.
2. **Backend Domain & Services:**
   - `src/backend/Elsasa.Sis.Academics` exists and is integrated into `Elsasa.Sis.sln` and `Elsasa.Sis.Api`.
   - Existing feature slices in `Elsasa.Sis.Academics` cover Learning Structures, Learning Units, Academic Requirements, Academic Participations, Learning Activities, Attendance, and Coursework Assignments.
   - There are **no feature slices, domain handlers, or Dapper repositories** for Assessment Planning, Evidence Collection, Evaluation/Scoring, Result Moderation/Approval/Publication, Report Card Generation, Transcript Compilation, or Academic Record Corrections.
   - Upstream contracts:
     - `IStudentsModule` exposes student enrollment checks (`IsStudentEnrolledAsync`).
     - `IOperationsCalendarQueryService` exposes active period validation.
     - `IAcademicsModule` exposes participation, learning unit, and attendance queries.
     - `students.applicants` table exists, but there is no cross-domain contract or table schema in Academics to link assessments to applicants for entrance diagnostic exams (`ACD-WF-20`).
3. **Frontend Implementation:**
   - `src/frontend/src/features/academics/curriculum` exists for `SCR-ACD-03` (Curriculum & Learning Structure Workspace).
   - `src/frontend/src/features/academics/participation` exists for `SCR-ACD-05` (Participation & Roster Workbench).
   - `src/frontend/src/features/academics/sessions` exists for `SCR-ACD-01` (Class Session Workbench).
   - `src/frontend/src/features/academics/coursework` exists for assignments in `SCR-ACD-02`, but does **not yet contain** assessment planning, rubric configuration, submission scoring, or formative feedback release.
   - There is **no implementation** for `SCR-ACD-07` (Academic Records Workbench: result moderation, approval, report card production, transcript generation, record correction).
   - Self-service portals:
     - `SCR-SELF-03` (`features/portal/learning/`) does not have viewing tabs for published assessment results, rubric breakdowns, or report cards.
     - `SCR-SELF-02` (`features/portal/`) does not have family viewing tabs for academic report cards.
   - Admission intake:
     - `SCR-STU-01` (`features/students/admission/`) contains a Stage 1 manual assessment evidence modal, but lacks Stage 2 integration with Academics entrance assessments.

## Existing Constraints

1. **Domain Isolation & ADR Compliance (ADR-001, ADR-002, ADR-010):**
   - `Elsasa.Sis.Academics` references only `Elsasa.Sis.Shared` and `Elsasa.Sis.Contracts`. Direct assembly references to `Students`, `Finance`, `Operations`, or `Teaching` are prohibited.
   - Schema isolation: `academics` schema owns all Submodule 3 tables. Physical foreign keys across schemas are prohibited; external keys (`student_id`, `applicant_id`, `academic_period_id`) are stored as logical UUIDs (`uniqueidentifier`).
2. **Authority & Governance Boundary Invariant (`academic-domain.md`, `use-cases.md`):**
   - **Formative vs Summative Separation:** Teachers possess autonomous authority to record and publish routine classroom marks, formative evaluations, and qualitative developmental feedback directly to students and parents (`UC-ACD-06`).
   - **Summative Moderation & Certification:** Summative examination results, academic standing determinations, report cards, and transcripts strictly require formal moderation and approval by the Academic Authority (or delegated Coordinator) prior to publication (`UC-ACD-16`, `UC-ACD-13`).
   - A provisional result must **never** be consumed by downstream progression or reporting workflows as if it were approved.
3. **Auditable Immutability Invariant (`academic-domain.md`):**
   - Approved academic results and published report snapshots cannot be silently overwritten.
   - Reassessments generate new immutable attempt records (`attempt_number = 2, 3...`), and post-approval adjustments require explicit, authorized entries in `academic_record_corrections`.
4. **Historical Document Reproducibility Invariant (`academic-entities.md` Q18):**
   - Periodic report cards (*Buku Rapor Merdeka*) and transcripts must be 100% reproducible under their historical rules as certified on that date, guaranteed through immutable `content_snapshot` archival.
5. **Boundary Rule on Document Ownership (`academic-domain.md`):**
   - Report cards and transcripts do **not** own grades or attendance. They compile and format approved operational results and attendance summaries.
6. **Dual Candidate Reference Rule (`GAP-07` / `operational-workflow-discovery.md`):**
   - Assessments must evaluate both enrolled students and admissions applicants. The database must maintain explicit, typed referential columns with native check constraints.

---

# 3. Gap Analysis

| ID | Severity | Gap | Status | Resolution |
|------|------|------|:---:|------|
| `GAP-001` | CRITICAL | **Missing Database Tables:** Database lacks tables `assessments`, `assessment_evidence`, `academic_results`, `academic_record_corrections`, `report_cards`, `report_card_entries`, and `transcripts` in schema `academics`. | **CLOSED** | Author DbUp migration script `20261105_0402_Academics_AssessmentResults_Tables.sql` with full table DDL, intra-schema foreign keys, indexes, and check constraints matching `academic-entities-tables.md`. |
| `GAP-002` | CRITICAL | **Missing Backend Vertical Slices in `Elsasa.Sis.Academics`:** No domain models, endpoint mappings, validators, or Dapper handlers exist for assessment planning, evidence collection, scoring, result approval, publication, report cards, transcripts, or corrections. | **CLOSED** | Implement vertical slices under `Features/Assessments/`, `Features/AssessmentEvidence/`, `Features/AcademicResults/`, `Features/ReportCards/`, `Features/Transcripts/`, and `Features/RecordCorrections/` using Minimal APIs and Dapper raw SQL queries per ADR-004 and ADR-005. |
| `GAP-003` | CRITICAL | **Candidate Reference in Assessment & Evidence (GAP-07 / ACD-WF-20 / UC-ACD-07):** Assessment evidence and results must support both enrolled students and admission applicants without orphan records or schema confusion. | **CLOSED** | Adopted **Explicit Dual Nullable References with CHECK Constraint**: `student_id UUID NULL`, `applicant_id UUID NULL`, enforced with `CHECK ((student_id IS NOT NULL AND applicant_id IS NULL) OR (student_id IS NULL AND applicant_id IS NOT NULL))`. |
| `GAP-004` | MAJOR | **Reassessment Policy Execution & Immutability:** System must support multiple retakes (`attempt_number`), retain complete remediation history, and adjudicate the operative outcome (`is_final = true`) according to configured policy (`capped_at_passing`, `highest_score`, `latest_attempt`, `average`, `none`). | **CLOSED** | Adopted **Application-Level Explicit Policy Evaluator in Command Transaction**: pure C# domain service `IReassessmentPolicyEvaluator` computes operative outcome and updates `is_final` flags atomically in a single SQL transaction. |
| `GAP-005` | MAJOR | **Result Governance Lifecycle & Formative vs. Summative Boundary:** Need explicit server-side enforcement allowing teachers to publish formative feedback directly (`UC-ACD-06`) while strictly locking summative evaluations to `provisional` until approved by Academic Authority (`UC-ACD-16`). | **CLOSED** | Adopted **Explicit Lifecycle Endpoints with Role & Type Guards**: separate command slices for `EvaluateStudentLearning`, `ApproveSummativeResults`, and `PublishAcademicResults` with explicit authorization rules. |
| `GAP-006` | MAJOR | **Report Card & Transcript Historical Snapshot Architecture:** System must guarantee 100% exact historical reproducibility of certified report cards (*Buku Rapor Merdeka*) and transcripts without database blob bloat or heavy PDF compilation dependencies. | **CLOSED** | Adopted **Structured JSON Snapshot with Native Print Layout Rendering**: freeze complete certified payload as JSON in `content_snapshot`, rendered via browser-native CSS `@media print` layout templates. |
| `GAP-007` | MAJOR | **Academic Record Correction Protocol Execution:** Must strictly prevent silent overwriting of approved grades while maintaining foreign key integrity and notifying downstream modules of material adjustments (`UC-ACD-19`). | **CLOSED** | Adopted **Transactional In-Place Mutation with Mandatory Audit Log**: atomically insert serialized audit log in `academic_record_corrections`, update operative row in `academic_results`, and publish `AcademicResultCorrectedEvent`. |
| `GAP-008` | MAJOR | **Missing Frontend Workbenches & Integrations (`SCR-ACD-02`, `SCR-ACD-07`, `SCR-SELF-03`, `SCR-SELF-02`, `SCR-STU-01`):** No UI screens exist for teacher assessment planning/scoring or authority records moderation, nor for self-service viewing or admission entrance score integration. | **CLOSED** | Adopted **Role-Aligned Slices with Contextual Portal Tabs**: scaffold `features/academics/assessment/` (`SCR-ACD-02`), `features/academics/records/` (`SCR-ACD-07`), add report card viewing tabs to self-service portals, and wire entrance score display in `SCR-STU-01`. |
| `GAP-009` | MINOR | **Cross-Domain Contracts & Domain Events:** Downstream modules (`Standing & Progression`, `Students Admission`, `Finance Benefits`) require contract queries and domain events for approved results and certified report cards. | **CLOSED** | Extend `IAcademicsModule` and define domain events (`AcademicResultApprovedEvent`, `AcademicResultPublishedEvent`, `AcademicResultCorrectedEvent`, `ReportCardPublishedEvent`, `TranscriptIssuedEvent`) in `Elsasa.Sis.Contracts/Academics`. |

---

# 4. Open Questions

| ID | Question | Impact | Status | Decision Summary |
|------|------|------|:---:|------|
| `OQ-001` | How should candidate references in `assessment_evidence` and `academic_results` support both enrolled students and admissions applicants (`ACD-WF-20` / `GAP-07` / `UC-ACD-07`)? | Dictates relational schema constraints, Dapper queries, and cross-domain admission integration. | **CLOSED** | **Explicit Dual Nullable References with CHECK Constraint:** Provision `student_id UUID NULL` and `applicant_id UUID NULL` with `CHECK ((student_id IS NOT NULL AND applicant_id IS NULL) OR (student_id IS NULL AND applicant_id IS NOT NULL))`. Reuses the entire evaluation pipeline cleanly. |
| `OQ-002` | How should assessment retakes and reassessment policies (`reassessment_policy`) calculate and maintain the operative result (`is_final = true`) across multiple attempts? | Dictates remediation history tracking, query indexing, and report card grade consistency. | **CLOSED** | **Application-Level Explicit Policy Evaluator in Command Transaction:** Use C# domain service `IReassessmentPolicyEvaluator` inside command transactions to evaluate the policy (`capped_at_passing`, `highest_score`, `latest_attempt`, `average`, `none`), update prior attempts `is_final = false`, and set `is_final = true` on the winner. |
| `OQ-003` | How should backend endpoints enforce the governance lifecycle between teacher formative release (`UC-ACD-06`) and Academic Authority summative approval/publication (`UC-ACD-16` / `ACD-WF-09`)? | Dictates API slice boundaries, role permissions, and compliance with the academic governance invariant. | **CLOSED** | **Explicit Lifecycle Endpoints with Role & Type Guards:** Separate command slices for `EvaluateStudentLearning` (teachers can immediately publish formative work; summative forced to `provisional`), `ApproveSummativeResults` (Authority sets `approved`), and `PublishAcademicResults` (Authority sets `published`). |
| `OQ-004` | How should the immutable historical snapshot (`content_snapshot` in `report_cards` and `transcripts`) be generated, stored, and rendered? | Dictates backend PDF dependencies, storage volume, and historical reproducibility guarantees. | **CLOSED** | **Structured JSON Snapshot with Native Print Layout Rendering:** Serialize an explicitly typed JSON snapshot containing student details, approved marks, attendance tallies, comments, and approval metadata into `content_snapshot`. Render via client-side CSS `@media print` templates (*Buku Rapor Merdeka*). |
| `OQ-005` | How should the Academic Record Correction Protocol (`ACD-WF-15` / `UC-ACD-19`) execute mutations on approved academic records? | Dictates audit trail integrity, foreign key stability, and downstream standing notifications. | **CLOSED** | **Transactional In-Place Mutation with Mandatory Audit Log:** In a single SQL transaction: validate Academic Authority role and mandatory reason (min length 10), insert immutable audit row into `academic_record_corrections`, update operative `academic_results` record, and publish `AcademicResultCorrectedEvent`. |
| `OQ-006` | How should the frontend features and views be structured to deliver the Teacher workbench (`SCR-ACD-02`), Records workbench (`SCR-ACD-07`), and Self-Service/Admission integrations? | Dictates frontend modularity, route configuration, and role ergonomics. | **CLOSED** | **Role-Aligned Slices with Contextual Portal Tabs:** Scaffold `features/academics/assessment/` (`SCR-ACD-02`) and `features/academics/records/` (`SCR-ACD-07`), embed report card viewing tabs into `SCR-SELF-03` and `SCR-SELF-02`, and integrate entrance exam scores into `SCR-STU-01`. |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| `ASM-001` | The database engine is SQL Server 2022+ with the `academics` schema already established by `IS008` and `IS009`. |
| `ASM-002` | Active academic periods (`academic_period_id`) and operational calendar dates are validated via `IOperationsCalendarQueryService`. |
| `ASM-003` | Enrolled student validity is checked via `IStudentsModule.IsStudentEnrolledAsync(studentId)`. For entrance exams, applicant validity is checked against `students.applicants`. |
| `ASM-004` | Report cards and transcripts do **not** own grades or attendance; they compile and present approved academic results (`academic_results WHERE is_final = 1 AND result_status = 'approved'`) and attendance tallies (`attendance_records`). |
| `ASM-005` | All user identity tracking (`evaluated_by`, `approved_by`, `corrected_by`) utilizes authenticated username/email strings from `ICurrentUser`. |
| `ASM-006` | Downstream standing determinations (`Good Standing`, `Probation`), academic honors conferral, and promotion/retention recommendations belong to `Standing & Progression` (`SCR-ACD-09`) and consume approved results delivered by this module. |
| `ASM-007` | Official final graduation transcript certification and accredited external transfer-in credit integration (`UC-STD-18`) are deferred to Stage 3 per roadmap specifications. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| `RISK-001` | Silent overwriting or tampering of approved academic grades, destroying institutional trust and regulatory audit compliance. | High | Enforce transactional audit logging in `academic_record_corrections` on any modification of approved results; require non-empty justification reasons (min 10 chars) and Academic Authority authorization. |
| `RISK-002` | Historical report cards changing dynamically if underlying student records or historical results are updated in subsequent terms. | High | Freeze a complete, self-contained structured JSON payload in `report_cards.content_snapshot` and `transcripts.content_snapshot` at approval time, rendering strictly from the snapshot. |
| `RISK-003` | Orphan or ambiguous applicant entrance scores when applicants convert to enrolled students. | High | Implement `GAP-07` explicit dual nullable references (`student_id` NULL, `applicant_id` NULL) with CHECK constraint, ensuring every result is strictly bound to exactly one candidate identity. |
| `RISK-004` | Performance degradation when compiling report cards across hundreds of students with complex multi-attempt reassessments. | Medium | Persist the pre-calculated `is_final` flag in `academic_results` via the command transaction, allowing report card compilation queries to filter with a simple, high-speed index condition `WHERE is_final = 1 AND result_status IN ('approved', 'published')`. |
| `RISK-005` | Massive coordinator manual data-entry fatigue when generating periodic report cards for entire cohorts. | Medium | Provide `BatchGenerateCohortReportCardsCommand` allowing one-click compilation of draft report cards for all active participants in a section/period. |

---

# 7. Recommendations

## Candidate Identity in Assessment Evidence & Results (GAP-003 / OQ-001)

### Option A: Explicit Dual Nullable References with CHECK Constraint — Recommended
- In `academics.assessment_evidence` and `academics.academic_results`:
  - `student_id UNIQUEIDENTIFIER NULL` (references enrolled student)
  - `applicant_id UNIQUEIDENTIFIER NULL` (references admissions applicant)
  - `CONSTRAINT chk_candidate_ref CHECK ((student_id IS NOT NULL AND applicant_id IS NULL) OR (student_id IS NULL AND applicant_id IS NOT NULL))`
- Filtered indexes:
  - `CREATE INDEX ix_academic_results_student ON academics.academic_results(student_id) WHERE student_id IS NOT NULL`
  - `CREATE INDEX ix_academic_results_applicant ON academics.academic_results(applicant_id) WHERE applicant_id IS NOT NULL`

#### Advantages
- **Pragmatic & Fully Reusable:** Enables applicant entrance exams (`ACD-WF-20`) to reuse 100% of the rubric scoring, assessor assignment, and approval workflow without duplicate tables.
- **Explicit Schema Contract:** Columns are explicitly typed and self-documenting; eliminates ambiguous polymorphic string keys.
- **Minimalist:** Requires only two columns and a standard SQL CHECK constraint per table.

#### Disadvantages
- Minor query condition `WHERE student_id = @StudentId` or `WHERE applicant_id = @ApplicantId` depending on assessment context.

---

### Option B: Polymorphic Candidate Columns (`candidate_id` + `candidate_type`)
- Store a single `candidate_id UNIQUEIDENTIFIER` with `candidate_type NVARCHAR(20)`.

#### Advantages
- Saves one column in the database table.

#### Disadvantages
- Sacrifices explicit schema typing, prevents foreign key clarity, and complicates Dapper object mapping.

---

## Reassessment Policy Execution Architecture (GAP-004 / OQ-002)

### Option A: Application-Level Explicit Policy Evaluator in Command Transaction — Recommended
- Define domain service `IReassessmentPolicyEvaluator`:
  - Input: Parent assessment's `reassessment_policy`, existing attempt records, new attempt score.
  - Output: Identification of the operative attempt and operative score.
- Policy rules:
  - `highest_score`: attempt with highest numeric/grade score is flagged `is_final = true`.
  - `capped_at_passing`: highest score is used, but capped at `passing_score` (KKM).
  - `latest_attempt`: highest `attempt_number` is flagged `is_final = true`.
  - `average`: operative score is the arithmetic mean; latest attempt record carries the calculated average and `is_final = true`.
  - `none`: first attempt is always operative (`attempt_number = 1`, `is_final = true`).
- In `RecordStudentEvaluationCommand`:
  - Evaluates policy in pure C# unit-testable domain logic.
  - In a single SQL transaction: inserts new attempt row and updates `is_final` flags across all attempts for that student and assessment.

#### Advantages
- **Pragmatic & Testable:** Business rules are explicit C# logic covered by comprehensive unit tests.
- **Minimalist:** Zero database triggers, stored procedures, or background worker jobs.
- **High Read Performance:** Downstream report card and progression queries simply query `WHERE is_final = 1`, avoiding expensive runtime window functions across thousands of historical retakes.

#### Disadvantages
- Requires a multi-row update within the evaluation transaction when a retake occurs.

---

### Option B: Dynamic SQL Window Functions (No physical `is_final` flag)
- Do not store `is_final`. Re-calculate the winner dynamically on every query using SQL window functions (`ROW_NUMBER() OVER (...)`).

#### Advantages
- Avoids multi-row updates during retake insertion.

#### Disadvantages
- Extreme query complexity in Dapper, especially for `capped_at_passing` and `average` policies; severely degrades report card batch compilation performance.

---

## Result Governance Lifecycle & Formative vs. Summative Boundaries (GAP-005 / OQ-003)

### Option A: Explicit Lifecycle Endpoints with Role & Type Guards — Recommended
- Provide 3 explicit vertical slices:
  1. `POST /api/v1/academics/assessments/{id}/results`:
     - Teacher records evaluations (`UC-ACD-06`).
     - If assessment is `formative` and `publish_immediately = true`, sets `result_status = 'published'` and `published_date = CURRENT_TIMESTAMP`.
     - If assessment is `summative`, server forces `result_status = 'provisional'`.
  2. `POST /api/v1/academics/assessments/{id}/results/approve`:
     - Restricted to `AcademicAuthority` / `AcademicCoordinator` (`UC-ACD-16`).
     - Transitions provisional results to `result_status = 'approved'`, setting `approved_by` and `approved_date`.
  3. `POST /api/v1/academics/assessments/{id}/results/publish`:
     - Restricted to `AcademicAuthority` / `AcademicCoordinator`.
     - Transitions approved results to `result_status = 'published'`, making them visible in student/family portals.

#### Advantages
- **Explicit Domain Governance:** Enforces the fundamental invariant that teachers cannot unilaterally approve official summative marks, while preserving full autonomy for routine formative feedback.
- **Clean CQRS Design:** Dedicated command contracts matching exact user case operations.
- **Minimalist:** Operates on the single `result_status` column without extra workflow state tables.

#### Disadvantages
- Requires distinct UI action triggers in `SCR-ACD-02` (teacher) and `SCR-ACD-07` (authority).

---

## Report Card & Transcript Historical Snapshot Architecture (GAP-006 / OQ-004)

### Option A: Structured JSON Snapshot with Native Print Layout Rendering — Recommended
- Upon formal approval/publication of a report card or interim transcript:
  - Command slice gathers all approved results (`is_final = true AND result_status = 'approved'`), attendance summaries, narrative comments, teacher sign-offs, and curriculum labels.
  - Serializes this complete dataset into an explicit JSON contract (`ReportCardSnapshotDto` or `TranscriptSnapshotDto`) and saves it to `content_snapshot NVARCHAR(MAX)`.
- Frontend Rendering:
  - When viewing or printing a certified document, the UI loads `content_snapshot` directly and renders it using Vue printable components styled with CSS `@media print` rules (*Buku Rapor Merdeka* layout).

#### Advantages
- **100% Historical Reproducibility:** Future edits to student names, attendance records, or retrospective grade adjustments will never alter the certified snapshot.
- **Minimalist & Zero Blob Bloat:** Eliminates large binary PDF files from SQL Server; eliminates external PDF generator packages (Puppeteer, QuestPDF).
- **Pragmatic School Ergonomics:** Native browser print dialog allows teachers to print directly to school printers or save as local PDF without server rendering lag.

#### Disadvantages
- Print styling relies on CSS `@page` and `@media print` rules (fully supported in modern Chromium/Firefox browsers).

---

## Academic Record Correction Protocol Architecture (GAP-007 / OQ-005)

### Option A: Transactional In-Place Mutation with Mandatory Audit Log — Recommended
- When `CorrectApprovedAcademicResultCommand` (`UC-ACD-19`) executes:
  1. Guard checks: Requires `AcademicAuthority` role (or Coordinator for non-material changes) and non-empty `reason` (min length 10).
  2. Serializes previous state (e.g., score, letter grade, label) as JSON into `original_value`.
  3. Serializes new state as JSON into `corrected_value`.
  4. In a single SQL transaction:
     - Inserts row into `academics.academic_record_corrections` with `record_type = 'academic_result'`, `record_id`, `original_value`, `corrected_value`, `reason`, `evidence`, `corrected_by`, `corrected_date`, `is_material`.
     - Updates `academic_results` with corrected values.
     - Re-evaluates reassessment policy if retakes exist to ensure `is_final` consistency.
     - Publishes `AcademicResultCorrectedEvent` for downstream standing re-evaluation.

#### Advantages
- **Preserves Relational Integrity:** Keeps the existing `academic_results.id` intact, preventing broken foreign keys in `report_card_entries`.
- **Zero Silent Overwrites:** Every change is permanently, immutably recorded with who, what, when, and why.
- **Explicit & Audit-Ready:** Satisfies all requirements of `ACD-WF-15` and `UC-ACD-19`.

#### Disadvantages
- Read queries must include `LEFT JOIN academic_record_corrections` if caller wishes to view complete change history for a result row.

---

## Frontend Feature Slicing & Screen Architecture (GAP-008 / OQ-006)

### Option A: Role-Aligned Slices with Contextual Portal Tabs — Recommended
- Scaffold two dedicated feature slices in `src/frontend/src/features/`:
  1. `features/academics/assessment/` (`SCR-ACD-02`):
     - Teacher Assessment & Evaluation Workbench.
     - Views: Assessment Planning, Rubric Configuration, Evidence Collection & Scoring Queue, Formative Release.
  2. `features/academics/records/` (`SCR-ACD-07`):
     - Academic Records Workbench for Coordinators and Authorities.
     - Views: Summative Result Moderation & Approval, Official Publication, Report Card Generator & *Buku Rapor* Print Preview, Interim Transcript Viewer, Record Correction Dialog.
  3. Contextual Portal Extensions:
     - `features/portal/learning/` (`SCR-SELF-03`): Add "Grades & Report Cards" tab for students.
     - `features/portal/` (`SCR-SELF-02`): Add "Academic Progress & Report Cards" tab for parents.
     - `features/students/admission/` (`SCR-STU-01`): Wire assessment evidence tab to read authorized entrance exam scores from Academics.

#### Advantages
- **Strict Screen Inventory Alignment:** Preserves the 36-screen inventory without creating rogue split screens.
- **Role Isolation:** Prevents teachers from viewing confidential records certification actions while keeping daily grading fast and intuitive.
- **Minimalist:** Reuses existing PrimeVue component styling and TanStack Query caching patterns.

---

# 8. Gap Closure

## GAP-001: Missing Database Tables
### Decision
Author DbUp migration script `20261105_0402_Academics_AssessmentResults_Tables.sql` provisioning the 7 Submodule 3 tables in schema `academics`: `assessments`, `assessment_evidence`, `academic_results`, `academic_record_corrections`, `report_cards`, `report_card_entries`, and `transcripts`.
### Rationale
Provides complete relational persistence matching `academic-entities-tables.md` lines 390–660, with explicit constraints, UUID PKs, intra-schema FKs, and performance indexes.
### Impact
Adds 7 tables to SQL Server database under schema `academics`.
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
- `Assessments/` (`CreateAssessment`, `UpdateAssessment`, `ConductAssessment`, `CancelAssessment`, `GetAssessmentsByUnit`, `GetAssessmentById`)
- `AssessmentEvidence/` (`RecordEvidence`, `GetEvidenceByAssessment`)
- `AcademicResults/` (`EvaluateStudentLearning`, `ApproveSummativeResults`, `PublishAcademicResults`, `GetResultsByAssessment`, `GetStudentResultsWorkspace`)
- `ReportCards/` (`CompileReportCard`, `BatchCompileCohortReportCards`, `ApproveReportCard`, `PublishReportCard`, `GetReportCardById`, `GetStudentReportCards`)
- `Transcripts/` (`GenerateInterimTranscript`, `IssueTranscript`, `GetStudentTranscript`)
- `RecordCorrections/` (`CorrectApprovedResult`, `GetResultCorrectionHistory`)
### Rationale
Follows ADR-004 (Dapper + raw SQL literals) and ADR-005 (Native Minimal APIs + direct slice handlers).
### Impact
Exposes REST endpoints under `/api/v1/academics/...` and `/api/v1/portal/...`.
### Architecture Impact
Zero controller overhead; direct F12 slice navigation; high performance.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-003: Candidate Reference in Assessment & Evidence (GAP-07 / ACD-WF-20 / UC-ACD-07)
### Decision
Adopt **Explicit Dual Nullable References with CHECK Constraint**: provision `student_id UNIQUEIDENTIFIER NULL` and `applicant_id UNIQUEIDENTIFIER NULL` on `assessment_evidence` and `academic_results`, with constraint `CHECK ((student_id IS NOT NULL AND applicant_id IS NULL) OR (student_id IS NULL AND applicant_id IS NOT NULL))`.
### Rationale
Directly adopts the established `GAP-07` resolution from `docs/operational-workflow-discovery.md`. Reuses the full assessment and rubric scoring pipeline for applicant entrance exams (`ACD-WF-20`) without schema duplication or untyped polymorphic keys.
### Impact
Both enrolled students and admissions applicants can be assessed in Academics.
### Architecture Impact
Clean relational integrity and explicit Dapper mapping.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-004: Reassessment Policy Execution & Immutability
### Decision
Adopt **Application-Level Explicit Policy Evaluator in Command Transaction**: implement `IReassessmentPolicyEvaluator` to evaluate the assessment's `reassessment_policy` (`highest_score`, `capped_at_passing`, `latest_attempt`, `average`, `none`) and atomically update `is_final = true` on the operative attempt and `is_final = false` on superseded attempts within the evaluation transaction.
### Rationale
Keeps complex academic business logic in testable pure C# domain services while keeping read queries high-speed and index-friendly (`WHERE is_final = 1`).
### Impact
Guarantees 100% remediation audit history retention while providing fast read performance for report cards and downstream standing.
### Architecture Impact
Clean separation of domain policy from storage mechanism.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-005: Result Governance Lifecycle & Formative vs. Summative Boundary
### Decision
Adopt **Explicit Lifecycle Endpoints with Role & Type Guards**: provide distinct command slices for `EvaluateStudentLearning` (teachers can immediately publish formative work; summative evaluations forced to `provisional`), `ApproveSummativeResults` (Academic Authority/Coordinator sets `approved`), and `PublishAcademicResults` (Academic Authority sets `published`).
### Rationale
Enforces the fundamental institutional governance rule that summative grades must be reviewed and approved by Academic Authority, while empowering teachers with autonomous authority for daily formative feedback (`UC-ACD-06`).
### Impact
Provisional grades are strictly prevented from leaking into official report cards, transcripts, or standing determinations.
### Architecture Impact
Explicit command boundaries with granular permission checks.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-006: Report Card & Transcript Historical Snapshot Architecture
### Decision
Adopt **Structured JSON Snapshot with Native Print Layout Rendering**: serialize certified report card and interim transcript payloads into explicit JSON contracts stored in `content_snapshot NVARCHAR(MAX)` at approval/issuance time. Render documents via client-side CSS `@media print` layout templates (e.g. *Buku Rapor Merdeka*).
### Rationale
Guarantees 100% exact historical reproducibility without storing heavy binary PDF blobs in the SQL database or requiring server-side headless browser dependencies.
### Impact
Fast compilation, zero database bloat, and tamper-proof historical record retention.
### Architecture Impact
Immutable snapshot pattern decoupled from presentation layout.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-007: Academic Record Correction Protocol Execution
### Decision
Adopt **Transactional In-Place Mutation with Mandatory Audit Log**: within a single atomic SQL transaction, validate Academic Authority permission and mandatory reason (min length 10), insert an immutable entry in `academic_record_corrections` capturing serialized original and corrected values, update the operative `academic_results` row, and publish `AcademicResultCorrectedEvent`.
### Rationale
Prevents silent overwrites and preserves complete audit history while keeping foreign keys in `report_card_entries` stable and unbroken.
### Impact
Satisfies regulatory audit compliance and alerts downstream modules when grades change.
### Architecture Impact
Tamper-evident audit pattern with transactional consistency.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-008: Missing Frontend Workbenches & Integrations
### Decision
Adopt **Role-Aligned Slices with Contextual Portal Tabs**: scaffold `features/academics/assessment/` (`SCR-ACD-02` for Teachers) and `features/academics/records/` (`SCR-ACD-07` for Coordinators and Authorities), embed report card viewing tabs into `SCR-SELF-03` and `SCR-SELF-02`, and wire entrance assessment score display into `SCR-STU-01`.
### Rationale
Strictly preserves the 36-screen inventory, maintains actor role ergonomics, and avoids bloated monolithic directories.
### Impact
Teachers, coordinators, authorities, students, parents, and admission officers receive dedicated, role-tailored views.
### Architecture Impact
Clean Vue 3 feature modularity with TypeScript schemas and TanStack Query.
### Resolved By
Drury (Architect / Lead Developer)
### Resolved Date
2026-11-05

---

## GAP-009: Cross-Domain Contracts & Domain Events
### Decision
Extend `IAcademicsModule` with synchronous query methods for downstream consumers (e.g. checking approved results, applicant entrance scores, and report card snapshots) and define domain event records (`AcademicResultApprovedEvent`, `AcademicResultPublishedEvent`, `AcademicResultCorrectedEvent`, `ReportCardPublishedEvent`, `TranscriptIssuedEvent`) in `Elsasa.Sis.Contracts/Academics/Events/`.
### Rationale
Provides clean synchronous contracts and post-commit side-effect events per ADR-003.
### Impact
Enables `Standing & Progression`, `Students Admission`, and `Finance Benefits` to consume Stage 2 assessment results cleanly.
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
Implementing Stage 2 Assessment & Results introduces:
1. **7 new core persistence tables** in SQL Server (`assessments`, `assessment_evidence`, `academic_results`, `academic_record_corrections`, `report_cards`, `report_card_entries`, `transcripts`).
2. **Cross-domain integration boundaries** with `Students` (applicant entrance exams `ACD-WF-20` and `GAP-07`) and `Operations` (calendar milestones).
3. **Complex governance state machines** separating teacher provisional scoring, teacher autonomous formative publication, Academic Authority summative approval, and official publication.
4. **Reassessment policy evaluation engine** governing multi-attempt remediation histories and `is_final` operative outcome adjudication.
5. **Auditable regulatory correction protocol** for approved records with mandatory audit serializations.
6. **Immutable frozen snapshot architecture** for *Buku Rapor Merdeka* report cards and interim academic transcripts.
7. **2 major frontend workbenches** (`SCR-ACD-02`, `SCR-ACD-07`) and 3 cross-domain portal integrations (`SCR-SELF-03`, `SCR-SELF-02`, `SCR-STU-01`).

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
Feasibility assessment is complete and fully resolved. All decisions strictly reflect the **Pragmatic**, **Minimalist**, and **Explicit** architectural principles. The next step is to create the formal **ARCHITECTURE** artifact for `CR-ACD-ASSESSMENT-RESULTS`, followed by the executable **IMPLEMENTATION-PLAN**.

---

# 11. References

Referenced artifacts:
- **DOMAIN:** [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md)
- **ENTITIES:** [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md), [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md) (`ACD-WF-07`, `ACD-WF-08`, `ACD-WF-09`, `ACD-WF-12`, `ACD-WF-15`, `ACD-WF-20`, `GAP-07`, `GAP-08`)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md) (`UC-ACD-05`, `UC-ACD-06`, `UC-ACD-07`, `UC-ACD-13`, `UC-ACD-16`, `UC-ACD-19`)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md) (`SCR-ACD-02`, `SCR-ACD-07`, `SCR-SELF-03`, `SCR-SELF-02`, `SCR-STU-01`)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md) (ADR-001 through ADR-010)
- **ISSUE:** [IS010 - ACD-ASSESSMENT-RESULTS-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS010%20-%20ACD-ASSESSMENT-RESULTS-ISSUE.md)

Referenced codebase locations:
- `src/backend/Elsasa.Sis.Academics/`
- `src/backend/Elsasa.Sis.Contracts/Academics/`
- `src/backend/Elsasa.Sis.Contracts/Students/IStudentsModule.cs`
- `src/backend/Elsasa.Sis.Contracts/Operations/IOperationsCalendarQueryService.cs`
- `src/backend/Elsasa.Sis.Database/Scripts/`
- `src/frontend/src/features/academics/`
- `src/frontend/src/features/portal/`
- `src/frontend/src/features/students/admission/`
