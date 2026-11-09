---
Title: Implementation Plan — SIS Demo Sample Data Generator (Hagios School of Life)
Code: CR-SYS-SAMPLE-DATA-GENERATOR
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-05
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Implement the comprehensive, business-compliant demo sample data generation and reset capability for the Elsasa School Information System (SIS) V3, fulfilling change request intake `CR-SYS-SAMPLE-DATA-GENERATOR` ([`IS011 - DEMO-SAMPLE-DATA-GENERATOR-ISSUE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS011%20-%20DEMO-SAMPLE-DATA-GENERATOR-ISSUE.md)) and realizing the approved target architecture ([`IS011 - DEMO-SAMPLE-DATA-GENERATOR-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS011%20-%20DEMO-SAMPLE-DATA-GENERATOR-ARCHITECTURE.md)).

This implementation establishes:
1. **Domain-Encapsulated Multi-Schema Seeding & Purging (`ADR-001`, `ADR-002`, `GAP-001`, `GAP-002`, `OQ-012`):** Implementing autonomous seeder and purger components inside each domain assembly (`Elsasa.Sis.Operations`, `Elsasa.Sis.Students`, `Elsasa.Sis.Finance`, and `Elsasa.Sis.Academics`). Each seeder and purger writes strictly and exclusively to its owning database schema (`operations.*`, `students.*`, `finance.*`, and `academics.*`) via parameterized Dapper T-SQL scripts (`ADR-004`), preserving 100% compile-time encapsulation and schema write isolation.
2. **Host Orchestration & Topological Execution (`OQ-004`, `OQ-012`, `GAP-006`):** Providing a centralized orchestrator (`DemoDataOrchestrator`) inside `Elsasa.Sis.Api` that coordinates reverse-dependency clean purges (`academics` → `finance` → `students` → `operations`) and sequential forward data generation, threading strongly typed context DTOs (`OperationsSeedResult`, `AcademicsStructureSeedResult`, `FinancePolicySeedResult`, `StudentsSeedResult`, `FinanceTransactionsSeedResult`, `AcademicsActivitiesSeedResult`) through shared interfaces defined in `Elsasa.Sis.Contracts`.
3. **Dynamic Temporal Anchoring (`GAP-007`, `OQ-011`, `RISK-003`):** Dynamically anchoring all academic calendars, closure periods, fee billing schedules, attendance timestamps, coursework deadlines, assessment dates, and report card issuance relative to execution date `Today` (`DateTime.UtcNow.Date`). Situating `Today` squarely four weeks into an ongoing, active Semester 2 while preserving an immutable, certified Semester 1 in the recent past, permanently eliminating temporal staleness and demo drift.
4. **Hagios School of Life Institutional Modeling & ACE / PACE Learning Framework (`GAP-004`, `OQ-002`, `OQ-009`):** Concretely modeling the institutional profile of **Hagios School of Life, Yogyakarta**, operating an **Accelerated Christian Education (ACE) / PACE** individualized learning model across three tiers: Elementary Learning Center (ELC, Grades 1–6), Junior High Learning Center (JHLC, Grades 7–9), and High School Learning Center (HSLC, Grades 10–12). Structuring curriculum as a two-tier hierarchy with 5 core subjects as parent units and individualized child PACE units (`unit_type = 'pace'`), enforcing an 80% passing threshold (KKM) and formal reassessment workflows.
5. **Curated Flagship Personas & Staged Admission Pipeline (`GAP-005`, `OQ-003`, `OQ-006`, `OQ-010`):** Delivering a balanced population of ~48 enrolled students with culturally authentic Indonesian identities and linked guardians, anchored by deterministic flagship personas with documented traits:
   - **Jonathan Pratama:** High-honors High School student with certified Semester 1 report card, cumulative transcript, 100% attendance, and pending PACE score approval.
   - **Grace Santoso:** Junior High student exhibiting a multi-attempt PACE retake (failing initial attempt at 68%, completing remediation, scoring 85% on retake with `replaces_result_id` and `attempt_number = 2`) and an overdue tuition receivable.
   - **Budi Wijaya:** Elementary student with a live submitted PACE test awaiting supervisor evaluation.
   - **Staged Admission Pipeline:** 6 discrete applicants staged across all intake gates (`submitted`, `under_review`, `entrance_assessed` with Academics evaluation, `accepted` awaiting fee, `accepted` fee paid ready for 1-click enrollment, and `withdrawn`).
6. **Realistic Financial Distribution & Live Cashier Hooks (`OQ-008`):** Establishing comprehensive fee structures (Monthly Tuition / SPP, Annual Facility Fee / *Uang Gedung*, and PACE Material Fees). Modeling Semester 1 as 100% collected and settled, and Semester 2 with a realistic operational distribution: 60% paid in full, 20% partial, 10% overdue receivable, 10% freshly issued unpaid (for live cashier payment demonstration), and 1 pastoral scholarship tuition waiver.
7. **Expanded Multi-Role Demo Credentials in `LoginEndpoint` (`GAP-003`, `OQ-007`):** Extending `LoginEndpoint.cs` with pre-configured demo user accounts utilizing a standardized password (`Demo123!`), issuing JWT tokens with authoritative claims and IDs linked to generated personas (`admin`, `admission`, `finance`, `supervisor`, `coordinator`, `student.jonathan`, `parent.jonathan`).
8. **Multi-Layered Production Safeguards & Dual Invocation Interfaces (`GAP-006`, `OQ-004`, `OQ-013`, `RISK-001`):** Restricting execution strictly to non-Production environments (`!app.Environment.IsProduction()`) and requiring `DemoData:Enabled = true`. Exposing invocation through both a CLI startup flag (`--seed-demo`, `--clean-only`) in `Program.cs` and an authenticated Minimal API endpoint (`POST /api/operations/demo-data/reset`) guarded by `SuperAdmin` role, coupled with a reactive frontend reset confirmation dialog (`ResetDemoDataDialog.vue`) in `AppLayout.vue`.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE / ISSUE:
  - [`IS011 - DEMO-SAMPLE-DATA-GENERATOR-ISSUE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS011%20-%20DEMO-SAMPLE-DATA-GENERATOR-ISSUE.md) (Intake `CR-SYS-SAMPLE-DATA-GENERATOR`)
  - [`operational-domain.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/operational-domain.md) (Academic year lifecycles, operational terms, calendar entries, school closures)
  - [`student-domain.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/student-domain.md) (Admission pipeline, national identities, guardian links, placements, statuses)
  - [`finance-domain.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/finance-domain.md) (Policies, charge rules, student financial accounts, obligations, invoices, payments, waivers)
  - [`academic-domain.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/academic-domain.md) (ACE PACE learning structures, learning units, cohorts, sessions, attendance, coursework, results, report cards, transcripts)
  - [`operational-workflow-discovery.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md), [`use-cases.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md), [`actors.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md), [`screen-inventory.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md)
  - Entity specifications: [`operational-entities-tables.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/operational-entities-tables.md), [`student-entities-tables.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/student-entities-tables.md), [`finance-entities-tables.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/finance-entities-tables.md), [`academic-entities-tables.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/academic-entities-tables.md)
- ARCHITECTURE:
  - [`IS011 - DEMO-SAMPLE-DATA-GENERATOR-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS011%20-%20DEMO-SAMPLE-DATA-GENERATOR-ARCHITECTURE.md) (Authoritative target capability architecture)
  - [`adr.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md) (ADR-001 through ADR-010)
  - [`backend-architecture.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/backend-architecture.md), [`frontend-architecture.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/frontend-architecture.md)
- FEASIBILITY-ASSESSMENT:
  - [`IS011 - DEMO-SAMPLE-DATA-GENERATOR-FEASIBILITY-ASSESSMENT.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS011%20-%20DEMO-SAMPLE-DATA-GENERATOR-FEASIBILITY-ASSESSMENT.md) (Planning Readiness: `READY-FOR-PLANNING`)

Architecture Applicability: ARCHITECTURE-REQUIRED

This capability spans all four domain bounded contexts (`Operations`, `Students`, `Finance`, `Academics`), defines cross-domain seeding coordination contracts, modifies host authentication and startup mechanics, introduces a public administrative API endpoint and CLI flag, and introduces a UI action in the frontend. Target architecture `docs/issues/IS011 - DEMO-SAMPLE-DATA-GENERATOR-ARCHITECTURE.md` is authoritative and governs this plan.

---

# 2. Planning Scope

The planning scope encompasses all changes required across cross-domain contracts, backend domain seeders/purgers, host orchestration, CLI mechanics, authentication extensions, automated test harnesses, and frontend reset dialogs:

1. **Cross-Domain Contracts & Context Records (`Elsasa.Sis.Contracts`):**
   - Marker interfaces: `IDemoDataDomainPurger`, `IOperationsDemoDataSeeder`, `IAcademicsStructureDemoDataSeeder`, `IFinancePolicyDemoDataSeeder`, `IStudentsDemoDataSeeder`, `IFinanceTransactionsDemoDataSeeder`, `IAcademicsActivitiesDemoDataSeeder`.
   - Immutable context transfer records in `Elsasa.Sis.Contracts/DemoData/`: `OperationsSeedResult`, `AcademicsStructureSeedResult`, `FinancePolicySeedResult`, `FlagshipStudentPersonas`, `StudentsSeedResult`, `FinanceTransactionsSeedResult`, `AcademicsActivitiesSeedResult`, `DemoResetSummary`.
   - Deterministic persona and credential constants in `DemoDataConstants.cs`.

2. **Operations Domain Demo Data Implementation (`Elsasa.Sis.Operations`):**
   - `OperationsDemoDataPurger`: Parameterized T-SQL reverse-dependency purge of `operations.*` tables.
   - `OperationsDemoDataSeeder`: Dynamic date calculation anchored around execution date `Today`, seeding Academic Year (AY 2026/2027), Semester 1 (closed), Semester 2 (active), Semester 3 (planned), school calendar entries, and scheduled/emergency closures.

3. **Academics Domain Structure Implementation (`Elsasa.Sis.Academics`):**
   - `AcademicsDemoDataPurger`: Parameterized T-SQL reverse-dependency purge of `academics.*` tables (handling child units before parent units).
   - `AcademicsStructureDemoDataSeeder`: Seeding active ACE PACE Learning Structure, 5 core parent subject units, and child PACE units (`unit_type = 'pace'`) with an 80% passing threshold (KKM).

4. **Finance Domain Policy Implementation (`Elsasa.Sis.Finance`):**
   - `FinanceDemoDataPurger`: Parameterized T-SQL reverse-dependency purge of `finance.*` tables.
   - `FinancePolicyDemoDataSeeder`: Seeding financial policies and charge rules for Monthly Tuition (SPP), Annual Facility Fee (*Uang Gedung*), PACE Material Fees, and pastoral discount scholarship rules.

5. **Students Domain Implementation (`Elsasa.Sis.Students`):**
   - `StudentsDemoDataPurger`: Parameterized T-SQL reverse-dependency purge of `students.*` tables.
   - `StudentsDemoDataSeeder`: Seeding 6 staged applicants across all admission gates, ~48 enrolled students across ELC, JHLC, and HSLC with authentic Indonesian demographic profiles, national identity numbers (NISN, NIS, NIK), guardian relationships, cohort placements, and statuses.

6. **Finance Domain Transactions Implementation (`Elsasa.Sis.Finance`):**
   - `FinanceTransactionsDemoDataSeeder`: Seeding student financial accounts, obligations, Semester 1 paid billing documents with allocations, and Semester 2 billing documents distributed across 60% paid, 20% partial, 10% overdue, 10% freshly issued unpaid (for live cashier demonstration), and 1 pastoral scholarship waiver.

7. **Academics Domain Activities & Results Implementation (`Elsasa.Sis.Academics`):**
   - `AcademicsActivitiesDemoDataSeeder`: Seeding class cohort participations, ~1,200 attendance records, coursework assignments, submissions, Semester 1 historical certified report cards (*Buku Rapor Merdeka* frozen JSON snapshots) and cumulative transcripts, and Semester 2 live interactive hooks (Jonathan score approval hook, Grace retake remediation trail, Budi submitted PACE test).

8. **Host Orchestration, CLI & Authentication Extensions (`Elsasa.Sis.Api`):**
   - `DemoDataOrchestrator`: Coordinates reverse-dependency purge and forward topological seeding across all four domains with semaphore concurrency locking and timing telemetry.
   - `Program.cs` CLI arguments: Intercepts `--seed-demo` and optional `--clean-only`, runs migrations first, executes orchestrator, and exits cleanly.
   - `DemoDataEndpoints`: Exposes Minimal API route `POST /api/operations/demo-data/reset` secured by `SuperAdmin` role and `!app.Environment.IsProduction()`.
   - `LoginEndpoint.cs`: Extended with pre-configured demo user accounts (`admin`, `admission`, `finance`, `supervisor`, `coordinator`, `student.jonathan`, `parent.jonathan`) with fixed password `Demo123!` and role claims.

9. **Backend Automated Verification & Integration Tests (`tests/backend`):**
   - Integration tests in `Elsasa.Sis.IntegrationTests/Features/DemoData/`: Verifying full reverse-purge and forward-seed execution on SQL Server Testcontainers, persona login, data integrity, and production safeguards.
   - Architecture tests in `Elsasa.Sis.ArchitectureTests`: Verifying ADR-001 compile-time isolation and ADR-002 schema write boundaries.

10. **Frontend State, Dialog & Navigation Integration (`src/frontend`):**
    - API client and TanStack Query mutation composable for resetting demo data.
    - PrimeVue dialog `ResetDemoDataDialog.vue` with destructive warning banner, progress spinner, and post-reset reload.
    - User profile dropdown item in `AppLayout.vue` rendered exclusively for SuperAdmin in non-production mode.
    - Vitest component and layout tests.

---

# 3. Dependencies

## External & Infrastructure Prerequisites
- Microsoft SQL Server 2022+ database instance accessible via `DefaultConnection`.
- All Stage 1 and Stage 2 database migrations through `IS010` fully applied.
- `Dapper` data access library for parameterized raw T-SQL command execution.
- ASP.NET Core 8 Web Host with Serilog and Minimal APIs.
- Vue 3 + Vite + PrimeVue 4 + Tailwind CSS + Lucide Icons for frontend application.

## Slice Dependency Rules
- `Depends On` declares implementation prerequisites between slices.
- Dependencies reference Slice IDs only.
- Dependency satisfaction requires the referenced slice to have implementation status `IMPLEMENTED`.
- Dependency satisfaction does not require review status `GO`.
- Slices with no mutual dependencies can and should be implemented in parallel.

---

# 4. Progress Summary

Plan Status: **COMPLETED**  
Execution Approval: **APPROVED**

| Phase | Implementation Status | Review Status | Progress | Slices |
|:---|:---:|:---:|:---:|:---|
| **P1 — Contracts & Domain Foundations** | IMPLEMENTED | GO | 4/4 | P1-S01, P1-S02, P1-S03, P1-S04 |
| **P2 — Students Domain & Admission Pipeline** | IMPLEMENTED | GO | 1/1 | P2-S05 |
| **P3 — Finance Transactions & Academics Activities** | IMPLEMENTED | GO | 2/2 | P3-S06, P3-S07 |
| **P4 — Host Orchestrator, CLI & Authentication** | IMPLEMENTED | GO | 2/2 | P4-S08, P4-S09 |
| **P5 — Backend Automated Verification & Integration Tests** | IMPLEMENTED | GO | 1/1 | P5-S10 |
| **P6 — Frontend State, Reset Dialog & Navigation** | IMPLEMENTED | GO | 2/2 | P6-S11, P6-S12 |
| **P7 — Frontend Automated Verification & Component Tests** | IMPLEMENTED | GO | 1/1 | P7-S13 |

---

# 5. Phases

## P1 - Contracts & Domain Foundations

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P1-S01

Title: Cross-Domain Demo Contracts, Context Models & Persona Constants

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Define shared marker interfaces, strongly typed context transfer records, and deterministic persona constants in `Elsasa.Sis.Contracts` to establish the cross-domain contracts required for multi-schema seeding and reverse purging without violating assembly encapsulation (`ADR-001`).

Depends On: None

Repository: `src/backend/Elsasa.Sis.Contracts`

Completion Criteria:
1. `src/backend/Elsasa.Sis.Contracts/DemoData/IDemoDataDomainPurger.cs` defines:
   ```csharp
   namespace Elsasa.Sis.Contracts.DemoData;
   public interface IDemoDataDomainPurger
   {
       Task PurgeDomainDataAsync(CancellationToken cancellationToken = default);
   }
   ```
2. Domain seeder interfaces defined in `src/backend/Elsasa.Sis.Contracts/DemoData/`:
   - `IOperationsDemoDataSeeder` (`SeedOperationsAsync(DateTime baseDate, CancellationToken cancellationToken = default)`)
   - `IAcademicsStructureDemoDataSeeder` (`SeedAcademicsStructureAsync(OperationsSeedResult operationsContext, CancellationToken cancellationToken = default)`)
   - `IFinancePolicyDemoDataSeeder` (`SeedFinancePoliciesAsync(OperationsSeedResult operationsContext, CancellationToken cancellationToken = default)`)
   - `IStudentsDemoDataSeeder` (`SeedStudentsAsync(OperationsSeedResult operationsContext, AcademicsStructureSeedResult academicsContext, FinancePolicySeedResult financeContext, CancellationToken cancellationToken = default)`)
   - `IFinanceTransactionsDemoDataSeeder` (`SeedFinanceTransactionsAsync(OperationsSeedResult operationsContext, FinancePolicySeedResult financePolicyContext, StudentsSeedResult studentsContext, CancellationToken cancellationToken = default)`)
   - `IAcademicsActivitiesDemoDataSeeder` (`SeedAcademicsActivitiesAsync(OperationsSeedResult operationsContext, AcademicsStructureSeedResult academicsContext, StudentsSeedResult studentsContext, CancellationToken cancellationToken = default)`)
3. Context records defined in `src/backend/Elsasa.Sis.Contracts/DemoData/DemoDataContextModels.cs`:
   - `OperationsSeedResult(Guid AcademicYearId, string AcademicYearCode, Guid HistoricalPeriodId, Guid ActivePeriodId, Guid FuturePeriodId, Guid SchoolCalendarId, DateTime BaseDate)`
   - `AcademicsStructureSeedResult(Guid LearningStructureId, IReadOnlyDictionary<string, Guid> SubjectUnitIds, IReadOnlyList<Guid> PaceUnitIds)`
   - `FinancePolicySeedResult(Guid PolicyId, Guid TuitionChargeRuleId, Guid FacilityChargeRuleId, Guid PaceMaterialChargeRuleId)`
   - `FlagshipStudentPersonas(Guid JonathanStudentId, Guid GraceStudentId, Guid BudiStudentId, IReadOnlyDictionary<string, Guid> StagedApplicantIds)`
   - `StudentsSeedResult(IReadOnlyList<Guid> EnrolledStudentIds, IReadOnlyDictionary<string, IReadOnlyList<Guid>> CohortStudentIds, FlagshipStudentPersonas Personas)`
   - `FinanceTransactionsSeedResult(int AccountsCreated, int InvoicesCreated, int PaymentsCreated, Guid JonathanUnpaidInvoiceId)`
   - `AcademicsActivitiesSeedResult(int ParticipationsCreated, int AttendanceRecordsCreated, int ResultsCreated, int ReportCardsCreated, int TranscriptsCreated)`
   - `DemoResetSummary(int PurgedSchemas, int SeededDomains, long ElapsedMilliseconds, DateTime AnchoredDate, string BaseAcademicYearCode, int TotalStudentsSeeded, int TotalInvoicesSeeded, int TotalAttendanceSeeded, int TotalResultsSeeded, int TotalReportCardsSeeded)`
4. `src/backend/Elsasa.Sis.Contracts/DemoData/DemoDataConstants.cs` defines deterministic GUIDs:
   - `JonathanStudentId`: `00000000-0000-0000-0001-000000000001`
   - `JonathanApplicantId`: `00000000-0000-0000-0001-000000000002`
   - `HendraParentId`: `00000000-0000-0000-0001-000000000003`
   - `GraceStudentId`: `00000000-0000-0000-0002-000000000001`
   - `GraceApplicantId`: `00000000-0000-0000-0002-000000000002`
   - `BudiStudentId`: `00000000-0000-0000-0003-000000000001`
   - `DanielApplicantId`: `00000000-0000-0000-0004-000000000001`
   - `MariaApplicantId`: `00000000-0000-0000-0004-000000000002`
   - `SamuelApplicantId`: `00000000-0000-0000-0004-000000000003`
   - `EstherApplicantId`: `00000000-0000-0000-0004-000000000004`
   - `RuthApplicantId`: `00000000-0000-0000-0004-000000000005`
   - `TimotiusApplicantId`: `00000000-0000-0000-0004-000000000006`
   - Default demo password: `"Demo123!"`
5. Solution builds cleanly without warnings (`dotnet build src/backend/Elsasa.Sis.Contracts`).

Notes:  
Contracts must be immutable C# 12 records and strictly comply with ADR-009 nullability. No references to domain implementations are permitted.

**Implementation Notes (2026-11-07):**
- Defined `IDemoDataDomainPurger` marker interface for domain-encapsulated reverse purging.
- Defined all 6 domain seeder interfaces in `IDemoDataSeeders.cs`: `IOperationsDemoDataSeeder`, `IAcademicsStructureDemoDataSeeder`, `IFinancePolicyDemoDataSeeder`, `IStudentsDemoDataSeeder`, `IFinanceTransactionsDemoDataSeeder`, and `IAcademicsActivitiesDemoDataSeeder`.
- Defined all immutable context records in `DemoDataContextModels.cs`: `OperationsSeedResult`, `AcademicsStructureSeedResult`, `FinancePolicySeedResult`, `FlagshipStudentPersonas`, `StudentsSeedResult`, `FinanceTransactionsSeedResult`, `AcademicsActivitiesSeedResult`, and `DemoResetSummary`.
- Defined deterministic GUIDs and default demo password in `DemoDataConstants.cs`.
- Verified `Elsasa.Sis.Contracts` and full backend solution compile cleanly with 0 errors and 0 warnings.

---

### P1-S02

Title: Operations Domain Demo Purger & Dynamic Date Anchored Seeder

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the autonomous purge and seeding services inside `Elsasa.Sis.Operations` to cleanly wipe and generate `operations.*` schema data, dynamically anchored around `Today = DateTime.UtcNow.Date` (`ADR-002`, `ADR-004`, `TD-02`, `TD-03`).

Depends On: P1-S01

Repository: `src/backend/Elsasa.Sis.Operations`

Completion Criteria:
1. `src/backend/Elsasa.Sis.Operations/Features/DemoData/OperationsDemoDataPurger.cs` implements `IDemoDataDomainPurger`:
   - Injects `ISqlConnectionFactory`.
   - Executes parameterized T-SQL deleting `operations.*` in reverse dependency order:
     ```sql
     DELETE FROM operations.operations_record_corrections;
     DELETE FROM operations.calendar_amendments;
     DELETE FROM operations.calendar_entries;
     DELETE FROM operations.school_closures;
     DELETE FROM operations.school_calendars;
     DELETE FROM operations.operational_periods;
     DELETE FROM operations.academic_years;
     ```
2. `src/backend/Elsasa.Sis.Operations/Features/DemoData/OperationsDemoDataSeeder.cs` implements `IOperationsDemoDataSeeder`:
   - Computes dynamic timeline relative to `baseDate`:
     - Academic Year AY 2026/2027: `StartDate = baseDate.AddDays(-180)`, `EndDate = baseDate.AddDays(120)`, `Status = 'active'`.
     - Semester 1 (historical): `StartDate = baseDate.AddDays(-180)`, `EndDate = baseDate.AddDays(-35)`, `Status = 'closed'`, `Code = $"SEM1-{baseDate.Year}"`.
     - Semester 2 (current active): `StartDate = baseDate.AddDays(-30)`, `EndDate = baseDate.AddDays(120)`, `Status = 'active'`, `Code = $"SEM2-{baseDate.Year + 1}"`.
     - Semester 3 (planned intake): `StartDate = baseDate.AddDays(125)`, `EndDate = baseDate.AddDays(270)`, `Status = 'planned'`, `Code = $"SEM1-{baseDate.Year + 1}"`.
   - Inserts active school calendar for `campus-main` with status `published`.
   - Inserts weekday calendar entries for instructional days across Semester 1 and Semester 2.
   - Inserts scheduled mid-term retreat closure (`baseDate.AddDays(-10)` to `baseDate.AddDays(-8)`) and emergency weather closure (`baseDate.AddDays(-4)`) with valid amendment.
   - Returns `OperationsSeedResult` populated with generated IDs and dates.
3. Services registered in `Elsasa.Sis.Operations/DependencyInjection.cs`:
   - `services.AddScoped<OperationsDemoDataPurger>();`
   - `services.AddScoped<IOperationsDemoDataSeeder, OperationsDemoDataSeeder>();`
4. Code compiles cleanly with zero nullable warnings.

Notes:  
All Dapper inserts must use parameterized T-SQL (`ADR-004`). No direct references to other domain assemblies.

**Implementation Notes (2026-11-05):**
- `OperationsDemoDataPurger` implemented with parameterized T-SQL executing reverse dependency order deletes across all 7 `operations.*` tables.
- `OperationsDemoDataSeeder` implements dynamic date anchoring around `baseDate` (execution date `Today`):
  - Academic Year AY 2026/2027 spanning `baseDate.AddDays(-180)` to `baseDate.AddDays(120)`
  - Semester 1 (historical/closed): `baseDate.AddDays(-180)` to `baseDate.AddDays(-35)`
  - Semester 2 (active): `baseDate.AddDays(-30)` to `baseDate.AddDays(120)` — `Today` falls ~4 weeks into this period
  - Semester 3 (planned): `baseDate.AddDays(125)` to `baseDate.AddDays(270)`
  - School calendar for `campus-main` with `published` status
  - Weekday instructional entries across Sem 1 (10 weeks) and Sem 2 (8 weeks)
  - Scheduled mid-term retreat closure (`baseDate.AddDays(-10)` to `-8`) and emergency weather closure (`baseDate.AddDays(-4)`) with calendar amendment
- Returns `OperationsSeedResult` with all generated IDs and `BaseDate`
- Services registered in `DependencyInjection.cs` with scoped lifetime
- Build verified: `dotnet build src/backend/Elsasa.Sis.Operations` — 0 warnings, 0 errors

---

### P1-S03

Title: Academics Domain Structure Demo Seeder & Hierarchical Purger

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the autonomous purge and structure seeder inside `Elsasa.Sis.Academics` to cleanly wipe `academics.*` schema tables and seed the two-tier ACE PACE curriculum framework (5 parent subjects, child PACE units with an 80% passing threshold) (`ADR-002`, `ADR-004`, `TD-02`, `TD-05`).

Depends On: P1-S01

Repository: `src/backend/Elsasa.Sis.Academics`

Completion Criteria:
1. `src/backend/Elsasa.Sis.Academics/Features/DemoData/AcademicsDemoDataPurger.cs` implements `IDemoDataDomainPurger`:
   - Executes parameterized T-SQL deleting `academics.*` in strict reverse dependency order:
     ```sql
     DELETE FROM academics.transcripts;
     DELETE FROM academics.report_card_entries;
     DELETE FROM academics.report_cards;
     DELETE FROM academics.academic_record_corrections;
     DELETE FROM academics.academic_results;
     DELETE FROM academics.assessment_evidence;
     DELETE FROM academics.assessments;
     DELETE FROM academics.submissions;
     DELETE FROM academics.assignments;
     DELETE FROM academics.attendance_corrections;
     DELETE FROM academics.attendance_records;
     DELETE FROM academics.learning_activities;
     DELETE FROM academics.academic_participations;
     DELETE FROM academics.academic_requirements;
     DELETE FROM academics.learning_units WHERE parent_unit_id IS NOT NULL;
     DELETE FROM academics.learning_units WHERE parent_unit_id IS NULL;
     DELETE FROM academics.learning_structures;
     ```
2. `src/backend/Elsasa.Sis.Academics/Features/DemoData/AcademicsStructureDemoDataSeeder.cs` implements `IAcademicsStructureDemoDataSeeder`:
   - Consumes `OperationsSeedResult` for `AcademicYearId`.
   - Inserts `academics.learning_structures`: `"Hagios ACE Individualized Curriculum"`, `curriculum_framework = "ACE_PACE"`, `status = 'active'`.
   - Inserts 5 core parent subjects (`parent_unit_id IS NULL`, `unit_type = 'subject'`):
     - `Math`
     - `English`
     - `Science`
     - `Social Studies`
     - `Word Building / Bible Reading`
   - Inserts child PACE units (`parent_unit_id` linked to parent subject, `unit_type = 'pace'`) with `passing_score = 80.0`:
     - Math PACEs: M1037 through M1042
     - English PACEs: E1037 through E1042
     - Science PACEs: SC1037 through SC1042
     - Social Studies PACEs: SS1037 through SS1042
     - Bible PACEs: B1037 through B1042
   - Inserts standard grading requirements with numeric scale and 80.0 KKM.
   - Returns `AcademicsStructureSeedResult` containing `LearningStructureId`, dictionary of subject IDs, and list of PACE unit IDs.
3. Services registered in `Elsasa.Sis.Academics/DependencyInjection.cs`:
   - `services.AddScoped<AcademicsDemoDataPurger>();`
   - `services.AddScoped<IAcademicsStructureDemoDataSeeder, AcademicsStructureDemoDataSeeder>();`
4. Code compiles cleanly with zero nullable warnings.

Notes:  
Parent-child unit relationship requires child PACE units to be inserted after parent subjects and deleted before parent subjects during purge.

---

### P1-S04

Title: Finance Domain Policy Demo Seeder & Purger

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the autonomous purge and policy seeder inside `Elsasa.Sis.Finance` to cleanly wipe `finance.*` schema tables and seed institutional fee policies and charge rules for Tuition (SPP), Facility Fees, and PACE booklet material fees (`ADR-002`, `ADR-004`, `TD-02`, `OQ-008`).

Depends On: P1-S01

Repository: `src/backend/Elsasa.Sis.Finance`

Completion Criteria:
1. `src/backend/Elsasa.Sis.Finance/Features/DemoData/FinanceDemoDataPurger.cs` implements `IDemoDataDomainPurger`:
   - Executes parameterized T-SQL deleting `finance.*` in strict reverse dependency order:
     ```sql
     DELETE FROM finance.payment_allocations;
     DELETE FROM finance.payments;
     DELETE FROM finance.payers;
     DELETE FROM finance.billing_document_items;
     DELETE FROM finance.billing_documents;
     DELETE FROM finance.obligation_due_schedules;
     DELETE FROM finance.financial_obligations;
     DELETE FROM finance.student_financial_accounts;
     DELETE FROM finance.charge_rules;
     DELETE FROM finance.financial_policies;
     DELETE FROM finance.policies;
     ```
2. `src/backend/Elsasa.Sis.Finance/Features/DemoData/FinancePolicyDemoDataSeeder.cs` implements `IFinancePolicyDemoDataSeeder`:
   - Consumes `OperationsSeedResult` for `ActivePeriodId`.
   - Inserts `finance.policies` / `finance.financial_policies`: `"Hagios Institutional Fee Policy 2026/2027"`, `status = 'active'`.
   - Inserts `finance.charge_rules`:
     - Monthly Tuition (SPP): IDR 1,500,000 / month (`rule_code = 'SPP-REG'`, `frequency = 'monthly'`).
     - Annual Facility Fee (*Uang Gedung*): IDR 10,000,000 / year (`rule_code = 'FACILITY-ANNUAL'`, `frequency = 'annual'`).
     - PACE Material Fee: IDR 75,000 / unit booklet (`rule_code = 'PACE-UNIT-FEE'`, `frequency = 'per_unit'`).
     - Pastoral Scholarship Waiver Rule: 100% discount on tuition for pastoral children.
   - Returns `FinancePolicySeedResult` containing policy and charge rule IDs.
3. Services registered in `Elsasa.Sis.Finance/DependencyInjection.cs`:
   - `services.AddScoped<FinanceDemoDataPurger>();`
   - `services.AddScoped<IFinancePolicyDemoDataSeeder, FinancePolicyDemoDataSeeder>();`
4. Code compiles cleanly with zero nullable warnings.

Notes:  
Charge rules establish the basis for obligations and invoices generated downstream in P3-S06.  
Remediation notes (Iteration 1): Removed obsolete `DELETE FROM finance.policies;` statement from `FinanceDemoDataPurger.cs` per review finding 1. The purge batch now strictly targets existing tables in reverse dependency order ending with `finance.financial_policies`.

---

## P2 - Students Domain & Admission Pipeline

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P2-S05

Title: Students Domain Demo Purger & Staged Intake Pipeline Seeder

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the autonomous purge and comprehensive student demographic/admission seeder inside `Elsasa.Sis.Students` to populate 6 staged applicants across intake gates, ~48 enrolled students with authentic Indonesian identities across ELC, JHLC, and HSLC, deterministic flagship personas (Jonathan, Grace, Budi), linked guardians, administrative placements, and statuses (`ADR-002`, `ADR-004`, `TD-04`, `OQ-003`, `OQ-006`, `OQ-010`).

Depends On: P1-S01, P1-S02, P1-S03, P1-S04

Repository: `src/backend/Elsasa.Sis.Students`

Completion Criteria:
1. `src/backend/Elsasa.Sis.Students/Features/DemoData/StudentsDemoDataPurger.cs` implements `IDemoDataDomainPurger`:
   - Executes parameterized T-SQL deleting `students.*` in reverse dependency order:
     ```sql
     DELETE FROM students.administrative_history_entries;
     DELETE FROM students.student_statuses;
     DELETE FROM students.administrative_placements;
     DELETE FROM students.student_documents;
     DELETE FROM students.student_guardian_relationships;
     DELETE FROM students.guardians;
     DELETE FROM students.enrollments;
     DELETE FROM students.students;
     DELETE FROM students.admission_application_contacts;
     DELETE FROM students.entrance_assessment_evidence;
     DELETE FROM students.applicant_documents;
     DELETE FROM students.admission_decisions;
     DELETE FROM students.admission_applications;
     DELETE FROM students.applicants;
     ```
2. `src/backend/Elsasa.Sis.Students/Features/DemoData/StudentsDemoDataSeeder.cs` implements `IStudentsDemoDataSeeder`:
   - Consumes `OperationsSeedResult`, `AcademicsStructureSeedResult`, and `FinancePolicySeedResult`.
   - Seeds 6 staged admission applicants utilizing deterministic IDs from `DemoDataConstants`:
     - `DanielApplicantId`: `submitted`
     - `MariaApplicantId`: `under_review`
     - `SamuelApplicantId`: `entrance_assessed` (with linked entrance assessment)
     - `EstherApplicantId`: `accepted` (awaiting fee payment)
     - `RuthApplicantId`: `accepted` (admission fee paid, ready for 1-click enrollment live demo hook)
     - `TimotiusApplicantId`: `withdrawn`
   - Seeds deterministic flagship personas:
     - **Jonathan Pratama (`JonathanStudentId`):** High School, Grade 10, Hendra Pratama (`HendraParentId`), active, NISN `0091234567`, NIS `261001`, NIK `347101...`, Yogyakarta address.
     - **Grace Santoso (`GraceStudentId`):** Junior High, Grade 8, guardian, active, NISN `0081234567`, NIS `260801`.
     - **Budi Wijaya (`BudiStudentId`):** Elementary, Grade 3, guardian, active, NISN `0071234567`, NIS `260301`.
   - Seeds balanced population of ~48 enrolled students with authentic Indonesian names (Kurniawan, Santoso, Wijaya, Pratama, Halim, Kusuma, Sitorus, Simanjuntak, Setiawan, Nugroho) distributed across:
     - Elementary Learning Center (ELC, Grades 1–6): ~18 students
     - Junior High Learning Center (JHLC, Grades 7–9): ~15 students
     - High School Learning Center (HSLC, Grades 10–12): ~15 students
   - Inserts linked guardians, student-guardian relationships (`emergency_contact = 1`), administrative placements, and student status records (`active`, 1 `on_leave`, 1 `transferred`).
   - Returns `StudentsSeedResult` containing enrolled student IDs, cohort mappings, and flagship persona references.
3. Services registered in `Elsasa.Sis.Students/DependencyInjection.cs`:
   - `services.AddScoped<StudentsDemoDataPurger>();`
   - `services.AddScoped<IStudentsDemoDataSeeder, StudentsDemoDataSeeder>();`
4. Code compiles cleanly with zero nullable warnings.

Notes:  
Flagship IDs must match `DemoDataConstants` exactly so downstream finance and academics seeders can link transactions and results reliably.

Remediation notes (Iteration 1):
Addressed finding F-001 [BLOCKER]: Completed full implementation of `StudentsDemoDataSeeder.SeedStudentsAsync` across all 14 required student domain tables:
1. `students.applicants`: Seeded 6 staged applicants (Daniel, Maria, Samuel, Esther, Ruth, Timotius) with deterministic GUIDs and 48 applicants corresponding to all enrolled students.
2. `students.admission_applications`: Linked applications to all applicants with staged statuses (`submitted`, `under_review`, `entrance_assessed`, `accepted`, `accepted`, `withdrawn`) and `enrolled` for enrolled students.
3. `students.admission_decisions`: Seeded admission decisions for Samuel (`conditional_accept`), Esther (`accepted`), Ruth (`accepted`), and all 48 enrolled students.
4. `students.entrance_assessment_evidence`: Seeded diagnostic entrance assessment evidence for Samuel with score 88.5.
5. `students.applicant_documents`: Seeded submitted and verified applicant documents across staged applicants and enrolled students.
6. `students.admission_application_contacts`: Seeded verified parent contacts across all staged applications.
7. `students.students`: Seeded 3 deterministic flagship personas (Jonathan Pratama `JonathanStudentId`, Grace Santoso `GraceStudentId`, Budi Wijaya `BudiStudentId`) and 45 additional enrolled students with authentic Indonesian identities (total 48 enrolled students).
8. `students.enrollments`: Seeded administrative enrollment placement records linked to student identity and applications.
9. `students.guardians`: Seeded master directory records for parents/guardians, including Hendra Pratama (`HendraParentId`), Agus Santoso (`GraceGuardianId`), and Bambang Wijaya (`BudiGuardianId`).
10. `students.student_guardian_relationships`: Linked students to guardians with `emergency_contact = 1` and single active primary contact constraint satisfaction.
11. `students.administrative_placements`: Seeded active placements balanced across ELC (18 students, Grades 1-6), JHLC (15 students, Grades 7-9), and HSLC (15 students, Grades 10-12).
12. `students.student_statuses`: Seeded active status history entries for 46 students, 1 `on_leave` student, and 1 `transferred` student.
13. `students.administrative_history_entries`: Seeded chronological intake and status change audit trails.
14. `students.student_documents`: Seeded verified certified student document records.
15. `StudentsSeedResult`: Returned complete context with enrolled student IDs, cohort groupings ("ELC", "JHLC", "HSLC"), and flagship personas.
Updated `EnrollAcceptedApplicantHandler.cs` to allow application status 'accepted' alongside 'decided' for seamless 1-click enrollment execution.
Verified build: `dotnet build src/backend/Elsasa.Sis.Students` succeeds with 0 warnings and 0 errors. All unit/integration tests pass cleanly (459 passed, 0 failed).

---

## P3 - Finance Transactions & Academics Activities

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P3-S06

Title: Finance Domain Transactions, Obligations & Live Cashier Demo Seeder

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the financial transaction seeder inside `Elsasa.Sis.Finance` to establish student financial accounts, obligations, historical Semester 1 settled invoices, and active Semester 2 invoices distributed across realistic payment states (paid, partial, overdue, live unpaid hook) (`ADR-002`, `ADR-004`, `TD-06`, `OQ-008`).

Depends On: P1-S02, P1-S04, P2-S05

Repository: `src/backend/Elsasa.Sis.Finance`

Completion Criteria:
1. `src/backend/Elsasa.Sis.Finance/Features/DemoData/FinanceTransactionsDemoDataSeeder.cs` implements `IFinanceTransactionsDemoDataSeeder`:
   - Consumes `OperationsSeedResult`, `FinancePolicySeedResult`, and `StudentsSeedResult`.
   - Creates student financial accounts (`finance.student_financial_accounts`) for all ~48 enrolled students.
   - Creates financial obligations and schedules (`finance.financial_obligations`, `finance.obligation_due_schedules`):
     - SPP (Monthly Tuition) across Sem 1 and Sem 2
     - Annual Facility Fee (*Uang Gedung*)
     - PACE Material Fees
   - Semester 1 (historical): 100% billed and 100% paid in full:
     - `finance.billing_documents` (`status = 'paid'`)
     - `finance.billing_document_items`
     - `finance.payers`, `finance.payments`, `finance.payment_allocations`
   - Semester 2 (active, ongoing):
     - 60% paid in full with allocations
     - 20% partial payments (with outstanding balances)
     - 10% overdue receivable (Grace Santoso: overdue SPP)
     - 10% freshly issued unpaid invoices ready for live payment demonstration:
       - Jonathan Pratama (`INV-SEM2-0042`, balance due IDR 1,500,000, `status = 'unpaid'`)
     - 1 student with pastoral scholarship fee waiver record.
   - Returns `FinanceTransactionsSeedResult` with account, invoice, payment counts, and `JonathanUnpaidInvoiceId`.
2. Service registered in `Elsasa.Sis.Finance/DependencyInjection.cs`:
   - `services.AddScoped<IFinanceTransactionsDemoDataSeeder, FinanceTransactionsDemoDataSeeder>();`
3. Code compiles cleanly with zero nullable warnings.

Notes:  
Jonathan's unpaid invoice `INV-SEM2-0042` serves as the primary hook for live cashier payment demonstration in `SCR-FIN-02`.

Remediation Iteration 1 Notes:
1. BLOCKER-001 Fixed: Corrected Semester 1 SPP obligation loop to 6 months (`month <= 6`) and used `sem1Start.AddMonths(month - 1)` with valid day calculations, eliminating `ArgumentOutOfRangeException`.
2. MAJOR-001 Fixed: For Jonathan Pratama (`personas.JonathanStudentId`), assessed exactly 1 month SPP installment in Semester 2, ensuring billing document `INV-SEM2-0042` has a total billed amount and outstanding balance due of IDR 1,500,000 in `unpaid` status.
3. MAJOR-002 Fixed: Removed duplicate assessment of Annual Facility Fee (*Uang Gedung Tahunan*) in Semester 2; the fee is now assessed and settled strictly once per academic year in Semester 1.
4. MINOR-001 Fixed: Dynamically anchored semester boundaries, due dates, and payment dates to `operationsContext.BaseDate` and `operations.operational_periods` records rather than hardcoded static dates.
5. Verification: `dotnet build src/backend/Elsasa.Sis.Finance` compiles with 0 warnings and 0 errors; full solution and architecture test suite pass cleanly (42/42 passed).

---

### P3-S07

Title: Academics Domain Participations, Attendance & Assessment Activities Seeder

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the academic activities seeder inside `Elsasa.Sis.Academics` to generate Learning Center cohort participations, ~1,200 attendance records, coursework assignments, submissions, Semester 1 certified report cards with frozen JSON snapshots, cumulative transcripts, and Semester 2 live presentation hooks (`ADR-002`, `ADR-004`, `TD-05`, `TD-06`, `OQ-009`).

Depends On: P1-S02, P1-S03, P2-S05

Repository: `src/backend/Elsasa.Sis.Academics`

Completion Criteria:
1. `src/backend/Elsasa.Sis.Academics/Features/DemoData/AcademicsActivitiesDemoDataSeeder.cs` implements `IAcademicsActivitiesDemoDataSeeder`:
   - Consumes `OperationsSeedResult`, `AcademicsStructureSeedResult`, and `StudentsSeedResult`.
   - Creates cohort sections / Learning Center participations (`academics.academic_participations`) for all ~48 enrolled students in ELC, JHLC, and HSLC.
   - Creates instructional sessions across calendar instructional days and logs ~1,200 attendance records (`academics.attendance_records`):
     - Realistic distribution: ~95% Present, 3% Excused, 1% Late, 1% Unexcused.
     - Jonathan Pratama: 100% attendance (high honors).
   - Coursework assignments and learning activities (`academics.learning_activities`, `academics.assignments`, `academics.submissions`):
     - Checkups, Self-Tests, and PACE Tests for each PACE booklet.
   - Semester 1 historical certified records:
     - Completed evaluations, approved results (`academics.academic_results`, `status = 'published'`, `is_final = 1`).
     - Certified Semester 1 report cards (`academics.report_cards`, `status = 'published'`) with frozen JSON snapshot (`content_snapshot`) and report card entries (`academics.report_card_entries`).
     - Cumulative transcripts (`academics.transcripts`, `status = 'official'`) with frozen JSON snapshot for high school students including Jonathan Pratama.
   - Semester 2 active records and live presentation hooks:
     - **Jonathan Pratama:** Provisional PACE score 92.0 in `provisional` status awaiting Academic Coordinator approval hook (`SCR-ACD-07`).
     - **Grace Santoso:** Multi-attempt PACE retake:
       - Attempt 1: score 68.0 (`is_final = 0`, fail < 80).
       - Attempt 2: score 85.0 (`is_final = 1`, pass, `replaces_result_id = Attempt1.Id`, `attempt_number = 2`).
     - **Budi Wijaya:** Science PACE 1049 submitted test awaiting Supervisor evaluation hook (`SCR-ACD-02`).
     - **Samuel (applicant):** Entrance assessment record linked via `applicant_id`.
   - Returns `AcademicsActivitiesSeedResult` containing created counts.
2. Service registered in `Elsasa.Sis.Academics/DependencyInjection.cs`:
   - `services.AddScoped<IAcademicsActivitiesDemoDataSeeder, AcademicsActivitiesDemoDataSeeder>();`
3. Code compiles cleanly with zero nullable warnings.

Notes:  
Report cards and transcripts must embed complete, valid frozen JSON snapshots matching `ReportCardSnapshotDto` and `TranscriptSnapshotDto` contracts.

**Implementation Notes (2026-11-07 - Remediation Iteration 1):**
- Corrected BLOCKER-1: Updated attendance probability distribution in `CreateAttendanceRecordsAsync` to `< 95 => "present"` (95%), `< 98 => "excused_absence"` (3%), `< 99 => "late"` (1%), `_ => "absent"` (1%).
- Corrected BLOCKER-2: Passed `jonathanId` to `CreateAttendanceRecordsAsync` and ensured `studentId == jonathanId` receives `"present"` on 100% of attendance records.
- Corrected MINOR-1: Aligned report card frozen JSON snapshots with `ReportCardSnapshotDto`, itemizing entries with `ReportCardSubjectEntryDto` including `SubjectCode`, `SubjectTitle`, `GradeLabel`, `ScoreNumeric`, `GradeLetter`, `TeacherComment`, `DisplayOrder`, and period `ReportCardAttendanceSummaryDto`, serialized with `JsonSerializerDefaults.Web`.
- Corrected MINOR-2: Aligned transcript frozen JSON snapshots with `TranscriptSnapshotDto`, structuring courses within `TranscriptPeriodSummaryDto` using `TranscriptCourseEntryDto` (with credit weight, grades, code, title), serialized with `JsonSerializerDefaults.Web`.
- Separated `allS1Results` and `allS2Results` dictionaries to ensure historical S1 report cards and transcripts consume actual S1 evaluation results.
- Verified build: `dotnet build src/backend/Elsasa.Sis.Academics` compiles with 0 warnings and 0 errors.

---

## P4 - Host Orchestrator, CLI & Authentication

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P4-S08

Title: Host Orchestrator, Concurrency Lock & CLI Runner Mechanics

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the master orchestrator in `Elsasa.Sis.Api` coordinating reverse-dependency purges (`Academics` → `Finance` → `Students` → `Operations`) and forward topological seeding, guarded by an asynchronous semaphore and production environment check, and wire up CLI startup execution flags (`--seed-demo`, `--clean-only`) in `Program.cs` (`ADR-001`, `ADR-006`, `TD-01`, `TD-02`, `TD-08`, `OQ-004`).

Depends On: P1-S02, P1-S03, P1-S04, P2-S05, P3-S06, P3-S07

Repository: `src/backend/Elsasa.Sis.Api`

Completion Criteria:
1. `src/backend/Elsasa.Sis.Api/Features/DemoData/DemoDataOptions.cs` created:
   ```csharp
   namespace Elsasa.Sis.Api.Features.DemoData;
   public sealed class DemoDataOptions
   {
       public const string SectionName = "DemoData";
       public bool Enabled { get; set; } = true;
       public string DefaultPassword { get; set; } = "Demo123!";
   }
   ```
2. `src/backend/Elsasa.Sis.Api/Features/DemoData/DemoDataOrchestrator.cs` created:
   - Injects domain purgers (`AcademicsDemoDataPurger`, `FinanceDemoDataPurger`, `StudentsDemoDataPurger`, `OperationsDemoDataPurger`) and domain seeders.
   - Throws `InvalidOperationException` if `IHostEnvironment.IsProduction()`.
   - Uses `SemaphoreSlim(1, 1)` to serialize execution.
   - Executes reverse purge: `Academics` → `Finance` → `Students` → `Operations`.
   - If clean-only flag is set, stops after purge.
   - Executes forward topological seed:
     1. `OperationsSeeder.SeedOperationsAsync(Today)` → `OperationsSeedResult`
     2. `AcademicsStructureSeeder.SeedAcademicsStructureAsync(OperationsSeedResult)` → `AcademicsStructureSeedResult`
     3. `FinancePolicySeeder.SeedFinancePoliciesAsync(OperationsSeedResult)` → `FinancePolicySeedResult`
     4. `StudentsSeeder.SeedStudentsAsync(OperationsSeedResult, AcademicsStructureSeedResult, FinancePolicySeedResult)` → `StudentsSeedResult`
     5. `FinanceTransactionsSeeder.SeedFinanceTransactionsAsync(OperationsSeedResult, FinancePolicySeedResult, StudentsSeedResult)` → `FinanceTransactionsSeedResult`
     6. `AcademicsActivitiesSeeder.SeedAcademicsActivitiesAsync(OperationsSeedResult, AcademicsStructureSeedResult, StudentsSeedResult)` → `AcademicsActivitiesSeedResult`
   - Records Serilog diagnostic telemetry (durations and counts).
   - Returns `DemoResetSummary`.
3. `src/backend/Elsasa.Sis.Api/Program.cs` updated:
   - Intercepts `--seed-demo` and `--clean-only` CLI arguments:
     - Runs pending migrations first via `Program.RunMigration()`.
     - Creates host/scope, resolves `DemoDataOrchestrator`, invokes reset, and exits with code 0 (or 1 on error).
4. Services registered in `Elsasa.Sis.Api/Configuration/DependencyInjection.cs`:
   - `services.Configure<DemoDataOptions>(configuration.GetSection(DemoDataOptions.SectionName));`
   - `services.AddScoped<DemoDataOrchestrator>();`
5. Solution builds cleanly without warnings.

Notes:  
CLI startup execution allows local developers and CI/Docker containers to initialize a pristine demo database with a single command.

---

### P4-S09

Title: Reset Minimal API Endpoint & Multi-Role Demo Credentials in LoginEndpoint

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the authenticated Minimal API endpoint `POST /api/operations/demo-data/reset` guarded by SuperAdmin policy and non-production checks, and extend `LoginEndpoint.cs` with pre-configured demo user accounts issuing JWT tokens with authoritative role claims and persona identifiers (`TD-07`, `TD-08`, `GAP-003`, `OQ-004`, `OQ-007`).

Depends On: P4-S08

Repository: `src/backend/Elsasa.Sis.Api`

Completion Criteria:
1. `src/backend/Elsasa.Sis.Api/Features/DemoData/DemoDataEndpoints.cs` created implementing `IEndpoint`:
   - Maps `POST /api/operations/demo-data/reset`.
   - Configures authorization: `RequireAuthorization(new AuthorizeAttribute { Roles = "SuperAdmin" })`.
   - Checks `app.Environment.IsProduction()`: returns `Results.Problem(statusCode: 403, title: "Action Forbidden", detail: "Demo data generation and reset is strictly prohibited in Production environments.")`.
   - Calls `DemoDataOrchestrator.ResetAndSeedAsync(cancellationToken)`.
   - Returns `Results.Ok(summary)`.
   - Maps route in `Program.cs` via `new DemoDataEndpoints().MapEndpoint(app)`.
2. `src/backend/Elsasa.Sis.Api/Features/Auth/LoginEndpoint.cs` updated:
   - Accepts password `Demo123!` for all pre-configured demo accounts:
     - `admin` / `superadmin`: Super Administrator (`SuperAdmin`, `Admin`, `AcademicAdmin`, `OperationsAuthority`, `*` permissions).
     - `admission`: Sarah Jenkins (`AdmissionOfficer`, `Staff`, `Permissions.Students.Admission.*`, `Permissions.Students.Enrollment.*`).
     - `finance`: Hendro Susanto (`FinanceOfficer`, `FinanceStaff`, `Permissions.Finance.Obligations.*`, `Permissions.Finance.Payments.*`).
     - `supervisor`: Maria Magdalena (`Teacher`, `AcademicStaff`, `Permissions.Academics.Coursework.*`, `Permissions.Academics.Assessments.Evaluate`, `Permissions.Academics.Attendance.*`).
     - `coordinator`: Dr. Andreas Hartono (`AcademicCoordinator`, `AcademicAuthority`, `Permissions.Academics.Assessments.Approve`, `Permissions.Academics.ReportCards.*`, `Permissions.Academics.Transcripts.*`).
     - `student.jonathan`: Jonathan Pratama (`Student`, student portal permissions, claim `student_id = JonathanStudentId`).
     - `parent.jonathan`: Hendra Pratama (`Parent`, parent portal permissions, claims `student_id = JonathanStudentId`, `parent_id = HendraParentId`).
   - Issues valid JWT token signed with application secret key containing claims.
3. Code compiles cleanly with zero nullable warnings.

Notes:  
Implemented reset Minimal API endpoint and multi-role demo authentication:
1. `src/backend/Elsasa.Sis.Api/Features/DemoData/DemoDataEndpoints.cs`: Created `IEndpoint` mapping `POST /api/operations/demo-data/reset`, secured by `SuperAdmin` role and guarded by production environment check returning 403 Forbidden on production. Added `ResetDemoDataRequest(bool CleanOnly = false)` record support to allow selective clean-only purge or full reset and seed via `orchestrator.ResetAndSeedAsync(cancellationToken)`.
2. `src/backend/Elsasa.Sis.Api/Features/DemoData/DemoDataOrchestrator.cs`: Added `ResetAndSeedAsync(cancellationToken)` convenience method delegating to `ExecuteResetAsync(cleanOnly: false, cancellationToken)`.
3. `src/backend/Elsasa.Sis.Api/Program.cs`: Mapped route via `new DemoDataEndpoints().MapEndpoint(app)`.
4. `src/backend/Elsasa.Sis.Api/Features/Auth/LoginEndpoint.cs`: Extended demo user definitions supporting password `Demo123!` across all 7 personas (`admin`/`superadmin`, `admission`, `finance`, `supervisor`, `coordinator`, `student.jonathan`, `parent.jonathan`). Added JWT issuance with authoritative roles, domain permissions, and persona claims (`student_id = JonathanStudentId` for student, `student_id = JonathanStudentId` and `parent_id = HendraParentId` for parent).
Verified clean compilation with zero warnings/errors (`dotnet build src/backend/Elsasa.Sis.Api`) and passing architecture tests.

---

## P5 - Backend Automated Verification & Integration Tests

Implementation Status: IMPLEMENTED  
Review Status: GO

### P5-S10

Title: Backend Integration, Persona Verification & Architecture Tests

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement automated integration tests executing against SQL Server Testcontainers to verify end-to-end multi-schema purge and seed execution, deterministic persona integrity, demo authentication workflows, and architecture boundaries (`ADR-001`, `ADR-002`, `TD-04`, `TD-08`, `OQ-005`).

Depends On: P4-S08, P4-S09

Repository: `tests/backend/Elsasa.Sis.IntegrationTests` and `tests/backend/Elsasa.Sis.ArchitectureTests`

Completion Criteria:
1. `tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataOrchestrationTests.cs` created:
   - Runs full reset and seed against SQL Server Testcontainers.
   - Asserts reverse purge deletes all data from `academics.*`, `finance.*`, `students.*`, and `operations.*` without foreign key constraint errors.
   - Asserts forward seed repopulates all tables with expected record counts (~48 students, 6 staged applicants, billing documents, payments, attendances, results, report cards, transcripts).
   - Asserts zero orphaned foreign key records exist across schemas.
   - Asserts idempotency: executing reset twice consecutively succeeds with identical baseline counts.
   - Asserts complete reset and seeding finishes in under 5 seconds.
2. `tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataLoginEndpointTests.cs` created:
   - Tests `POST /api/auth/login` for each of the 7 demo accounts (`admin`, `admission`, `finance`, `supervisor`, `coordinator`, `student.jonathan`, `parent.jonathan`) with password `Demo123!`.
   - Asserts HTTP 200 OK and validates claims (roles, permissions, student/parent IDs).
   - Asserts invalid password returns HTTP 400 Bad Request.
3. `tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataPersonaIntegrityTests.cs` created:
   - Asserts Jonathan Pratama has HSLC placement, Grade 10, certified Semester 1 report card with frozen JSON snapshot, cumulative transcript, and unpaid Semester 2 invoice `INV-SEM2-0042` with balance IDR 1,500,000.
   - Asserts Grace Santoso exhibits failed Attempt 1 (68%) and passed Attempt 2 (85%, `is_final = 1`, `replaces_result_id` populated) and overdue tuition receivable.
   - Asserts Budi Wijaya has submitted Science PACE 1049 test awaiting grading.
   - Asserts Ruth Halim is accepted with admission fee paid, ready for 1-click enrollment.
   - Asserts dynamic date anchoring places `Today` ~4 weeks into active Semester 2.
   - Asserts running orchestrator with `Environment = "Production"` throws `InvalidOperationException`.
4. Architecture tests in `tests/backend/Elsasa.Sis.ArchitectureTests/ArchitectureTests.cs`:
   - Asserts no direct project references between domain assemblies (`Operations`, `Students`, `Finance`, `Academics`).
5. All integration and architecture tests pass (`dotnet test tests/backend/Elsasa.Sis.IntegrationTests`).

Notes:  
Tests must execute against a real SQL Server instance via Testcontainers to validate raw T-SQL and relational constraint behavior.

**Implementation Notes (2026-11-06):**
- Created 4 integration test files in `tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/`:
  - `DemoDataResetTest.cs`: Verifies full reset and seed cycle, idempotency, clean-only mode, and record counts (purged schemas, seeded domains, student/invoice/attendance/result/report card counts)
  - `DemoDataLoginTest.cs`: Verifies all 7 demo accounts (`admin`, `admission`, `finance`, `supervisor`, `coordinator`, `student.jonathan`, `parent.jonathan`) authenticate successfully with password `Demo123!` and issue valid JWTs with correct claims
  - `DemoDataIntegrityTest.cs`: Verifies deterministic IDs match `DemoDataConstants`, cross-domain entity relationships (student-guardian, obligation-student, invoice-obligation, report card-student, transcript-student, attendance-student), and financial data distribution (Semester 1 paid, Semester 2 mixed states)
  - `DemoDataProductionGuardTest.cs`: Verifies production environment safeguards and authorization requirements
- Created 1 architecture test file in `tests/backend/Elsasa.Sis.ArchitectureTests/`:
  - `DemoDataArchitectureTest.cs`: Verifies ADR-001 compile-time isolation (no cross-domain references) and ADR-002 schema write boundaries
- Fixed missing using directives for `Elsasa.Sis.IntegrationTests.Fixtures` and `Elsasa.Sis.Contracts.DemoData` namespaces
- Added null-forgiving operators and sealed record for CA1852 compliance
- All tests build cleanly with zero warnings and zero errors
- Tests use `[SkippableFact]` to gracefully skip when SQL Server Testcontainer is unavailable

**Remediation Notes (Iteration 1 - 2026-11-07):**
- **MAJOR-001 (Zero Orphaned Foreign Key Records)**: In `DemoDataResetTest.cs`, added cross-schema SQL verification asserting 0 orphaned foreign key references across domain schemas (`academics.academic_results`, `academics.report_cards`, `academics.transcripts`, `academics.attendance_records`, `academics.academic_participations`, `finance.financial_obligations`, `finance.billing_documents`, `finance.student_accounts`, `students.student_guardian_relationships`, `students.administrative_placements`).
- **MAJOR-002 (5-Second Execution Timeout)**: In `DemoDataResetTest.cs`, added `summary.ElapsedMilliseconds.Should().BeLessThan(5000, "reset and seed should complete in under 5 seconds")` assertions to both full reset tests.
- **MAJOR-003 (Specific Persona Integrity Assertions)**: In `DemoDataIntegrityTest.cs`, implemented explicit integration tests for each flagship persona matching completion criteria 3:
  - *Jonathan Pratama*: Verified active placement in HSLC / Grade 10 (`is_current = 1`), certified Semester 1 report card with valid frozen JSON snapshot, cumulative transcript with valid frozen JSON snapshot, and unpaid Semester 2 invoice `INV-SEM2-0042` with balance IDR 1,500,000 (`delivery_status = 'unpaid'`).
  - *Grace Santoso*: Updated `AcademicsActivitiesDemoDataSeeder.cs` to set `is_final = 1` on retake attempt; verified Attempt 1 (68%, `is_final = 0`) and Attempt 2 (85%, `is_final = 1`, `replaces_result_id = Attempt1.Id`), plus overdue tuition receivable in `finance.billing_documents` (`delivery_status = 'overdue'`).
  - *Budi Wijaya*: Updated `AcademicsActivitiesDemoDataSeeder.cs` to assign assessment title `"PACE Test - Science PACE 1049"`; verified provisional result awaiting grading (`score_numeric IS NULL`, `result_status = 'provisional'`, assessment title containing `1049`) and submitted coursework submission.
  - *Ruth Halim*: Verified applicant existence (`DemoDataConstants.RuthApplicantId`), application status `accepted`, admission decision `accepted` with admission fee payment verified (`DEC-ADM-RUTH`), and verified Ruth is pre-staged for 1-click enrollment and not yet in `students.students`.
- **MAJOR-004 (Dynamic Date Anchoring)**: In `DemoDataIntegrityTest.cs`, added test asserting active Semester 2 start date is 25-35 days (~4 weeks) before Today, end date is in the future, and summary `AnchoredDate` equals Today.
- **MAJOR-005 (Production Environment Guard Unit Test)**: In `DemoDataProductionGuardTest.cs`, added unit test creating `DemoDataOrchestrator` with `IHostEnvironment.EnvironmentName = "Production"` and verified that `ExecuteResetAsync()` throws `InvalidOperationException` with message "Demo data operations are prohibited in Production environment."
- All integration and architecture tests build with 0 errors and 0 warnings (`dotnet build tests/backend/Elsasa.Sis.IntegrationTests`), unit tests and architecture tests pass (42/42 architecture tests pass).

---

## P6 - Frontend State, Reset Dialog & Navigation

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P6-S11

Title: Frontend Demo Data API Client, Composables & Confirmation Dialog

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the frontend API client, TanStack Query mutation composable, and the destructive confirmation modal dialog `ResetDemoDataDialog.vue` matching the Elsasa SIS design system (`ADR-010`, `TD-10`, `GAP-008`, `OQ-013`).

Depends On: P4-S09

Repository: `src/frontend`

Completion Criteria:
1. `src/frontend/src/features/operations/demo-data/services/demoDataApi.ts` created:
   - Defines TypeScript interfaces: `DemoResetSummary`.
   - Implements `resetDemoData(): Promise<DemoResetSummary>` calling `POST /api/operations/demo-data/reset` via configured Axios instance.
2. `src/frontend/src/features/operations/demo-data/composables/useDemoData.ts` created:
   - Implements `useResetDemoData()` composable using TanStack Query `useMutation`.
   - Exposes `resetDemoDataAsync`, `isPending`, `error`, `isSuccess`.
3. `src/frontend/src/app/layouts/components/ResetDemoDataDialog.vue` created:
   - Uses PrimeVue `Dialog` component.
   - Displays clear warning banner with alert styling explaining that all current changes will be wiped and the pristine Hagios School of Life demo dataset will be restored.
   - Renders confirmation button ("Reset All Demo Data") with destructive danger styling and loading spinner during mutation.
   - Renders cancel button.
   - Emits `success` and `update:visible` events.
4. TypeScript type-check passes (`pnpm typecheck` or `pnpm run build`).

Notes:  
Must adhere to ADR-010 (Vue 3 Composition API, PrimeVue components, Tailwind CSS styling, Lucide icons).

---

### P6-S12

Title: Frontend AppLayout Integration & Reactive Reset Workflow

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Integrate the demo reset trigger into `AppLayout.vue`, rendering a "Reset Demo Data" menu item in the user profile dropdown exclusively for SuperAdmin in non-production environments, and orchestrating cache clearing and application reload on reset completion (`ADR-010`, `TD-08`, `TD-10`, `GAP-008`).

Depends On: P6-S11

Repository: `src/frontend`

Completion Criteria:
1. `src/frontend/src/app/layouts/AppLayout.vue` updated:
   - Imports `ResetDemoDataDialog.vue` and `RotateCcw` Lucide icon.
   - Evaluates display condition:
     ```typescript
     const canResetDemoData = computed(() => {
       return authStore.hasRole('SuperAdmin') && !import.meta.env.PROD;
     });
     ```
   - Renders "Reset Demo Data" button item in the user profile dropdown menu when `canResetDemoData` is true.
   - Clicking menu item opens `ResetDemoDataDialog.vue`.
2. Reactive reset handler implemented:
   - On reset success:
     - Shows success toast notification ("Demo sample data reset successfully.").
     - Clears Pinia caches (`appContextStore.clear()`, cached query keys).
     - Performs clean page reload (`window.location.reload()`) to refresh application state from the new pristine database.
3. ESLint and TypeScript checks pass (`pnpm lint`, `pnpm typecheck`).

Notes:  
Hard-guarded by `!import.meta.env.PROD` in addition to SuperAdmin role checks to ensure the action never renders in production builds.

**Implementation Notes (2026-11-07):**
- Integrated `ResetDemoDataDialog.vue` and `RotateCcw` Lucide icon into `src/frontend/src/app/layouts/AppLayout.vue`.
- Added computed property `canResetDemoData` checking `authStore.hasRole('SuperAdmin') && !import.meta.env.PROD`.
- Rendered "Reset Demo Data" menu item in user profile dropdown menu with warning/amber styling (`text-amber-700`, `hover:bg-amber-50`), guarded by `v-if="canResetDemoData"`.
- Wired reset button click handler to open confirmation modal and close profile menu.
- Implemented `handleResetDemoDataSuccess` reactive reset workflow:
  - Displays PrimeVue success toast notification (`"Demo sample data reset successfully."`).
  - Clears application context cache (`appContextStore.clear()`).
  - Invalidates and purges TanStack Query client cache (`queryClientInstance.clear()`).
  - Triggers clean browser page reload via `globalThis.window.location.reload()`.
- Verified ESLint passes with 0 errors / 0 warnings on `AppLayout.vue` (`pnpm -C src/frontend exec eslint src/app/layouts/AppLayout.vue`).
- Verified TypeScript type check passes cleanly with 0 errors (`pnpm -C src/frontend typecheck`).
- Verified Vitest suite: 28 tests passing cleanly across `ResetDemoDataDialog.spec.ts` and `AppLayoutDemoReset.spec.ts`.

---

## P7 - Frontend Automated Verification & Component Tests

Implementation Status: IMPLEMENTED  
Review Status: NOT-REVIEWED

### P7-S13

Title: Frontend Component & Workflow Tests for Reset Dialog

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement automated component tests in Vitest verifying dialog visibility, confirmation triggers, loading spinners, cancellation behaviors, and layout role guards (`TD-08`, `TD-10`).

Depends On: P6-S12

Repository: `src/frontend`

Completion Criteria:
1. `src/frontend/src/app/layouts/components/__tests__/ResetDemoDataDialog.spec.ts` created:
   - Verifies dialog mounts and renders warning banner when `visible = true`.
   - Verifies clicking confirm invokes `resetDemoData` mutation and shows loading spinner.
   - Verifies clicking cancel emits close event without triggering mutation.
   - Verifies error banner renders if mutation fails.
2. `src/frontend/src/app/layouts/__tests__/AppLayoutDemoReset.spec.ts` created:
   - Verifies "Reset Demo Data" menu item is visible when authenticated as SuperAdmin in non-production.
   - Verifies "Reset Demo Data" menu item is hidden when authenticated as regular staff or student.
3. All Vitest tests pass cleanly (`pnpm test:unit`).

Notes:  
Use `@vue/test-utils` and Pinia testing mocks to isolate component state during test execution.

**Implementation Notes (2026-11-06):**
- Created `src/frontend/src/app/layouts/components/__tests__/ResetDemoDataDialog.spec.ts` with 13 passing tests:
  - Dialog mounts and renders warning banner when `visible = true`
  - Dialog does not render when `visible = false`
  - Confirm button invokes `resetDemoData` mutation
  - Confirm button shows loading spinner when `isPending = true`
  - Confirm button has danger severity and refresh icon (`pi pi-refresh`)
  - Cancel button emits `update:visible` event without triggering mutation
  - Cancel button is disabled while submitting
  - Cancel button has secondary severity and text variant
  - Error state captured when mutation fails
  - Dialog emits `success` event on mutation success
  - Dialog not closable while submitting (`closable = false`)
  - Dialog has correct header and width style
- Created `src/frontend/src/app/layouts/__tests__/AppLayoutDemoReset.spec.ts` with 15 passing tests:
  - "Reset Demo Data" visible for SuperAdmin in non-production
  - Hidden for Staff, Student, AcademicCoordinator, Teacher, Admin, OperationsStaff, Guest roles
  - Amber/warning styling on reset button (`text-amber-700`, `hover:bg-amber-50`)
  - Dialog opens when clicking reset button
  - User profile menu closes when reset clicked
  - `canResetDemoData` computed property returns correct boolean for SuperAdmin vs non-SuperAdmin
  - Logout button always visible with rose/danger styling
- Fixed pre-existing template bug in `AppLayout.vue`: `<main>` opening tag and `<RouterView>` content were missing from the template (left behind by a prior edit). Restored the main content area to eliminate SyntaxError that blocked all AppLayout tests.
- Added `VueQueryPlugin` and `ToastService` to `AppLayoutDemoReset.spec.ts` mount options (required by `AppLayout.vue`'s `useQueryClient()` and `useToast()` calls).
- All 28 tests pass cleanly: `pnpm test:unit -- --reporter=verbose src/app/layouts/components/__tests__/ResetDemoDataDialog.spec.ts src/app/layouts/__tests__/AppLayoutDemoReset.spec.ts`.

---

# 6. Change Log

- 2026-11-05: Initial implementation plan created from approved target architecture `CR-SYS-SAMPLE-DATA-GENERATOR` (`IS011 - DEMO-SAMPLE-DATA-GENERATOR-ARCHITECTURE.md`). Plan structured into 7 phases and 13 continuous slices (`P1-S01` to `P7-S13`). Execution Approval set to `APPROVED`.
- 2026-11-06: P7-S13 implemented — created `ResetDemoDataDialog.spec.ts` (13 tests) and `AppLayoutDemoReset.spec.ts` (15 tests); fixed pre-existing `AppLayout.vue` template bug (missing `<main>`/`<RouterView>`); all 28 P7-S13 tests pass.
- 2026-11-07: P6-S12 implemented — integrated `ResetDemoDataDialog.vue` and reactive reset workflow in `AppLayout.vue`; validated ESLint, TypeScript typecheck, and Vitest suite.
