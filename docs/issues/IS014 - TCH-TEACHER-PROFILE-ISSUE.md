# ISSUE

## Metadata

ID: CR-TCH-TEACHER-PROFILE-AUTHORIZATION
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-2 Teaching Teacher Profile & Authorization Full Scope

## Source

Reported By: Drury
Reported Date: 2026-11-10

## Description

Implement the full scope of Stage 2 "Teacher Profile & Authorization" under the Teaching domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses four cohesive submodules managing teacher readiness and qualification governance:

1. **Teaching Profile Management (`TCH-WF-01`, `UC-TCH-02`, `SCR-TCH-01`):**
   - Establish and maintain authoritative records representing the school's recognized relationship with individuals as teaching resources (`teacher_profiles`).
   - Manage institutional teacher identifier (`teacher_number`), legal full name, display name with academic credentials/titles, institutional contact information, campus location affiliation, and department affiliation.
   - Support configurable teaching roles (`teaching_roles`, e.g., Guru Tetap, Guru Kontrak, Guru Tamu, Relawan, Penilai Eksternal, Fasilitator) with qualification check mandates (`requires_qualification_check`).
   - Track teacher standing states (`active`, `restricted`, `suspended`, `inactive`), standing justification rationale, effective dates (`effective_from`, `effective_to`), and formal school authority approval (`approved_by`, `approved_date`).
   - Support non-employee teaching resources (visiting lecturers, external assessors, volunteers) by retaining a nullable employee reference (`employee_ref`) while ensuring identity uniqueness via national identity numbers (`national_id_or_external_ref`), strictly decoupling teaching identity from HR/payroll systems.

2. **Teaching Capability (`TCH-WF-01`, `UC-TCH-02`, `SCR-TCH-01`):**
   - Manage the institutional taxonomy and categories of teaching capabilities (`capability_types`, e.g., subject area, educational stage/level, delivery mode, language of instruction, specialist activity).
   - Record potential teaching capabilities for teacher profiles (`teaching_capabilities`), describing potential pedagogical suitability across subject domains, grade stages, delivery formats, and languages independent of specific curriculum structures.
   - Enforce the foundational architectural principle that a capability describes potential suitability only; it does not by itself authorize teaching or commit teaching capacity.

3. **Qualification Evidence & Verification (`TCH-WF-02`, `UC-TCH-03`, `SCR-TCH-01`):**
   - Define recognized qualification and certification types (`qualification_types`, e.g., Sertifikat Pendidik / Serdik, Ijazah S1/S2, Sertifikat P3K, Child Protection) with renewal requirements and validity duration rules (`requires_renewal`, `default_validity_months`).
   - Store documentary evidence supporting teacher capabilities and authorizations (`qualification_evidence`), tracking the full verification lifecycle: `claimed`, `submitted`, `verified`, `rejected`, `expired`, and `revoked`.
   - Capture issuing authority, credential identifier, issue date, renewal expiry date, secure document archive reference, verification authority (`verified_by`), verification date, verification notes, and rejection reasons.
   - Enforce strict separation between qualification verification and teaching authorization: verified qualification confirms documentary authenticity, but does not automatically grant authorization to teach or assign work.

4. **Teaching Authorization Lifecycle (`TCH-WF-03`, `TCH-WF-12`, `UC-TCH-09`, `UC-TCH-12`, `SCR-TCH-01`):**
   - Record formal school permissions for teaching resources to perform specified teaching work within defined scopes and effective dates (`teaching_authorizations`).
   - Manage authorization scope descriptions, campus boundaries, educational program scopes, grade level scopes, delivery mode scopes, and approving authority decisions (`decision_authority`, `approved_date`).
   - Support authorization lifecycle states: `active`, `authorized_under_supervision`, `restricted`, `suspended`, `revoked`, and `expired`.
   - Maintain explicit traceability linking authorizations to supporting verified qualification evidence (`authorization_evidence`).
   - Manage conditional supervision requirements (`supervision_requirements`) when authorization is granted under supervision, capturing designated supervising mentor teachers, supervision types (co-teaching, periodic observation, lesson plan review, mentorship), validity periods, and resolution notes.
   - Support in-place auditable corrections of finalized faculty profiles, qualifications, and authorization records (`TCH-WF-12`, `UC-TCH-12`, `SCR-TCH-01`) with mandatory justification and audit logging.

### Downstream Consumer Alignment

- Establish the prerequisite baseline for Module 2 `Assignment & Workload` (`SCR-TCH-02`, `UC-TCH-04`, `UC-TCH-05`, `UC-TCH-10`), providing verified faculty dossiers and valid teaching authorizations to match against academic teaching demands.
- Provide authoritative teacher qualification and authorization validation for Academic `Participation & Learning Activities` (`IS009`, `SCR-ACD-01`, `ACD-WF-04`) to ensure learning sessions are conducted exclusively by authorized faculty.
- Provide authorization and supervision context to Operations for scheduling timetable occurrences and emergency substitutions (`SCR-OPS-02`, `SCR-TCH-02`).

### Scope Boundaries & Explicit Deferrals

The following capabilities belong to companion modules, subsequent stages, or external domains and are explicitly out of scope for this issue:

- **Teaching Demand Intake & Assignment Allocation:** Receiving staffing demand from Academics, candidate eligibility evaluation, binding assignment approvals to learning units or homerooms, and assignment completions belong to `Assignment & Workload` (`TCH-WF-05`, `TCH-WF-06`, `TCH-WF-07`, `TCH-WF-11`, `SCR-TCH-02`).
- **Teacher Availability & Workload Monitoring:** Teacher working hour declarations, blocked windows, capacity thresholds, overload monitoring, and workload exceptions belong to `Assignment & Workload` (`TCH-WF-04`, `TCH-WF-10`, `SCR-TCH-03`, `SCR-TCH-04`).
- **Emergency Substitution:** Arranging substitute teachers and managing coverage workflows belong to `Assignment & Workload` (`TCH-WF-09`, `SCR-TCH-02`).
- **Curriculum and Course Frameworks:** Defining learning units, course requirements, and syllabi belongs to Academic `Learning Structure & Requirements` (`IS008`, `SCR-ACD-03`).
- **Instructional Delivery & Student Attendance:** Conducting classroom activities, recording student attendance, and logging coursework belong to Academic `Participation & Learning Activities` (`IS009`, `SCR-ACD-01`).
- **Operational Timetabling & Room Allocations:** Room reservations, facility conflict resolution, and timetable occurrence management belong to Operational `Scheduling & Rooms` (`SCR-OPS-02`).
- **Human Resources & Payroll Administration:** Employment contracts, salary scales, benefits, leave entitlements, and payroll processing are strictly outside the SIS Teaching domain. Teaching maintains only the minimal identity and qualification attributes necessary for academic authorization and staffing.

## Desired Outcome

1. Teaching Coordinators can create, update, and manage teacher professional profiles with institutional affiliations, contact info, and configurable teaching roles in `SCR-TCH-01` (`Faculty Resource Workspace`, `UC-TCH-02`, `TCH-WF-01`).
2. Teaching Coordinators can capture teacher capabilities categorized across subject areas, stage levels, delivery modes, and instructional languages (`UC-TCH-02`, `SCR-TCH-01`).
3. Teaching Coordinators can record, inspect, verify, or reject documentary qualification evidence with credential numbers and expiration dates (`UC-TCH-03`, `TCH-WF-02`, `SCR-TCH-01`).
4. Teaching Authorities can grant, condition (under supervision), restrict, suspend, or revoke formal teaching authorizations backed by verified qualification evidence (`UC-TCH-09`, `TCH-WF-03`, `SCR-TCH-01`).
5. Teaching Authorities can configure supervision requirements (co-teaching, periodic observation, mentoring) and assign senior mentors for teachers authorized under supervision (`UC-TCH-09`, `SCR-TCH-01`).
6. Teaching Authorities can execute auditable corrections on finalized faculty profiles and authorization records with mandatory justification logging (`UC-TCH-12`, `TCH-WF-12`, `SCR-TCH-01`).
7. Fulfills the foundational faculty readiness requirement of SIS Implementation Roadmap Stage 2 Completion Criterion 7: "A teacher has a verified profile, granted authorization, and an approved teaching assignment linking them to a learning unit."

## Current Situation

1. **Database Schema Status:**
   - The tables for Module 1 specified in `docs/entities/teaching-entities-tables.md` (lines 22–340) — `teaching_roles`, `teacher_profiles`, `capability_types`, `teaching_capabilities`, `qualification_types`, `qualification_evidence`, `teaching_authorizations`, `authorization_evidence`, and `supervision_requirements` — are not yet provisioned in the database schema or EF Core migrations.
2. **Backend Status:**
   - The `Elsasa.Sis.Teaching` backend project is not yet established in `src/backend`. No domain entities, EF configurations, repository contracts, or MediatR commands/queries exist for Teaching Profiles, Capabilities, Qualifications, or Authorizations.
3. **Frontend Status:**
   - In `src/frontend/src/features`, no `teaching` feature module exists. The UI component and routes for `SCR-TCH-01` (`Faculty Resource Workspace`) are not yet implemented.
4. **Prerequisites Status:**
   - According to the Roadmap (`docs/sub-modul-and-development-phases.md`), Module 1 "Teacher Profile & Authorization" has **no internal SIS prerequisites** and can be implemented directly.
   - Operations Calendar & Periods (`IS001`, `IS003`) and Academic Learning Structure (`IS008`) provide established operational context for upcoming Stage 2 teaching assignments.

## Evidence

- `docs/sub-modul-and-development-phases.md`: Line 35 (Module Decomposition), Line 79 (Stage 2 Roadmap Scope), Line 133 (Criterion 7), Line 183 (Employee/HR Boundary Resolution).
- `docs/domains/teaching-domain.md`: Lines 1–286 (Purpose, Scope, Teaching Profile, Teaching Capability, Qualification Evidence, Teaching Authorization).
- `docs/entities/teaching-entities.md`: Lines 20–160 (Module 1 Entities: `TeacherProfile`, `TeachingRole`, `TeachingCapability`, `CapabilityType`, `QualificationEvidence`, `QualificationType`, `TeachingAuthorization`, `AuthorizationEvidence`, `SupervisionRequirement`).
- `docs/entities/teaching-entities-tables.md`: Lines 22–361 (Submodules 1.1–1.4 Data Model Specifications & Mermaid ERD).
- `docs/operational-workflow-discovery.md`: Lines 152–154 (`TCH-WF-01`, `TCH-WF-02`, `TCH-WF-03`), Line 163 (`TCH-WF-12`), Lines 451–479 (Teaching & Learning Matrix), Lines 617, 635 (Stage 2 Delivery).
- `docs/use-cases.md`: Lines 956–967, Lines 988–1009 (`UC-TCH-02`, `UC-TCH-03`), Lines 1072–1079 (`UC-TCH-09`), Lines 1104–1111 (`UC-TCH-12`).
- `docs/screen-inventory.md`: Lines 154, 529, 890–911 (`SCR-TCH-01` Faculty Resource Workspace).

## Notes

- Intake Classification: `CHANGE-REQUEST` (new capability implementation under Stage 2).
- Working Knowledge artifact adhering to `ics-issue-creation` standard.
- Workflow Routing: ISSUE (CHANGE-REQUEST) -> `ica-analyst`: Discovery, then `FEASIBILITY-ASSESSMENT` ([IS014 - TCH-TEACHER-PROFILE-FEASIBILITY-ASSESSMENT.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS014%20-%20TCH-TEACHER-PROFILE-FEASIBILITY-ASSESSMENT.md)).
