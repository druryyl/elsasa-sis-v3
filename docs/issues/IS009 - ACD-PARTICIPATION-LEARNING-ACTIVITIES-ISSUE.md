# ISSUE

## Metadata

ID: CR-ACD-PARTICIPATION-LEARNING-ACTIVITIES
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-2 Academic Participation & Learning Activities Full Scope

## Source

Reported By: Drury
Reported Date: 2026-11-05

## Description

Implement the full scope of Stage 2 "Participation & Learning Activities" under the Academic domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses four cohesive submodules:
1. **Academic Participation (`ACD-WF-03`, `ACD-WF-11`, `UC-ACD-10`, `UC-ACD-12`, `SCR-ACD-05`):**
   - Establish and maintain the formal relationship between eligible students and approved learning units (`academic_participations`).
   - Track participation lifecycle statuses (`in_progress`, `paused`, `withdrawn`, `completed`) with start, end, and completion dates.
   - Support participation types (`standard`, `repeat`, `transferred_in`, `recognized_prior`) and attempt counters for retakes.
   - Track participation origin (`assigned`, `student_selected`, `learning_plan`) and optional cohort partitioning via `section_label`.
   - Record credit recognition for transferred-in or prior recognized learning (`source_institution`, `external_grade_or_credit`, `recognition_basis`, `approved_by`).
   - Enforce the boundary rule that administrative enrollment does not automatically create academic participation; learning participation must be explicitly established within Academics.
2. **Learning Activity Conduct (`ACD-WF-04`, `UC-ACD-02`, `SCR-ACD-01`):**
   - Manage instructional occurrences and learning containers (`learning_activities`) tied to active learning units.
   - Support activity types (`lesson`, `tutorial`, `laboratory`, `independent_work`, `project`, `discussion`, `practice`, `examination`, `remediation`, `homeroom_session`, `other`).
   - Track scheduling context (`activity_date`, `start_time`, `end_time`), cohort section (`section_label`), instructional delivery notes, and status (`planned`, `conducted`, `cancelled`).
   - Link to authorized teacher assignments (external reference `teacher_assignment_id`, supporting provisional assignment references during concurrent Stage 2 rollout).
   - Support statutory daily homeroom roll-call sessions (`activity_type = 'homeroom_session'`, per GAP-03) to generate statutory daily attendance totals without conflating with physical gate presence.
3. **Student Attendance & Audit Corrections (`ACD-WF-05`, `ACD-WF-18`, `UC-ACD-03`, `UC-ACD-15`, `SCR-ACD-01`):**
   - Record session attendance for required activities (`attendance_records`) across standard statuses: `present`, `absent`, `late`, `excused_absence`, `partial`, capturing recorder identity, timestamp, and supporting evidence.
   - Enforce strict audit trails for attendance corrections (`attendance_corrections`), recording previous status, new status, mandatory justification reason, authorizer identity, and timestamp, guaranteeing that historical attendance records are never silently overwritten.
4. **Assignment & Submission Management (`ACD-WF-06`, `UC-ACD-04`, `UC-ACD-01`, `SCR-ACD-02`, `SCR-SELF-03`):**
   - Publish and manage academic work tasks (`assignments`) strictly originating from a learning activity (`Learning Activity → Assignment → Submission`), defining title, instructions, release date, due date, and lifecycle status (`draft`, `released`, `closed`).
   - Record and track student submission responses (`submissions`) with delivery timestamp, submission reference/attachment, resubmission tracking (`is_resubmission`, `previous_submission_id`), and submission statuses (`submitted`, `late`, `missing`, `excused`, `exempted`).
   - Maintain strict separation between submission status and academic scoring/grading (evaluations belong to Assessment & Results).

Scope Boundaries & Deferrals:
The following Academic, Teaching, and Operational capabilities are explicitly out of scope for this issue and belong to companion or subsequent Stage 2 modules:
- Curriculum framework and learning unit configuration (`learning_structures`, `learning_units`, `academic_requirements`) are owned by `Learning Structure & Requirements` (`IS008`).
- Formal assessment planning, rubrics, scoring/grading, result moderation, result approval, report card generation, and transcript certification (`assessment_plans`, `assessment_evidence`, `assessment_evaluations`, `academic_results`, `report_cards`, `transcripts`) belong to `Assessment & Results`.
- Individualized Student Learning Plans (SLP drafting, accommodation rules, approval) belong to `Student Learning Plan`.
- Academic standing determination, progression recommendations (promotion/retention), and academic honors belong to `Standing & Progression`.
- Formal teacher profiling, credential verification, and official teacher assignment approval belong to the `Teaching` domain (`Teacher Profile & Authorization`, `Assignment & Workload`).
- Operational timetable creation and room allocation belong to `Scheduling & Rooms` (`Operational` domain).

## Desired Outcome

1. Academic Coordinators can register active students into approved learning units, manage roster cohorts/sections, and conclude learning unit participation in `SCR-ACD-05` (`Participation & Roster Workbench`, `UC-ACD-10`, `UC-ACD-12`, `ACD-WF-03`, `ACD-WF-11`).
2. Teachers can manage daily instructional activities, record curriculum milestones, log session delivery, take roll call, and submit auditable attendance corrections in `SCR-ACD-01` (`Class Session Workbench`, `UC-ACD-02`, `UC-ACD-03`, `ACD-WF-04`, `ACD-WF-05`).
3. Teachers can create and publish coursework assignments originating from learning activities in `SCR-ACD-02` (`Coursework & Assessment Workbench`, `UC-ACD-04`, `ACD-WF-06`).
4. Students can view assigned coursework, track deadlines, and upload submissions in `SCR-SELF-03` (`Student Learning Workspace`, `UC-ACD-01`, `ACD-WF-06`).
5. Downstream Stage 2 modules can consume participation, learning activity, attendance, and submission evidence:
   - `Assessment & Results` can attach assessment evidence to learning activities and evaluate submitted coursework.
   - `Standing & Progression` can verify completion criteria and attendance thresholds.
   - `Teaching Assignment & Workload` can reconcile assigned teaching delivery against planned learning activities.
6. Meets Stage 2 Completion Criteria:
   - **Criterion 2:** "An active student has academic participation in at least one learning unit."
   - **Criterion 3:** "A learning activity has been conducted with attendance recorded."
   - **Criterion 4:** "An assignment has been issued, a submission received..."

## Current Situation

1. Schema status: The database currently provisions tables for Operations, Students, and Finance domains (Stage 1), and `IS008` defines the learning structure tables. The tables for Submodule 2 (`academic_participations`, `learning_activities`, `attendance_records`, `attendance_corrections`, `assignments`, `submissions`) specified in `docs/entities/academic-entities-tables.md` (lines 150–380) are not yet provisioned.
2. Backend status: In `src/backend`, there are no domain models, entity configurations, repositories, or MediatR commands/queries for academic participation, learning activities, session attendance, or coursework submissions.
3. Frontend status: In `src/frontend`, `src/features/` does not yet contain feature implementations for `SCR-ACD-05` (`Participation & Roster Workbench`), `SCR-ACD-01` (`Class Session Workbench`), `SCR-ACD-02` (`Coursework Workbench`), or `SCR-SELF-03` (`Student Learning Workspace`).
4. Upstream prerequisites:
   - Operational Calendar & Periods (`IS001`, `IS003`) provides active academic period context.
   - Student Enrollment & Identity (`IS004`) provides eligible enrolled student identity records.
   - Learning Structure & Requirements (`IS008`) provides approved curriculum frameworks and learning units.

## Evidence

- Roadmap Specification: `docs/sub-modul-and-development-phases.md` (Stage 2, Module: Participation & Learning Activities — full scope; Section D Stage 2 Criteria 2, 3, 4; Section E Cross-Domain Dependencies).
- Academic Domain Blueprint: `docs/domains/academic-domain.md` (Section: Academic Participation; Section: Learning Activities; Section: Student Attendance; Section: Assignments and Student Work; Preferred hierarchy: `Learning Structure → Learning Unit → Learning Activity → Assignment → Submission`).
- Academic Entities Specification: `docs/entities/academic-entities.md` (Submodule 2 — Participation & Learning Activities: `Academic Participation`, `Learning Activity`, `Attendance Record`, `Attendance Correction`, `Assignment`, `Submission`).
- Academic Database Tables Specification: `docs/entities/academic-entities-tables.md` (Submodule 2: `academic_participations`, `learning_activities`, `attendance_records`, `attendance_corrections`, `assignments`, `submissions`, lines 150–380).
- Operational Workflow Discovery: `docs/operational-workflow-discovery.md` (`ACD-WF-03`, `ACD-WF-04`, `ACD-WF-05`, `ACD-WF-06`, `ACD-WF-11`, `ACD-WF-18`; Section B.2; Section E.3 Resolution of GAP-03: School Attendance Architecture).
- Use Cases Specification: `docs/use-cases.md` (`UC-ACD-01`, `UC-ACD-02`, `UC-ACD-03`, `UC-ACD-04`, `UC-ACD-10`, `UC-ACD-12`, `UC-ACD-15`).
- Screen Inventory: `docs/screen-inventory.md` (`SCR-ACD-01`, `SCR-ACD-02`, `SCR-ACD-05`, `SCR-SELF-03`).
- Existing Baseline Code: `src/backend/Elsasa.Sis.sln`, `src/frontend/src/features/`.

## Notes

- This artifact serves as the formal change intake. It defines WHAT capability is requested and the operational outcomes expected, maintaining solution neutrality without prescribing technical architecture, database migration designs, API schemas, or UI component implementations.
- Scope boundary discipline: Student participation registration, instructional activity conduct, session attendance with correction audit trails, and assignment/submission workflows belong to this issue. Curriculum structures (`IS008`), formal evaluations and grading (`Assessment & Results`), individualized SLPs (`Student Learning Plan`), and official teacher authorization (`Teaching`) belong to distinct modules.
- Technical feasibility assessment, domain modeling, feature decomposition, and architecture updates will be performed in downstream SDLC stages.
