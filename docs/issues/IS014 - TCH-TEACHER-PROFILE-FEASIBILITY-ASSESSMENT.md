---
Title: Feasibility Assessment — Stage 2 Teaching Teacher Profile & Authorization Full Scope
Code: CR-TCH-TEACHER-PROFILE-AUTHORIZATION
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-10
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Implementation of the full scope of Stage 2 "Teacher Profile & Authorization" under the Teaching domain as specified in [IS014 - TCH-TEACHER-PROFILE-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS014%20-%20TCH-TEACHER-PROFILE-ISSUE.md) and the SIS implementation roadmap ([sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)).

Referenced artifacts:
- **DOMAIN:** [teaching-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/teaching-domain.md) (Purpose, Scope, Teaching Profile, Capability, Qualification Evidence, Teaching Authorization, Record Corrections)
- **ENTITIES:** [teaching-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/teaching-entities.md), [teaching-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/teaching-entities-tables.md) (Module 1: lines 22–361; Table 2.15: lines 913–950)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md) (`TCH-WF-01`, `TCH-WF-02`, `TCH-WF-03`, `TCH-WF-12`)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md) (`UC-TCH-02` Maintain Teaching Profile, `UC-TCH-03` Verify Teacher Qualifications, `UC-TCH-09` Determine Teaching Authorization, `UC-TCH-12` Correct Approved Teaching Record)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md) (Teaching Coordinator, Teaching Authority, School Leadership, Teacher, Qualification Verifier)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md) (`SCR-TCH-01` Faculty Resource Workspace)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md) (ADR-001, ADR-002, ADR-004, ADR-005, ADR-006, ADR-010)

## Objective

Evaluate technical feasibility, architectural consistency, codebase readiness, and structural impacts of establishing the Teaching domain backend and frontend in Elsasa SIS v3, strictly adhering to **Pragmatic**, **Minimalist**, and **Explicit** architectural principles:

1. **Teaching Profile Management (`TCH-WF-01`, `UC-TCH-02`, `SCR-TCH-01`):**
   - Establish and maintain authoritative records representing the school's recognized relationship with individuals as teaching resources (`teacher_profiles`).
   - Manage institutional teacher identifier (`teacher_number`), legal full name, professional display name with credentials, contact details, campus affiliation, and subject department.
   - Support configurable teaching roles (`teaching_roles`, e.g., Guru Tetap, Guru Kontrak, Guru Tamu, Relawan, Penilai Eksternal, Fasilitator) with qualification check mandates (`requires_qualification_check`).
   - Support non-employee teaching resources (visiting lecturers, external assessors, volunteers) by retaining a nullable employee reference (`employee_ref`) while guaranteeing identity uniqueness via national identity numbers (`national_id_or_external_ref`), strictly decoupling teaching identity from HR/payroll systems.
   - Track teacher standing states (`pending_approval`, `active`, `restricted`, `suspended`, `inactive`), standing justification rationale, effective dates (`effective_from`, `effective_to`), and formal school authority approval (`approved_by`, `approved_date`).
2. **Teaching Capability (`TCH-WF-01`, `UC-TCH-02`, `SCR-TCH-01`):**
   - Manage the institutional taxonomy and categories of teaching capabilities (`capability_types`, e.g., subject area, educational stage/level, delivery mode, language of instruction, specialist activity).
   - Record potential teaching capabilities for teacher profiles (`teaching_capabilities`), describing potential pedagogical suitability across subject domains, grade stages, delivery formats, and languages independent of specific curriculum structures.
   - Enforce the foundational architectural principle that a capability describes potential suitability only; it does not by itself authorize teaching or commit teaching capacity.
3. **Qualification Evidence & Verification (`TCH-WF-02`, `UC-TCH-03`, `SCR-TCH-01`):**
   - Define recognized qualification and certification types (`qualification_types`, e.g., Sertifikat Pendidik / Serdik, Ijazah S1/S2, Sertifikat P3K, Child Protection) with renewal requirements and validity duration rules (`requires_renewal`, `default_validity_months`).
   - Store documentary evidence supporting teacher capabilities and authorizations (`qualification_evidence`), tracking the full verification lifecycle: `claimed`, `submitted`, `verified`, `rejected`, `expired`, and `revoked`.
   - Capture issuing authority, credential identifier, issue date, renewal expiry date, secure document archive reference, verification authority (`verified_by`), verification date, verification notes, and rejection reasons.
   - Enforce strict conceptual separation between qualification verification and teaching authorization: verified qualification confirms documentary authenticity, but does not automatically grant authorization to teach or assign work.
4. **Teaching Authorization Lifecycle (`TCH-WF-03`, `UC-TCH-09`, `SCR-TCH-01`):**
   - Record formal school permissions for teaching resources to perform specified teaching work within defined scopes and effective dates (`teaching_authorizations`).
   - Manage authorization scope descriptions, campus boundaries, educational program scopes, grade level scopes, delivery mode scopes, and approving authority decisions (`decision_authority`, `approved_date`).
   - Support authorization lifecycle states: `active`, `authorized_under_supervision`, `restricted`, `suspended`, `revoked`, and `expired`.
   - Support permissive guidance gating: allow granting provisional/conditional authorizations when evidence is in `submitted` state, stamping an explicit `unverified_credential_warning` flag and notifying Teaching Authority.
   - Maintain explicit traceability linking authorizations to supporting qualification evidence (`authorization_evidence`).
   - Manage conditional supervision requirements (`supervision_requirements`) when authorization is granted under supervision, capturing designated supervising mentor teachers, supervision types (co-teaching, periodic observation, lesson plan review, mentorship), validity periods, and resolution notes.
5. **Auditable Record Corrections (`TCH-WF-12`, `UC-TCH-12`, `SCR-TCH-01`):**
   - Provision `teaching_record_corrections` (table 2.15) to execute auditable corrections on finalized faculty profiles and authorization records with mandatory justification logging and before/after delta tracking.
   - Implement role-aware direct application: Teaching Authority can apply corrections directly in a single atomic transaction, while non-authority coordinators submit a `requested` correction requiring Teaching Authority approval to apply.
6. **Frontend Faculty Resource Workspace (`SCR-TCH-01`):**
   - Implement a unified Work Queue and Master-Detail Workbench at `/teaching/faculty` with queue filters (All Faculty, Pending Profile Approval, Pending Verification, Pending Authorization, Corrections) and search.
   - Provide a teacher header with standing badges and 5 cohesive tabs (*Profile & Affiliation*, *Teaching Capabilities*, *Qualification Evidence*, *Authorizations & Supervision*, *Corrections Audit*).
7. **Demo Data Generator & Purger Support for Presentation & Verification:**
   - Establish `ITeachingDemoDataSeeder` and `ITeachingDemoDataPurger` wired into `DemoDataOrchestrator` in forward topological and reverse-purge order.
   - Seed 4 realistic Hagios School of Life personas: (1) Senior Permanent Teacher (Budi Hartono - active authorization, verified Serdik), (2) Contract Teacher under Supervision (Siti Nurhaliza - conditional authorization, co-teaching mentor assigned), (3) Non-Employee Visiting Teacher (John Miller - null employee_ref, passport identity), and (4) Pending Onboarding Candidate (Hendra Setiawan - pending profile approval & submitted credential awaiting verification).

### Scope Boundaries & Explicit Deferrals

The following capabilities belong to companion modules, subsequent stages, or external domains and are explicitly out of scope for this issue:

- **Teaching Demand Intake & Assignment Allocation:** Receiving staffing demand from Academics, candidate eligibility evaluation, binding assignment approvals to learning units or homerooms, and assignment completions belong to `Assignment & Workload` (`TCH-WF-05`, `TCH-WF-06`, `TCH-WF-07`, `TCH-WF-11`, `SCR-TCH-02`).
- **Teacher Availability & Workload Monitoring:** Teacher working hour declarations, blocked windows, capacity thresholds, overload monitoring, and workload exceptions belong to `Assignment & Workload` (`TCH-WF-04`, `TCH-WF-10`, `SCR-TCH-03`, `SCR-TCH-04`).
- **Emergency Substitution:** Arranging substitute teachers and managing coverage workflows belong to `Assignment & Workload` (`TCH-WF-09`, `SCR-TCH-02`).
- **Curriculum and Course Frameworks:** Defining learning units, course requirements, and syllabi belongs to Academic `Learning Structure & Requirements` (`IS008`, `SCR-ACD-03`).
- **Instructional Delivery & Student Attendance:** Conducting classroom activities, recording student attendance, and logging coursework belong to Academic `Participation & Learning Activities` (`IS009`, `SCR-ACD-01`).
- **Operational Timetabling & Room Allocations:** Room reservations, facility conflict resolution, and timetable occurrence management belong to Operational `Scheduling & Rooms` (`SCR-OPS-02`).
- **Human Resources & Payroll Administration:** Employment contracts, salary scales, benefits, leave entitlements, and payroll processing are strictly outside the SIS Teaching domain. Teaching maintains only the minimal identity and qualification attributes necessary for academic authorization and staffing.

---

# 2. Current State

## Existing Behavior

1. **Database Schema:**
   - The database contains provisioned tables for Operations (`operations`), Students (`students`), Finance (`finance`), and Academics (`academics`).
   - The isolated schema `teaching` has **not yet been created**.
   - None of the 9 Module 1 tables specified in [teaching-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/teaching-entities-tables.md) lines 22–340 (`teaching_roles`, `teacher_profiles`, `capability_types`, `teaching_capabilities`, `qualification_types`, `qualification_evidence`, `teaching_authorizations`, `authorization_evidence`, `supervision_requirements`) or table 2.15 (`teaching_record_corrections`) are provisioned.
2. **Backend Domain & Services:**
   - `src/backend` contains projects `Elsasa.Sis.Operations`, `Elsasa.Sis.Students`, `Elsasa.Sis.Finance`, `Elsasa.Sis.Academics`, `Elsasa.Sis.Contracts`, `Elsasa.Sis.Shared`, `Elsasa.Sis.Database`, and `Elsasa.Sis.Api`.
   - The project `Elsasa.Sis.Teaching` is **not yet established** in `src/backend` and is not registered in `Elsasa.Sis.sln` or `Elsasa.Sis.Api`.
   - There are **no domain entities, Dapper repositories, MediatR commands/queries, or Minimal API endpoints** for Teacher Profiles, Capabilities, Qualification Evidence, Teaching Authorizations, or Record Corrections.
   - In `Elsasa.Sis.Contracts`, there is no `ITeachingModule` contract or teaching-related DTOs.
3. **Frontend Implementation:**
   - `src/frontend/src/features` contains feature modules for `academics`, `auth`, `dashboard`, `finance`, `operations`, `portal`, and `students`.
   - There is **no `teaching` feature directory** under `src/frontend/src/features`.
   - `src/frontend/src/app/router/routes.ts` does not declare routes for `/teaching/faculty` or any teaching workspaces.
   - Screen `SCR-TCH-01` (`Faculty Resource Workspace`) is not yet implemented.
4. **Demo Data Seeding & Purging:**
   - `src/backend/Elsasa.Sis.Contracts/DemoData` defines `IOperationsDemoDataSeeder`, `IAcademicsStructureDemoDataSeeder`, `IFinancePolicyDemoDataSeeder`, `IStudentsDemoDataSeeder`, `IFinanceTransactionsDemoDataSeeder`, `IAcademicsActivitiesDemoDataSeeder`, and domain purgers.
   - `DemoDataOrchestrator.cs` in `Elsasa.Sis.Api` orchestrates purging in reverse order (Academics → Finance → Students → Operations) and seeding in forward order.
   - There are no seeders, purgers, or constants for the Teaching domain.

## Existing Constraints

1. **Domain Isolation & ADR Compliance (ADR-001, ADR-002, ADR-010):**
   - `Elsasa.Sis.Teaching` must reference only `Elsasa.Sis.Shared` and `Elsasa.Sis.Contracts`. Direct assembly references to sibling domains (`Students`, `Finance`, `Operations`, `Academics`) are strictly prohibited.
   - Cross-domain type dependencies are verified by NetArchTest in `AssemblyDependencyTests.cs`.
   - Schema isolation: `teaching` schema owns all Teaching tables. Zero physical foreign keys across domain schemas; external references (e.g., campus strings, user identifiers) are stored as logical strings/UUIDs.
2. **Domain Boundary & Identity Decoupling (`teaching-domain.md`):**
   - A Teaching Profile is not an HR employee record. Non-employee teaching resources (contractors, visiting lecturers, external assessors, volunteers) must be supported with nullable `employee_ref` and unique `national_id_or_external_ref`.
   - Capability describes potential suitability only; it does not authorize teaching or commit capacity.
   - Qualification verification confirms document authenticity; it does not automatically grant teaching authorization.
3. **Governance & Decision Invariant (`teaching-domain.md`, `actors.md`):**
   - Authorizations must define explicit scopes (campus, program, level, delivery mode) and effective dates.
   - Auditable immutability: approved Teaching records are never silently updated in place. Approved corrections must preserve before/after values, justification, and approval authority in `teaching_record_corrections` (`TCH-WF-12`).

---

# 3. Gap Analysis

| ID | Severity | Gap | Status | Resolution |
|------|------|------|:---:|------|
| `GAP-001` | CRITICAL | **Missing Backend Project `Elsasa.Sis.Teaching`:** No Teaching domain project exists in the solution. | **CLOSED** | Scaffold `src/backend/Elsasa.Sis.Teaching` referencing `Elsasa.Sis.Shared` and `Elsasa.Sis.Contracts`; wire into `Elsasa.Sis.sln` and register `AddTeachingModule()` in `Elsasa.Sis.Api`. Update architecture tests in `AssemblyDependencyTests.cs` and `TestAssemblies.cs`. |
| `GAP-002` | CRITICAL | **Missing Database Schema and Tables in `teaching`:** Isolated schema `teaching` and the 9 Module 1 tables plus record corrections table are not provisioned. | **CLOSED** | Author DbUp migration script `20261110_0501_Teaching_TeacherProfile_Tables.sql` idempotently provisioning schema `teaching`, 9 Module 1 tables, and `teaching.teaching_record_corrections` (table 2.15) with intra-schema foreign keys, indexes, and unique constraints. |
| `GAP-003` | MAJOR | **Teacher Profile Creation & Standing Lifecycle (`TCH-WF-01`, `UC-TCH-02`):** Need clear lifecycle supporting candidate intake, authority approval, and standing states without conflating HR data. | **CLOSED** | Adopted **Candidate Intake with Authority Approval**: support standing lifecycle `pending_approval`, `active`, `restricted`, `suspended`, `inactive`. Teaching Coordinator can register candidate profiles (`pending_approval`), appearing in the `SCR-TCH-01` queue until Teaching Authority formally executes `ApproveTeacherProfileCommand`. |
| `GAP-004` | MAJOR | **Qualification Verification vs Teaching Authorization Gating (`TCH-WF-02`, `TCH-WF-03`, `UC-TCH-09`):** Balance strict qualification verification with operational flexibility during real-world faculty onboarding. | **CLOSED** | Adopted **Permissive Guidance Gate**: allow granting provisional/conditional authorizations even if qualification evidence is in `submitted` state, stamping an explicit `unverified_credential_warning` flag on the authorization and notifying Teaching Authority. Mandate supervision requirements for `authorized_under_supervision`. |
| `GAP-005` | MAJOR | **Auditable Record Corrections Workflow (`TCH-WF-12`, `UC-TCH-12`):** Approved records must not be silently overwritten, but corrections must not cause undue bureaucratic friction. | **CLOSED** | Adopted **Role-Aware Direct Application**: Teaching Authority can apply corrections directly in a single atomic transaction (logging diff, reason, and approval in `teaching_record_corrections`), while non-authority coordinators submit a `requested` correction requiring Teaching Authority approval before application. |
| `GAP-006` | MAJOR | **Missing Frontend Workspace (`SCR-TCH-01` Faculty Resource Workspace):** No UI exists for Teaching Coordinators and Authorities to manage faculty profiles, verify credentials, grant authorizations, and audit corrections. | **CLOSED** | Adopted **Unified Work Queue & Multi-Tab Workbench (`/teaching/faculty`)**: implement `SCR-TCH-01` with a filterable left worklist (All Faculty, Pending Profile Approval, Pending Verification, Pending Authorization, Corrections) and a 5-tab main workbench (*Profile*, *Capabilities*, *Qualifications*, *Authorizations*, *Corrections Audit*). |
| `GAP-007` | MAJOR | **Cross-Domain Contracts & Domain Events (`ITeachingModule`):** Academics and Operations need contract inquiries and domain events for teacher authorization status and profiles. | **CLOSED** | Adopted **Targeted Contract & Domain Events**: expose `ITeachingModule` on `Elsasa.Sis.Contracts` (`GetTeacherProfileAsync`, `IsTeacherAuthorizedAsync`, `GetActiveTeachersAsync`) and publish MediatR domain events for profile approval, standing changes, and authorization state changes. |
| `GAP-008` | MAJOR | **Demo Data Generator & Purger Strategy:** Demo data generator must seed realistic Teaching scenarios for Hagios personas and clean up in reverse topological order. | **CLOSED** | Adopted **Multi-Persona Realism Scenario**: seed 4 Hagios personas (Budi Hartono - senior permanent; Siti Nurhaliza - contract under supervision; John Miller - visiting non-employee; Hendra Setiawan - pending candidate); wire `ITeachingDemoDataSeeder` and `ITeachingDemoDataPurger` into `DemoDataOrchestrator`. |

---

# 4. Open Questions

| ID | Question | Impact | Status | Decision Summary |
|------|------|------|:---:|------|
| `OQ-001` | How should auditable record corrections (`TCH-WF-12` / `UC-TCH-12`) and the database schema for Module 1 be provisioned? | Dictates schema scope, auditability, and migration stability. | **CLOSED** | **Provision 9 Module 1 Tables + `teaching_record_corrections` (Table 2.15):** Author initial migration `20261110_0501_Teaching_TeacherProfile_Tables.sql` provisioning all 9 Module 1 tables and the generic `teaching_record_corrections` table to explicitly log before/after deltas, rationale, and authority approval for finalized profiles and authorizations. |
| `OQ-002` | How should the Teacher Profile creation and standing lifecycle be handled? | Dictates coordinator intake workflow, approval gate, and standing states. | **CLOSED** | **Candidate Intake with Authority Approval (`pending_approval`):** Support standing states `pending_approval`, `active`, `restricted`, `suspended`, `inactive`. Teaching Coordinator registers profiles into `pending_approval` for review in `SCR-TCH-01`, transitioning to `active` upon Teaching Authority approval. |
| `OQ-003` | How strictly should Teaching Authorization be gated by Qualification Evidence verification and role policy? | Dictates onboarding flexibility vs qualification enforcement. | **CLOSED** | **Permissive Guidance Gate:** Allow granting authorizations even if qualification evidence is in `submitted` state, stamping an explicit `unverified_credential_warning` flag on the authorization and alerting Teaching Authority. For `authorized_under_supervision`, a supervision requirement is mandatory. |
| `OQ-004` | How should the auditable correction workflow (`TCH-WF-12` / `UC-TCH-12`) be executed? | Dictates authority UX, approval steps, and data integrity. | **CLOSED** | **Role-Aware Direct Application:** Teaching Authority can apply corrections directly in a single atomic transaction (logging diff, reason, and approval in `teaching_record_corrections`), while non-authority coordinators submit a `requested` correction requiring Teaching Authority approval to apply. |
| `OQ-005` | How should the Frontend UI (`SCR-TCH-01` Faculty Resource Workspace) be structured? | Dictates user experience, routing, and role ergonomics. | **CLOSED** | **Unified Work Queue & Multi-Tab Workbench (`/teaching/faculty`):** Left sidebar features queue filters (All Faculty, Pending Profile Approval, Pending Verification, Pending Authorization, Corrections) and search; main panel provides a teacher header with standing badges and 5 focused tabs (*Profile*, *Capabilities*, *Qualifications*, *Authorizations*, *Corrections Audit*). |
| `OQ-006` | Which demo personas and scenarios should be seeded by the Data Generator to support feature demonstration? | Dictates persona realism, demonstration quality, and database reset integrity. | **CLOSED** | **Multi-Persona Realism:** Seed 4 distinct Hagios personas: (1) Senior Permanent Teacher (Budi Hartono - active authorization, verified Serdik), (2) Contract Teacher under Supervision (Siti Nurhaliza - conditional authorization, co-teaching mentor assigned), (3) Non-Employee Visiting Teacher (John Miller - null employee_ref, passport identity), and (4) Pending Onboarding Candidate (Hendra Setiawan - pending profile approval & submitted credential awaiting verification). |
| `OQ-007` | How should the cross-domain contract surface (`ITeachingModule`) and domain events be established in `Elsasa.Sis.Contracts`? | Dictates downstream inter-module communication and decoupling. | **CLOSED** | **Targeted Inquiry Contract & MediatR Domain Events:** Expose `ITeachingModule` with `GetTeacherProfileAsync`, `IsTeacherAuthorizedAsync`, and `GetActiveTeachersAsync`, plus publish MediatR domain events for profile approval, standing changes, and authorization state changes. |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| `ASM-001` | The database engine is SQL Server 2022+ with isolated schemas managed via DbUp migrations (`Elsasa.Sis.Database`). |
| `ASM-002` | `Elsasa.Sis.Teaching` follows the exact architectural blueprint of `Academics`, `Students`, and `Finance`: Minimal APIs, Dapper repositories, MediatR handlers, and FluentValidation (ADR-004, ADR-005, ADR-006). |
| `ASM-003` | Non-employee teaching resources are fully supported by keeping `employee_ref` nullable while guaranteeing identity uniqueness via `national_id_or_external_ref`. |
| `ASM-004` | Teaching capabilities describe potential suitability across subject domains, grade stages, delivery modes, and languages, independent of specific curriculum frameworks. |
| `ASM-005` | Qualification verification confirms document authenticity; teaching authorization is a distinct institutional decision. |
| `ASM-006` | Teaching Coordinator maintains profiles and verifies credentials; Teaching Authority grants authorizations, approves profiles, and authorizes record corrections. |
| `ASM-007` | Downstream teaching demand intake, assignment allocation, availability scheduling, and substitutions belong to Module 2 (`Assignment & Workload`) and are deferred. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| `RISK-001` | Unauthorized or unqualified teachers assigned to classes during downstream scheduling. | High | Enforce authorization queries (`IsTeacherAuthorizedAsync`) validating active standing, authorization status (`active` or `authorized_under_supervision`), and effective date ranges before downstream assignment can occur. |
| `RISK-002` | Silent overwrites or unrecorded changes to approved teacher qualifications or authorizations. | High | Adopt `teaching_record_corrections` (table 2.15): all edits to approved records log before/after deltas, justification rationale, and approving authority. |
| `RISK-003` | Foreign key constraint failures during demo data reset/purge. | High | Wire `ITeachingDemoDataPurger` to delete in strict reverse-dependency order: `teaching_record_corrections` → `supervision_requirements` → `authorization_evidence` → `teaching_authorizations` → `qualification_evidence` → `teaching_capabilities` → `teacher_profiles` → `qualification_types` → `capability_types` → `teaching_roles`. |
| `RISK-004` | Cross-domain architecture test violations when introducing `Elsasa.Sis.Teaching`. | Medium | Update `TestAssemblies.cs` and `AssemblyDependencyTests.cs` to include `Elsasa.Sis.Teaching` and enforce ADR-001/ADR-010 project reference rules. |

---

# 7. Recommendations

## Persistence Scope & Record Corrections (GAP-002 / OQ-001)

### Option A: 9 Module 1 Tables + `teaching_record_corrections` (Table 2.15) — Recommended
- Provision `teaching.teaching_roles`, `teaching.teacher_profiles`, `teaching.capability_types`, `teaching.teaching_capabilities`, `teaching.qualification_types`, `teaching.qualification_evidence`, `teaching.teaching_authorizations`, `teaching.authorization_evidence`, `teaching.supervision_requirements`, and `teaching.teaching_record_corrections`.
- **Pragmatic, Minimalist, Explicit:** Direct alignment with `teaching-entities-tables.md`, zero future migration churn, and first-class support for `TCH-WF-12` "No Silent Overwrites".

---

## Teacher Profile Lifecycle & Standing (GAP-003 / OQ-002)

### Option A: Candidate Intake with Authority Approval (`pending_approval`) — Recommended
- Teacher profile standing supports: `pending_approval`, `active`, `restricted`, `suspended`, `inactive`.
- Teaching Coordinator registers candidate profiles into `pending_approval`.
- Teaching Authority reviews and executes `ApproveTeacherProfileCommand`, setting `standing = 'active'`, `approved_by`, and `approved_date`.
- **Pragmatic & Explicit:** Reflects school onboarding reality while maintaining clear separation between coordinator intake and authority approval.

---

## Qualification Verification vs Teaching Authorization Gating (GAP-004 / OQ-003)

### Option A: Permissive Guidance Gate — Recommended
- Allows granting provisional/conditional authorizations even if qualification evidence is in `submitted` state, stamping an explicit `unverified_credential_warning` flag and notifying Teaching Authority.
- When granted under `authorized_under_supervision`, a linked `supervision_requirements` record is mandatory.
- **Pragmatic & Minimalist:** Prevents administrative gridlock during real-world faculty onboarding while ensuring complete visibility into unverified credentials.

---

## Auditable Record Corrections Execution (GAP-005 / OQ-004)

### Option A: Role-Aware Direct Application — Recommended
- Teaching Authority can apply corrections directly in a single atomic transaction: fetches original value, logs `teaching_record_corrections` with `status = 'applied'`, and updates the target record field.
- Non-authority coordinators submit a `requested` correction requiring Teaching Authority approval.
- **Pragmatic & Explicit:** Zero bureaucratic delay for authorized school leaders while maintaining 100% auditable traceability.

---

## Frontend UX Architecture (GAP-006 / OQ-005)

### Option A: Unified Work Queue & Multi-Tab Workbench (`/teaching/faculty`) — Recommended
- Scaffold route `/teaching/faculty` mapped to `FacultyResourceWorkspaceView.vue`.
- Left Sidebar: Work Queue Filter (`All Faculty`, `Pending Profile Approval`, `Pending Verification`, `Pending Authorization`, `Corrections`) + Search (name, teacher number, NIK).
- Main Workbench:
  - Header: Teacher profile summary banner, standing badge, quick action bar (`Approve Profile`, `Change Standing`, `Audit Correction`).
  - Tab 1: **Profile & Affiliation** (legal name, display name, roles, campus, department, contact, NIK, employee ref).
  - Tab 2: **Teaching Capabilities** (categorized list, add capability modal).
  - Tab 3: **Qualification Evidence** (credentials list, document links, verification badges, verify/reject actions).
  - Tab 4: **Teaching Authorizations & Supervision** (authorizations list, scope pills, supervision cards, grant authorization modal).
  - Tab 5: **Audit & Corrections History** (chronological before/after diffs from `teaching_record_corrections`).
- **Role-Aligned & Cohesive:** Delivers a complete, ergonomic workplace matching `screen-inventory.md` (`SCR-TCH-01`).

---

## Demo Data Generator Integration (GAP-008 / OQ-006)

### Option A: Multi-Persona Realism Scenario — Recommended
- Wire `ITeachingDemoDataSeeder` and `ITeachingDemoDataPurger` into `DemoDataOrchestrator.cs`.
- Seed 4 distinct Hagios personas:
  1. **Budi Hartono, M.Pd.** (Senior Permanent Teacher): `Guru Tetap`, Kampus Utama, active standing, verified Serdik & S1 Matematika, active authorization for Matematika SD Kelas 4–6.
  2. **Siti Nurhaliza, S.Pd.** (Contract Teacher under Supervision): `Guru Kontrak`, Kampus Utama, active standing, verified S1 Bahasa Inggris, authorized under supervision for Bahasa Inggris SMP with senior mentor Budi Hartono.
  3. **John Miller, B.Ed.** (Visiting Non-Employee Teacher): `Guru Tamu`, Kampus Utama, null employee_ref, passport ID, verified Cambridge CELTA, active authorization for English Immersion.
  4. **Hendra Setiawan** (Pending Candidate): `Guru Kontrak`, pending_approval standing, submitted coaching certificate awaiting verification.
- Update `DemoDataOrchestrator`: forward seeding (Operations → Teaching → Academics Structure → ...) and reverse purge (... → Teaching → Operations).

---

# 8. Gap Closure

### GAP-001: Missing Backend Project `Elsasa.Sis.Teaching`
- **Decision:** Scaffold `src/backend/Elsasa.Sis.Teaching` as a .NET 9 class library referencing `Elsasa.Sis.Shared` and `Elsasa.Sis.Contracts`. Wire into `Elsasa.Sis.sln`, register in `Elsasa.Sis.Api`, and add to `AssemblyDependencyTests.cs`.
- **Rationale:** Strictly enforces ADR-001, ADR-002, and ADR-010 domain boundaries.
- **Impact:** Establishes the authoritative Teaching domain codebase.

### GAP-002: Missing Database Schema and Tables in `teaching`
- **Decision:** Author DbUp migration `20261110_0501_Teaching_TeacherProfile_Tables.sql` provisioning isolated schema `teaching`, the 9 Module 1 tables, and `teaching_record_corrections`.
- **Rationale:** Adheres to schema isolation (ADR-002) and prepares all required tables in a single cohesive migration.
- **Impact:** Database schema fully provisioned for Module 1.

### GAP-003: Teacher Profile Creation & Standing Lifecycle
- **Decision:** Support standing lifecycle `pending_approval`, `active`, `restricted`, `suspended`, `inactive`. Teaching Coordinator registers candidate profiles into `pending_approval`; Teaching Authority approves to activate.
- **Rationale:** Clarifies workflow boundary between coordinator intake and authority governance.
- **Impact:** Establishes clear intake queue in `SCR-TCH-01`.

### GAP-004: Qualification Verification vs Teaching Authorization Gating
- **Decision:** Adopt Permissive Guidance Gate: authorizations may be granted with evidence in `submitted` state, stamping `unverified_credential_warning` flag. Supervision requirements mandatory for `authorized_under_supervision`.
- **Rationale:** Balances regulatory compliance with real-world onboarding flexibility.
- **Impact:** Prevents operational paralysis while maintaining explicit compliance warnings.

### GAP-005: Auditable Record Corrections Workflow
- **Decision:** Role-Aware Direct Application: Teaching Authority applies corrections directly in a single atomic transaction; non-authority coordinators submit requests.
- **Rationale:** Minimizes administrative overhead for authorized leaders while enforcing ADR immutability invariants.
- **Impact:** Transparent, tamper-evident audit history in `teaching_record_corrections`.

### GAP-006: Missing Frontend Workspace (`SCR-TCH-01`)
- **Decision:** Implement unified Work Queue & Multi-Tab Workbench at `/teaching/faculty` with queue filters and 5 focused tabs (*Profile*, *Capabilities*, *Qualifications*, *Authorizations*, *Corrections Audit*).
- **Rationale:** Matches screen inventory specifications and ergonomic standards of existing workspaces.
- **Impact:** High-productivity workbench for Teaching Coordinators and Authorities.

### GAP-007: Cross-Domain Contracts & Domain Events
- **Decision:** Expose `ITeachingModule` in `Elsasa.Sis.Contracts` with `GetTeacherProfileAsync`, `IsTeacherAuthorizedAsync`, and `GetActiveTeachersAsync`, plus MediatR domain events.
- **Rationale:** Enables loose coupling and asynchronous notifications for downstream Academics and Operations modules.
- **Impact:** Clear cross-domain integration points.

### GAP-008: Demo Data Generator & Purger Strategy
- **Decision:** Implement `ITeachingDemoDataSeeder` and `ITeachingDemoDataPurger` seeding 4 Hagios personas and cleaning up in reverse topological order.
- **Rationale:** Guarantees repeatable demo data generation and clean database resets without foreign key violations.
- **Impact:** Immediate, realistic verification for stakeholders.

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

This change introduces:
1. A completely new domain boundary: `Elsasa.Sis.Teaching` backend project, `teaching` database schema, and `teaching` frontend feature module.
2. 10 new database tables across core, reference, junction, and supporting categories.
3. Cross-domain contracts and MediatR domain events consumed by downstream modules.
4. Core business lifecycle transitions and auditable correction patterns.

A formal Target Architecture artifact (`TCH-TEACHER-PROFILE-AUTHORIZATION-ARCHITECTURE.md`) is required to define project structure, vertical feature slices, Dapper query contracts, Minimal API routes, frontend components, and migration specifications before implementation planning begins.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture applicability determined (`ARCHITECTURE-REQUIRED`)
- [x] Architecture can be finalized or updated based on approved decisions

## Status

**READY-FOR-PLANNING**

## Notes

All architectural gaps, open questions, and domain integration points have been rigorously assessed and closed with approved pragmatic, minimalist, and explicit decisions. Target architecture and implementation planning can proceed immediately.

---

# 11. References

Referenced artifacts:
- **DOMAIN:** [teaching-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/teaching-domain.md)
- **ENTITIES:** [teaching-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/teaching-entities.md), [teaching-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/teaching-entities-tables.md)
- **DISCOVERY:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md)
- **USE CASES:** [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md)
- **ACTORS:** [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md)
- **SCREENS:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md)
- **ROADMAP:** [sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md)

Referenced codebase locations:
- `src/backend/Elsasa.Sis.Database/Scripts/`
- `src/backend/Elsasa.Sis.Contracts/`
- `src/backend/Elsasa.Sis.Api/Features/DemoData/`
- `src/frontend/src/features/`
- `tests/backend/Elsasa.Sis.ArchitectureTests/`
