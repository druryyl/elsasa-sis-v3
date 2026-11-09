# ISSUE

## Metadata

ID: CR-ACD-STUDENT-LEARNING-PLAN
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-2 Academic Student Learning Plan Full Scope

## Source

Reported By: Drury
Reported Date: 2026-11-07

## Description

Implement the full scope of Stage 2 "Student Learning Plan" under the Academic domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses three cohesive submodules:

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

### Downstream Consumer Alignment

- Provide authoritative SLP context to `Participation & Roster Workbench` (`SCR-ACD-05`, `UC-ACD-10`) so registered academic participations can reference the governing SLP (`academic_participations.source = 'learning_plan'`, `student_learning_plan_id`).
- Surface governing learning plan context and adjustments in `Academic Records Workbench` (`SCR-ACD-07`) during evaluation and record review.

### Scope Boundaries & Explicit Deferrals

The following capabilities belong to companion modules, subsequent stages, or separate domains and are explicitly out of scope for this issue:

- **Administrative Enrollment and Placement:** An SLP does not alter the student's official administrative placement (campus, grade level, homeroom cohort) or administrative lifecycle status (`SCR-STU-01`, `SCR-STU-02`, `SCR-STU-03`), which remain strictly owned by the Students domain (`Placement & Status`).
- **Foundational Curriculum & Learning Units:** Definition, syllabus configuration, and requirement versioning for standard learning units belong to `Learning Structure & Requirements` (`IS008`).
- **Instructional Delivery & Attendance:** Conducting classes, tracking attendance, and logging daily coursework belong to `Participation & Learning Activities` (`IS009`).
- **Assessment Scoring & Report Cards:** Evaluating assessments, scoring rubrics, grade approval, and report cards belong to `Assessment & Results` (`IS010`).
- **Academic Standing & Progression:** Determining academic standing, completion tracking, and promotion recommendations belong to `Standing & Progression` (`IS012`).
- **Non-Academic Interventions & Pastoral Support:** Pastoral care, wellbeing referrals, counselling cases, and behavioural discipline belong strictly to the Development domain (`SCR-DEV-03`, `SCR-DEV-02`). Development recommendations may provide supporting context for an SLP referral but do not approve or govern academic plans.
- **Financial Adjustments & Scholarships:** Fee waivers, tuition discounts, and scholarship stacking belong exclusively to the Finance domain (`Benefits`, `Obligations & Billing`).

## Desired Outcome

1. Academic Coordinators can formulate, edit, and revise individualized Student Learning Plans (SLP) for exceptional learners, specifying constituent learning units and required sequence in `SCR-ACD-04` (`Learning Plan Workspace`, `UC-ACD-09`, `ACD-WF-02`).
2. Academic Coordinators can configure academic accommodations, course substitutions, and exemptions with clear rationale and validity dates within the SLP (`SCR-ACD-04`, `UC-ACD-09`).
3. Academic Authorities can review, approve, or reject initial SLPs and revised plan versions, maintaining an auditable approval chain in `SCR-ACD-04` (`UC-ACD-09`).
4. System enforces that a student has at most one active SLP per academic period, with prior approved versions preserved as `revised` or `archived`.
5. Approved SLP records are available to govern academic participation registration (`source = 'learning_plan'`) in `SCR-ACD-05` and learning progress evaluation in `SCR-ACD-07`.
6. Fulfills SIS Implementation Roadmap Stage 2 Completion Criterion 12: "A student learning plan is created, approved, and governs an exceptional student's learning."

## Current Situation

1. **Database Schema Status:**
   - The tables for Submodule 5 specified in `docs/entities/academic-entities-tables.md` (lines 960–1090) — `student_learning_plans`, `slp_learning_units`, `academic_plan_adjustments`, and `slp_approvals` — are not yet provisioned in the database schema or EF Core migrations.
2. **Backend Status:**
   - In `src/backend/Elsasa.Sis.Academics`, there are currently no domain entities, entity configurations, repository contracts/implementations, or MediatR commands/queries for Student Learning Plans, SLP learning units, academic plan adjustments, or SLP approvals.
3. **Frontend Status:**
   - In `src/frontend/src/features/academics`, the feature module and UI component for `SCR-ACD-04` (`Learning Plan Workspace`) does not yet exist.
4. **Upstream Prerequisites Status:**
   - Operations Calendar & Periods (`IS001`, `IS003`) is active, providing operational academic period context.
   - Student Enrollment & Identity (`IS004`) is active, providing enrolled student records.
   - Learning Structure & Requirements (`IS008`) is implemented, providing approved learning units.
   - Participation & Learning Activities (`IS009`), Assessment & Results (`IS010`), and Standing & Progression (`IS012`) provide downstream academic integration context.

## Evidence

- `docs/sub-modul-and-development-phases.md`: Line 34 (Module Decomposition), Line 78 (Stage 2 Roadmap Scope), Line 138 (Criterion 12).
- `docs/entities/academic-entities-tables.md`: Lines 960–1090 (Submodule 5 Data Model Specification: `student_learning_plans`, `slp_learning_units`, `academic_plan_adjustments`, `slp_approvals`).
- `docs/operational-workflow-discovery.md`: Line 87 (`ACD-WF-02` Establish Student Learning Plan).
- `docs/use-cases.md`: Lines 411–420 (`UC-ACD-09` Establish Student Learning Plan).
- `docs/screen-inventory.md`: Lines 663–685 (`SCR-ACD-04` Learning Plan Workspace).

## Notes

- Intake Classification: `CHANGE-REQUEST` (new capability implementation under Stage 2).
- Working Knowledge artifact adhering to `ics-issue-creation` standard.
- Workflow Routing: ISSUE (CHANGE-REQUEST) -> `ica-analyst`: Discovery, then `FEASIBILITY-ASSESSMENT` (`IS013 - ACD-STUDENT-LEARNING-PLAN-FEASIBILITY-ASSESSMENT.md`).
