---
Title: Feasibility Assessment — Stage 2 Academic Standing & Progression Core Scope
Code: CR-ACD-STANDING-PROGRESSION
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-07
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Implementation of the core scope of Stage 2 "Standing & Progression" under the Academic domain as specified in [IS012 - ACD-STANDING-PROGRESSION-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS012%20-%20ACD-STANDING-PROGRESSION-ISSUE.md) and the SIS implementation roadmap ([sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)).

Referenced artifacts:
- **DOMAIN:** [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md) (Academic Intervention, Academic Achievements, Academic Results and History, Progression)
- **ENTITIES:** [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md), [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md) (Submodule 4: lines 674–950)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md) (`ACD-WF-10`, `ACD-WF-11`, `ACD-WF-13`, `ACD-WF-19`)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md) (`UC-ACD-11`, `UC-ACD-12`, `UC-ACD-15`, `UC-ACD-17`, `UC-ACD-21`)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md) (Teacher, Academic Coordinator, Academic Authority, Student Development Officer)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md) (`SCR-ACD-06`, `SCR-ACD-07`, `SCR-ACD-09`, `SCR-STU-03`)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md) (ADR-001 through ADR-010)

## Objective

Evaluate technical feasibility, architectural consistency, codebase readiness, and structural impacts of implementing Stage 2 Standing & Progression across five cohesive submodules, strictly adhering to **Pragmatic**, **Minimalist**, and **Explicit** architectural principles:

1. **Learning Progress & Completion (`ACD-WF-11`, `UC-ACD-12`, `SCR-ACD-07`):**
   - Record and track student advancement toward defined learning expectations for specific learning units (`learning_progress`).
   - Support the Phase-2 minimal progress model: `not_started`, `in_progress`, `completed`, `needs_remediation`, `exempted` (percentage-based tracking is prohibited in Phase 2).
   - Ensure learning progress strictly originates from authorized operational evidence (`assessment_evidence`, `learning_work`, or `authorized_decision`); completion must never be inferred solely from calendar boundaries, administrative placement, or attendance alone.
   - Support completion recorded outside academic period boundaries with an explicit completion date (`completion_date`), accommodating self-paced modular packets, holiday completion, and post-term remediation.
   - Support direct recording of transferred or externally recognized learning (`status = 'completed'`, `basis = 'authorized_decision'`).
   - Enable Academic Coordinators to review completion criteria and formally conclude learning unit participation (`UC-ACD-12`).
2. **Academic Standing Determination (`ACD-WF-13`, `UC-ACD-17`, `SCR-ACD-09`):**
   - Record formal academic conclusions regarding a student's overall academic condition within an academic period (`academic_standings`).
   - Enforce the domain rule that Academic Standing is an academic determination based on policy and evidence, not a simple numerical grade or GPA.
   - Support the standard standing classification model: `good_standing`, `conditional_standing`, and `not_in_good_standing`.
   - Capture governing policy references (`policy_reference`) and mandatory stipulations/conditions when standing is conditional (`conditions`).
   - Embed governance attributes directly on decision records for exceptions: `is_exception`, `exception_reason`, `committee_reference` (e.g., formal decree or meeting minutes reference such as *SK Dewan Guru*), and approving `authorized_by`.
   - Provide authoritative academic standing records to downstream progression, retention, and graduation processes without altering the student's administrative status.
3. **Promotion & Retention Recommendation (`ACD-WF-13`, `UC-ACD-17`, `SCR-ACD-09`, `SCR-STU-03`):**
   - Record Academics' formal evaluation and recommendations regarding student advancement to the next educational level or grade (`promotion_recommendations`).
   - Support standard recommendation outcomes: `promotion_eligible`, `retention_recommended`, and `conditional_promotion`.
   - Record current level (`from_level`), recommended destination level (`recommended_to_level`, nullable for retention), mandatory conditional requirements (`conditions`), supporting evidence summary (`supporting_evidence`), and linked academic standing (`academic_standing_id`).
   - Embed governance attributes directly on exception records (`is_exception`, `exception_reason`, `committee_reference`, `authorized_by`).
   - Enforce the cross-domain boundary rule: Academics owns promotion and retention eligibility recommendations; the Students domain is the sole authority that executes official administrative promotion or retention transitions (`STD-WF-08`, `SCR-STU-03`).
4. **Academic Intervention (`ACD-WF-10`, `UC-ACD-11`, `SCR-ACD-06`):**
   - Record and manage structured academic actions taken when a student requires additional learning support or educational challenge (`academic_interventions`).
   - Support standard intervention types: `remediation`, `reassessment`, `tutoring_recommendation`, `extension_work`, `recovery_plan`, `learning_plan_adjustment`, and `academic_warning`.
   - Implement the dual-trigger intake model:
     - System Early-Warning alerts triggered by objective academic indicators (`assessment_below_threshold`, `missing_work`, `attendance_concern`, `standing_concern`).
     - Staff-initiated educator referrals (`teacher_referral`) based on classroom observations, learning difficulties, or gifted/accelerated learning needs.
   - Manage the intervention lifecycle: `planned`, `in_progress`, `completed`, `cancelled`.
   - Capture detailed intervention plans, target learning units, initiating actors, milestone dates, recorded outcomes, and notes.
   - Enforce domain boundaries: Academics owns interventions directly tied to academic performance and learning; behavioural, psychological, counselling, and pastoral interventions belong strictly to the Development domain (`Development Referral & Wellbeing`). Academic interventions do not independently alter official administrative enrollment status.
5. **Academic Achievement Recognition (`ACD-WF-19`, `UC-ACD-21`, `SCR-ACD-09`):**
   - Formally record and confer achievements derived from academic learning or assessment (`academic_achievements`).
   - Support standard academic achievement types: `subject_distinction`, `academic_honors` (e.g., honor roll / *Juara Umum*), `mastery_recognition`, `academic_certification`, and `academic_competition`.
   - Capture achievement title, linked learning unit (where applicable), academic period, evaluation criteria, supporting academic result evidence, recognizing authority, and recognition date.
   - Enforce domain boundaries: Academics owns achievements derived from academic learning and assessment; Development owns extracurricular, non-academic, and service achievements.
6. **Demo Data Generator Support for Presentation & Verification:**
   - Extend the demo data seeding orchestrator in `AcademicsActivitiesDemoDataSeeder` to generate realistic standing, progression recommendations, interventions, and achievements for Hagios School of Life student personas (e.g., Rina Kusuma, Budi Santoso, Timotius).
   - Update `AcademicsDemoDataPurger` to clean the 5 new tables in strict reverse-dependency order.

### Scope Boundaries & Explicit Deferrals

- **Graduation Eligibility Determination (`ACD-WF-14`, `UC-ACD-18`, `graduation_eligibilities`):** Formally determining cumulative graduation eligibility against multi-year program requirements is deferred to **Stage 3** as planned in `sub-modul-and-development-phases.md`.
- **Purpose-Specific Academic Clearance (`ACD-WF-16`, `ACD-WF-17`, `UC-ACD-14`, `UC-ACD-20`, `SCR-ACD-08`, `academic_clearances`):** Evaluating academic clearance for transfer-out is deferred to **Stage 3** as planned in `sub-modul-and-development-phases.md`.
- **Administrative Lifecycle Transition Execution:** Executing official grade promotion, retention, campus/program reassignment, or homeroom cohort rollover belongs exclusively to the `Student Placement & Status` module in the Students domain (`SCR-STU-03`, `STD-WF-08`). Academics only provides the recommendation.
- **Individualized Learning Plans (SLP):** Formulation, adjustments (accommodations, substitutions, exemptions), and formal approvals of individualized student learning plans belong to `Student Learning Plan` (`SCR-ACD-04`).

---

# 2. Current State

## Existing Behavior

1. **Database Schema:**
   - The database contains provisioned tables for Operations (`operations`), Students (`students`), and Finance (`finance`).
   - The `academics` schema exists and contains tables from Submodule 1 (`IS008`: `learning_structures`, `learning_units`, `academic_requirements`), Submodule 2 (`IS009`: `academic_participations`, `learning_activities`, `attendance_records`, `attendance_corrections`, `assignments`, `submissions`), and Submodule 3 (`IS010`: `assessments`, `assessment_evidence`, `academic_results`, `academic_record_corrections`, `report_cards`, `report_card_entries`, `transcripts`).
   - The tables specified for Submodule 4 in [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md) lines 674–950 (`academics.learning_progress`, `academics.academic_standings`, `academics.promotion_recommendations`, `academics.academic_interventions`, `academics.academic_achievements`) are **not yet provisioned**.
   - Note: `academics.academic_record_corrections` is already provisioned with check constraint `CK_academics_academic_record_corrections_type` permitting `'academic_standing'` and `'promotion_recommendation'`.
2. **Backend Domain & Services:**
   - `src/backend/Elsasa.Sis.Academics` exists and is integrated into `Elsasa.Sis.sln` and `Elsasa.Sis.Api`.
   - Feature slices exist for Learning Structures, Learning Units, Academic Requirements, Academic Participations, Learning Activities, Attendance, Assignments, Submissions, Assessments, Results, Report Cards, Transcripts, and Corrections.
   - There are **no feature slices, domain handlers, or Dapper repositories** for Learning Progress, Academic Standings, Promotion Recommendations, Academic Interventions, or Academic Achievements.
   - Upstream contracts in `Elsasa.Sis.Contracts`:
     - `IStudentsModule` exposes student enrollment validation (`IsStudentEnrolledAsync`).
     - `IOperationsCalendarQueryService` exposes active period validation.
     - `IAcademicsModule` exposes approved results, completed units, attendance summaries, and learning unit details.
3. **Frontend Implementation:**
   - `src/frontend/src/features/academics/records` exists for `SCR-ACD-07` (Academic Records Workbench: result moderation, report cards, transcripts, corrections), but does **not yet contain** the "Learning Progress & Completion" tab.
   - There is **no implementation** for `SCR-ACD-06` (`Academic Intervention Workbench`: intervention planning, at-risk detection, review checkpoints).
   - There is **no implementation** for `SCR-ACD-09` (`Cohort Academic Review`: cohort standing review, progression recommendations, achievement conferral).
4. **Demo Data Seeding & Purging:**
   - `src/backend/Elsasa.Sis.Academics/Features/DemoData/AcademicsActivitiesDemoDataSeeder.cs` seeds participations, attendance, assignments, submissions, approved results, report cards, and transcripts.
   - `src/backend/Elsasa.Sis.Academics/Features/DemoData/AcademicsDemoDataPurger.cs` cleans all `academics.*` tables in reverse dependency order, but does not yet include Submodule 4 tables.

## Existing Constraints

1. **Domain Isolation & ADR Compliance (ADR-001, ADR-002, ADR-010):**
   - `Elsasa.Sis.Academics` references only `Elsasa.Sis.Shared` and `Elsasa.Sis.Contracts`. Direct assembly references to `Students`, `Finance`, `Operations`, or `Teaching` are prohibited.
   - Schema isolation: `academics` schema owns all Submodule 4 tables. Zero physical foreign keys across domain schemas; external keys (`student_id`, `academic_period_id`) are stored as logical UUIDs (`uniqueidentifier`).
2. **Governance & Decision Invariant (`academic-domain.md`, `actors.md`):**
   - Academic Standing and Promotion/Retention Recommendations strictly require formal authorization by the `Academic Authority` (`UC-ACD-17`).
   - Academics issues progression recommendations only; official student promotion or retention execution belongs exclusively to the Students domain (`SCR-STU-03`, `STD-WF-08`).
   - Academic Interventions related to learning performance are owned by Academics (`UC-ACD-11`); pastoral, behavioural, and counselling interventions belong strictly to Development (`UC-DEV-06`).
   - Academic interventions and standing determinations do not independently alter official student enrollment or placement status.
3. **Auditable Immutability & Exception Invariant (`academic-domain.md`, Resolved Question 16):**
   - Direct embedded governance columns (`is_exception`, `exception_reason`, `committee_reference`, `authorized_by`, `conditions`) are recorded on decision tables, eliminating unnecessary committee subsystem tables.
   - Approved standing determinations and progression recommendations can only be modified through the formal `academic_record_corrections` protocol with mandatory audit logs and Academic Authority authorization.

---

# 3. Gap Analysis

| ID | Severity | Gap | Status | Resolution |
|------|------|------|:---:|------|
| `GAP-001` | CRITICAL | **Missing Database Tables:** Database lacks tables `learning_progress`, `academic_standings`, `promotion_recommendations`, `academic_interventions`, and `academic_achievements` in schema `academics`. | **CLOSED** | Author DbUp migration script `20261107_0401_Academics_StandingProgression_Tables.sql` with full table DDL, intra-schema foreign keys, indexes, and check constraints matching `academic-entities-tables.md`. |
| `GAP-002` | CRITICAL | **Missing Backend Vertical Slices in `Elsasa.Sis.Academics`:** No domain models, endpoint mappings, validators, or Dapper handlers exist for learning progress completion, academic standing determination, promotion recommendations, academic interventions, or academic achievements. | **CLOSED** | Implement vertical slices under `Features/LearningProgress/`, `Features/AcademicStandings/`, `Features/PromotionRecommendations/`, `Features/AcademicInterventions/`, and `Features/AcademicAchievements/` using Minimal APIs and Dapper raw SQL queries per ADR-004 and ADR-005. |
| `GAP-003` | MAJOR | **Standing & Progression Determination Engine in `SCR-ACD-09` (`UC-ACD-17`):** Academic Authorities need an efficient, auditable way to evaluate cohort performance without manual data-entry fatigue or rigid opaque background jobs. | **CLOSED** | Adopted **Batch Evaluation Preview & Approval**: an on-demand cohort evaluation query computes proposed standing and progression from approved results against threshold rules, presenting a review grid where the Authority can adjust individual records (with exception rationale/decree) and commit in batch. |
| `GAP-004` | MAJOR | **Academic Intervention Dual-Trigger Intake in `SCR-ACD-06` (`UC-ACD-11`):** Need both system early-warning alerts (`assessment_below_threshold`, `missing_work`, `attendance_concern`, `standing_concern`) and educator referrals (`teacher_referral`) without over-engineering persistent alert queues. | **CLOSED** | Adopted **On-Demand At-Risk Query**: `SCR-ACD-06` features an "At-Risk Learners" tab querying active assessments, attendance, and submissions in real-time with 1-click conversion into an intervention plan, alongside manual educator referral intake modals. |
| `GAP-005` | MAJOR | **Learning Progress Tracking & Out-of-Period Completion in `SCR-ACD-07` (`UC-ACD-12`):** System must support the Phase-2 minimal model (`not_started`, `in_progress`, `completed`, `needs_remediation`, `exempted`), out-of-period explicit `completion_date` stamping, and authorized transfer recognition. | **CLOSED** | Adopted **Explicit Evidence-Driven Progress Ledger**: add "Learning Progress & Completion" tab to `SCR-ACD-07` allowing coordinators to review completion criteria, stamp explicit completion dates, or record recognized prior learning. |
| `GAP-006` | MAJOR | **Missing Frontend Workbenches (`SCR-ACD-06`, `SCR-ACD-09`, `SCR-ACD-07` extension):** No UI screens exist for intervention work queue or cohort academic review. | **CLOSED** | Adopted **Role-Aligned Slices per Screen Inventory**: scaffold `features/academics/interventions/` (`SCR-ACD-06`), `features/academics/cohort-review/` (`SCR-ACD-09`), and add the completion review tab to `features/academics/records/` (`SCR-ACD-07`). |
| `GAP-007` | MAJOR | **Cross-Domain Contracts & Domain Events:** Students and Finance domains require contract inquiries and domain events for progression recommendations, standing, and academic honors. | **CLOSED** | Adopted **Hybrid Contract & Domain Events**: expose query methods on `IAcademicsModule` (`GetStudentAcademicStandingAsync`, `GetPromotionRecommendationAsync`, `GetStudentAcademicAchievementsAsync`) and publish MediatR domain events (`AcademicStandingDeterminedEvent`, `PromotionRecommendationIssuedEvent`, `AcademicAchievementConferredEvent`). |
| `GAP-008` | MAJOR | **Demo Data Generator & Purger for Sample Presentation:** Demo data generator must seed realistic standing, progression, interventions, and achievements for Hagios personas and clean up without FK constraint failures. | **CLOSED** | Adopted **Extend Academics Seeder & Update Purger**: extend `AcademicsActivitiesDemoDataSeeder` to seed the 5 tables with Hagios student personas (Rina, Budi, Timotius) and update `AcademicsDemoDataPurger` in reverse-dependency order. |
| `GAP-009` | MINOR | **Non-Grade Record Correction Protocol Integration (`UC-ACD-15`):** Post-approval modifications to standing or promotion recommendations must not silently overwrite records. | **CLOSED** | Adopted **Unified Correction Logging via `academic_record_corrections`**: execute transactional corrections with mandatory reason (min 10 chars) and Academic Authority authorization, recording pre/post state in `academic_record_corrections`. |

---

# 4. Open Questions

| ID | Question | Impact | Status | Decision Summary |
|------|------|------|:---:|------|
| `OQ-001` | How should the 5 submodules of Standing & Progression be distributed across the frontend workbenches? | Dictates UI component architecture, routing, and role ergonomics. | **CLOSED** | **Follow screen-inventory.md exactly:** Implement `SCR-ACD-06` (`features/academics/interventions/`) and `SCR-ACD-09` (`features/academics/cohort-review/`) as new workbenches, and add a "Learning Progress & Completion" tab to `SCR-ACD-07` (`features/academics/records/`). |
| `OQ-002` | How should Academic Standing and Promotion Recommendations be determined in `SCR-ACD-09` (Cohort Academic Review)? | Dictates computation complexity, user interaction flow, and authority governance. | **CLOSED** | **Batch Evaluation Preview & Approval:** System runs an on-demand cohort query that evaluates approved results and proposes standing/progression. Academic Authority reviews the proposal grid, adjusts individual records (with exception rationale/decree reference), and batch-commits. |
| `OQ-003` | How should the System Early-Warning portion of the Academic Intervention dual-trigger model be implemented in `SCR-ACD-06`? | Dictates background daemon vs real-time query complexity and alert persistence overhead. | **CLOSED** | **On-Demand At-Risk Query:** Provide an "At-Risk Learners" tab in `SCR-ACD-06` that dynamically queries existing assessment scores, attendance records, and missing submissions in real-time, allowing 1-click conversion into a formal intervention plan. |
| `OQ-004` | What evaluation heuristic should the batch proposal engine use to suggest Academic Standing for a cohort? | Dictates the automated proposal calculation algorithm in the backend. | **CLOSED** | **Completion & Passing Threshold Rule:** `Good Standing` if all required learning units in the period have passing approved results and no active attendance/missing work alerts; `Conditional Standing` if 1–2 units are below threshold or have remediation needs; `Not In Good Standing` if >2 units fail or substantial requirements are unfulfilled. Manual committee exception overrides (*SK Dewan Guru*) are supported. |
| `OQ-005` | How should Standing, Promotion Recommendations, and Achievements be exposed to consuming domains (Students and Finance)? | Dictates cross-domain API surface, coupling, and asynchronous event notifications. | **CLOSED** | **Hybrid Contract & Domain Events:** Expose query methods on `IAcademicsModule` for synchronous inquiry by Students/Finance, and publish MediatR domain events (`AcademicStandingDeterminedEvent`, `PromotionRecommendationIssuedEvent`, `AcademicAchievementConferredEvent`) for asynchronous notifications. |
| `OQ-006` | How should the Demo Data Generator be updated to support the demo of Academic Standing & Progression? | Dictates seeder orchestration, Hagios persona richness, and database reset integrity. | **CLOSED** | **Extend Academics Activities Seeder & Update Purger:** In `AcademicsActivitiesDemoDataSeeder`, seed realistic standing, progression, intervention, and achievement records immediately following approved results using established Hagios personas (Rina, Budi, Timotius), and update `AcademicsDemoDataPurger` to clean the 5 tables in reverse-dependency order. |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| `ASM-001` | The database engine is SQL Server 2022+ with the `academics` schema established by prior issues (`IS008`, `IS009`, `IS010`). |
| `ASM-002` | Approved academic results (`academic_results WHERE is_final = 1 AND result_status = 'approved'`) and learning unit definitions (`learning_units`) serve as authoritative inputs. |
| `ASM-003` | The Students domain owns official promotion and retention administrative transitions (`SCR-STU-03`, `STD-WF-08`); Academics issues recommendations only. |
| `ASM-004` | The Development domain owns behavioural, pastoral, and non-academic interventions; Academics owns learning performance interventions only. |
| `ASM-005` | Graduation Eligibility Determination (`ACD-WF-14`, `UC-ACD-18`) and Academic Clearance (`ACD-WF-16`, `UC-ACD-14`) are deferred to Stage 3 per roadmap planning. |
| `ASM-006` | Progress tracking follows the Phase-2 minimal model (`not_started`, `in_progress`, `completed`, `needs_remediation`, `exempted`); percentage-based tracking is prohibited in Phase 2. |
| `ASM-007` | Exception governance fields (`is_exception`, `exception_reason`, `committee_reference`, `authorized_by`, `conditions`) are embedded directly on decision tables per Resolved Question 16, avoiding unnecessary committee subsystem tables. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| `RISK-001` | Academic Authority cognitive overload during cohort progression review for large student groups. | Medium | Provide `Batch Evaluation Preview` with summary badges, filterable columns, and 1-click cohort batch approval in `SCR-ACD-09`. |
| `RISK-002` | Inconsistent or unpassed progression recommendations executing in Students domain without academic authorization. | High | Enforce strict domain boundaries: `promotion_recommendations` must be in approved status before the Students domain `IStudentsModule` transition can consume them. |
| `RISK-003` | Stale or phantom early-warning alert notifications if underlying grades or attendance are corrected. | Medium | Use on-demand real-time query detection for "At-Risk Learners" in `SCR-ACD-06`, eliminating out-of-sync alert queue persistence. |
| `RISK-004` | Foreign key constraint failures during demo data reset/purge. | High | Update `AcademicsDemoDataPurger` to delete from Submodule 4 tables (`academic_achievements`, `promotion_recommendations`, `academic_standings`, `academic_interventions`, `learning_progress`) in strict reverse-dependency order before `academic_results` and `learning_units`. |

---

# 7. Recommendations

## Screen and Submodule Distribution (GAP-006 / OQ-001)

### Option A: Follow screen-inventory.md exactly — Recommended
- Implement `SCR-ACD-06` (`features/academics/interventions/`) as a dedicated Academic Intervention Workbench for Academic Coordinators.
- Implement `SCR-ACD-09` (`features/academics/cohort-review/`) as a dedicated Cohort Academic Review workbench for Academic Authorities (standing determinations, progression recommendations, achievement conferral).
- Add "Learning Progress & Completion" tab to `SCR-ACD-07` (`features/academics/records/`) for Academic Coordinators to review completion criteria and formally conclude learning unit participation.
- **Pragmatic & Explicit:** Respects established role permissions, workflow rhythms, and screen boundaries without fragmenting or overloading workbenches.

---

## Standing & Progression Determination Model (GAP-003 / OQ-002, OQ-004)

### Option A: Batch Evaluation Preview & Approval — Recommended
- In `SCR-ACD-09`, the Academic Authority selects an Academic Period and Grade/Cohort.
- The system executes an on-demand evaluation query applying the **Completion & Passing Threshold Rule**:
  - `Good Standing` + `Promotion Eligible`: 100% of required units passed, no active alerts.
  - `Conditional Standing` + `Conditional Promotion`: 1–2 units below passing threshold or needing remediation; mandatory condition text required.
  - `Not In Good Standing` + `Retention Recommended`: >2 units failed or major requirements unfulfilled.
- The Authority reviews the cohort preview grid, can adjust individual standing/progression with mandatory exception rationale / committee decree reference (*SK Dewan Guru*), and clicks "Approve & Commit Cohort".
- **Minimalist & Explicit:** Eliminates tedious manual student-by-student data entry while strictly retaining full human Academic Authority governance and auditability.

---

## Academic Intervention Intake Model (GAP-004 / OQ-003)

### Option A: On-Demand At-Risk Query with Dual-Trigger Intake — Recommended
- In `SCR-ACD-06`, provide two views:
  1. **"Active Interventions"** tab: Filterable work queue of active and historical interventions (`planned`, `in_progress`, `completed`, `cancelled`).
  2. **"At-Risk Learners"** tab: Live query identifying students with scores below passing threshold, missing submissions, low attendance (<75%), or standing concerns.
- 1-click action "Initiate Intervention" from the At-Risk grid pre-populates the intervention modal with detected trigger type and description.
- Manual "Refer Student" button allows staff to initiate educator referrals (`teacher_referral`) based on qualitative classroom observations or gifted/extension needs.
- **Pragmatic & Minimalist:** Zero background daemon, cron job, or persistent alert table overhead.

---

## Cross-Domain Integration (GAP-007 / OQ-005)

### Option A: Hybrid IAcademicsModule Contract & Domain Events — Recommended
- Expose synchronous inquiry methods on `IAcademicsModule`:
  - `Task<Result<StudentAcademicStandingDto?>> GetStudentAcademicStandingAsync(Guid studentId, Guid academicPeriodId, CancellationToken ct = default);`
  - `Task<Result<PromotionRecommendationDto?>> GetPromotionRecommendationAsync(Guid studentId, Guid academicPeriodId, CancellationToken ct = default);`
  - `Task<Result<IReadOnlyList<AcademicAchievementDto>>> GetStudentAcademicAchievementsAsync(Guid studentId, Guid? academicPeriodId = null, CancellationToken ct = default);`
- Publish MediatR notification domain events:
  - `AcademicStandingDeterminedEvent`
  - `PromotionRecommendationIssuedEvent`
  - `AcademicAchievementConferredEvent`
- **Explicit & Isolated:** Supports compile-time isolated synchronous inquiries for transactional validation in Students and Finance, while offering asynchronous events for audit trails and notifications.

---

## Demo Data Generator Integration (GAP-008 / OQ-006)

### Option A: Extend AcademicsActivitiesDemoDataSeeder & Update Purger — Recommended
- In `AcademicsActivitiesDemoDataSeeder`, append seeding for Submodule 4 tables immediately following approved academic results:
  - `learning_progress`: Seed realistic progress states across enrolled units for Hagios students (e.g., completed for Grade 4 units, needs_remediation for Budi's Math).
  - `academic_standings`: Seed `good_standing` for Rina Kusuma, `conditional_standing` with SK Dewan Guru reference for Budi Santoso, and `not_in_good_standing` for Timotius.
  - `promotion_recommendations`: Seed `promotion_eligible` for Rina, `conditional_promotion` for Budi, and `retention_recommended` for Timotius.
  - `academic_interventions`: Seed active remediation intervention for Budi Santoso (low math score) and extension work for Rina Kusuma (gifted math).
  - `academic_achievements`: Seed subject distinction (*Nilai Tertinggi Matematika*) and academic honors (*Juara Umum Grade 4*) for Rina Kusuma.
- In `AcademicsDemoDataPurger`, prepend SQL deletion commands in reverse-dependency order:
  `academic_achievements` → `promotion_recommendations` → `academic_standings` → `academic_interventions` → `learning_progress` before `academic_results`.
- **Minimalist & Zero-Friction:** Reuses the existing CLI and UI demo data reset pipeline without modifying `DemoDataOrchestrator` construction.

---

# 8. Gap Closure

### GAP-001: Missing Database Tables in schema `academics`
- **Status:** CLOSED
- **Decision:** Author DbUp migration script `20261107_0401_Academics_StandingProgression_Tables.sql` provisioning tables `academics.learning_progress`, `academics.academic_standings`, `academics.promotion_recommendations`, `academics.academic_interventions`, and `academics.academic_achievements` strictly matching `docs/entities/academic-entities-tables.md`.
- **Rationale:** Foundational persistence layer required before backend slices can be constructed. Preserves zero cross-schema physical foreign keys and enforces explicit check constraints.
- **Impact:** Provisions 5 new core tables with indexes and check constraints.
- **Architecture Impact:** Low complexity, direct schema migration following established DbUp patterns.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-002: Missing Backend Vertical Slices in `Elsasa.Sis.Academics`
- **Status:** CLOSED
- **Decision:** Implement vertical slices under `Features/LearningProgress/`, `Features/AcademicStandings/`, `Features/PromotionRecommendations/`, `Features/AcademicInterventions/`, and `Features/AcademicAchievements/` using Minimal APIs and Dapper raw SQL queries.
- **Rationale:** Adheres to ADR-004 (Vertical Slice Architecture) and ADR-005 (Dapper raw SQL queries), ensuring compile-time isolation and clear separation of concerns.
- **Impact:** Backend API endpoints for all 5 submodules.
- **Architecture Impact:** Standard vertical slice expansion within `Elsasa.Sis.Academics`.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-003: Standing & Progression Determination Engine in `SCR-ACD-09`
- **Status:** CLOSED
- **Decision:** Adopt Batch Evaluation Preview & Approval with the Completion & Passing Threshold Rule. Academic Authority reviews proposed standing and progression in a cohort grid, applies manual decree overrides (*SK Dewan Guru*) if needed, and commits in batch.
- **Rationale:** Eliminates coordinator fatigue while preserving mandatory human Academic Authority governance and full auditability.
- **Impact:** Enables one-click cohort progression evaluation with override flexibility.
- **Architecture Impact:** Query handler computes proposals dynamically from approved results; command handler batch-inserts standing and recommendations in a single SQL transaction.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-004: Academic Intervention Dual-Trigger Intake in `SCR-ACD-06`
- **Status:** CLOSED
- **Decision:** Adopt on-demand real-time At-Risk detection query over existing assessment scores, attendance, and submissions, complemented by manual staff educator referral modals.
- **Rationale:** Delivers full dual-trigger compliance without the operational overhead of persistent alert tables or background cron workers.
- **Impact:** Real-time visibility into struggling learners with 1-click intervention plan creation.
- **Architecture Impact:** Read query aggregates metrics dynamically; standard CRUD slice manages intervention lifecycle.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-005: Learning Progress Tracking & Out-of-Period Completion in `SCR-ACD-07`
- **Status:** CLOSED
- **Decision:** Implement Phase-2 minimal model progress tracking (`not_started`, `in_progress`, `completed`, `needs_remediation`, `exempted`) in a dedicated tab on `SCR-ACD-07`, supporting explicit completion dates and recognized transfer learning.
- **Rationale:** Complies with domain invariants prohibiting percentage progress tracking in Phase 2 and prohibiting completion inference solely from calendar boundaries.
- **Impact:** Formal learning completion audit and credit tallying.
- **Architecture Impact:** Standard feature slice linked to learning units and participations.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-006: Missing Frontend Workbenches (`SCR-ACD-06`, `SCR-ACD-09`, `SCR-ACD-07` extension)
- **Status:** CLOSED
- **Decision:** Scaffold `features/academics/interventions/` (`SCR-ACD-06`), `features/academics/cohort-review/` (`SCR-ACD-09`), and add completion tab to `features/academics/records/` (`SCR-ACD-07`) using PrimeVue and TanStack Query.
- **Rationale:** Strictly respects `docs/screen-inventory.md` allocations, role boundaries, and UX consistency.
- **Impact:** Frontend views matching actor responsibilities.
- **Architecture Impact:** Vue 3 component scaffolding following existing feature directory conventions.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-007: Cross-Domain Contracts & Domain Events
- **Status:** CLOSED
- **Decision:** Extend `IAcademicsModule` with synchronous query methods (`GetStudentAcademicStandingAsync`, `GetPromotionRecommendationAsync`, `GetStudentAcademicAchievementsAsync`) and publish MediatR domain events (`AcademicStandingDeterminedEvent`, `PromotionRecommendationIssuedEvent`, `AcademicAchievementConferredEvent`).
- **Rationale:** Satisfies ADR-001/ADR-003 compile-time isolation while enabling immediate cross-domain enablement for Students (`SCR-STU-03` promotion execution) and Finance (`Finance Benefits` scholarships).
- **Impact:** Full cross-domain integration readiness for Stage 2 Criteria 6 and 9.
- **Architecture Impact:** Contracts assembly extension with zero circular dependencies.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-008: Demo Data Generator & Purger for Sample Presentation
- **Status:** CLOSED
- **Decision:** Extend `AcademicsActivitiesDemoDataSeeder` to seed standing, progression recommendations, interventions, and achievements using Hagios student personas (Rina, Budi, Timotius), and update `AcademicsDemoDataPurger` to clean the 5 tables in reverse-dependency order.
- **Rationale:** Ensures demo data reset (`--seed-demo`) runs seamlessly and populates rich realistic scenarios across all newly implemented workbenches.
- **Impact:** Stakeholders and developers can demonstrate Standing & Progression capabilities out-of-the-box.
- **Architecture Impact:** Self-contained within `Elsasa.Sis.Academics/Features/DemoData/`.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

### GAP-009: Non-Grade Record Correction Protocol Integration (`UC-ACD-15`)
- **Status:** CLOSED
- **Decision:** Re-use the existing `academics.academic_record_corrections` table (`record_type = 'academic_standing'` and `'promotion_recommendation'`) with mandatory reason and Academic Authority authorization.
- **Rationale:** Invariant: approved academic decisions cannot be silently overwritten. Reusing the unified correction table avoids schema duplication.
- **Impact:** Regulatory audit compliance and full traceability for post-approval amendments.
- **Architecture Impact:** Reuses existing correction infrastructure established in `IS010`.
- **Resolved By:** Drury (Architect / Lead Developer)
- **Resolved Date:** 2026-11-07

---

# 9. Architecture Applicability

## Decision
**ARCHITECTURE-REQUIRED**

## Rationale
Implementing Stage 2 Standing & Progression core scope introduces:
1. **5 new core persistence tables** in SQL Server (`learning_progress`, `academic_standings`, `promotion_recommendations`, `academic_interventions`, `academic_achievements`).
2. **Cross-domain integration boundaries** providing authoritative progression recommendations consumed by `Students` (`SCR-STU-03`, `STD-WF-08`) and honors consumed by `Finance` (`Finance Benefits`).
3. **Automated cohort evaluation engine** computing standing and progression recommendations from approved results against threshold rules with exception override governance (*SK Dewan Guru*).
4. **Dual-trigger intake architecture** for academic interventions combining real-time at-risk metric detection with educator referrals.
5. **2 major frontend workbenches** (`SCR-ACD-06`, `SCR-ACD-09`) and 1 screen extension (`SCR-ACD-07`).
6. **Demo data seeder and purger extensions** ensuring reliable presentation and verification capabilities.

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
Feasibility assessment is complete and fully resolved. All decisions strictly embody the **Pragmatic**, **Minimalist**, and **Explicit** architectural principles. The next step is to create the formal **ARCHITECTURE** artifact for `CR-ACD-STANDING-PROGRESSION` (`IS012 - ACD-STANDING-PROGRESSION-ARCHITECTURE.md`), followed by the executable **IMPLEMENTATION-PLAN**.

---

# 11. References

Referenced artifacts:
- **DOMAIN:** [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md)
- **ENTITIES:** [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md), [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md) (`ACD-WF-10`, `ACD-WF-11`, `ACD-WF-13`, `ACD-WF-19`)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md) (`UC-ACD-11`, `UC-ACD-12`, `UC-ACD-15`, `UC-ACD-17`, `UC-ACD-21`)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md) (`SCR-ACD-06`, `SCR-ACD-07`, `SCR-ACD-09`, `SCR-STU-03`)
- **ROADMAP:** [sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md) (Stage 2: Standing & Progression; Criteria 6 and 9)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md)
