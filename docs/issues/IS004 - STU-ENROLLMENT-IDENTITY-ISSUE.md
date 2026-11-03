# ISSUE

## Metadata

ID: CR-STU-ENROLLMENT-IDENTITY
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-1 Enrollment & Identity Full Scope

## Source

Reported By: Drury
Reported Date: 2026-11-03

## Description

Implement the full scope of Stage 1 "Enrollment & Identity" under the Student domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses four cohesive submodules:
1. **Enrollment Execution:** Formal administrative transformation of an accepted applicant into an official student of the school, recording enrollment date, initial administrative placement context (campus, level/grade, academic year, program, cohort), approval authority, and verification of enrollment conditions (such as policy-driven payment confirmation or authorized fee waiver).
2. **Student Number Assignment:** Authoritative generation and assignment of a persistent, unique student number (e.g., format `STD-YYYY-NNNN`) that serves as the universal student identifier across all SIS domains and must never be reused for another person.
3. **Student Identity Maintenance:** Creation and ongoing maintenance of the authoritative student identity record (`students`), preserving identity continuity from the applicant record, managing student supporting documents (`student_documents`), and managing initial administrative status (`enrolled`, `active`).
4. **Parent & Guardian Management:** Establishment and management of parent and legal guardian profiles (`guardians`) and their formal relationships to students (`student_guardian_relationships`), including designated responsibility roles (primary contact, emergency contact, financial contact, and authorized pickup).

## Desired Outcome

1. Accepted applicants from the Admission process can be formally enrolled into the institution by authorized Student Administration Officers.
2. Each enrolled student is assigned a persistent, unique student number and established as the authoritative student identity record across the system.
3. Initial administrative placement parameters (campus, level, program, academic year) are recorded at enrollment.
4. Parent and legal guardian identities and relationships are recorded with explicit responsibility roles (primary, emergency, financial, authorized pickup).
5. When school policy requires enrollment fee payment prior to enrollment completion, the system verifies payment confirmation or an authorized administrative waiver/override with justification before enrollment executes.
6. Meets Stage 1 Completion Criteria:
   - **Criterion 3:** "An accepted applicant can be enrolled, assigned a unique student number, and placed in an administrative location (campus, level, program)."
   - **Criterion 4:** "Parent/guardian relationships are recorded for enrolled students."
   - **Criterion 9:** "Enrollment payment confirmation is available to Students when policy requires it."
   - **Criterion 10:** "All domain events for the above are recorded and historically traceable."
7. Establishes the core student identity records and relationships required by downstream Stage 1 modules (`Placement & Status`, `Obligations & Billing`, `Payments & Receivables`) and Stage 2 modules (`Academic`, `Teaching`).

## Current Situation

1. Schema baseline: Initial student tables (`students.applicants`, `students.admission_applications`) and admission lifecycle tables (`students.admission_decisions`, `students.applicant_documents`, `students.entrance_assessment_evidence`, `students.admission_application_contacts`) exist in the database from prior migrations (`20261101_0201`, `20261101_0202`, and `20261103_0201`), but tables for official student identity, enrollments, guardians, relationships, and student documents (`students.students`, `students.enrollments`, `students.guardians`, `students.student_guardian_relationships`, `students.student_documents`) are not yet defined or migrated.
2. Backend project: `src/backend/Elsasa.Sis.Students` contains implementation for the Admission module (`Features/Admission`), but contains no feature vertical slices, endpoints, command/query handlers, or validators for enrollment execution, student number assignment, student identity management, or guardian management.
3. Frontend: `src/frontend/src/features/students/enrollment/views/EnrollmentDeskView.vue` exists only as a placeholder view stub. `src/frontend/src/features/students/admission/views/StudentIntakeWorkbenchView.vue` does not yet feature the enrollment transition action or modal for accepted applicants, and `SCR-STU-02` (Student Records Desk) is not yet implemented.
4. Upstream prerequisites: Stage 1 Admission (`CR-STU-ADMISSION`) has been implemented, providing accepted applicants with finalized admission decisions ready for enrollment intake. Operations Calendar & Periods (`CR-OPS-CALENDAR-PERIODS`) is operational.

## Evidence

- Roadmap Specification: `docs/sub-modul-and-development-phases.md` (Stage 1, Module: Enrollment & Identity — full scope; Section D Criteria 3, 4, 9, 10; Section E Cross-Domain Dependencies).
- Student Domain Blueprint: `docs/domains/student-domain.md` (Section: Enrollment, lines 122–148; Section: Parent and Guardian Information, lines 78–90, 459–475; Section: Student Number, lines 592–600; Section: Student, lines 588–591).
- Student Entities & Schema Specification: `docs/entities/student-entities.md` (Submodule 2 — Enrollment & Identity, lines 117–205) and `docs/entities/student-entities-tables.md` (Submodule 2: `students`, `enrollments`, `guardians`, `student_guardian_relationships`, `student_documents`, lines 226–380).
- Operational Workflow Discovery: `docs/operational-workflow-discovery.md` (`STD-WF-03: Enroll Accepted Applicant`, `STD-WF-04: Maintain Student Identity`, and Section F.2 GAP-01 on payment gate enforcement).
- Use Cases: `docs/use-cases.md` (`UC-STD-04: Enroll Accepted Applicant`, `UC-STD-05: Maintain Student Identity`).
- Screen Inventory: `docs/screen-inventory.md` (`SCR-STU-01: Student Intake Workbench` - enrollment execution; `SCR-STU-02: Student Records Desk`).
- Existing Baseline Code: `src/backend/Elsasa.Sis.Students`, `src/backend/Elsasa.Sis.Database/Scripts/20261103_0201_Students_Admission_Tables.sql`, `src/frontend/src/features/students/enrollment/views/EnrollmentDeskView.vue`, `src/frontend/src/features/students/admission/views/StudentIntakeWorkbenchView.vue`.

## Notes

- This artifact serves as the formal change intake capturing WHAT capability is requested and the operational outcomes expected, maintaining solution neutrality without prescribing technical architecture, API schemas, or UI implementations.
- Payment Gate handling: Governed by school policy. When required, enrollment checks confirmed payment receipt or an authorized waiver/administrative override (`GAP-01`). In Stage 1, manual entry or confirmation flags are supported if full automated Finance payment integration is pending.
- Technical feasibility assessment, domain modeling, feature decomposition, and architecture updates will be performed in downstream SDLC stages.
