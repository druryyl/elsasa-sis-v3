# ISSUE

## Metadata

ID: CR-STU-PLACEMENT-STATUS
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-1 Placement & Status Core Submodules

## Source

Reported By: Drury
Reported Date: 2026-11-04

## Description

Implement the Stage 1 scope of "Placement & Status — core submodules only: Administrative Placement, Student Status Management, Administrative History" under the Student domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses three foundational submodules:
1. **Administrative Placement (including homeroom cohort):** Manage where the student is officially registered within the school's organizational structure. Minimal model encompasses core mandatory attributes (`campus`, `school_level`, `grade`, `academic_year`, `effective_date`) and optional extension attributes (`program`, `stream`, `cohort`, `homeroom`). Enforce that a student has at most one active administrative placement across all campuses at any given time (`is_current = true`). Support administrative placement changes with effective dating and audit reasons while preserving historical placement records.
2. **Student Status Management:** Manage the student's official administrative relationship with the school. Support authoritative lifecycle status recording and transitions (specifically managing the transition to `Active` post-enrollment, alongside `enrolled`). Ensure the Students domain retains exclusive authority over student status changes and maintains an authoritative status period history (`student_statuses`) with denormalized operational status on the student record.
3. **Administrative History:** Provide an immutable audit trail capturing every significant administrative event across the student lifecycle (e.g., application submitted, application accepted, enrollment completed, student activated, administrative placement assigned/changed, identity corrected) with event type, date, detail, authorizer, and decision reference. Historical records must never be silently overwritten; corrections must preserve what changed, when, and who authorized the correction.

Stage 2 and Stage 3 capabilities (Leave of Absence, Re-enrollment, Promotion/Retention Execution, Internal Transfer, External Transfer Out/In, Withdrawal, Graduation Execution, and Alumni) are explicitly deferred to subsequent stages.

## Desired Outcome

1. Enrolled students receive their initial administrative placement record (campus, level, grade, academic year, optional cohort and homeroom) linked to the student identity.
2. Student administrative status is managed and set to `Active`, with status period history accurately tracked in an authoritative status history table.
3. Authorized Student Administration Officers can view, assign, and update administrative placements (`UC-STD-06`, `STD-WF-05`) with effective dating and reason, preserving historical placement periods.
4. The system enforces the single active administrative placement rule across all campuses per student at any point in time.
5. An immutable administrative history log is established (`administrative_history_entries`), recording significant lifecycle events and enabling Student Administration Officers to inspect the chronological audit trail from `SCR-STU-02` (Student Records Desk).
6. Meets Stage 1 Completion Criteria:
   - **Criterion 3:** "An accepted applicant can be enrolled, assigned a unique student number, and placed in an administrative location (campus, level, program)."
   - **Criterion 5:** "Student status is set to Active and administrative history records the complete intake sequence."
   - **Criterion 10:** "All domain events for the above are recorded and historically traceable."
7. Delivers the verified administrative placement and active status foundation required by downstream Stage 1 modules (Finance `Obligations & Billing` for tuition assessment) and Stage 2 modules (Academic `Participation & Learning Activities`, Teaching `Assignment & Workload`).

## Current Situation

1. Schema baseline: Initial student intake and identity tables (`students.applicants`, `students.admission_applications`, `students.admission_decisions`, `students.applicant_documents`, `students.entrance_assessment_evidence`, `students.students`, `students.enrollments`, `students.guardians`, `students.student_guardian_relationships`, `students.student_documents`) exist in the database from migrations `20261101_0201`, `20261101_0202`, `20261103_0201`, and `20261103_0301`. However, tables for Submodule 3 (`students.administrative_placements`, `students.student_statuses`, `students.administrative_history_entries`, and supporting transitions/decisions) are not yet defined or migrated.
2. Backend status: `EnrollAcceptedApplicantHandler` currently provisions the student record (leaving `current_status = 'enrolled'`), enrollment record, guardians, and documents, but does not provision a distinct `administrative_placements` record, does not transition status to `Active` via `student_statuses`, and does not emit or record entries in `administrative_history_entries`. Additionally, no API vertical slices exist for updating administrative placement (`ChangeAdministrativePlacement`) or retrieving placement and administrative history.
3. Frontend status: `src/frontend/src/features/students/records/` provides student lookup and identity/guardian maintenance, but `SCR-STU-02` (Student Records Desk) lacks UI components and tabs for viewing/updating administrative placements (campus, level, grade, academic year, cohort, homeroom) and inspecting the chronological administrative history audit log.
4. Upstream prerequisites: Stage 1 Admission (`CR-STU-ADMISSION`) and Enrollment & Identity (`CR-STU-ENROLLMENT-IDENTITY`) are implemented and active, producing official student records. Operations Calendar & Periods (`CR-OPS-CALENDAR-PERIODS`) provides active academic year context.

## Evidence

- Roadmap Specification: `docs/sub-modul-and-development-phases.md` (Stage 1, Module: Placement & Status — core submodules only: Administrative Placement, Student Status Management, Administrative History; Section D Criteria 3, 5, 10; Section E Domain Boundary Issues - Homeroom ownership resolution).
- Student Domain Blueprint: `docs/domains/student-domain.md` (Section: Administrative Placement, lines 151–171; Section: Student Status, lines 173–196; Section: Administrative History, lines 320–330, 1590; Section: Cohort Placement, lines 180).
- Student Entities & Schema Specification: `docs/entities/student-entities.md` (Submodule 3 — Placement & Status, lines 208–254, 320–330; Decision 5: Global Identity; Decision 6: Exactly One Active Placement; Decision 9: Minimum Administrative Placement Model) and `docs/entities/student-entities-tables.md` (Submodule 3: `administrative_placements`, `student_statuses`, `administrative_history_entries`, lines 418–485, 742–770).
- Operational Workflow Discovery: `docs/operational-workflow-discovery.md` (`STD-WF-05: Change Administrative Placement`; Section F Stage 1 Completion Criteria Traceability Criteria 2–5).
- Use Cases: `docs/use-cases.md` (`UC-STD-06: Change Administrative Placement`).
- Screen Inventory: `docs/screen-inventory.md` (`SCR-STU-02: Student Records Desk` - Administrative placement history and administrative history audit trail).
- Baseline Implementation: `src/backend/Elsasa.Sis.Students/Features/Enrollment/EnrollAcceptedApplicant/EnrollAcceptedApplicantHandler.cs`, `src/backend/Elsasa.Sis.Database/Scripts/20261103_0301_Students_EnrollmentIdentity_Tables.sql`, `src/frontend/src/features/students/records/`.

## Notes

- This artifact serves as the formal change request capturing WHAT capability is requested and the operational outcomes expected, maintaining solution neutrality without prescribing technical architecture, API schemas, or UI implementations.
- Scope boundary discipline: Advanced lifecycle transitions (Leave of Absence, Re-enrollment, Promotion/Retention, Internal Transfer, External Transfer, Withdrawal, Graduation, Alumni) belong to Stage 2 and Stage 3 and must not be conflated with this Stage 1 core issue.
- Technical feasibility assessment, domain modeling, feature decomposition, and architecture updates will be performed in downstream SDLC stages.
