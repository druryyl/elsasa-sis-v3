# ISSUE

## Metadata

ID: CR-TCH-ASSIGNMENT-WORKLOAD
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-2 Teaching Assignment & Workload Full Scope

## Source

Reported By: Drury
Reported Date: 2026-11-10

## Description

Implement the full scope of Stage 2 "Assignment & Workload" under the Teaching domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses seven cohesive submodules managing teaching demand intake, candidate eligibility evaluation, binding assignment approvals (including homeroom advisory), teacher capacity and workload governance, substitution and reassignment, assignment conclusion, and teaching history preservation:

1. **Teaching Demand Intake (`TCH-WF-05`, `UC-TCH-04`, `SCR-TCH-02`):**
   - Receive, inspect, and clarify formal requests for teaching capacity originating from owning domains (`teaching_demands`), primarily Academics (learning units/courses), Students (homeroom cohorts), and Development (extracurricular activities).
   - Capture demand attributes: traceable demand code (`demand_code`), source domain, external work reference (`source_work_reference`) and human-readable label (`source_work_label`), target academic period, required capability code, requested assignment role, estimated workload effort and unit, assessment authority mandate, requested date range, campus location, and required teacher headcount.
   - Support demand lifecycle management: `received`, `accepted_for_staffing`, `returned_for_clarification`, `changed`, `cancelled`, `declined`, and `staffed`, recording decline reasons and clarification feedback.
   - Maintain strict separation of concerns: Teaching staffs the demand without redefining the underlying academic curriculum, student cohort, or operational timetable.

2. **Teacher Eligibility Evaluation (`TCH-WF-06`, `UC-TCH-05`, `SCR-TCH-02`):**
   - Provide an explainable, policy-driven evaluation of candidate teachers against specific teaching demand requirements (`teacher_eligibility_evaluations`).
   - Evaluate multi-factor eligibility criteria: active teacher profile standing, matching active teaching authorization, verified qualification evidence requirements, scheduling availability, and workload capacity thresholds.
   - Persist evaluation outcomes: binary eligibility determination (`is_eligible`), detailed status (`eligible`, `eligible_under_supervision`, `ineligible_unqualified`, `ineligible_conflicted`, `ineligible_capacity_exceeded`), check breakdown flags, and human-readable explanation notes.

3. **Teaching Assignment Approval (incl. Homeroom Advisory) (`TCH-WF-07`, `TCH-WF-08`, `UC-TCH-10`, `UC-TCH-11`, `SCR-TCH-02`):**
   - Define configurable assignment roles (`assignment_roles`, e.g., Guru Utama, Guru Pendamping, Asisten Guru, Penilai, Wali Kelas, Pembimbing Akademik, Fasilitator, Guru Pengganti) with lead role indicators and workload counting rules.
   - Connect eligible faculty to authorized teaching responsibilities through formal assignments (`teaching_assignments`), capturing assignment number, assigned teacher, demand link, assignment role, assignment scope (`learning_unit`, `cohort_group`, `individual_student`, `extracurricular_group`), scope reference and label, academic period, effective date range, committed workload value, and approval authority metadata (`approved_by`, `approved_date`).
   - Manage assignment lifecycle states: `proposed`, `approved`, `rejected`, `withdrawn`, `returned_for_revision`, `suspended`, `cancelled`, and `completed`.
   - Support shared responsibility and co-teaching models via distinct assignments per teacher referencing the same demand and scope.
   - Support homeroom advisory assignments (`assignment_role = 'HOMEROOM_ADVISOR'`) decoupled from student cohort placement (owned by Student domain) and homeroom physical room allocation (owned by Operations domain).
   - Identify, classify, and track capacity and assignment conflicts (`capacity_conflicts`, e.g., expired authorization, workload limit exceeded, schedule overlap) with severity (`advisory` vs. `blocking`) and resolution workflows.

4. **Workload & Capacity Management (`TCH-WF-04`, `TCH-WF-10`, `UC-TCH-01`, `UC-TCH-07`, `SCR-TCH-03`, `SCR-TCH-04`):**
   - Enable teachers to declare weekly working availability, preferred teaching blocks, and recurring constraints across academic periods (`teacher_availabilities`, `SCR-TCH-04`).
   - Record explicit temporary unavailability windows (`teacher_unavailability_windows`, e.g., medical leave, official training, personal leave, blackout dates) without silently cancelling active assignments.
   - Establish versioned and effective-dated workload policies (`workload_policies`) defining measurement units (default: Teaching Hours), standard capacity limits, warning thresholds (approaching limit), prohibited thresholds (capacity ceiling), and countable activity inclusions (direct teaching, assessment, advising).
   - Track reproducible snapshots of planned, committed, and remaining teacher capacity per academic period (`workload_commitments`, `SCR-TCH-03`).
   - Manage formal workload exception requests (`workload_exceptions`) when an assignment requires exceeding prohibited capacity ceilings, enforcing strict governance: Teaching Coordinator recommends → Teaching Authority approves → Exception Granted (teachers cannot self-approve exceptions).

5. **Substitution & Reassignment (`TCH-WF-09`, `UC-TCH-06`, `SCR-TCH-02`):**
   - Manage urgent temporary substitutions (`teaching_substitutions`) for absent faculty, capturing original assignment, substitute teacher, coverage date range, coverage scope (`full` vs. `partial`), reason, workload impact, handover notes, and approval.
   - Preserve original assignment records and historical integrity during temporary substitution without altering past academic records.
   - Manage permanent or material reassignments (`teaching_reassignments`) transferring responsibility to a new teacher, preserving predecessor links, effective transition dates, formal justification, and syllabus/grading handover completion status (`requires_handover`, `handover_status`).

6. **Assignment Completion (`TCH-WF-11`, `UC-TCH-08`, `SCR-TCH-02`):**
   - Conclude teaching assignments upon reaching authorized period ends or completion milestones (`assignment_completions`).
   - Verify academic deliverables completion (grade submissions and learning activity wrap-up confirmed by Academics), confirm handover of unresolved items, and formally release committed teacher capacity back to the available pool.

7. **Teaching History & Record Correction (`TCH-WF-12`, `UC-TCH-12`, `SCR-TCH-01`, `SCR-TCH-02`):**
   - Preserve an immutable append-only log of significant decisions, transitions, and domain events across a teacher's assignments and capacity lifecycle (`teaching_history_entries`).
   - Support formal, auditable corrections to finalized teaching records (`teaching_record_corrections`) preserving original values, reason, supporting evidence, downstream impact assessment, and Teaching Authority approval without silent overwrites.

### Downstream Consumer Alignment

- **Academic Participation & Learning Activities (`IS009`, `SCR-ACD-01`):** Consumes approved `teaching_assignments` to verify faculty authority when conducting classroom sessions, recording student attendance, and submitting coursework evaluations.
- **Operations Scheduling & Rooms (`SCR-OPS-02`):** Consumes approved `teaching_assignments` and `teacher_availabilities` to perform operational timetable generation, room allocation, and timetable conflict verification.
- **Student Placement & Status (`IS005`, `SCR-STU-03`):** Correlates administrative cohort/homeroom placements with approved homeroom advisor assignments.

### Scope Boundaries & Explicit Deferrals

- **Faculty Profile & Authorization Governance:** Creating faculty profiles, verifying qualification evidence, and granting formal teaching authorizations belong to Module 1 `Teacher Profile & Authorization` (`IS014`, `SCR-TCH-01`).
- **Curriculum & Learning Unit Definition:** Course structures, academic requirements, syllabus units, and term configurations belong to Academic `Learning Structure & Requirements` (`IS008`, `SCR-ACD-03`).
- **Classroom Delivery & Attendance Capture:** Recording daily session delivery, logging student attendance, and student coursework submissions belong to Academic `Participation & Learning Activities` (`IS009`, `SCR-ACD-01`).
- **Operational Timetabling & Facility Allocation:** Physical classroom scheduling, period bells, room conflict resolution, and master timetable publication belong to Operational `Scheduling & Rooms` (`SCR-OPS-02`).
- **Student Homeroom Cohort Placement:** Grouping students into administrative homeroom cohorts belongs to Student `Placement & Status` (`IS005`, `SCR-STU-03`).
- **HR, Payroll & Employment Administration:** Employment contracts, payroll calculation, compensation scales, and general personnel leave administration remain strictly external to the SIS Teaching domain.

## Desired Outcome

1. Teaching Coordinators can intake, validate, and track teaching staffing demands from Academics, Students, and Development in `SCR-TCH-02` (`Teaching Assignment & Coverage Workbench`, `UC-TCH-04`, `TCH-WF-05`).
2. Teaching Coordinators can perform explainable, policy-driven eligibility evaluations matching candidate faculty against demand requirements (`UC-TCH-05`, `TCH-WF-06`, `SCR-TCH-02`).
3. Teaching Authorities can grant binding approval, modification, suspension, or cancellation of teaching assignments for academic learning units, homeroom advisories, and extracurricular activities in `SCR-TCH-02` (`UC-TCH-10`, `UC-TCH-11`, `TCH-WF-07`, `TCH-WF-08`).
4. Teachers can declare weekly availability, preferred time blocks, and personal scheduling constraints in `SCR-TCH-04` (`My Teaching Availability`, `UC-TCH-01`, `TCH-WF-04`).
5. Teaching Coordinators can monitor faculty teaching workload against policy thresholds (warning and prohibited caps) in `SCR-TCH-03` (`Faculty Workload Monitoring`, `UC-TCH-07`, `TCH-WF-10`).
6. Teaching Authorities can review and approve formal workload exception requests for capacity ceiling breaches (`UC-TCH-07`, `SCR-TCH-03`, `SCR-TCH-02`).
7. Teaching Coordinators can arrange emergency substitute teachers from eligible pools with expedited workflows in `SCR-TCH-02` (`UC-TCH-06`, `TCH-WF-09`).
8. Teaching Authorities can execute permanent reassignments with predecessor traceability and handover verification in `SCR-TCH-02` (`TCH-WF-09`).
9. Teaching Coordinators can formally conclude assignments upon academic confirmation, releasing faculty capacity (`UC-TCH-08`, `TCH-WF-11`, `SCR-TCH-02`).
10. Teaching Authorities can view immutable teaching history entries and execute auditable corrections to approved teaching records with mandatory justification logging (`UC-TCH-12`, `TCH-WF-12`).
11. Fulfills the assignment, workload, and substitution mandates of SIS Implementation Roadmap Stage 2 Completion Criteria:
    - **Criterion 7:** "A teacher has a verified profile, granted authorization, and an approved teaching assignment linking them to a learning unit."
    - **Criterion 8:** "Teaching workload is visible and substitution for an absent teacher is recorded."

## Current Situation

1. **Database Schema Status:**
   - The tables for Module 2 specified in `docs/entities/teaching-entities-tables.md` (lines 365–950) — `teaching_demands`, `teacher_eligibility_evaluations`, `assignment_roles`, `teaching_assignments`, `capacity_conflicts`, `workload_policies`, `teacher_availabilities`, `teacher_unavailability_windows`, `workload_commitments`, `workload_exceptions`, `teaching_substitutions`, `teaching_reassignments`, `assignment_completions`, `teaching_history_entries`, and `teaching_record_corrections` — are not yet provisioned in the database schema or EF Core migrations.
2. **Backend Status:**
   - The `Elsasa.Sis.Teaching` backend project is not yet established in `src/backend`. No domain entities, EF Core configurations, repository contracts, or MediatR commands/queries exist for Teaching Demands, Eligibility Evaluations, Assignments, Workload, Substitutions, Reassignments, or Completions.
3. **Frontend Status:**
   - In `src/frontend/src/features`, no `teaching` feature module exists. The UI components and routes for `SCR-TCH-02` (`Teaching Assignment & Coverage Workbench`), `SCR-TCH-03` (`Faculty Workload Monitoring`), and `SCR-TCH-04` (`My Teaching Availability`) are not yet implemented.
4. **Prerequisites Status:**
   - Module 1 "Teacher Profile & Authorization" (`IS014`) provides prerequisite teacher profiles, capabilities, verified qualifications, and teaching authorizations.
   - Operations Calendar & Periods (`IS001`, `IS003`) and Academic Learning Structure (`IS008`) provide prerequisite operational periods and learning units for teaching demand intake.

## Evidence

- `docs/sub-modul-and-development-phases.md`: Line 36 (Module Decomposition: Assignment & Workload), Line 80 (Stage 2 Roadmap Scope), Lines 133–134 (Stage 2 Completion Criteria 7 & 8), Lines 172, 180 (Dependencies and Homeroom Ownership Resolution).
- `docs/domains/teaching-domain.md`: Lines 150–350 (Teacher Availability, Teaching Demand, Teacher Eligibility, Teaching Assignment, Workload and Capacity, Capacity Conflicts, Substitution and Reassignment, Assignment Completion, Teaching History).
- `docs/entities/teaching-entities.md`: Lines 171–390 (Module 2 Entities: `TeachingDemand`, `DemandSourceReference`, `EligibilityEvaluation`, `TeachingAssignment`, `AssignmentRole`, `CapacityConflict`, `TeacherAvailability`, `WorkloadCommitment`, `WorkloadException`, `WorkloadPolicy`, `Substitution`, `Reassignment`, `AssignmentCompletion`, `TeachingHistoryEntry`, `TeachingRecordCorrection`).
- `docs/entities/teaching-entities-tables.md`: Lines 365–950 (Submodules 2.1–2.7 Data Model Specifications & Mermaid ERD).
- `docs/operational-workflow-discovery.md`: Lines 155–163 (`TCH-WF-04` to `TCH-WF-12`), Lines 451–479 (Teaching & Learning Matrix), Lines 617, 635 (Stage 2 Delivery).
- `docs/use-cases.md`: Lines 956, 959–963, 965–967 (`UC-TCH-01`, `UC-TCH-04`, `UC-TCH-05`, `UC-TCH-06`, `UC-TCH-07`, `UC-TCH-08`, `UC-TCH-10`, `UC-TCH-11`, `UC-TCH-12`).
- `docs/screen-inventory.md`: Lines 155–157, 530–532, 913–980 (`SCR-TCH-02` Teaching Assignment & Coverage Workbench, `SCR-TCH-03` Faculty Workload Monitoring, `SCR-TCH-04` My Teaching Availability).

## Notes

- Intake Classification: `CHANGE-REQUEST` (new capability implementation under Stage 2).
- Working Knowledge artifact adhering to `ics-issue-creation` standard.
- Workflow Routing: ISSUE (CHANGE-REQUEST) -> `ica-analyst`: Discovery, then `FEASIBILITY-ASSESSMENT` ([IS015 - TCH-ASSIGNMENT-WORKLOAD-FEASIBILITY-ASSESSMENT.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS015%20-%20TCH-ASSIGNMENT-WORKLOAD-FEASIBILITY-ASSESSMENT.md)).
