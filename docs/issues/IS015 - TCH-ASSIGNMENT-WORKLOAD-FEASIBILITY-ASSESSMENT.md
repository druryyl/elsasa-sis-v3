---
Title: Feasibility Assessment — Stage 2 Teaching Assignment & Workload Full Scope
Code: CR-TCH-ASSIGNMENT-WORKLOAD
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-10
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Implementation of the full scope of Stage 2 "Teaching Assignment & Workload" under the Teaching domain as specified in [IS015 - TCH-ASSIGNMENT-WORKLOAD-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS015%20-%20TCH-ASSIGNMENT-WORKLOAD-ISSUE.md) and the SIS implementation roadmap ([sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)).

Referenced artifacts:
- **DOMAIN:** [teaching-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/teaching-domain.md) (Submodules 2.1–2.7: Teacher Availability, Teaching Demand, Teacher Eligibility, Teaching Assignment, Workload and Capacity, Capacity Conflicts, Substitution and Reassignment, Assignment Completion, Teaching History)
- **ENTITIES:** [teaching-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/teaching-entities.md), [teaching-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/teaching-entities-tables.md) (Module 2: lines 365–950)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md) (`TCH-WF-04` to `TCH-WF-12`)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md) (`UC-TCH-01`, `UC-TCH-04`, `UC-TCH-05`, `UC-TCH-06`, `UC-TCH-07`, `UC-TCH-08`, `UC-TCH-10`, `UC-TCH-11`, `UC-TCH-12`)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md) (Teacher, Teaching Coordinator, Teaching Authority; Resolution 3.8 Pre-Authorized Pool Delegation)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md) (`SCR-TCH-02` Teaching Assignment & Coverage Workbench, `SCR-TCH-03` Faculty Workload Monitoring, `SCR-TCH-04` My Teaching Availability)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md) (ADR-001, ADR-002, ADR-004, ADR-005, ADR-006, ADR-010)

## Objective

Evaluate technical feasibility, architectural consistency, codebase readiness, and structural impacts of implementing Stage 2 Teaching Assignment & Workload in Elsasa SIS v3, strictly adhering to **Pragmatic**, **Minimalist**, and **Explicit** architectural principles:

1. **Teaching Demand Intake (`TCH-WF-05`, `UC-TCH-04`, `SCR-TCH-02`):**
   - Ingest and clarify formal teaching capacity requests (`teaching_demands`) originating from Academics (learning units/courses), Students (homeroom cohorts), and Development (extracurricular activities).
   - Capture demand code (`demand_code`), source domain, opaque external work reference (`source_work_reference`), human-readable label (`source_work_label`), target academic period, required capability code, requested assignment role, estimated workload effort and unit, assessment authority mandate, requested dates, campus location, and teacher headcount.
   - Support demand lifecycle transitions: `received`, `accepted_for_staffing`, `returned_for_clarification`, `changed`, `cancelled`, `declined`, `staffed`.
   - Maintain strict separation of concerns: Teaching staffs the demand without redefining the underlying academic curriculum, student cohort, or operational timetable.

2. **Teacher Eligibility Evaluation Engine (`TCH-WF-06`, `UC-TCH-05`, `SCR-TCH-02`):**
   - Provide an explainable, policy-driven automated evaluation of candidate faculty against demand requirements (`teacher_eligibility_evaluations`).
   - Evaluate 5 policy checks automatically: (1) active teacher profile standing, (2) matching active teaching authorization, (3) verified qualification evidence if required, (4) scheduling availability, and (5) remaining workload capacity.
   - Persist evaluation records with binary determination (`is_eligible`), detailed status (`eligible`, `eligible_under_supervision`, `ineligible_unqualified`, `ineligible_conflicted`, `ineligible_capacity_exceeded`), check breakdown flags, human-readable natural language explanation, and coordinator remarks.

3. **Teaching Assignment Approval & Homeroom Integration (`TCH-WF-07`, `TCH-WF-08`, `UC-TCH-10`, `UC-TCH-11`, `SCR-TCH-02`):**
   - Define configurable assignment roles (`assignment_roles`, e.g., Guru Utama, Guru Pendamping, Asisten Guru, Penilai, Wali Kelas, Pembimbing Akademik, Fasilitator, Guru Pengganti) with lead role indicators and workload counting flags.
   - Connect eligible faculty to authorized teaching responsibilities through formal assignments (`teaching_assignments`), capturing assignment number, assigned teacher, demand link, assignment role, scope (`learning_unit`, `cohort_group`, `individual_student`, `extracurricular_group`), scope reference/label, academic period, effective date range, committed workload, and authority approval metadata.
   - Manage assignment lifecycle: `proposed`, `approved`, `rejected`, `withdrawn`, `returned_for_revision`, `suspended`, `cancelled`, `completed`.
   - Support co-teaching and shared responsibility via distinct individual assignments referencing the same demand and scope.
   - Support homeroom advisory assignments (`assignment_role = 'HOMEROOM_ADVISOR'`) decoupled from student cohort placement (owned by Students) and physical homeroom allocation (owned by Operations).
   - Identify, classify, and track capacity conflicts (`capacity_conflicts`) with severity (`advisory` vs. `blocking`) and resolution tracking.

4. **Workload & Capacity Management (`TCH-WF-04`, `TCH-WF-10`, `UC-TCH-01`, `UC-TCH-07`, `SCR-TCH-03`, `SCR-TCH-04`):**
   - Enable teachers to declare weekly working availability, preferred teaching blocks, and recurring constraints across academic periods (`teacher_availabilities`, `SCR-TCH-04`).
   - Record explicit temporary unavailability windows (`teacher_unavailability_windows`, e.g., medical leave, official training, personal leave, blackout dates) without silently cancelling active assignments.
   - Establish versioned workload policies (`workload_policies`) defining measurement units, standard capacity limits, warning thresholds, prohibited capacity ceilings, and countable activity inclusions.
   - Atomically synchronize reproducible snapshots of planned, committed, and remaining teacher capacity per period (`workload_commitments`, `SCR-TCH-03`).
   - Manage formal workload exception requests (`workload_exceptions`) when an assignment requires exceeding prohibited capacity ceilings, enforcing strict governance: Coordinator Recommends → Teaching Authority Approves → Exception Granted (no self-approval).

5. **Substitution & Reassignment (`TCH-WF-09`, `UC-TCH-06`, `SCR-TCH-02`):**
   - Implement **Pre-Authorized Pool Delegation** (Resolution 3.8): Teaching Coordinator has unilateral authority to commit emergency substitutes (`teaching_substitutions`) holding active matching authorizations within workload caps. Escalates to Teaching Authority only if the substitute lacks authorization or breaches capacity ceilings.
   - Manage permanent reassignments (`teaching_reassignments`) transferring responsibility to a new teacher with predecessor links, formal justification, Teaching Authority approval, and syllabus/grading handover verification (`requires_handover`, `handover_status`).
   - Strictly preserve historical integrity: substitutions and reassignments take effect prospectively and never alter past academic or attendance records.

6. **Assignment Completion (`TCH-WF-11`, `UC-TCH-08`, `SCR-TCH-02`):**
   - Formally conclude teaching assignments upon reaching period end or milestones (`assignment_completions`).
   - Verify academic deliverables completion (grade submissions confirmed by Academics), confirm handover, and atomically release committed teacher capacity back to the available pool.

7. **Teaching History Preservation (`TCH-WF-12`, `UC-TCH-12`, `SCR-TCH-01`, `SCR-TCH-02`):**
   - Preserve an immutable append-only log of significant decisions, transitions, and domain events across a teacher's assignments and capacity lifecycle (`teaching_history_entries`).
   - Leverage existing `teaching_record_corrections` table provisioned in Stage 1 to support formal, auditable corrections on finalized assignments.

8. **Frontend Cohesive Workspaces (`SCR-TCH-02`, `SCR-TCH-03`, `SCR-TCH-04`):**
   - Implement `SCR-TCH-02` (`/teaching/assignments`) as a Work Queue workbench with tabs: *Demands Intake*, *Candidate Eligibility*, *Active & Proposed Assignments*, *Coverage & Substitutions*, *Conclusions*.
   - Implement `SCR-TCH-03` (`/teaching/workload`) as a Monitoring workbench with period selector, policy indicators, faculty capacity cards, assignment drill-down drawer, and workload exception modal.
   - Implement `SCR-TCH-04` (`/teaching/my-availability`) as a Planning workbench with weekly time grid and temporary unavailability windows modal.

9. **Two-Phase Demo Data Seeding & Purging Support:**
   - Update `DemoDataOrchestrator` to introduce `ITeachingAssignmentsDemoDataSeeder` as an explicit second-phase seeder running after Academics Structure and Students.
   - Bind real created Learning Units (e.g., Matematika SD) and Cohorts (e.g., Kelas 4A) to Hagios faculty personas: (1) Budi Hartono (Math Primary + Homeroom Advisor + Medical Leave), (2) Siti Nurhaliza (Co-teaching English + Emergency Substitute for Budi), (3) John Miller (Visiting Lecturer + Workload Exception), (4) Hendra Setiawan (Ineligible Candidate check).
   - Update `ITeachingDemoDataPurger` to clean up assignment and workload tables in reverse dependency order.

---

# 2. Current State

## Existing Behavior

1. **Database Schema:**
   - The isolated schema `teaching` has been provisioned by Stage 1 migration `20261110_0501_Teaching_TeacherProfile_Tables.sql`.
   - The 10 Stage 1 tables are active: `teaching_roles`, `teacher_profiles`, `capability_types`, `teaching_capabilities`, `qualification_types`, `qualification_evidence`, `teaching_authorizations`, `authorization_evidence`, `supervision_requirements`, and `teaching_record_corrections`.
   - The remaining 14 tables for Module 2 specified in [teaching-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/teaching-entities-tables.md) lines 365–910 — `teaching_demands`, `teacher_eligibility_evaluations`, `assignment_roles`, `teaching_assignments`, `capacity_conflicts`, `workload_policies`, `teacher_availabilities`, `teacher_unavailability_windows`, `workload_commitments`, `workload_exceptions`, `teaching_substitutions`, `teaching_reassignments`, `assignment_completions`, and `teaching_history_entries` — are **not yet provisioned**.
   - Note: Table 2.15 (`teaching_record_corrections`) was already provisioned in Stage 1 and is ready for use.

2. **Backend Domain & Services:**
   - `src/backend/Elsasa.Sis.Teaching` exists and is registered in `Elsasa.Sis.sln` and `Elsasa.Sis.Api`.
   - Vertical slices exist for `TeacherProfiles`, `TeachingCapabilities`, `QualificationEvidence`, `TeachingAuthorizations`, and `RecordCorrections`.
   - No features exist for `TeachingDemands`, `TeacherEligibility`, `TeachingAssignments`, `Workload`, `Substitutions`, `Reassignments`, `Completions`, or `TeachingHistory`.
   - In `Elsasa.Sis.Contracts`, `ITeachingModule` exposes `GetTeacherProfileAsync`, `IsTeacherAuthorizedAsync`, and `GetActiveTeachersAsync`. It does not yet expose assignment or workload query contracts needed by Academics (for attendance/grading authority verification) or Operations (for timetabling feasibility).

3. **Frontend Implementation:**
   - `src/frontend/src/features/teaching` contains submodule `faculty` representing `SCR-TCH-01` (`Faculty Resource Workspace` at `/teaching/faculty`).
   - No submodules or routes exist for `SCR-TCH-02` (`Teaching Assignment & Coverage Workbench`), `SCR-TCH-03` (`Faculty Workload Monitoring`), or `SCR-TCH-04` (`My Teaching Availability`).

4. **Demo Data Generator & Purger:**
   - `ITeachingDemoDataSeeder` seeds 4 Hagios faculty personas (Budi, Siti, John, Hendra) with profiles, qualifications, and authorizations.
   - In `DemoDataOrchestrator.cs`, Teaching runs at Step 2 (before Academics Structure and Students). Therefore, current Teaching seeding cannot reference learning units or student cohorts that are created later in the seed pipeline.
   - `TeachingDemoDataPurger.cs` only purges the 10 Stage 1 tables.

## Existing Constraints

1. **Domain Boundary & Zero Cross-Domain Physical Foreign Keys (ADR-001, ADR-002, ADR-010):**
   - `Elsasa.Sis.Teaching` must not have physical database foreign keys to tables in `operations`, `students`, `finance`, or `academics`.
   - All references to external entities (e.g., `academic_period_id`, `source_work_reference`, `scope_reference_id`) must be stored as opaque GUIDs / logical strings.
   - NetArchTest rules prohibit direct assembly references from `Elsasa.Sis.Teaching` to sibling domains.

2. **Separation of Staffing vs. Ownership (`teaching-domain.md`):**
   - Teaching owns teacher staffing and capacity commitment.
   - Academics owns curriculum content, course learning units, syllabus definitions, and student grade outcomes.
   - Students owns student cohort placement (including homeroom student lists).
   - Operations owns physical room allocation and timetable generation.
   - Homeroom advisory is represented in Teaching strictly as a `teaching_assignment` with `assignment_role = 'HOMEROOM_ADVISOR'`, decoupled from cohort student rosters and physical rooms.

3. **Auditable Immutability & Historical Integrity (`teaching-domain.md`, `actors.md`):**
   - Approved teaching assignments are never deleted or silently modified. Changes, suspensions, and cancellations must be recorded prospectively.
   - Temporary substitutions do not modify original assignment records.
   - Reassignments preserve predecessor links and never alter past academic or attendance records already submitted by predecessor faculty.

---

# 3. Gap Analysis

| ID | Severity | Gap | Status | Resolution |
|------|------|------|:---:|------|
| `GAP-001` | CRITICAL | **Missing Database Tables for Module 2:** 14 tables (`teaching_demands`, `teacher_eligibility_evaluations`, `assignment_roles`, `teaching_assignments`, `capacity_conflicts`, `workload_policies`, `teacher_availabilities`, `teacher_unavailability_windows`, `workload_commitments`, `workload_exceptions`, `teaching_substitutions`, `teaching_reassignments`, `assignment_completions`, `teaching_history_entries`) are unprovisioned. | **CLOSED** | Author single cohesive migration script `20261110_0502_Teaching_AssignmentWorkload_Tables.sql` idempotently provisioning all 14 tables in schema `teaching` with intra-schema foreign keys, indexes, check constraints, and default values. |
| `GAP-002` | CRITICAL | **Missing Backend Vertical Feature Slices:** `Elsasa.Sis.Teaching` has no command/query handlers or endpoints for Demands, Eligibility, Assignments, Workload, Substitutions, Reassignments, Completions, or History. | **CLOSED** | Scaffold 7 vertical feature slices under `Elsasa.Sis.Teaching/Features/`: `TeachingDemands`, `TeacherEligibility`, `TeachingAssignments`, `Workload`, `Substitutions`, `AssignmentCompletions`, `TeachingHistory`. Map endpoints in `TeachingEndpoints.cs`. |
| `GAP-003` | MAJOR | **Teacher Eligibility Evaluation Mechanism (`TCH-WF-06`, `UC-TCH-05`):** Need an explainable, automated rule evaluation engine to evaluate candidates against demand requirements without brittle manual checklists. | **CLOSED** | Adopted **Automated Rule Evaluation with Explainable Persistence**: backend evaluates the 5 policy criteria in memory and persists an explainable `teacher_eligibility_evaluations` record. Coordinator reviews the breakdown and can attach remarks. |
| `GAP-004` | MAJOR | **Workload Calculation & Threshold Synchronization (`TCH-WF-10`, `UC-TCH-07`):** Need reliable tracking of planned, committed, and remaining capacity without stale data or performance bottlenecks. | **CLOSED** | Adopted **Transactional Snapshot Synchronization with On-Demand Recalculation**: update `workload_commitments` atomically on assignment/substitution/exception state changes. Provide coordinator endpoint for full period recalculation. Status strictly reflects policy thresholds (`normal`, `approaching_limit`, `breached`, `overloaded_with_exception`). |
| `GAP-005` | MAJOR | **Emergency Substitution Governance (`TCH-WF-09`, `UC-TCH-06`):** Balance rapid morning classroom coverage with institutional governance without requiring principal sign-off for routine substitutions. | **CLOSED** | Adopted **Pre-Authorized Pool Delegation (Resolution 3.8)**: Teaching Coordinator has unilateral authority to commit emergency substitutes from the pre-authorized pool within workload caps. Exceptions (unauthorized substitute or workload ceiling breach) raise blocking `capacity_conflicts` requiring Teaching Authority approval. |
| `GAP-006` | MAJOR | **Missing Frontend Screens (`SCR-TCH-02`, `SCR-TCH-03`, `SCR-TCH-04`):** Teaching Coordinators, Authorities, and Teachers lack web interfaces for assignments, workload monitoring, and availability declarations. | **CLOSED** | Adopted **Cohesive Sub-Features under `features/teaching`**: implement `features/teaching/assignments` (SCR-TCH-02 at `/teaching/assignments`), `features/teaching/workload` (SCR-TCH-03 at `/teaching/workload`), and `features/teaching/availability` (SCR-TCH-04 at `/teaching/my-availability`). |
| `GAP-007` | MAJOR | **Cross-Domain Contract Alignment (`ITeachingModule`):** Downstream domains (Academics for attendance/grading verification, Operations for scheduling feasibility) lack contracts to query active assignments and teacher availability. | **CLOSED** | Extend `ITeachingModule` in `Elsasa.Sis.Contracts` with: `GetApprovedAssignmentForLearningUnitAsync`, `GetTeacherActiveAssignmentsAsync`, `GetTeacherAvailabilityAsync`, and `IsTeacherAssignedToScopeAsync`. |
| `GAP-008` | MAJOR | **Demo Data Generator Orchestration Dependency:** Demo seeder needs to bind teaching assignments to real learning units and student cohorts, but Teaching currently seeds before Academics and Students. | **CLOSED** | Adopted **Explicit Two-Phase Seeding in Orchestrator**: retain Stage 1 faculty seeding at Step 2, and add `ITeachingAssignmentsDemoDataSeeder` at Step 6/8 running after Academics Structure and Students, consuming real created entity IDs for end-to-end relational demonstration. Update purger. |

---

# 4. Open Questions

| ID | Question | Impact | Status | Decision Summary |
|------|------|------|:---:|------|
| `OQ-001` | How should the database schema and backend architecture be structured for Stage 2 Teaching Assignment & Workload? | Dictates migration script structure, schema stability, and code organization. | **CLOSED** | **Single Stage-2 Schema Migration & Modular Vertical Slices:** Provision all 14 remaining tables in a single migration script `20261110_0502_Teaching_AssignmentWorkload_Tables.sql` and organize backend into vertical slices under `Elsasa.Sis.Teaching/Features/`. |
| `OQ-002` | How should the Demo Data Generator handle the dependency where Teaching Demands & Assignments link to Academics Learning Units and Student Cohorts? | Dictates demo data integrity, execution order, and realism. | **CLOSED** | **Explicit Two-Phase Seeding in Orchestrator:** Retain Stage 1 faculty seeding at Step 2, and introduce `ITeachingAssignmentsDemoDataSeeder` running after Academics Structure and Students, consuming real created Learning Units and Cohorts to seed Demands, Assignments, Availabilities, Workloads, and Substitutions with full relational integrity. |
| `OQ-003` | How should the Teacher Eligibility Evaluation Engine (`TCH-WF-06`, `UC-TCH-05`) evaluate candidate faculty against teaching demands? | Dictates coordinator ergonomics, policy automation, and auditability. | **CLOSED** | **Automated Rule Evaluation with Explainable Persistence:** The backend evaluates the 5 policy checks automatically against current profile standing, authorizations, availability, and workload, generating an explainable determination and status. Coordinator reviews and can attach remarks. |
| `OQ-004` | How should teacher workload commitments and capacity threshold states be calculated and synchronized? | Dictates capacity data freshness, calculation performance, and monitoring consistency. | **CLOSED** | **Transactional Snapshot Synchronization with On-Demand Recalculation:** Update `workload_commitments` atomically whenever an assignment is proposed, approved, suspended, cancelled, completed, or an exception is granted. Expose a period recalculation endpoint for coordinators. |
| `OQ-005` | How should Emergency Substitution and Permanent Reassignment approvals be governed (`TCH-WF-09`, `UC-TCH-06`, `actors.md` Resolution 3.8)? | Dictates coordinator autonomy during urgent absences vs. institutional governance. | **CLOSED** | **Pre-Authorized Pool Delegation with Exception Escalation (Resolution 3.8):** Teaching Coordinator has unilateral authority to commit emergency substitutes holding active matching authorizations within workload caps. Capacity breaches or unauthorized substitutes raise blocking `capacity_conflicts` requiring Teaching Authority approval. Permanent reassignments always require Teaching Authority approval. |
| `OQ-006` | How should the Frontend Feature Architecture and Routes for `SCR-TCH-02`, `SCR-TCH-03`, and `SCR-TCH-04` be structured? | Dictates user experience, URL design, and code maintainability. | **CLOSED** | **Cohesive Sub-Features under `features/teaching`:** Maintain `features/teaching/faculty` (SCR-TCH-01), and add `features/teaching/assignments` (SCR-TCH-02 at `/teaching/assignments`), `features/teaching/workload` (SCR-TCH-03 at `/teaching/workload`), and `features/teaching/availability` (SCR-TCH-04 at `/teaching/my-availability`). |
| `OQ-007` | Which Demo Data scenarios should the updated Data Generator seed to showcase Stage 2 Assignment, Workload, Homeroom, and Substitution? | Dictates presentation quality, persona realism, and verification of Criteria 7 & 8. | **CLOSED** | **Comprehensive Persona-Driven Demo Journey:** Seed reference assignment roles and workload policy, then bind Stage 1 personas to real seeded curriculum and cohorts: (1) Budi Hartono (Math Primary + Homeroom 4A + Medical Leave), (2) Siti Nurhaliza (Co-teaching English + Emergency Substitute for Budi), (3) John Miller (Visiting Lecturer + Workload Exception), (4) Hendra Setiawan (Ineligible candidate rejection demo), plus a completed assignment showing capacity release. |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| `ASM-001` | The database engine is SQL Server 2022+ with isolated schemas managed via DbUp migrations (`Elsasa.Sis.Database`). |
| `ASM-002` | `Elsasa.Sis.Teaching` continues to follow the exact architectural pattern established in Stage 1: Minimal APIs, Dapper repositories, MediatR handlers, and FluentValidation (ADR-004, ADR-005, ADR-006). |
| `ASM-003` | Opaque references (`source_work_reference`, `scope_reference_id`, `academic_period_id`) maintain zero physical cross-domain foreign keys, preserving domain independence (ADR-001, ADR-002). |
| `ASM-004` | Homeroom advisory is structurally a standard `teaching_assignment` with role `HOMEROOM_ADVISOR`, leaving student placement to Students domain and classroom allocation to Operations. |
| `ASM-005` | Pre-Authorized Pool Delegation (Resolution 3.8) allows the Teaching Coordinator to immediately commit qualified substitute teachers without awaiting Principal approval unless an exception is required. |
| `ASM-006` | Workload policies define warning and prohibited capacity thresholds in Teaching Hours; exceptions require Teaching Authority approval and cannot be self-approved by teachers. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| `RISK-001` | **Stale or Desynchronized Workload Snapshots:** Concurrent assignment changes could cause `workload_commitments` to drift from actual active assignments. | Workload monitoring in `SCR-TCH-03` displays incorrect capacity, risking unauthorized over-allocation. | Execute transactional snapshot updates within the assignment command handlers and provide an idempotent recalculation command (`POST /api/teaching/workload/recalculate`) for Teaching Coordinators. |
| `RISK-002` | **Orchestrator Two-Phase Seeding Complexity:** Introducing a second teaching seeder phase in `DemoDataOrchestrator` after Academics Structure and Students could disrupt the seeder pipeline if not guarded properly. | Demo data reset fails or leaves partial demo records. | Implement `ITeachingAssignmentsDemoDataSeeder` as an explicit step in forward seeding and expand `TeachingDemoDataPurger` to clean assignment tables before faculty tables during reverse purge. |
| `RISK-003` | **Emergency Substitution Blocking Classroom Delivery:** Requiring strict checks during morning teacher illness could delay substitute assignment if candidate evaluation is too rigid. | Classes are left unattended while coordinators navigate validation errors. | Follow Pre-Authorized Pool Delegation: immediate one-click assignment for faculty in the active authorized pool; non-blocking advisory warnings for minor scheduling preferences. |
| `RISK-004` | **Downstream Timetabling Desynchronization:** Operations might schedule sessions without checking active teacher availability or assignments. | Room/timetable collisions occur when Operations schedules a teacher during an unavailability window. | Expose targeted query methods on `ITeachingModule` (`GetTeacherAvailabilityAsync`, `GetTeacherActiveAssignmentsAsync`) so Operations can validate feasibility before committing timetable occurrences. |

---

# 7. Recommendations

## Recommended Approach: Pragmatic, Minimalist, Explicit Architecture

The implementation strictly honors the three guiding values:

### 1. Pragmatic Architecture
- **Single Migration Script:** Avoid micro-migrations; author a single, robust migration script `20261110_0502_Teaching_AssignmentWorkload_Tables.sql` provisioning all 14 tables with explicit indexes, unique invariants, and intra-schema foreign keys.
- **Explainable Rule Engine:** Automate candidate eligibility evaluation in memory with clear human-readable explanations, avoiding complex dynamic rule engines while eliminating tedious manual checklists.
- **Pre-Authorized Pool Delegation:** Empower the Teaching Coordinator to resolve emergency morning absences immediately from the verified faculty pool without unnecessary bureaucratic escalation.

### 2. Minimalist Scope
- **Leverage Existing Assets:** Reuse the existing `teaching_record_corrections` table (table 2.15) already provisioned in Stage 1 for assignment record auditing, avoiding duplicate auditing tables.
- **Cohesive Submodules:** Group related frontend capabilities under `features/teaching/` (`assignments`, `workload`, `availability`) rather than creating sprawling, separate top-level features.
- **Opaque String/UUID Cross-Domain References:** Keep cross-domain links minimal and strictly decoupled (source work reference, scope reference, period ID), preventing database schema entanglement.

### 3. Explicit Decisions & State
- **Explicit Lifecycle States:** Every operational entity (`teaching_demands`, `teaching_assignments`, `teaching_substitutions`, `workload_commitments`, `workload_exceptions`) has strictly enumerated lifecycle states with explicit transition commands.
- **No Silent Overwrites:** Historical teaching records and predecessor assignment links are preserved across substitutions, reassignments, and completions.
- **Two-Phase Demo Data Seeding:** Make the cross-domain demo data dependency explicit in `DemoDataOrchestrator`: Phase 1 seeds faculty profiles and credentials; Phase 2 seeds assignments and workload binding real curriculum units and cohorts.

---

# 8. Gap Closure

## GAP-001: Missing Database Tables for Module 2
### Decision
Author DbUp migration script `20261110_0502_Teaching_AssignmentWorkload_Tables.sql` idempotently provisioning the 14 Module 2 tables: `teaching_demands`, `teacher_eligibility_evaluations`, `assignment_roles`, `teaching_assignments`, `capacity_conflicts`, `workload_policies`, `teacher_availabilities`, `teacher_unavailability_windows`, `workload_commitments`, `workload_exceptions`, `teaching_substitutions`, `teaching_reassignments`, `assignment_completions`, and `teaching_history_entries`.
### Rationale
Consolidating Module 2 tables into a single migration ensures atomicity, avoids migration fragmentation, and matches the established database development pattern.
### Impact
Database schema is complete for all Stage 2 Assignment & Workload submodules.
### Architecture Impact
Completes persistence model for Teaching Stage 2 without introducing physical cross-domain foreign keys.
### Resolved By
Analyst / Architect pair
### Resolved Date
2026-11-10

---

## GAP-002: Missing Backend Vertical Feature Slices
### Decision
Scaffold 7 modular vertical feature slices under `src/backend/Elsasa.Sis.Teaching/Features/`: `TeachingDemands`, `TeacherEligibility`, `TeachingAssignments`, `Workload`, `Substitutions`, `AssignmentCompletions`, and `TeachingHistory`. Map endpoints in `TeachingEndpoints.cs`.
### Rationale
Follows the established Minimal API + MediatR vertical slice pattern used in `Academics`, `Students`, and `Finance`.
### Impact
Provides complete, testable API endpoints for all Stage 2 operations.
### Architecture Impact
Maintains strict internal modularity and high cohesion within `Elsasa.Sis.Teaching`.
### Resolved By
Analyst / Architect pair
### Resolved Date
2026-11-10

---

## GAP-003: Teacher Eligibility Evaluation Mechanism
### Decision
Implement an automated rule evaluation engine that checks profile standing, matching authorization, verified qualification, availability, and capacity, generating an explainable evaluation record in `teacher_eligibility_evaluations`.
### Rationale
Automates policy checks while providing full explainability and allowing coordinators to attach remarks before confirming candidate selection.
### Impact
Prevents arbitrary staffing decisions and satisfies institutional audit requirements.
### Architecture Impact
Establishes an explainable policy evaluation pattern within the Teaching domain.
### Resolved By
Analyst / Architect pair
### Resolved Date
2026-11-10

---

## GAP-004: Workload Calculation & Threshold Synchronization
### Decision
Update `workload_commitments` atomically whenever an assignment is proposed, approved, suspended, cancelled, completed, or an exception is granted. Expose a period recalculation endpoint for coordinators. Status strictly reflects policy thresholds (`normal`, `approaching_limit`, `breached`, `overloaded_with_exception`).
### Rationale
Guarantees immediate capacity freshness during staffing sessions and enables fast, indexed queries in `SCR-TCH-03`.
### Impact
Workload monitoring is real-time, responsive, and reproducible.
### Architecture Impact
Establishes transactional snapshot synchronization with an on-demand reconciliation endpoint.
### Resolved By
Analyst / Architect pair
### Resolved Date
2026-11-10

---

## GAP-005: Emergency Substitution Governance
### Decision
Adopt Pre-Authorized Pool Delegation (Resolution 3.8): Teaching Coordinator has unilateral authority to commit emergency substitutes holding active matching authorizations within workload caps. Exceptions (unauthorized substitute or workload ceiling breach) raise blocking `capacity_conflicts` requiring Teaching Authority approval. Permanent reassignments always require Teaching Authority approval.
### Rationale
Eliminates administrative bottlenecks during urgent morning teacher absences while strictly enforcing institutional governance for exceptions.
### Impact
Coordinators can resolve emergency coverage in seconds from pre-authorized faculty pools.
### Architecture Impact
Directly operationalizes Resolution 3.8 from `actors.md` within the domain workflow.
### Resolved By
Analyst / Architect pair
### Resolved Date
2026-11-10

---

## GAP-006: Missing Frontend Screens
### Decision
Implement cohesive sub-features under `src/frontend/src/features/teaching/`:
- `features/teaching/assignments/` (`SCR-TCH-02` at `/teaching/assignments`)
- `features/teaching/workload/` (`SCR-TCH-03` at `/teaching/workload`)
- `features/teaching/availability/` (`SCR-TCH-04` at `/teaching/my-availability`)
### Rationale
Maintains clean feature-sliced architecture, clear role separation, and high component reusability.
### Impact
Provides intuitive, role-tailored UIs for Coordinators, Authorities, and Teachers.
### Architecture Impact
Preserves frontend feature encapsulation under `features/teaching/`.
### Resolved By
Analyst / Architect pair
### Resolved Date
2026-11-10

---

## GAP-007: Cross-Domain Contract Alignment
### Decision
Extend `ITeachingModule` in `Elsasa.Sis.Contracts` with targeted inquiry methods: `GetApprovedAssignmentForLearningUnitAsync`, `GetTeacherActiveAssignmentsAsync`, `GetTeacherAvailabilityAsync`, and `IsTeacherAssignedToScopeAsync`.
### Rationale
Provides clean, asynchronous contract queries for Academics (attendance/grading verification) and Operations (scheduling feasibility) without assembly coupling.
### Impact
Downstream domains can verify teacher staffing authority reliably.
### Architecture Impact
Maintains strict ADR-001/ADR-002 cross-domain contract boundaries.
### Resolved By
Analyst / Architect pair
### Resolved Date
2026-11-10

---

## GAP-008: Demo Data Generator Orchestration Dependency
### Decision
Implement `ITeachingAssignmentsDemoDataSeeder` as an explicit second-phase seeder in `DemoDataOrchestrator` running after Academics Structure and Students. Seed realistic scenarios binding Budi Hartono, Siti Nurhaliza, and John Miller to real created curriculum units and cohorts. Update `ITeachingDemoDataPurger` to clean assignment tables in reverse order.
### Rationale
Ensures full relational integrity and authentic user demonstration across all domains without artificial or dangling IDs.
### Impact
Demo data reset produces a rich, interconnected, end-to-end demonstration verifying Criteria 7 & 8.
### Architecture Impact
Establishes two-phase domain seeding in `DemoDataOrchestrator` while honoring topological dependencies.
### Resolved By
Analyst / Architect pair
### Resolved Date
2026-11-10

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Implementation of Stage 2 Teaching Assignment & Workload requires formal technical target-state architecture definition because it entails:
1. **New Persistence Schema:** Provisioning 14 authoritative tables in schema `teaching` with intra-schema foreign keys, unique invariants, and performance indexes.
2. **Cross-Domain Integration Boundaries:** Managing opaque references to Academics (learning units) and Students (cohorts) while maintaining zero physical foreign keys.
3. **Complex Domain Engines:** Establishing the explainable 5-factor Teacher Eligibility Evaluation Engine and the transactional Workload Commitment Synchronization Engine.
4. **Governance Delegations:** Implementing Pre-Authorized Pool Delegation (Resolution 3.8) and formal workload exception approval workflows.
5. **Orchestrator Pipeline Evolution:** Updating `DemoDataOrchestrator` to support two-phase topological seeding.
6. **Frontend Workspace Delivery:** Implementing 3 major screens (`SCR-TCH-02`, `SCR-TCH-03`, `SCR-TCH-04`) with multi-tab worklists, monitoring gauges, and availability grids.

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

All architectural ambiguities, database schema specifications, governance policies, frontend structures, and demo data generator lifecycles have been resolved through the interactive grill-me interview. The system is ready for formal Target Architecture finalization and executable Implementation Planning.

---

# 11. References

Referenced artifacts:
- **DOMAIN:** [teaching-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/teaching-domain.md)
- **ENTITIES:** [teaching-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/teaching-entities.md), [teaching-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/teaching-entities-tables.md)
- **ISSUE:** [IS015 - TCH-ASSIGNMENT-WORKLOAD-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS015%20-%20TCH-ASSIGNMENT-WORKLOAD-ISSUE.md)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md)

Referenced codebase locations:
- [Elsasa.Sis.Teaching](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching)
- [Elsasa.Sis.Database](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database)
- [DemoDataOrchestrator.cs](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Api/Features/DemoData/DemoDataOrchestrator.cs)
- [TeachingDemoDataSeeder.cs](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/DemoData/TeachingDemoDataSeeder.cs)
- [src/frontend/src/features/teaching](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching)
