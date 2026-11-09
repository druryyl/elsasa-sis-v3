---
Title: Feasibility Assessment — SIS Demo Sample Data Generator (Hagios School of Life)
Code: CR-SYS-SAMPLE-DATA-GENERATOR
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-05
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Implementation of a comprehensive, business-compliant sample data generation capability for the Elsasa School Information System (SIS) as requested in [IS011 - DEMO-SAMPLE-DATA-GENERATOR-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS011%20-%20DEMO-SAMPLE-DATA-GENERATOR-ISSUE.md).

The sample data generator enables product evaluators, sales presenters, and training leads to showcase Elsasa SIS end-to-end to prospective clients. The generated dataset must be coherent, realistic, culturally grounded, and strictly compliant with all domain invariants, state lifecycles, and referential integrity rules established across the four implemented SIS domains: **Operations**, **Students**, **Finance**, and **Academics**.

Referenced artifacts:
- **ISSUE:** [IS011 - DEMO-SAMPLE-DATA-GENERATOR-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS011%20-%20DEMO-SAMPLE-DATA-GENERATOR-ISSUE.md)
- **DOMAINS:** 
  - [operational-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/operational-domain.md)
  - [student-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/student-domain.md)
  - [finance-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/finance-domain.md)
  - [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md)
- **ENTITIES:** 
  - [operational-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/operational-entities.md), [operational-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/operational-entities-tables.md)
  - [student-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/student-entities.md), [student-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/student-entities-tables.md)
  - [finance-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/finance-entities.md), [finance-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/finance-entities-tables.md)
  - [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md), [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md)
- **DISCOVERY & WORKFLOWS:** [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md), [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md), [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md), [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md)
- **ARCHITECTURE & ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md) (ADR-001 through ADR-010), [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/backend-architecture.md), [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/frontend-architecture.md)

## Objective

Evaluate technical feasibility, architectural placement, domain invariants, and implementation requirements for delivering a robust demo data generation and reset facility tailored to **Hagios School of Life, Yogyakarta**, operating under an **Accelerated Christian Education (ACE) / PACE-based learning model**.

---

# 2. Current State

## Existing Behavior

1. **Database Schema & Migrations:**
   - SQL Server 2022 database schema contains isolated schemas: `operations`, `students`, `finance`, and `academics`.
   - All migrations up to Stage 2 Assessment & Results (`20261105_0402_Academics_AssessmentResults_Tables.sql`) are established.
   - The database contains full relational tables, check constraints, indexes, and status enum constraints for all 4 domains.
   - The database schema natively supports ACE PACE structures via `academics.learning_units.unit_type = 'pace'`, self-referencing parent-child unit hierarchies, multiple reassessment policies, and applicant entrance evaluation linkage.
2. **Backend Architecture & Slices:**
   - Modular monolith with compile-time encapsulation (ADR-001): `Elsasa.Sis.Operations`, `Elsasa.Sis.Students`, `Elsasa.Sis.Finance`, and `Elsasa.Sis.Academics` reference only `Shared` and `Contracts`.
   - Pragmatic schema write isolation (ADR-002): Commands write strictly to their owning schema; cross-schema physical foreign keys are prohibited.
   - Host `Elsasa.Sis.Api` aggregates all domains and routes endpoints.
   - `Program.cs` currently supports `--migrate` for standalone out-of-band schema migrations, but has no seed or demo generation option.
   - Integration tests use isolated helpers (`FinanceIntegrationTestHelpers.cs`) and Respawn for ad-hoc test isolation, but no unified multi-domain demo generator exists.
3. **Authentication & Session Mocking:**
   - `LoginEndpoint.cs` issues mock JWT tokens based on username. It currently only recognizes "admin" / "superadmin" (SuperAdmin role) and generic staff (AcademicStaff/FinanceStaff).
   - There are no user accounts or claims for Learning Center Supervisors/Teachers, Admission Officers, Finance Officers, or specific Student/Parent portal users.
4. **Frontend Implementation:**
   - Rich workbenches exist for Operations (`CalendarWorkspaceView.vue`), Students (`Admission`, `Enrollment`, `Placement`), Finance (`Billing`, `Payments`), Academics (`Curriculum`, `Participation`, `Sessions`, `Coursework`, `Records`), and Portals (`StudentLearningWorkspaceView.vue`, `FamilyTasksView.vue`).
   - The UI currently has no trigger or status display for resetting or generating demo datasets.

## Existing Constraints

1. **Domain Assembly Isolation (ADR-001):**
   - Domain class libraries must not reference each other. Seeding logic for Students cannot directly instantiate or call Finance entities.
2. **Schema Write Boundaries (ADR-002):**
   - Seeding routines must respect schema write boundaries: Operations writes to `operations.*`, Students writes to `students.*`, Finance writes to `finance.*`, and Academics writes to `academics.*`.
3. **Production Data Protection:**
   - Purge and re-seeding operations must never execute against a production database.
4. **Referential Integrity & Temporal Consistency:**
   - Generated records must preserve logical referential integrity across schemas (UUIDs) and chronological validity (e.g., invoices before payments, attendance during operational term dates, assignments within unit dates, entrance exams before admission decisions).

---

# 3. Gap Analysis

| ID | Severity | Gap | Status | Resolution |
|------|------|------|:---:|------|
| `GAP-001` | CRITICAL | **Missing Automated Demo Seeding & Reset Capability:** No facility exists to generate a coherent multi-domain school dataset or restore a pristine demo state. Manual data entry is slow, incomplete, and produces broken referential relationships. | **CLOSED** | Adopted **Domain-Encapsulated Seeders with API Host Orchestration**: Each domain assembly provides a dedicated seeder/purger; the host orchestrates topological execution and exposes both a CLI startup flag and an admin API endpoint. |
| `GAP-002` | CRITICAL | **Domain Encapsulation & Schema Write Isolation (ADR-001, ADR-002):** A monolithic seeder would violate schema ownership and assembly isolation rules if implemented as a single external script writing directly across all schemas. | **CLOSED** | Implement internal domain seeders behind a shared contract (`IDemoDataDomainSeeder` / `IDemoDataDomainPurger`) inside each domain assembly (`Operations`, `Students`, `Finance`, `Academics`). Each seeder writes exclusively to its own schema using Dapper. |
| `GAP-003` | CRITICAL | **Missing Multi-Role Demo Accounts & Portal Credentials:** Presenters cannot demonstrate role-specific screens (Teacher/Supervisor, Admission, Finance, Student, Parent) because `LoginEndpoint` only supports `admin` and generic `staff`. | **CLOSED** | Extend `LoginEndpoint` with curated demo user accounts (`admin`, `admission`, `finance`, `supervisor`, `student.jonathan`, `parent.jonathan`) with a standardized password (`Demo123!`) and correct claims linking to generated domain records. |
| `GAP-004` | MAJOR | **ACE PACE Curriculum & Learning Model Representation:** The system requires realistic modeling of Hagios School of Life's PACE-based individualized curriculum (subjects as parent units, PACE booklets as child units, PACE tests, mastery threshold, supervisor roles). | **CLOSED** | Provision a two-tier learning structure in Academics: 5 core subjects (Math, English, Science, Social Studies, Bible) with child PACE units (`unit_type = 'pace'`), checkups, self-tests, summative PACE tests, and retakes. |
| `GAP-005` | MAJOR | **Operational Lifecycle Timeline & Live Demo Hooks:** Presenters need both historical data (to display populated report cards, transcripts, aging charts) and in-flight pending work (to click "Approve Admission", "Receive Payment", "Grade Submission" live). | **CLOSED** | Model an **in-flight mid-year state**: 1 completed historical semester (with certified report cards, transcripts, and paid ledgers) + 1 active ongoing semester (with 1 pending applicant decision, 1 un-graded PACE test, 1 pending score approval, 1 overdue invoice, 1 unpaid invoice ready to receive payment). |
| `GAP-006` | MAJOR | **Safe Multi-Schema Purge Routine:** No safe procedure exists to wipe the database clean of previous demo mutations without violating foreign key constraints or risking production data. | **CLOSED** | Implement a coordinated purge service executing reverse-dependency deletion (`academics` → `finance` → `students` → `operations`) with an explicit environment guard (`!Environment.IsProduction()`) and configuration gate. |
| `GAP-007` | MAJOR | **Temporal Drift & Dynamic Date Anchoring:** Static calendar dates (e.g. fixed 2026 dates) age quickly, causing demo calendars to drift into the past over time. | **CLOSED** | Implement **dynamic date anchoring relative to execution date (`Today`)**: dynamically place `Today` mid-way through the active semester, with the prior semester in the past and future dates ahead. |
| `GAP-008` | MINOR | **Missing Frontend SuperAdmin Reset Action:** Presenters demonstrating in a web browser must leave the presentation to trigger Swagger or a terminal to reset data between sessions. | **CLOSED** | Add a "Reset Demo Data" action with confirmation dialog in the `AppLayout.vue` user profile menu, visible strictly to SuperAdmin in non-Production environments. |

---

# 4. Open Questions

| ID | Question | Impact | Status | Decision Summary |
|------|------|------|:---:|------|
| `OQ-001` | What operational lifecycle timeline should the generated demo dataset represent? | Determines historical depth, active entities, and presenter workflow interactivity. | **CLOSED** | **In-flight mid-year state with historical context:** 1 completed past semester (to showcase historical transcripts, archived report cards, and settled financial ledgers) plus 1 active ongoing semester (with live applicants, unpaid/partial invoices, active class sessions, and pending grading for live walkthroughs). |
| `OQ-002` | What institutional profile and grade level structure should the demo model? | Shapes curriculum, subjects, student grouping, tuition policies, and naming conventions. | **CLOSED** | **Hagios School of Life, Yogyakarta (ACE / PACE Model):** Multi-tier representation across Elementary, Junior High, and High School Learning Centers (LCs), using ACE individualized PACE learning rather than Kurikulum Merdeka. |
| `OQ-003` | What student population scale and data volume should the generator produce? | Dictates generation speed, UI responsiveness, and dashboard density. | **CLOSED** | **Balanced demo scale (~40–60 enrolled students, 6–10 applicants):** ~15–20 students per Learning Center tier. Dense enough for charts, pagination, and diverse personas; fast enough to generate in under 5 seconds. |
| `OQ-004` | How should presenters and operators trigger demo data generation and reset? | Determines CLI integration, API endpoints, and operational ergonomics. | **CLOSED** | **Dual CLI command & Admin API endpoint:** CLI startup argument (`--seed-demo`) on `Elsasa.Sis.Api` for automated environments, and authenticated Minimal API endpoint (`POST /api/operations/demo-data/reset`) guarded by SuperAdmin role and non-Production check. |
| `OQ-005` | What data cleanup and reset strategy should be applied? | Governs database cleanliness, idempotency, and data safety. | **CLOSED** | **Clean purge & repopulate with strict environment guard:** Reverse-dependency deletion of domain schema tables (`academics` → `finance` → `students` → `operations`) before repopulating pristine demo data. Hard-blocked on `Production`. |
| `OQ-006` | Should demo data be deterministic with curated personas or randomly generated? | Affects demo predictability, training documentation, and sales scripts. | **CLOSED** | **Deterministic with curated flagship personas:** Fixed seed and named student/family personas (e.g. Jonathan Pratama for high honors, Grace Santoso for overdue tuition and PACE retake, Budi Wijaya for pending admission) paired with realistic Yogyakarta cultural naming. |
| `OQ-007` | What user accounts and role personas should be available for login? | Governs role demonstration across workbenches and self-service portals. | **CLOSED** | **Multi-role demo accounts with portal personas:** Standardized password (`Demo123!`) for SuperAdmin (`admin`), Admission Officer (`admission`), Finance Officer (`finance`), Learning Center Supervisor (`supervisor`), Student (`student.jonathan`), and Parent (`parent.jonathan`). |
| `OQ-008` | What fee structure and payment scenario distribution should be modeled? | Dictates charge rules, invoice cycles, receivables aging, and cashier demos. | **CLOSED** | **Comprehensive realistic mix:** Monthly Tuition (SPP), Annual Facility Fee, and PACE Material Fee. Semester 2 payment distribution: 60% paid, 20% partial, 10% overdue receivable, 10% freshly issued unpaid (for live payment demonstration), plus 1 scholarship scenario. |
| `OQ-009` | How should the ACE PACE curriculum hierarchy and evaluation scenarios be structured? | Governs learning units, rubrics, retake audit trails, and report cards. | **CLOSED** | **Two-tier Subject & PACE hierarchy with live demonstration hooks:** Subjects as parent units, PACEs as child units (`unit_type = 'pace'`). Includes 1 submitted test awaiting teacher grading, 1 score awaiting coordinator approval, 1 PACE retake showing remediation history, and certified Semester 1 report cards / transcripts. |
| `OQ-010` | How should applicant records and student lifecycle statuses be staged? | Determines admission pipeline demonstration and student status tracking. | **CLOSED** | **Staged lifecycle pipeline with ready-to-act hooks:** Applicants pre-staged at each discrete gate (1 `submitted`, 1 `under_review`, 1 entrance-assessed awaiting decision, 1 `accepted` awaiting fee, 1 `accepted` with fee paid ready for 1-click enrollment, 1 `rejected`/`withdrawn`), plus enrolled students with status variations (`active`, `on_leave`, `transferred`). |
| `OQ-011` | How should calendar dates and academic year timelines be anchored? | Governs long-term validity of demo calendars and prevents staleness. | **CLOSED** | **Dynamically anchored relative to execution date (`Today`):** Calculate dates dynamically around `Today` so that `Today` falls squarely mid-term in the active semester, ensuring deadlines and dates always appear live and relevant. |
| `OQ-012` | How should the generator be architected across solution assemblies? | Preserves ADR-001 compile-time isolation and ADR-002 schema write boundaries. | **CLOSED** | **Domain-encapsulated seeders with API host orchestration:** Each domain assembly implements internal seeding/purging for its schema; `Elsasa.Sis.Api` orchestrates the topological dependency sequence and exposes CLI/API endpoints. |
| `OQ-013` | Should the frontend include a UI control for triggering demo data reset? | Dictates UI presentation convenience during live client demos. | **CLOSED** | **Include a SuperAdmin UI action in the frontend:** A "Reset Demo Data" button with confirmation dialog in the top-bar profile menu, visible only when logged in as SuperAdmin in non-Production environments. |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| `ASM-001` | The database engine is Microsoft SQL Server 2022+ with all Stage 1 and Stage 2 DbUp schema migrations through `IS010` fully applied. |
| `ASM-002` | Hagios School of Life operates in Yogyakarta, Indonesia, utilizing an Accelerated Christian Education (ACE) PACE-based individualized learning model. |
| `ASM-003` | Core demo operations write directly through Dapper with explicit T-SQL per ADR-004, maintaining compile-time assembly isolation per ADR-001 and schema write boundaries per ADR-002. |
| `ASM-004` | All demo seed and reset capabilities are strictly disabled in `Production` environments (`app.Environment.IsProduction()`) and guarded by the `SuperAdmin` policy. |
| `ASM-005` | Standardized demo credentials use the password `Demo123!` to enable seamless multi-role switching without external email verification or password reset flows. |
| `ASM-006` | Dates and operational periods are dynamically anchored relative to `DateTime.UtcNow` at generation time, placing `Today` squarely mid-way through the active term. |
| `ASM-007` | The primary currency modeled is Indonesian Rupiah (`IDR`) with realistic private Christian school fee structures. |

---

# 6. Risks

| ID | Risk | Impact | Likelihood | Mitigation |
|------|------|------|:---:|------|
| `RISK-001` | **Accidental Data Purge on Production:** Presenter or administrator triggers reset on a live production environment, destroying actual institutional data. | Critical | Low | Implement multi-layer defense: compile-time environment check (`if (app.Environment.IsProduction()) return Forbid();`), configuration gate (`DemoData:Enabled`), explicit API header/payload confirmation, and UI confirmation modal. |
| `RISK-002` | **Violation of Architecture Boundaries (ADR-001, ADR-002):** A cross-domain monolithic seeder directly updates tables in multiple schemas, eroding the modular monolith architecture. | High | Low | Enforce strict domain-encapsulated seeders: `Operations` seeds `operations.*`, `Students` seeds `students.*`, `Finance` seeds `finance.*`, and `Academics` seeds `academics.*`. Slices are orchestrated by `Api` using strongly typed context DTOs. |
| `RISK-003` | **Temporal Drift Rendering Demo Stale:** Static calendar dates cause demo records to appear months or years in the past when demonstrations occur in the future. | Medium | High | Dynamically anchor all dates relative to `DateTime.Today` at the moment of generation, guaranteeing that active classes, invoices, and submissions are always in mid-flight. |
| `RISK-004` | **Foreign Key Constraint Violations During Reset:** Deleting tables in an incorrect order or leaving orphaned records causes SQL constraint exceptions during purge. | High | Medium | Execute purge routines in strict reverse topological dependency order (`academics` → `finance` → `students` → `operations`) within wrapped transaction blocks. |
| `RISK-005` | **Presentation Script Breakage Due to Non-Deterministic Data:** Presenters cannot follow prepared demo scripts because student names, invoice numbers, or test scores change on every seed run. | High | Medium | Enforce deterministic generation using fixed seeds and curated flagship student personas (`Jonathan Pratama`, `Grace Santoso`, `Budi Wijaya`) with known, documented traits. |

---

# 7. Recommendations

## Architectural Organization: Domain-Encapsulated Seeders with API Host Orchestration (OQ-012 / GAP-002)

### Option A: Domain-Encapsulated Seeders with API Host Orchestration — Recommended
- Define domain seeder contracts in `Elsasa.Sis.Contracts` (or internal to domains and registered in DI via marker interfaces):
  - `IOperationsDemoDataSeeder` & `IOperationsDemoDataPurger` in `Elsasa.Sis.Operations`
  - `IStudentsDemoDataSeeder` & `IStudentsDemoDataPurger` in `Elsasa.Sis.Students`
  - `IFinanceDemoDataSeeder` & `IFinanceDemoDataPurger` in `Elsasa.Sis.Finance`
  - `IAcademicsDemoDataSeeder` & `IAcademicsDemoDataPurger` in `Elsasa.Sis.Academics`
- Slices write exclusively to their owning database schema via Dapper:
  - Operations writes to `operations.*`
  - Students writes to `students.*`
  - Finance writes to `finance.*`
  - Academics writes to `academics.*`
- Master orchestrator `DemoDataOrchestrator` in `Elsasa.Sis.Api` coordinates the execution order:
  1. Purge: Academics → Finance → Students → Operations
  2. Seed Operations: Academic Year, Sem 1 (closed), Sem 2 (active), Sem 3 (future), School Calendar, Closures → yields `OperationsSeedContext`
  3. Seed Academics Structure: ACE PACE Learning Structures, parent subject units, child PACE units, grading requirements → yields `AcademicsStructureContext`
  4. Seed Finance Policies: Tuition (SPP), Facility, PACE material fee rules, discount scholarship rules → yields `FinancePolicyContext`
  5. Seed Students: Applicants (staged across 6 gates), Enrolled Students (~50 students across Elementary, Junior High, High School Learning Centers), Guardians, Administrative Placements, Status histories → yields `StudentsSeedContext`
  6. Seed Finance Transactions: Financial Accounts, Obligations, Billing Documents (Invoices), Payments, Allocations (Semester 1 paid in full; Semester 2: 60% paid, 20% partial, 10% overdue, 10% unpaid ready to demo)
  7. Seed Academics Participations & Results: Learning Center sessions, attendance records, PACE submissions, assessments, provisional evaluations, approved results, PACE retakes, certified Semester 1 report cards, and High School transcripts.

#### Advantages
- **100% ADR-001 & ADR-002 Compliant:** Preserves assembly isolation and schema write isolation.
- **Maintainable & Evolutionary:** When domain schemas evolve in future stages (e.g. Stage 3 Teaching or Development), those domains simply implement their own seeders without altering a monolithic script.
- **High Performance:** Uses batched Dapper parameterized inserts; entire dataset generates in under 5 seconds.

#### Disadvantages
- Requires passing context DTOs (e.g., generated UUIDs) between orchestration steps in `Elsasa.Sis.Api`.

---

### Option B: Centralized Monolithic Seeder in `Elsasa.Sis.Api`
- Implement a single large service in `Elsasa.Sis.Api` executing all SQL inserts directly across all four schemas.

#### Disadvantages
- Blatantly violates ADR-002 by allowing the API host to write directly across all schema tables, bypassing domain ownership.

---

## Invocation Interfaces (OQ-004 / OQ-013)

### Option A: Dual CLI Flag + Admin API Endpoint + Frontend UI Action — Recommended
1. **CLI Execution (`Program.cs`):**
   - Command: `dotnet run --project src/backend/Elsasa.Sis.Api -- --seed-demo` (optional: `--force-clean`)
   - Allows CI pipelines, container setups, and local developers to initialize databases out-of-band.
2. **Admin API Endpoint (`DemoDataEndpoints.cs` in `Elsasa.Sis.Api`):**
   - Route: `POST /api/operations/demo-data/reset`
   - Authorization: `RequireAuthorization("SuperAdminPolicy")`
   - Safeguard: Returns `403 Forbidden` if `app.Environment.IsProduction()`.
3. **Frontend UI Action (`AppLayout.vue`):**
   - In the user profile menu, when `authStore.isSuperAdmin` is true and `!isProduction`:
   - Adds a "Reset Demo Data" item with a warning confirmation dialog ("This will restore the pristine Hagios demo dataset").

#### Advantages
- Maximum flexibility for developers, CI runners, and live sales presenters.
- Zero need to open a terminal or Swagger during a live client meeting.

---

# 8. Implementation Impact

```
src/backend/
├── Elsasa.Sis.Api/
│   ├── Program.cs                                # CLI argument handling (--seed-demo)
│   ├── Features/Auth/LoginEndpoint.cs            # Extended multi-role demo accounts
│   ├── Features/DemoData/
│   │   ├── DemoDataEndpoints.cs                  # POST /api/operations/demo-data/reset
│   │   ├── DemoDataOrchestrator.cs               # Sequences purge and domain seeders
│   │   └── DemoDataOptions.cs                    # Config options (seed, scale, personas)
├── Elsasa.Sis.Contracts/
│   └── DemoData/                                 # Seed context models and interfaces
├── Elsasa.Sis.Operations/
│   └── DemoData/                                 # Operations schema seeder and purger
├── Elsasa.Sis.Students/
│   └── DemoData/                                 # Students schema seeder and purger
├── Elsasa.Sis.Finance/
│   └── DemoData/                                 # Finance schema seeder and purger
└── Elsasa.Sis.Academics/
    └── DemoData/                                 # Academics schema seeder and purger

src/frontend/
└── src/app/layouts/
    ├── AppLayout.vue                             # Top-bar reset action & confirmation modal
    └── components/ResetDemoDataDialog.vue        # Dedicated confirmation modal
```

---

# 9. Architecture Applicability

## Decision
**ARCHITECTURE-REQUIRED**

## Rationale
Implementing the Demo Sample Data Generator touches all four active domain bounded contexts (`Operations`, `Students`, `Finance`, `Academics`), defines cross-domain seeding coordination contracts, modifies host authentication and startup mechanics, introduces a public administrative API endpoint and CLI flag, and introduces a UI action in the frontend. 

A formal `ARCHITECTURE` artifact must be produced to define:
1. The exact topological execution sequence and context DTO contracts.
2. The concrete curated personas, Learning Center groupings, and PACE subject structures.
3. The specific Dapper query strategies and reverse-dependency deletion scripts.
4. The exact authentication claims and tokens for the demo persona accounts.

---

# 10. Planning Readiness

## Readiness Checklist
- [x] All critical gaps identified and resolved (`GAP-001` through `GAP-008` CLOSED)
- [x] All open questions thoroughly investigated and decided (`OQ-001` through `OQ-013` CLOSED)
- [x] Institutional profile established (Hagios School of Life, Yogyakarta with ACE / PACE curriculum)
- [x] Balanced scale, determinism, and curated personas established
- [x] Dual invocation (CLI + Admin API) and frontend UI trigger specified
- [x] Multi-schema reverse purge and production safeguards established
- [x] Architecture Applicability decided (`ARCHITECTURE-REQUIRED`)

## Status
**READY-FOR-PLANNING**

## Notes
Feasibility assessment is complete and fully resolved. All decisions adhere strictly to **Pragmatic**, **Minimalist**, and **Explicit** architectural principles. The next step is to author the formal **ARCHITECTURE** artifact for `CR-SYS-SAMPLE-DATA-GENERATOR`.

---

# 11. References

Referenced artifacts:
- **ISSUE:** [IS011 - DEMO-SAMPLE-DATA-GENERATOR-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS011%20-%20DEMO-SAMPLE-DATA-GENERATOR-ISSUE.md)
- **DOMAINS:** 
  - [operational-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/operational-domain.md)
  - [student-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/student-domain.md)
  - [finance-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/finance-domain.md)
  - [academic-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md)
- **ENTITIES:** 
  - [academic-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities.md) (Resolutions Q2, Q4, Q9, Q10, Q13, Q18 for PACE and dual-track support)
  - [operational-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/operational-entities-tables.md)
  - [student-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/student-entities-tables.md)
  - [finance-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/finance-entities-tables.md)
  - [academic-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md)
- **ADRs:** [adr.md](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md) (ADR-001, ADR-002, ADR-004, ADR-005, ADR-009)
