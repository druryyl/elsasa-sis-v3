---
Title: Feasibility Assessment — Stage 2 Academic Student Learning Plan Full Scope
Code: CR-ACD-STUDENT-LEARNING-PLAN
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-07
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Implementation of the full scope of Stage 2 "Student Learning Plan" under the Academic domain as specified in [IS013 - ACD-STUDENT-LEARNING-PLAN-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS013%20-%20ACD-STUDENT-LEARNING-PLAN-ISSUE.md) and the SIS implementation roadmap ([sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)).

Referenced artifacts:
- **DOMAIN:** [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md) (Student Learning Plan, Concurrency, Academic Plan Adjustments, Lifecycle)
- **ENTITIES:** [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md), [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md) (Submodule 5: lines 960–1090)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md) (`ACD-WF-02` Establish Student Learning Plan)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md) (`UC-ACD-09` Establish Student Learning Plan, `UC-ACD-10` Register Academic Participation)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md) (Academic Coordinator, Academic Authority, Teacher, Student Development Officer)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md) (`SCR-ACD-04` Learning Plan Workspace, `SCR-ACD-05` Participation & Roster Workbench, `SCR-ACD-07` Academic Records Workbench)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md) (ADR-001 through ADR-010)

## Objective

Evaluate technical feasibility, architectural consistency, codebase readiness, and structural impacts of implementing Stage 2 Student Learning Plan across three cohesive submodules, strictly adhering to **Pragmatic**, **Minimalist**, and **Explicit** architectural principles:

1. **SLP Creation & Revision (`ACD-WF-02`, `UC-ACD-09`, `SCR-ACD-04`):**
   - Formulate and maintain individualized Student Learning Plans (SLP) for students requiring individualized academic pathways or exceptional learning adaptations (`student_learning_plans`).
   - Enforce the foundational domain constraint: default academic operation proceeds without an SLP; when created, a student may have at most one `active` SLP per academic period.
   - Support the standard SLP lifecycle status model: `draft`, `active`, `revised`, `completed`, and `archived`.
   - Implement version tracking (`version`), incrementing the plan version upon each revision while preserving prior versions for historical auditability.
   - Link constituent learning units (`slp_learning_units`) to the plan with sequence ordering (`sequence_order`), requirement flags (`is_required`), and level/adaptation notes.
   - Enable Academic Coordinators to create draft plans, update constituent units, and submit plans for Academic Authority review.
2. **Academic Plan Adjustment (`ACD-WF-02`, `UC-ACD-09`, `SCR-ACD-04`):**
   - Record formal academic accommodations, course substitutions, and exemptions applied to a Student Learning Plan (`academic_plan_adjustments`).
   - Support all three standard adjustment categories under a unified model:
     - `accommodation` (e.g., extended time for written assessments, assistive media).
     - `substitution` (e.g., replacing standard physical education with adaptive physical education).
     - `exemption` (e.g., waiving a non-core elective requirement based on verified documentation).
   - Capture detailed adjustment metadata: adjustment type, description, justification rationale, effective date (`effective_date`), optional conclusion date (`end_date`), and approving authority reference.
3. **SLP Approval & Governance (`ACD-WF-02`, `UC-ACD-09`, `SCR-ACD-04`):**
   - Record formal Academic Authority approval and re-approval decisions (`slp_approvals`).
   - Support approval types: `initial_approval` (activating an initial draft plan) and `revision_approval` (activating an amended plan version).
   - Enforce governance rules: every new plan version requires explicit Academic Authority re-approval before transitioning to `active` status; approving a revision automatically marks the previous active version as `revised`.
   - Maintain an immutable audit chain of approvals including approving authority (`approved_by`), approval timestamp (`approved_date`), plan version (`plan_version`), approval type, and governance notes.
4. **Downstream Consumer Alignment (`SCR-ACD-05`, `SCR-ACD-07`):**
   - Provide authoritative SLP context to `Participation & Roster Workbench` (`SCR-ACD-05`, `UC-ACD-10`) with an explicit "Sync Participations from Plan" action (`academic_participations.source = 'learning_plan'`, `student_learning_plan_id`).
   - Surface governing learning plan context and adjustments as informative badges in `Academic Records Workbench` (`SCR-ACD-07`) during evaluation and record review.
5. **Demo Data Generator Support for Presentation & Verification:**
   - Extend the demo data seeding orchestrator in `AcademicsActivitiesDemoDataSeeder` to generate realistic SLPs, constituent learning units, adjustments, approval audit records, and linked participations for Hagios School of Life student personas (active revised v2 SLP for Budi Santoso and pending draft v1 SLP for Timotius).
   - Update `AcademicsDemoDataPurger` to clean the 4 new tables in strict reverse-dependency order.

### Scope Boundaries & Explicit Deferrals

The following capabilities belong to companion modules, subsequent stages, or separate domains and are explicitly out of scope for this issue:

- **Administrative Enrollment and Placement:** An SLP does not alter the student's official administrative placement (campus, grade level, homeroom cohort) or administrative lifecycle status (`SCR-STU-01`, `SCR-STU-02`, `SCR-STU-03`), which remain strictly owned by the Students domain (`Placement & Status`).
- **Foundational Curriculum & Learning Units:** Definition, syllabus configuration, and requirement versioning for standard learning units belong to `Learning Structure & Requirements` (`IS008`).
- **Instructional Delivery & Attendance:** Conducting classes, tracking attendance, and logging daily coursework belong to `Participation & Learning Activities` (`IS009`).
- **Assessment Scoring & Report Cards:** Evaluating assessments, scoring rubrics, grade approval, and report cards belong to `Assessment & Results` (`IS010`).
- **Academic Standing & Progression:** Determining academic standing, completion tracking, and promotion recommendations belong to `Standing & Progression` (`IS012`).
- **Non-Academic Interventions & Pastoral Support:** Pastoral care, wellbeing referrals, counselling cases, and behavioural discipline belong strictly to the Development domain (`SCR-DEV-03`, `SCR-DEV-02`). Development recommendations may provide supporting context for an SLP referral but do not approve or govern academic plans.
- **Financial Adjustments & Scholarships:** Fee waivers, tuition discounts, and scholarship stacking belong exclusively to the Finance domain (`Benefits`, `Obligations & Billing`).

---

# 2. Current State

## Existing Behavior

1. **Database Schema:**
   - The database contains provisioned tables for Operations (`operations`), Students (`students`), Finance (`finance`), and Academics (`academics`).
   - The `academics` schema contains Submodule 1 (`learning_structures`, `learning_units`, `academic_requirements`), Submodule 2 (`academic_participations`, `learning_activities`, `attendance_records`, `attendance_corrections`, `assignments`, `submissions`), Submodule 3 (`assessments`, `assessment_evidence`, `academic_results`, `academic_record_corrections`, `report_cards`, `report_card_entries`, `transcripts`), and Submodule 4 (`learning_progress`, `academic_standings`, `promotion_recommendations`, `academic_interventions`, `academic_achievements`).
   - In `academics.academic_participations`, columns `student_learning_plan_id` (`UNIQUEIDENTIFIER NULL`) and `source` (`CHECK source IN ('assigned', 'student_selected', 'learning_plan')`) are already provisioned, ready to receive SLP linkage.
   - The tables specified for Submodule 5 in [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md) lines 960–1090 (`academics.student_learning_plans`, `academics.slp_learning_units`, `academics.academic_plan_adjustments`, `academics.slp_approvals`) are **not yet provisioned**.
2. **Backend Domain & Services:**
   - `src/backend/Elsasa.Sis.Academics` exists and is integrated into `Elsasa.Sis.sln` and `Elsasa.Sis.Api`.
   - Feature slices exist for Submodules 1, 2, 3, and 4.
   - There are **no feature slices, domain handlers, or Dapper repositories** for Student Learning Plans, SLP learning units, academic plan adjustments, or SLP approvals.
   - Upstream contracts in `Elsasa.Sis.Contracts`:
     - `IStudentsModule` exposes student enrollment validation (`IsStudentEnrolledAsync`).
     - `IOperationsCalendarQueryService` exposes active period validation.
     - `IAcademicsModule` exposes approved results, completed units, attendance summaries, standings, and learning unit details, but does not yet expose SLP inquiries.
3. **Frontend Implementation:**
   - `src/frontend/src/features/academics` contains feature modules for `curriculum`, `participation`, `sessions`, `coursework`, `assessment`, `interventions`, `cohort-review`, and `records`.
   - There is **no implementation** for `SCR-ACD-04` (`Learning Plan Workspace`).
   - `src/frontend/src/app/router/routes.ts` does not yet declare a route for `/academics/learning-plans`.
4. **Demo Data Seeding & Purging:**
   - `src/backend/Elsasa.Sis.Academics/Features/DemoData/AcademicsActivitiesDemoDataSeeder.cs` seeds participations, attendance, assignments, submissions, approved results, report cards, transcripts, standings, progression recommendations, interventions, and achievements.
   - `src/backend/Elsasa.Sis.Academics/Features/DemoData/AcademicsDemoDataPurger.cs` cleans all `academics.*` tables in reverse dependency order, but does not yet include Submodule 5 tables.

## Existing Constraints

1. **Domain Isolation & ADR Compliance (ADR-001, ADR-002, ADR-010):**
   - `Elsasa.Sis.Academics` references only `Elsasa.Sis.Shared` and `Elsasa.Sis.Contracts`. Direct assembly references to `Students`, `Finance`, `Operations`, or `Teaching` are prohibited.
   - Schema isolation: `academics` schema owns all Submodule 5 tables. Zero physical foreign keys across domain schemas; external keys (`student_id`, `academic_period_id`) are stored as logical UUIDs (`uniqueidentifier`).
2. **Governance & Decision Invariant (`academic-domain.md`, `actors.md`):**
   - Student Learning Plan is optional; default academic operations work without one.
   - When a plan exists, a student may have at most **one Active SLP** per Academic Period.
   - Initial approval and any subsequent plan version revisions strictly require formal authorization by the `Academic Authority` (`UC-ACD-09`).
   - Academic Plan Adjustments (accommodations, substitutions, exemptions) are unified under a single model and must preserve justification rationale, approving authority, and effective dates.
   - An SLP does not alter the student's administrative placement or administrative enrollment status.
3. **Auditable Immutability Invariant (`academic-domain.md`, `academic-entities-tables.md`):**
   - Historical versions of plans (`revised`, `completed`, `archived`) must be preserved for auditable academic history.
   - Approval decisions in `slp_approvals` are strictly append-only and immutable.

---

# 3. Gap Analysis

| ID | Severity | Gap | Status | Resolution |
|------|------|------|:---:|------|
| `GAP-001` | CRITICAL | **Missing Database Tables in schema `academics`:** Database lacks tables `student_learning_plans`, `slp_learning_units`, `academic_plan_adjustments`, and `slp_approvals`. | **CLOSED** | Author DbUp migration script `20261107_0402_Academics_StudentLearningPlan_Tables.sql` with full table DDL, intra-schema foreign keys, indexes, check constraints, and a SQL Server partial unique index enforcing at most one active plan per student per period. |
| `GAP-002` | CRITICAL | **Missing Backend Vertical Slices in `Elsasa.Sis.Academics`:** No domain models, endpoint mappings, validators, or Dapper handlers exist for SLP formulation, revising, unit configuration, adjustments, authority approvals, or participation syncing. | **CLOSED** | Implement vertical slices under `Features/StudentLearningPlans/` using Minimal APIs and Dapper raw SQL queries per ADR-004 and ADR-005. |
| `GAP-003` | MAJOR | **SLP Lifecycle, Revision Workflow, and Active Concurrency Enforcement (`ACD-WF-02`, `UC-ACD-09`):** System must enforce version increments (`v+1`), atomic status transitions, and the 1-active-plan-per-period constraint without disrupting operational continuity. | **CLOSED** | Adopted **Clone-on-Revise with Atomic Status Hand-off**: active plans remain operative while edits occur in a cloned draft (version n+1); upon Academic Authority revision approval, the old plan atomically transitions to `revised` and the new draft becomes `active` in a single transaction, backed by a SQL Server partial unique index. |
| `GAP-004` | MAJOR | **Academic Plan Adjustments Workflow & Approval Governance:** Accommodations, substitutions, and exemptions must be managed with complete rationale and authority attribution without over-complicating approval queues. | **CLOSED** | Adopted **Plan-Coupled Unified Governance**: adjustments are drafted within the SLP; Academic Authority plan approval simultaneously approves and stamps `approved_by` and `approved_date` on all adjustments. Editing adjustments on an active plan requires creating a revision (v+1). |
| `GAP-005` | MAJOR | **Downstream Roster & Participation Sync Model (`SCR-ACD-05` / `academic_participations`):** Approved SLP constituent learning units must be registered into student participations (`source = 'learning_plan'`) transparently and reliably. | **CLOSED** | Adopted **Explicit Roster Sync Action + Contextual Badge**: provide an explicit 'Sync Participations from Plan' action in `SCR-ACD-04` to generate/reconcile required participations, while `SCR-ACD-05` displays an active SLP badge and pre-links the plan ID during manual roster additions. |
| `GAP-006` | MAJOR | **Missing Frontend Workbench (`SCR-ACD-04`):** No UI exists for Academic Coordinators and Authorities to formulate, adjust, review, and approve individualized learning plans. | **CLOSED** | Adopted **Dedicated Master-Detail Workspace (`/academics/learning-plans`)**: implement `SCR-ACD-04` with a filterable left sidebar (Academic Period, Status, Search) and a main workspace containing active plan context, role-based action buttons, and 3 cohesive tabs (*Constituent Learning Units*, *Plan Adjustments*, *Approval History & Versions*). |
| `GAP-007` | MAJOR | **Cross-Domain Contracts & Domain Events:** SIS consumers (Records, Participation, Students) require contract inquiries and domain events for SLPs. | **CLOSED** | Adopted **Targeted Contract & Domain Events**: expose `GetActiveStudentLearningPlanAsync` on `IAcademicsModule` and publish MediatR domain events (`StudentLearningPlanCreatedEvent`, `StudentLearningPlanApprovedEvent`, `StudentLearningPlanRevisedEvent`). |
| `GAP-008` | MAJOR | **Demo Data Generator & Purger Strategy:** Demo data generator must seed realistic SLP scenarios for Hagios personas and clean up without FK constraint violations. | **CLOSED** | Adopted **Realistic Dual-Persona Demo Scenario**: seed an active revised v2 SLP for Budi Santoso (remedial Grade 3 Math unit, adaptive PJOK substitution, 50% extra exam time accommodation, full approval history, and linked `academic_participations`) plus a pending draft v1 SLP for Timotius; update `AcademicsDemoDataPurger` in reverse-dependency order. |
| `GAP-009` | MINOR | **Contextual Adjustments Visibility in Academic Records (`SCR-ACD-07`):** Assessors and coordinators reviewing student progress or report cards lack visibility into approved accommodations (e.g., extra exam time). | **CLOSED** | Adopted **Contextual Adjustment Badges**: surface active learning plan adjustments as informative badges and tooltips in `SCR-ACD-07` (Academic Records Workbench) and `SCR-ACD-05` (Participation). |

---

# 4. Open Questions

| ID | Question | Impact | Status | Decision Summary |
|------|------|------|:---:|------|
| `OQ-001` | How should the Student Learning Plan (SLP) revision lifecycle and the single-active-plan invariant be managed? | Dictates persistence concurrency, draft editing flow, and operational continuity. | **CLOSED** | **Clone-on-Revise with Atomic Status Hand-off:** Active plan remains operative while amendments occur in a cloned draft (version n+1); upon Academic Authority revision approval, the previous plan atomically transitions to `revised` and the new version becomes `active`, enforced by application checks and a SQL Server partial unique index (`WHERE status = 'active'`). |
| `OQ-002` | How should Academic Plan Adjustments (accommodations, substitutions, exemptions) be approved and governed? | Dictates approval granularity, data model simplicity, and authority workflow. | **CLOSED** | **Plan-Coupled Unified Governance:** Adjustments are drafted within the SLP; Academic Authority plan approval simultaneously approves and stamps `approved_by` and `approved_date` on all adjustments. Editing adjustments on an active plan requires creating a revision (v+1). |
| `OQ-003` | How should approved Student Learning Plans synchronize with Academic Participations (`SCR-ACD-05` / `academic_participations`)? | Dictates automation vs manual control and roster integrity. | **CLOSED** | **Explicit Roster Sync Action + Contextual Badge:** Provide an explicit 'Sync Participations from Plan' action in `SCR-ACD-04` to register required units (setting `source = 'learning_plan'` and `student_learning_plan_id`), while `SCR-ACD-05` displays an active SLP badge and pre-links the plan during manual roster additions. |
| `OQ-004` | How should the Frontend UI (`SCR-ACD-04` Learning Plan Workspace) be structured? | Dictates user experience, routing, and role ergonomics. | **CLOSED** | **Dedicated Master-Detail Workspace (`/academics/learning-plans`):** Left sidebar displays filterable student SLP roster (Period, Status, Search); main workspace displays active plan header with role-based actions and 3 cohesive tabs (*Constituent Units*, *Plan Adjustments*, *Approval History & Versions*). |
| `OQ-005` | How should the Demo Data Generator be updated to showcase the Student Learning Plan feature? | Dictates persona realism, demonstration quality, and database reset integrity. | **CLOSED** | **Realistic Dual-Persona Demo Scenario:** Seed an active revised SLP (v2) for Budi Santoso (remedial Grade 3 Math unit, adaptive PJOK substitution, 50% extra exam time accommodation, full approval history, and linked `academic_participations`) plus a pending draft SLP (v1) for Timotius to demonstrate the review/approval workflow; update `AcademicsDemoDataPurger` in reverse-dependency order. |
| `OQ-006` | How should the Student Learning Plan contract and adjustments context integrate across the SIS modules? | Dictates cross-domain API surface and downstream visibility. | **CLOSED** | **Targeted Contract & Contextual Badges:** Expose `GetActiveStudentLearningPlanAsync` on `IAcademicsModule` and publish MediatR domain events (`StudentLearningPlanCreatedEvent`, `StudentLearningPlanApprovedEvent`, `StudentLearningPlanRevisedEvent`); surface active adjustments as contextual badges/tooltips in Academic Records Workbench (`SCR-ACD-07`) and Participation (`SCR-ACD-05`). |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| `ASM-001` | The database engine is SQL Server 2022+ with the `academics` schema established by prior issues (`IS008`, `IS009`, `IS010`, `IS012`). |
| `ASM-002` | `academics.academic_participations` already contains columns `student_learning_plan_id` (nullable UUID) and `source` allowing `'learning_plan'`. |
| `ASM-003` | Default academic operation proceeds without an SLP; an SLP is required only for exceptional or individualized learners. |
| `ASM-004` | An SLP does not modify the student's administrative enrollment, campus assignment, grade level, or homeroom placement in the Students domain. |
| `ASM-005` | Behavioral, psychological, counselling, and pastoral interventions belong strictly to the Development domain (`SCR-DEV-03`); they may provide supporting context for an SLP referral but do not approve or govern academic plans. |
| `ASM-006` | Academic Coordinators draft and revise plans; Academic Authorities (Curriculum Lead, Academic Head, VP Academics) possess sole authority to approve or re-approve plans. |
| `ASM-007` | Concurrency invariant: a student may have at most one `active` SLP per academic period. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| `RISK-001` | Race condition creating multiple active SLPs for the same student in the same academic period. | High | Enforce dual-layer protection: application check in the command handler and a SQL Server partial unique index: `CREATE UNIQUE NONCLUSTERED INDEX UQ_academics_slp_active ON academics.student_learning_plans (student_id, academic_period_id) WHERE status = 'active'`. |
| `RISK-002` | Roster disruption when an active plan is revised, leaving students temporarily without registered participations. | Medium | Adopt Clone-on-Revise pattern: existing active plan remains operative until new revision version is explicitly approved, preventing operational downtime. |
| `RISK-003` | Modifying approved adjustments directly without authority oversight, violating governance rules. | High | Enforce Plan-Coupled Unified Governance: once active, plans and their adjustments are immutable; changes mandate creating a revision draft (v+1) requiring Academic Authority re-approval. |
| `RISK-004` | Foreign key constraint failures during demo data purge/reset. | High | Update `AcademicsDemoDataPurger` to delete in reverse dependency order: `academic_participations` (clearing `student_learning_plan_id` references or deleting participations) → `slp_approvals` → `academic_plan_adjustments` → `slp_learning_units` → `student_learning_plans`. |

---

# 7. Recommendations

## Plan Lifecycle, Revision & Concurrency (GAP-003 / OQ-001)

### Option A: Clone-on-Revise with Atomic Status Hand-off — Recommended
- Initial plan starts as `draft` (v1). Academic Coordinator can freely edit notes, add/remove constituent learning units, and configure adjustments.
- Academic Authority reviews and executes `ApproveStudentLearningPlanCommand`, which records an initial approval in `slp_approvals` and transitions the plan to `active`.
- When an active plan needs changes, the Academic Coordinator executes `ReviseStudentLearningPlanCommand`, which clones the active plan, constituent units, and adjustments into a new record with `status = 'draft'` and `version = version + 1`.
- The existing active plan remains `active` and operative.
- When the Academic Authority reviews and approves the revision, a single database transaction atomically:
  1. Updates the prior active plan to `status = 'revised'`.
  2. Updates the new draft plan to `status = 'active'`.
  3. Records a `revision_approval` in `slp_approvals`.
- Dual-layer enforcement guarantees at most one active plan: application check and SQL Server partial unique index.
- **Pragmatic, Minimalist, Explicit:** Clean linear versioning, zero background state machine complexity, zero operational interruption.

---

## Academic Plan Adjustments Governance (GAP-004 / OQ-002)

### Option A: Plan-Coupled Unified Governance — Recommended
- Accommodations, substitutions, and exemptions are stored in `academics.academic_plan_adjustments` linked to `student_learning_plan_id`.
- Adjustments are formulated during plan drafting or revision.
- When the Academic Authority approves the plan, the command handler stamps `approved_by` and `approved_date` across all linked adjustments.
- Once active, adjustments cannot be modified in-place; modifications require triggering a revision draft (v+1), preserving a complete audit history.
- **Explicit & Minimalist:** Eliminates multi-stage adjustment approval queues while preserving strict authority governance and immutable audit trails.

---

## Downstream Participation Synchronization (GAP-005 / OQ-003)

### Option A: Explicit Roster Sync Action + Contextual Badge — Recommended
- In `SCR-ACD-04` (Learning Plan Workspace), provide an explicit "Sync Participations from Plan" button.
- Executing this command creates or reconciles `academic_participations` for the student's constituent units with `source = 'learning_plan'` and `student_learning_plan_id = @slpId`.
- In `SCR-ACD-05` (Participation & Roster Workbench), students with an active SLP display an "Active Learning Plan" badge; adding a participation for this student automatically suggests or pre-populates `source = 'learning_plan'` and links the SLP ID.
- **Pragmatic & Explicit:** Empowers coordinators with deliberate control and total visibility, avoiding opaque background side-effects.

---

## Frontend UX Architecture (GAP-006 / OQ-004)

### Option A: Dedicated Master-Detail Workspace (/academics/learning-plans) — Recommended
- Scaffold route `/academics/learning-plans` mapped to `LearningPlanWorkspaceView.vue`.
- Left Sidebar: Filter by Academic Period, Status (`All`, `Active`, `Draft`, `Revised/Archived`), and search by Student Name/NIS. Displays list of students with SLP summary cards.
- Main Workspace:
  - Header: Selected Student banner, administrative placement context, academic period, plan version, status badge, and role-based actions (`Submit for Review`, `Approve`, `Reject`, `Revise Plan`, `Sync Participations`, `Archive`).
  - Tab 1: **Constituent Learning Units** — Sequence ordering, required flag, level/adaptation notes, and Add/Remove unit modal.
  - Tab 2: **Plan Adjustments** — Accommodations, substitutions, exemptions with category badges, descriptions, justification rationales, and effective dates.
  - Tab 3: **Approval History & Audit** — Chronological timeline of approvals (`initial_approval`, `revision_approval`) with version, approving authority, date, and governance notes.
- **Role-Aligned & Cohesive:** Matches existing UI patterns in `CurriculumWorkspaceView.vue` and `CohortAcademicReviewView.vue`.

---

## Cross-Domain Contract & Records Integration (GAP-007 / GAP-009 / OQ-006)

### Option A: Targeted Contract & Contextual Badges — Recommended
- Expose synchronous inquiry on `IAcademicsModule`:
  - `Task<Result<ActiveStudentLearningPlanDto?>> GetActiveStudentLearningPlanAsync(Guid studentId, Guid academicPeriodId, CancellationToken ct = default);`
- Publish MediatR notification domain events:
  - `StudentLearningPlanCreatedEvent`
  - `StudentLearningPlanApprovedEvent`
  - `StudentLearningPlanRevisedEvent`
- In `SCR-ACD-07` (Academic Records Workbench), surface active adjustments (e.g., "Extended Exam Time 50%") as informative tooltips/badges next to student results during evaluation and moderation.
- **Explicit & Non-Intrusive:** Delivers cross-domain awareness without tight coupling or breaking boundaries.

---

## Demo Data Generator Integration (GAP-008 / OQ-005)

### Option A: Realistic Dual-Persona Demo Scenario — Recommended
- In `AcademicsActivitiesDemoDataSeeder`:
  - Persona 1 (Budi Santoso - Active Revised Plan):
    - SLP v1: Initial draft formulated and approved for Semester 1 TP 2029/2030 with Grade 3 Remedial Math and Bahasa Indonesia.
    - SLP v2: Revised to add Adaptive PJOK substitution and 50% extra exam time accommodation; approved by Ibu Ratna (VP Academics).
    - Status: `active` (v2), with prior v1 preserved as `revised`.
    - Linked `academic_participations` with `source = 'learning_plan'` and `student_learning_plan_id`.
  - Persona 2 (Timotius - Pending Draft Plan):
    - SLP v1: Newly formulated draft plan with proposed accommodations awaiting Academic Authority review.
    - Status: `draft` (v1).
- In `AcademicsDemoDataPurger`:
  - Clear `student_learning_plan_id` on `academic_participations` (or purge participations), then purge `slp_approvals` → `academic_plan_adjustments` → `slp_learning_units` → `student_learning_plans` in strict reverse-dependency order before `learning_units`.
- **Zero-Friction Verification:** Enables immediate testing of active plan governance, revision diffing, approval actions, and roster syncing out-of-the-box.

---

# 8. Gap Closure

### GAP-001: Missing Database Tables in schema `academics`
- **Status:** CLOSED
- **Decision:** Author DbUp migration script `20261107_0402_Academics_StudentLearningPlan_Tables.sql` provisioning tables `academics.student_learning_plans`, `academics.slp_learning_units`, `academics.academic_plan_adjustments`, and `academics.slp_approvals`, including the partial unique index `UQ_academics_slp_active`.
- **Rationale:** Foundational persistence layer strictly adhering to `docs/entities/academic-entities-tables.md` and ADR-002 (zero physical cross-schema foreign keys).
- **Impact:** Provisions 4 core tables, indexes, and constraints.
- **Architecture Impact:** Low complexity, direct schema migration following established DbUp patterns.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-002: Missing Backend Vertical Slices in `Elsasa.Sis.Academics`
- **Status:** CLOSED
- **Decision:** Implement vertical slices under `Features/StudentLearningPlans/` using Minimal APIs and Dapper raw SQL queries per ADR-004 and ADR-005. Slices include: `CreateStudentLearningPlan`, `UpdateStudentLearningPlan`, `GetStudentLearningPlanById`, `GetStudentLearningPlans`, `ReviseStudentLearningPlan`, `ApproveStudentLearningPlan`, `AddSlpLearningUnit`, `RemoveSlpLearningUnit`, `AddAcademicPlanAdjustment`, `RemoveAcademicPlanAdjustment`, and `SyncSlpParticipations`.
- **Rationale:** Preserves Vertical Slice purity, extreme locality of behavior, and zero ORM overhead.
- **Impact:** Complete backend API surface for SLP operations.
- **Architecture Impact:** Standard vertical slice expansion within `Elsasa.Sis.Academics`.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-003: SLP Lifecycle, Revision Workflow, and Active Concurrency Enforcement
- **Status:** CLOSED
- **Decision:** Adopt Clone-on-Revise with Atomic Status Hand-off. A student has at most one active plan per period, enforced by application check and SQL Server partial unique index (`WHERE status = 'active'`). Active plan remains operative while amendments happen in a draft v+1. Upon revision approval, old plan -> `revised`, draft -> `active`, written in a single transaction with `slp_approvals`.
- **Rationale:** Preserves continuous academic operations without downtime or inconsistent concurrent active plans.
- **Impact:** Unambiguous lifecycle state transitions and robust concurrency safety.
- **Architecture Impact:** Single atomic SQL transaction executing the status hand-off.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-004: Academic Plan Adjustments Workflow & Approval Governance
- **Status:** CLOSED
- **Decision:** Adopt Plan-Coupled Unified Governance. Adjustments (`accommodation`, `substitution`, `exemption`) are drafted within the SLP; Academic Authority plan approval simultaneously approves all adjustments, stamping `approved_by` and `approved_date`. Modifying adjustments on an active plan requires creating a revision (v+1).
- **Rationale:** Enforces explicit authority approval without fracturing governance into fragmented sub-queues.
- **Impact:** Unified approval workflow and guaranteed historical auditability.
- **Architecture Impact:** Adjustment creation and approval tied to SLP transaction boundaries.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-005: Downstream Roster & Participation Sync Model
- **Status:** CLOSED
- **Decision:** Implement explicit "Sync Participations from Plan" command in `SCR-ACD-04` that registers or reconciles `academic_participations` with `source = 'learning_plan'` and `student_learning_plan_id`. In `SCR-ACD-05`, display active SLP badges and auto-link plan IDs on manual additions.
- **Rationale:** Provides explicit, transparent control to coordinators while leveraging already-provisioned columns on `academic_participations`.
- **Impact:** Seamless bridge between planned individual learning and operational class rosters.
- **Architecture Impact:** Slice performs batch merge/insert into `academics.academic_participations`.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-006: Missing Frontend Workbench (`SCR-ACD-04`)
- **Status:** CLOSED
- **Decision:** Scaffold `features/academics/learning-plans/` (`LearningPlanWorkspaceView.vue`) at route `/academics/learning-plans` featuring a Master-Detail layout with 3 tabs (*Constituent Units*, *Plan Adjustments*, *Approval History & Versions*).
- **Rationale:** Strictly respects `docs/screen-inventory.md` allocations, role boundaries, and UX consistency.
- **Impact:** Clean, intuitive UI tailored for Academic Coordinators and Academic Authorities.
- **Architecture Impact:** Standard Vue 3 + PrimeVue component scaffolding.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-007: Cross-Domain Contracts & Domain Events
- **Status:** CLOSED
- **Decision:** Extend `IAcademicsModule` with `GetActiveStudentLearningPlanAsync` and publish MediatR domain events (`StudentLearningPlanCreatedEvent`, `StudentLearningPlanApprovedEvent`, `StudentLearningPlanRevisedEvent`).
- **Rationale:** Satisfies ADR-001/ADR-003 compile-time isolation and allows synchronous inquiries from downstream modules.
- **Impact:** Standardized cross-domain inquiry surface.
- **Architecture Impact:** Contracts assembly extension with zero circular dependencies.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-008: Demo Data Generator & Purger Strategy
- **Status:** CLOSED
- **Decision:** Extend `AcademicsActivitiesDemoDataSeeder` to seed an active revised v2 SLP for Budi Santoso and a draft v1 SLP for Timotius, and update `AcademicsDemoDataPurger` to clean the 4 Submodule 5 tables in strict reverse-dependency order.
- **Rationale:** Ensures demo data reset (`--seed-demo`) runs seamlessly and populates rich realistic scenarios across all newly implemented workbenches.
- **Impact:** Stakeholders and developers can demonstrate SLP capabilities out-of-the-box.
- **Architecture Impact:** Self-contained within `Elsasa.Sis.Academics/Features/DemoData/`.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-009: Contextual Adjustments Visibility in Academic Records (`SCR-ACD-07`)
- **Status:** CLOSED
- **Decision:** Surface active accommodations and substitutions as contextual badges/tooltips in `SCR-ACD-07` (Academic Records Workbench) and `SCR-ACD-05` (Participation).
- **Rationale:** Assures assessors and moderators are aware of approved adaptations (e.g. extended exam time) during grading.
- **Impact:** Prevents unfair grading and guarantees compliance with approved adaptations.
- **Architecture Impact:** Query projection inclusion in records and participation views.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

---

# 9. Architecture Applicability

## Decision
**ARCHITECTURE-REQUIRED**

## Rationale
Implementing Stage 2 Student Learning Plan full scope introduces:
1. **4 new core persistence tables** in SQL Server (`student_learning_plans`, `slp_learning_units`, `academic_plan_adjustments`, `slp_approvals`).
2. **Atomic revision and status hand-off concurrency engine** enforcing the single-active-plan-per-period invariant with a SQL Server partial unique index.
3. **Plan-coupled unified governance model** for accommodations, substitutions, and exemptions.
4. **Participation and roster synchronization mechanism** linking planned units to operational class rosters (`academic_participations`).
5. **New frontend planning workbench** (`SCR-ACD-04` at `/academics/learning-plans`).
6. **Cross-domain contract inquiry** on `IAcademicsModule` and domain events.
7. **Demo data generator and purger extensions** for presentation and automated testing.

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
Feasibility assessment is complete and fully resolved. All decisions strictly embody the **Pragmatic**, **Minimalist**, and **Explicit** architectural principles. The next step is to create the formal **ARCHITECTURE** artifact for `CR-ACD-STUDENT-LEARNING-PLAN` (`IS013 - ACD-STUDENT-LEARNING-PLAN-ARCHITECTURE.md`), followed by the executable **IMPLEMENTATION-PLAN**.

---

# 11. References

Referenced artifacts:
- **DOMAIN:** [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md)
- **ENTITIES:** [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md), [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md) (Submodule 5)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md) (`ACD-WF-02`)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md) (`UC-ACD-09`, `UC-ACD-10`)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md) (`SCR-ACD-04`, `SCR-ACD-05`, `SCR-ACD-07`)
- **ROADMAP:** [sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md) (Stage 2: Student Learning Plan; Criterion 12)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md)
