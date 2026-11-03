# ISSUE

## Metadata

ID: CR-STU-ADMISSION
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-1 Admission Full Scope

## Source

Reported By: Drury
Reported Date: 2026-11-03

## Description

Implement the full scope of Stage 1 "Admission" under the Student domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses four cohesive submodules:
1. Application Registration: registration of prospective student applications and identity context (for both new learners and external transfer-in applicants), capturing biographical details, requested admission period, entry level, program/campus preferences, and guardian contact details.
2. Document & Identity Verification: tracking and verification of required initial applicant credentials and documents (e.g., birth certificate, family card, previous school records), with submission and verification status management.
3. Admission Review & Decision: administrative evaluation of applications against institutional admission criteria, and recording official admission decisions (Accepted, Rejected, Waitlisted, Returned for Correction, Withdrawn, Deferred) with effective dates and traceable decision authority.
4. Entrance Assessment Evidence Intake (Stage 1 manual/external input mode): manual capture and attachment of diagnostic entrance assessment scores and evidence provided by external assessment centers or external examiners to an application, prior to full Academics integration deferred to Stage 2.

## Desired Outcome

1. Prospective learners (or parents/guardians) can register an admission application and submit required initial identity credentials and documents for an active admission period.
2. Admission Officers can verify submitted credentials, log external/manual entrance diagnostic scores, evaluate applicant admissibility, and record authoritative admission decisions (Accepted, Rejected, Waitlisted, Returned for Correction, Withdrawn, Deferred).
3. Meets Stage 1 Completion Criterion 2: "An applicant can register an admission application, undergo review, and receive an acceptance or rejection decision."
4. Produces accepted applicant records and application state ready for subsequent Stage 1 enrollment handover (`Enrollment & Identity` module).
5. Preserves a complete, auditable intake history for all applicant interactions and decision events.

## Current Situation

1. Core schema tables exist for initial applicant storage and basic application records (`students.applicants`, `students.admission_applications` created via early initialization scripts `20261101_0201_Students_Applicants.sql` and `20261101_0202_Students_AdmissionApplications.sql`), but lack full Stage 1 column structures, document tracking, manual assessment evidence intake, decision records, and contact associations.
2. Backend project `src/backend/Elsasa.Sis.Students` exists only as an assembly marker without vertical slice endpoints, domain commands/queries, or business rule validation for application registration, document verification, entrance assessment evidence intake, or admission decisions.
3. Frontend features (`src/frontend/src/features/students/admission/views/AdmissionQueueView.vue` and `ApplicationDetailView.vue`) exist only as placeholder views without functional forms, queues, verification workflows, or API integration.
4. The operational prerequisite for Stage 1 admission (Calendar & Periods — admission periods) has been implemented in Stage 1 under `CR-OPS-CALENDAR-PERIODS`, allowing admission periods to be queried as context.

## Evidence

- Roadmap Specification: `docs/sub-modul-and-development-phases.md` (Stage 1, Module: Admission — full scope; Section D Criterion 2; Section E Model B entrance assessment decision).
- Student Domain Blueprint: `docs/domains/student-domain.md` (Section: Admission, lines 98–120; Section 1: Register Admission Application, lines 713–747; Section 2: Review Admission Application, lines 749–786).
- Student Entities & Schema Specification: `docs/entities/student-entities.md` and `docs/entities/student-entities-tables.md` (Submodule 1 — Admission: `applicants`, `admission_applications`, `admission_decisions`, `applicant_documents`, `entrance_assessment_evidence`, `admission_application_contacts`).
- Operational Workflow Discovery: `docs/operational-workflow-discovery.md` (`STD-WF-01: Register Admission Application`, `STD-WF-02: Review Admission Application (Stage 1 vs. Stage 2 Refinement)`).
- Use Cases: `docs/use-cases.md` (`UC-STD-01: Register Admission Application`, `UC-STD-03: Review Admission Application`).
- Screen Inventory: `docs/screen-inventory.md` (`SCR-SELF-01: Admission Application Portal`, `SCR-STU-01: Student Intake Workbench`).
- Existing Baseline Code: `src/backend/Elsasa.Sis.Students/StudentsAssemblyMarker.cs`, `src/backend/Elsasa.Sis.Database/Scripts/20261101_0201_Students_Applicants.sql`, `src/backend/Elsasa.Sis.Database/Scripts/20261101_0202_Students_AdmissionApplications.sql`, `src/frontend/src/features/students/admission/views/AdmissionQueueView.vue`, `src/frontend/src/features/students/admission/views/ApplicationDetailView.vue`.

## Notes

- This artifact serves as the formal change intake. It defines WHAT capability is requested and the operational outcomes expected, without prescribing technical architecture, API schemas, or UI implementations.
- Stage 1 specifically specifies that entrance assessment evidence is entered as manual/external input; full digital integration with the Academics domain assessment workflow (`ACD-WF-20`) is deferred to Stage 2.
- Enrollment execution, student number assignment, administrative placement, and fee assessment/payment gates belong to downstream Stage 1 modules (`Enrollment & Identity`, `Placement & Status`, `Obligations & Billing`, `Payments & Receivables`) and are outside the scope of this admission intake change request.
- Technical feasibility, detailed domain modeling, feature decomposition, and architecture updates will be performed in downstream SDLC stages.
