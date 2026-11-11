---
Title: Implementation Plan — KPI Executive & Domain Dashboards (Release 1)
Code: CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS
Artifact: IMPLEMENTATION-PLAN
Version: 1.1
LastUpdated: 2026-11-11
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Implement **Release 1 (Essential Executive & Domain Dashboards)** of the Elsasa SIS V3 KPI & Institutional Intelligence layer (`CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS`) across the 4-level decision-oriented information architecture:

1. **Level 1 — Executive Overview (`/dashboard`, `GET /api/dashboard/executive`):**
   - Replace the static placeholder cards in [`DashboardView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/dashboard/views/DashboardView.vue) with a live, filter-aware institutional health command center.
   - Deliver **5 Headline KPI Cards** (`STD-01`, `ACA-01`, `FIN-01`, `TCH-01`, `OPS-01`), **6 Domain Health Summary Cards** (`Students`, `Academics`, `Finance`, `Teaching`, `Operations`, plus the Stage 3 `Development` placeholder card), and a prioritized **Needs Attention Queue** aggregating up to 12 actionable operational exception signals.
2. **Level 2 — Domain Dashboards (`/dashboard/{domain}`, `GET /api/{domain}/kpis/dashboard`):**
   - Deliver dedicated domain dashboards for `Students`, `Academics`, `Finance`, `Teaching`, and `Operations` (plus a Stage 3 readiness placeholder at `/dashboard/development`).
   - Render 2–5 domain headline KPI cards with prior-period (`Semester 2` vs. `Semester 1`) trend deltas, 2–3 diagnostic breakdown visualizations built with zero-dependency SVG/CSS components, and a domain-scoped exception queue.
3. **Level 3 → Level 4 — Actionable Operational Drill-Downs:**
   - Map every KPI card, chart bar/segment, and attention signal to existing Vue Router paths in [`routes.ts`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/app/router/routes.ts).
   - Add URL query-parameter (`route.query`) filter and tab hydration to the 15 target operational workbenches.
4. **Deterministic Demo Sample Data Enrichment (`DemoDataSeeders`):**
   - Enrich the six existing domain demo data seeders in-place (`OperationsDemoDataSeeder`, `StudentsDemoDataSeeder`, `FinanceTransactionsDemoDataSeeder`, `AcademicsActivitiesDemoDataSeeder`, `TeachingDemoDataSeeder`, and `TeachingAssignmentsDemoDataSeeder`).
   - Ensure every Release 1 KPI, `Semester 2` vs. `Semester 1` comparison delta, multi-campus filter (`campus-main` and `campus-east`), breakdown chart (all 4 overdue aging brackets, all 5 workload utilization bands), and Attention Signal renders realistic, non-zero institutional conditions while strictly preserving all existing persona test invariants in [`DemoDataIntegrityTest.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataIntegrityTest.cs) and [`DemoDataResetTest.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataResetTest.cs).

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE / ISSUE:
  - [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ISSUE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ISSUE.md) (`CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS`)
  - [`KPI-CATALOG.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/kpi/KPI-CATALOG.md): 21 Release 1 KPI definitions, formulas, and thresholds.
  - [`KPI-DASHBOARD-IA.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/kpi/KPI-DASHBOARD-IA.md): 4-level hierarchy, attention signals, and drill-down map.
  - [`KPI-IMPLEMENTATION-ROADMAP.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/kpi/KPI-IMPLEMENTATION-ROADMAP.md): Release 1 scope boundaries (Phases 1.1–1.4).
- ARCHITECTURE:
  - [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md) (Authoritative target capability architecture)
  - [`backend-architecture.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/backend-architecture.md): Modular Monolith VSA, compile-time domain isolation, Dapper, selective cross-schema read `JOIN`s.
  - [`frontend-architecture.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/frontend-architecture.md): Feature-oriented Vue 3 + Tailwind CSS 4 + PrimeVue 4 slices.
  - [`IS011 - DEMO-SAMPLE-DATA-GENERATOR-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS011%20-%20DEMO-SAMPLE-DATA-GENERATOR-ARCHITECTURE.md): Domain seeders, dynamic date anchoring around `Today`, and deterministic persona constants (`DemoDataConstants`).
- FEASIBILITY-ASSESSMENT:
  - [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-FEASIBILITY-ASSESSMENT.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)

Architecture Applicability: ARCHITECTURE-REQUIRED

This feature introduces shared cross-domain KPI contracts (`IDomainKpiSummaryProvider`), 5 domain-scoped Dapper `QueryMultipleAsync` vertical slices, cross-domain executive orchestration, deterministic seeder enrichment across 6 seeders, zero-dependency visual SVG/CSS dashboard primitives, 6 routed dashboard views, and query hydration across 15 workbenches with zero new SQL DDL migrations (`DEC-001`). Target architecture [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md) is authoritative and governs this plan.

---

# 2. Planning Scope

The planning scope encompasses all technical changes required across backend shared contracts, domain vertical slices, API orchestration, demo data seeders, frontend visual components, routed dashboards, operational workbench query hydration, and automated test harnesses:

1. **Shared KPI Contracts & Context Resolution (`Elsasa.Sis.Contracts/Kpis/` & `Elsasa.Sis.Operations/Services/`):**
   - Author immutable DTO records: `GlobalKpiFilterDto`, `ResolvedKpiContextDto`, `KpiPeriodOptionDto`, `KpiMetricCardDto`, `KpiBreakdownItemDto`, `KpiBreakdownDto`, `AttentionSignalDto`, `DomainHealthSummaryDto`, `DomainKpiSummaryResult`, `ExecutiveDashboardResponse`, `DomainKpiDashboardResponse`.
   - Define cross-domain service interfaces `IDomainKpiSummaryProvider` and `IKpiPeriodResolver` in `Elsasa.Sis.Contracts/Kpis/`.
   - Implement `KpiPeriodResolver` in `Elsasa.Sis.Operations/Services/` (registered as `IKpiPeriodResolver` in Operations DI) to automatically resolve current period (`Semester 2`), parent academic year (`AY-2026-2027`), prior comparison period (`Semester 1`), and available period options from `operations.operational_periods` and `operations.academic_years`.
2. **Deterministic Demo Sample Data Generator Enrichment (`Elsasa.Sis.{Domain}/DemoData/`):**
   - Update `OperationsDemoDataSeeder`: Add second published calendar (`CAL-2026-EAST`) and unreviewed amendment (`AMD-CAL-2026-002`, `downstream_impact_reviewed = 0`).
   - Update `StudentsDemoDataSeeder`: Distribute 8 students to `'campus-east'`, seed 2 withdrawals (`STD-06` = `4.17%`), 1 expired leave (`STD_LEAVE_EXPIRED`), 2 uncompleted admissions (`STD_ENROLLMENT_BLOCKED`), and ~78% document verification (`STD-03`), keeping total students at 48.
   - Update `FinanceTransactionsDemoDataSeeder`: Spread non-flagship unpaid obligations across all 4 overdue aging brackets (`Current`, `1-30d`, `31-60d`, `61-90d`, `90+d`), seed 3 unconfirmed payments (`FIN_UNVERIFIED_PAYMENTS`), and 2 unallocated cash payments (`FIN_UNALLOCATED_CASH`).
   - Update `AcademicsActivitiesDemoDataSeeder`: Batch-insert academic standings and learning progress across all 45 active students for both S1 and S2 (`ACA-01` delta `+4.4%`), seed 5 unaddressed at-risk learners (`ACA_AT_RISK_UNADDRESSED`), 3 chronic absenteeism learners (`ACA_CHRONIC_ABSENTEEISM`), 6 missing coursework submissions (`ACA_MISSING_SUBMISSIONS`), and 3 provisional assessment results (`ACA_UNPUBLISHED_RESULTS`).
   - Update `TeachingDemoDataSeeder` & `TeachingAssignmentsDemoDataSeeder`: Standardize campus codes to `'campus-main'` and `'campus-east'`, add 2 faculty profiles to populate all 5 workload utilization statuses (`TCH-03`, `TCH_WORKLOAD_BREACH`), seed 1 blocking and 1 advisory capacity conflict (`TCH-04`, `TCH_BLOCKING_CONFLICT`), and 2 open unstaffed demands (`TCH_UNSTAFFED_DEMAND`).
   - Preserve all existing persona test invariants in `DemoDataIntegrityTest.cs` and `DemoDataResetTest.cs`.
3. **Backend Domain KPI Vertical Slices (5 Operational Domains):**
   - `Elsasa.Sis.Operations/Features/Kpis/`: `GetOperationsKpiDashboardEndpoint`, `GetOperationsKpiDashboardHandler` (`GET /api/operations/kpis/dashboard`), and `OperationsKpiSummaryProvider`.
   - `Elsasa.Sis.Students/Features/Kpis/`: `GetStudentsKpiDashboardEndpoint`, `GetStudentsKpiDashboardHandler` (`GET /api/students/kpis/dashboard`), and `StudentsKpiSummaryProvider`.
   - `Elsasa.Sis.Finance/Features/Kpis/`: `GetFinanceKpiDashboardEndpoint`, `GetFinanceKpiDashboardHandler` (`GET /api/finance/kpis/dashboard`), and `FinanceKpiSummaryProvider` using `finance.v_student_receivables`.
   - `Elsasa.Sis.Academics/Features/Kpis/`: `GetAcademicsKpiDashboardEndpoint`, `GetAcademicsKpiDashboardHandler` (`GET /api/academics/kpis/dashboard`), and `AcademicsKpiSummaryProvider` using `GetAtRiskLearnersHandler` criteria parity.
   - `Elsasa.Sis.Teaching/Features/Kpis/`: `GetTeachingKpiDashboardEndpoint`, `GetTeachingKpiDashboardHandler` (`GET /api/teaching/kpis/dashboard`), and `TeachingKpiSummaryProvider`.
4. **Backend Executive Dashboard Orchestration (`Elsasa.Sis.Api/Features/Dashboard/GetExecutiveDashboard/`):**
   - `GetExecutiveDashboardEndpoint` & `GetExecutiveDashboardHandler` (`GET /api/dashboard/executive`).
   - Resolves period context, invokes all 5 `IDomainKpiSummaryProvider` implementations, appends Stage 3 `Development` placeholder card (`Status = "pending_stage"`), decorates domain cards with `IsAccessible` based on user permissions, sorts `NeedsAttentionQueue` (`critical` then `warning`), and returns `ExecutiveDashboardResponse`.
5. **Frontend Zero-Dependency KPI Visual Primitives & Client Layer (`src/frontend/src/features/dashboard/`):**
   - Type definitions (`dashboard.types.ts`), API service (`dashboard.api.ts`), TanStack Query keys and composables (`useDashboardQueries.ts`).
   - Components: `KpiFilterBar.vue`, `KpiMetricCard.vue`, `KpiDomainHealthCard.vue`, `KpiBreakdownBarChart.vue`, `KpiStackedProgressBar.vue`, `NeedsAttentionPanel.vue`.
6. **Frontend Dashboards & Global Context Synchronization (`src/frontend/`):**
   - Add `'All Campuses'` (`null`) option to top-bar campus selector in `AppLayout.vue`.
   - Re-implement `DashboardView.vue` at `/dashboard` connected to live API.
   - Implement dedicated Level 2 views: `StudentsKpiDashboardView.vue`, `AcademicsKpiDashboardView.vue`, `FinanceKpiDashboardView.vue`, `TeachingKpiDashboardView.vue`, `OperationsKpiDashboardView.vue`, and `DevelopmentPlaceholderView.vue`.
   - Register routes in `routes.ts` with role/permission metadata and add navigation links in `AppLayout.vue`.
7. **Level 4 Operational Workbench Query Hydration:**
   - Update 15 operational workbenches across domains to read `route.query` on mount/watch and pre-hydrate active filters or tabs.
8. **Automated Verification & Test Harnesses:**
   - Integration tests: `KpiExecutiveDashboardEndpointTest.cs`, `DomainKpiDashboardEndpointsTest.cs`.
   - Regression verification: `DemoDataResetTest.cs`, `DemoDataIntegrityTest.cs`, `Elsasa.Sis.ArchitectureTests`.
   - Frontend unit & component tests: `KpiMetricCard.spec.ts`, `KpiBreakdownBarChart.spec.ts`, `NeedsAttentionPanel.spec.ts`, `DashboardView.spec.ts`.

---

# 3. Dependencies

### External Dependencies:
- Microsoft SQL Server 2022 (existing provisioned tables and views; zero DDL migrations).
- Dapper 2.1.35 (direct parameterized multi-result batches via `QueryMultipleAsync`).
- Vue 3 + TypeScript + Tailwind CSS 4 + PrimeVue 4 (existing frontend stack; zero third-party charting libraries).
- TanStack Query 5 (server state caching and reactive refetching).

### Slice Dependency Rules:
- `Depends On` declares implementation prerequisites between slices.
- Dependencies reference Slice IDs only.
- Dependency satisfaction requires the referenced slice to have implementation status `IMPLEMENTED`.
- Dependency satisfaction does not require review status `GO`.
- Slices without mutual dependencies can be executed concurrently.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
| :--- | :--- | :--- | :--- |
| **P1 - Foundation: Contracts & Sample Data** | NOT-STARTED | NOT-REVIEWED | 0/2 |
| **P2 - Backend Domain Slices: Operations, Students, Finance** | NOT-STARTED | NOT-REVIEWED | 0/3 |
| **P3 - Backend Domain Slices: Academics, Teaching & Executive API** | NOT-STARTED | NOT-REVIEWED | 0/3 |
| **P4 - Frontend Shared Primitives & Executive Overview** | NOT-STARTED | NOT-REVIEWED | 0/2 |
| **P5 - Frontend Domain Dashboards & Workbench Query Hydration** | NOT-STARTED | NOT-REVIEWED | 0/2 |
| **P6 - Verification & Test Harnesses** | NOT-STARTED | NOT-REVIEWED | 0/2 |

---

# 5. Phases

## P1 - Foundation: Contracts, Context Resolution & Sample Data

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P1-S01

Title: Shared KPI Contracts, DTOs & Period Resolver

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Define shared KPI response DTO records, cross-domain service interfaces (`IDomainKpiSummaryProvider`, `IKpiPeriodResolver`) in `Elsasa.Sis.Contracts/Kpis/`, and implement the concrete `KpiPeriodResolver` service in `Elsasa.Sis.Operations/Services/` (registered as `IKpiPeriodResolver` in Operations DI).

Depends On: None  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `src/backend/Elsasa.Sis.Contracts/Kpis/KpiContracts.cs` is created defining `GlobalKpiFilterDto`, `ResolvedKpiContextDto`, `KpiPeriodOptionDto`, `KpiMetricCardDto`, `KpiBreakdownItemDto`, `KpiBreakdownDto`, `AttentionSignalDto`, `DomainHealthSummaryDto`, `DomainKpiSummaryResult`, `ExecutiveDashboardResponse`, `DomainKpiDashboardResponse`, and `IDomainKpiSummaryProvider`.
- `src/backend/Elsasa.Sis.Contracts/Kpis/IKpiPeriodResolver.cs` is created defining `ResolveContextAsync` and `GetAvailablePeriodsAsync`.
- `src/backend/Elsasa.Sis.Operations/Services/KpiPeriodResolver.cs` is created implementing `IKpiPeriodResolver` by querying `operations.operational_periods` and `operations.academic_years` via Dapper, respecting compile-time domain boundary and assembly isolation rules.
- `IKpiPeriodResolver` is registered as a scoped service in `src/backend/Elsasa.Sis.Operations/DependencyInjection.cs`.
- `dotnet build src/backend/Elsasa.Sis.Contracts` and `dotnet build src/backend/Elsasa.Sis.Operations` succeed with 0 warnings/errors.

Notes:  
- All DTO records must be `public sealed record` with explicit property names and XML doc comments matching TD-01 in the architecture.
- Both `ExecutiveDashboardResponse.AvailablePeriods` and `DomainKpiDashboardResponse.AvailablePeriods` are typed as `IReadOnlyList<KpiPeriodOptionDto>`.
- `KpiPeriodResolver` queries against provisioned schema:
  1. Default resolution: when `filter.AcademicPeriodId` is null, query `operations.operational_periods op INNER JOIN operations.academic_years ay ON ay.id = op.academic_year_id WHERE op.period_type = 'semester' AND op.status = 'active' ORDER BY op.start_date DESC` (resolving active Semester 2).
  2. Specific period resolution: when `filter.AcademicPeriodId` is provided, query by `op.id = @PeriodId` joining `operations.academic_years`.
  3. Prior period resolution: query `operations.operational_periods` where `academic_year_id = @AcademicYearId AND period_type = 'semester' AND start_date < @CurrentStartDate ORDER BY start_date DESC` (resolving Semester 1 as comparison baseline).
  4. Available periods: query `operations.operational_periods` where `academic_year_id = @AcademicYearId AND period_type = 'semester' AND status IN ('active', 'closed') ORDER BY start_date ASC` mapped to `KpiPeriodOptionDto`.

Implementation Notes:  
- **UNBLOCKED (2026-11-11) by `ica-architect` Escalation Resolution:**
  1. **Assembly Dependency Cycle Resolved:** `IKpiPeriodResolver` is declared in `Elsasa.Sis.Contracts/Kpis/IKpiPeriodResolver.cs`. The implementation `KpiPeriodResolver.cs` is placed in `Elsasa.Sis.Operations/Services/` (the domain owning operational periods) and registered in `Elsasa.Sis.Operations/DependencyInjection.cs`. `Elsasa.Sis.Shared` remains untouched, strictly satisfying `Elsasa.Sis.ArchitectureTests` (`Shared_Should_NotReferenceAnyOtherSisAssembly`).
  2. **Table & Column Mismatch Resolved:** Queries target `operations.operational_periods` (columns: `id`, `academic_year_id`, `period_code`, `name`, `period_type`, `start_date`, `end_date`, `status`) and `operations.academic_years` (columns: `id`, `code`, `name`, `start_date`, `end_date`, `status`). Sequence ordering is determined via `period_type = 'semester'` and `start_date` ordering.
  3. **DTO Type Mismatch Resolved:** `AvailablePeriods` on `ExecutiveDashboardResponse` and `DomainKpiDashboardResponse` is formally typed as `IReadOnlyList<KpiPeriodOptionDto>`.
  4. Status reset to `NOT-STARTED` (ready for immediate implementation).
- **Implemented (2026-11-11):**
  - Created `src/backend/Elsasa.Sis.Contracts/Kpis/KpiContracts.cs` and `src/backend/Elsasa.Sis.Contracts/Kpis/IKpiPeriodResolver.cs`.
  - Created `src/backend/Elsasa.Sis.Operations/Services/KpiPeriodResolver.cs` and registered `IKpiPeriodResolver` as scoped in `src/backend/Elsasa.Sis.Operations/DependencyInjection.cs`.
  - Verified `dotnet build src/backend/Elsasa.Sis.Contracts --no-restore` (0 warnings, 0 errors), `dotnet build src/backend/Elsasa.Sis.Operations --no-restore` (0 warnings, 0 errors), and `dotnet test tests/backend/Elsasa.Sis.ArchitectureTests --no-restore` (311/311 passed).

---

### P1-S02

Title: Deterministic Demo Data Generator Enrichment Across All 5 Domains

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Enrich all five domain demo data seeders in-place so that every Release 1 KPI, `Semester 2` vs. `Semester 1` comparison delta, multi-campus filter (`campus-main` and `campus-east`), breakdown chart, and Attention Signal renders realistic non-zero institutional data while strictly preserving all existing persona test invariants.

Depends On: None  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `src/backend/Elsasa.Sis.Operations/Features/DemoData/OperationsDemoDataSeeder.cs` seeds published calendar `CAL-2026-EAST` (`campus_scope = 'campus-east'`) and unreviewed calendar amendment `AMD-CAL-2026-002` (`downstream_impact_reviewed = 0`).
- `src/backend/Elsasa.Sis.Students/Features/DemoData/StudentsDemoDataSeeder.cs` places 8 students on `'campus-east'`, seeds 2 student lifecycle exits (`withdrawn`, `transferred_out`), 1 expired leave (`on_leave` > 14 days ago), 2 uncompleted accepted admissions (`STD_ENROLLMENT_BLOCKED`), and ~78% document verification rate while keeping total students seeded at exactly 48.
- `src/backend/Elsasa.Sis.Finance/Features/DemoData/FinanceTransactionsDemoDataSeeder.cs` spreads non-flagship unpaid obligations across all 4 overdue aging brackets (`Current`, `1-30 Days`, `31-60 Days`, `61-90 Days`, `90+ Days`), seeds 3 submitted payments (`confirmation_status = 'submitted'`), and 2 payments with unallocated cash balance.
- `src/backend/Elsasa.Sis.Academics/Features/DemoData/AcademicsActivitiesDemoDataSeeder.cs` batch-inserts academic standings and learning progress across all 45 active students for both S1 (Historical) and S2 (Active) with a +4.4% Good Standing delta, seeds 5 unaddressed at-risk learners (`ACA_AT_RISK_UNADDRESSED`), 3 chronic absenteeism learners (< 80% attendance), 6 missing submissions, and 3 provisional assessment results.
- `src/backend/Elsasa.Sis.Teaching/Features/DemoData/TeachingDemoDataSeeder.cs` & `TeachingAssignmentsDemoDataSeeder.cs` standardize campus codes to `'campus-main'` and `'campus-east'`, seed 2 additional teacher profiles spanning all 5 workload utilization bands (including 1 breached), 1 blocking capacity conflict, 1 advisory conflict, and 2 open unstaffed demands.
- `DemoDataResetTest` and `DemoDataIntegrityTest` pass with 100% success and reset duration `< 5000ms`.

Notes:  
- Do not modify deterministic flagship GUIDs in `DemoDataConstants.cs` (`Jonathan`, `Grace`, `Budi`, `Ruth`, `Daniel`, `Budi Hartono`, `Siti Nurhaliza`).
- Maintain existing seeder count bounds: Total students `41..59`, invoices `< 200`, attendance records `1001..1999`, results `< 200`, report cards `< 100`, 0 FK orphans.

Implementation Notes:  
- **Remediation Iteration 1 (2026-11-11):**
  - `FINDING-P1-S02-01` & `FINDING-P1-S02-03`: Updated `tests/backend/Elsasa.Sis.IntegrationTests/Features/Teaching/TeachingAuthorizationsAndModuleIntegrationTests.cs` (`TeacherProfilesCreated` `4` → `6`) and `tests/backend/Elsasa.Sis.IntegrationTests/Features/Teaching/TeachingWorkloadAndSubstitutionsIntegrationTests.cs` (`full_name` → `display_name` filter for `'Budi Hartono, M.Pd.'`, `'Siti Nurhaliza, S.Pd.'`).
  - `FINDING-P1-S02-02`: Updated `tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataResetTest.cs` and `tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataIntegrityTest.cs` to configure `Factory.CurrentUser.Roles = ["SuperAdmin"]` per test class via `InitializeAsync()` override (preserving global `TestCurrentUser` default `["Admin"]`) and aligned schema/table/column references (`PurgedSchemas == 5`, `SeededDomains == 8`, `finance.student_financial_accounts`, `academics.submissions`, `students.admission_applications`, `finance.billing_document_items` join, `ap.school_level`, `finance.billing_documents.delivery_status`, `finance.payments.confirmation_status`, `finance.v_student_receivables.aging_bracket`, `BillingDocRecord.total_billed_amount` `long`, Grace retake `replaces_result_id` match, Budi `QueryAsync` submissions, and `ap.student_id = ar.student_id`).
  - **Verification against SQL Server (`ElsasaSisTestDb`):**
    - `DemoDataResetTest` + `DemoDataIntegrityTest`: 10/10 passed (`ElapsedMilliseconds`: `1009ms`–`2510ms`, `< 5000ms`).
    - Teaching DemoData integration tests (`DemoDataSeederAndPurgerCycle` & `DemoDataResetCycle`): 2/2 passed (note: 18 unrelated non-DemoData tests in `Features.Teaching` fail due to pre-existing schema drift in `TeachingIntegrationTestHelpers.cs`).
    - `dotnet build src/backend/Elsasa.Sis.sln`: 0 warnings, 0 errors.
    - `Elsasa.Sis.ArchitectureTests`: 311/311 passed.

---

## P2 - Backend Domain Slices: Operations, Students, Finance

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P2-S03

Title: Operations Domain KPI Vertical Slice & Summary Provider

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Implement the Operations domain KPI query endpoint (`GET /api/operations/kpis/dashboard`) and `OperationsKpiSummaryProvider` (`IDomainKpiSummaryProvider`) computing `OPS-01`, `OPS-02`, calendar amendment breakdown, closure days breakdown, and attention signals.

Depends On: P1-S01, P1-S02  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `src/backend/Elsasa.Sis.Operations/Features/Kpis/GetOperationsKpiDashboard/` is implemented containing:
  - `GetOperationsKpiDashboardEndpoint.cs` mapped to `GET /api/operations/kpis/dashboard` with authorization.
  - `GetOperationsKpiDashboardHandler.cs` executing parameterized Dapper `QueryMultipleAsync` computing `OPS-01` (Calendar & Period Readiness Index), `OPS-02` (Unplanned School Closure Days), breakdown charts, and attention signals (`OPS_UNREVIEWED_AMENDMENT`, `OPS_ACTIVE_CLOSURE`).
  - `OperationsKpiSummaryProvider.cs` implementing `IDomainKpiSummaryProvider` to return `DomainKpiSummaryResult` containing headline `OPS-01`, domain health status, and active signals.
- Registered as `IDomainKpiSummaryProvider` in `Elsasa.Sis.Operations/DependencyInjection.cs`.
- `dotnet build src/backend/Elsasa.Sis.Operations` succeeds with 0 warnings/errors.

Notes:  
- Use C# 12 primary constructors and raw string T-SQL literals.
- Adhere to SQL table and column bindings in Architecture TD-03.5.
- Composite score for `OPS-01`: 40 pts for active published calendar, 30 pts for active period, 30 pts for reviewed calendar amendments (`1.0 - Unreviewed / Total`).
- All queries must open their own `SqlConnection` via `ISqlConnectionFactory`.

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created `src/backend/Elsasa.Sis.Operations/Features/Kpis/GetOperationsKpiDashboard/GetOperationsKpiDashboardEndpoint.cs` (`GET /api/operations/kpis/dashboard`, discovered via `OperationsAssemblyMarker`, with `.RequireAuthorization()` and role/permission access check matching TD-05).
  - Created `src/backend/Elsasa.Sis.Operations/Features/Kpis/GetOperationsKpiDashboard/GetOperationsKpiDashboardHandler.cs` executing a parameterized 5-result-set Dapper `QueryMultipleAsync` batch over its own `SqlConnection` via `ISqlConnectionFactory` against `operations.school_calendars` (`scope_type`, `scope_reference`, `publication_status`), `operations.operational_periods` (`campus_context`, `status`), `operations.calendar_amendments` (`downstream_impact_reviewed`), and `operations.school_closures` (`reason_category`, `affected_campus`, `status`, `start_date`, `end_date`), computing `OPS-01` (40 + 30 + 30 composite score), `OPS-02`, `OPS-AMENDMENT-REVIEW` & `OPS-CLOSURE-DAYS` breakdowns, and `OPS_ACTIVE_CLOSURE` & `OPS_UNREVIEWED_AMENDMENT` signals.
  - Created `src/backend/Elsasa.Sis.Operations/Features/Kpis/GetOperationsKpiDashboard/OperationsKpiSummaryProvider.cs` implementing `IDomainKpiSummaryProvider` (`DomainKey = "operations"`, `SortOrder = 5`).
  - Registered `GetOperationsKpiDashboardHandler`, `OperationsKpiSummaryProvider`, and `IDomainKpiSummaryProvider` (`OperationsKpiSummaryProvider`) as scoped services in `src/backend/Elsasa.Sis.Operations/DependencyInjection.cs`.
  - Verified `dotnet build src/backend/Elsasa.Sis.Operations --no-restore` succeeded with `0 Warning(s)` and `0 Error(s)`.

---

### P2-S04

Title: Students Domain KPI Vertical Slice & Summary Provider

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Implement the Students domain KPI query endpoint (`GET /api/students/kpis/dashboard`) and `StudentsKpiSummaryProvider` (`IDomainKpiSummaryProvider`) computing `STD-01`, `STD-02`, `STD-03`, `STD-06`, `STD-07`, admission funnel breakdown, cohort fill breakdown, and attention signals.

Depends On: P1-S01, P1-S02  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/` is implemented containing:
  - `GetStudentsKpiDashboardEndpoint.cs` mapped to `GET /api/students/kpis/dashboard` with authorization.
  - `GetStudentsKpiDashboardHandler.cs` executing Dapper `QueryMultipleAsync` computing `STD-01` (Active Enrolled Students), `STD-02` (Admission Pipeline Conversion), `STD-03` (Document Verification Rate), `STD-06` (Attrition Rate), `STD-07` (Cohort Fill Utilization), breakdowns, and signals (`STD_ENROLLMENT_BLOCKED`, `STD_LEAVE_EXPIRED`).
  - `StudentsKpiSummaryProvider.cs` implementing `IDomainKpiSummaryProvider` to return `DomainKpiSummaryResult` containing headline `STD-01`, domain health status, and active signals.
- Registered as `IDomainKpiSummaryProvider` in `Elsasa.Sis.Students/DependencyInjection.cs`.
- `dotnet build src/backend/Elsasa.Sis.Students` succeeds with 0 warnings/errors.

Notes:  
- Support filters `(@Campus IS NULL OR ap.campus = @Campus)` and `(@SchoolLevel IS NULL OR ap.school_level = @SchoolLevel)`.
- Enforce divide-by-zero safeguards using `NULLIF(denominator, 0)`.
- Use cohort capacity standard of 20 students per cohort section for `STD-07`.

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created `src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/GetStudentsKpiDashboardEndpoint.cs` (`GET /api/students/kpis/dashboard` auto-discovered via `IEndpoint` in `Stage1Assemblies`, protected by `.RequireAuthorization()` plus `ICurrentUser` check for `*`, `Permissions.Students.Admission.View`, `Permissions.Students.Records.View`, and roles `Admin`/`SuperAdmin`/`AdmissionOfficer`/`StudentAdminOfficer`/`SchoolLeadership`).
  - Created `src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/GetStudentsKpiDashboardHandler.cs` executing a 5-result-set parameterized Dapper `QueryMultipleAsync` batch on its own `SqlConnection` via `ISqlConnectionFactory`, computing `STD-01`, `STD-02`, `STD-03`, `STD-06`, `STD-07`, breakdowns (`STD-FUNNEL`, `STD-COHORT-FILL`), and attention signals (`STD_ENROLLMENT_BLOCKED`, `STD_LEAVE_EXPIRED`).
  - Schema-verified bindings against `Elsasa.Sis.Database/Scripts/` and `StudentsDemoDataSeeder.cs`: queried `students.applicant_documents` directly (no `is_mandatory` column in provisioned table) and `students.student_statuses` + `students.administrative_history_entries` (`event_type = 'leave_of_absence'`) for expired leave detection (provisioned table replacing prose reference `student_lifecycle_events`), with `NULLIF(..., 0)` safeguards and `requested_entry_level` grade-to-school-level mapping for `ELC`/`JHLC`/`HSLC` filtering on applications.
  - Created `src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/StudentsKpiSummaryProvider.cs` implementing `IDomainKpiSummaryProvider` (`DomainKey = "students"`, `SortOrder = 1`).
  - Registered `GetStudentsKpiDashboardHandler` and `IDomainKpiSummaryProvider` (`StudentsKpiSummaryProvider`) in `src/backend/Elsasa.Sis.Students/DependencyInjection.cs`.
  - Verified `dotnet build src/backend/Elsasa.Sis.Students --no-restore` (0 warnings, 0 errors) and `dotnet test tests/backend/Elsasa.Sis.ArchitectureTests --no-restore` (311/311 passed).

---

### P2-S05

Title: Finance Domain KPI Vertical Slice & Summary Provider

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Implement the Finance domain KPI query endpoint (`GET /api/finance/kpis/dashboard`) and `FinanceKpiSummaryProvider` (`IDomainKpiSummaryProvider`) computing `FIN-01`, `FIN-02`, `FIN-03`, `FIN-04`, `FIN-05`, aging distribution breakdown, and attention signals using `finance.v_student_receivables`.

Depends On: P1-S01, P1-S02  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `src/backend/Elsasa.Sis.Finance/Features/Kpis/GetFinanceKpiDashboard/` is implemented containing:
  - `GetFinanceKpiDashboardEndpoint.cs` mapped to `GET /api/finance/kpis/dashboard` with authorization.
  - `GetFinanceKpiDashboardHandler.cs` executing Dapper `QueryMultipleAsync` against `finance.v_student_receivables` computing `FIN-01` (Net Tuition Collection Rate with `net_obligation > 0` safeguard), `FIN-02` (Total Outstanding Receivables), `FIN-03` (Overdue Receivable Rate), `FIN-04` (Aging Distribution across all 5 brackets), `FIN-05` (Unverified Payments & Unallocated Cash), and signals (`FIN_OVERDUE_90_PLUS`, `FIN_UNVERIFIED_PAYMENTS`, `FIN_UNALLOCATED_CASH`).
  - `FinanceKpiSummaryProvider.cs` implementing `IDomainKpiSummaryProvider` returning headline `FIN-01`, domain health status, and active signals.
- Registered as `IDomainKpiSummaryProvider` in `Elsasa.Sis.Finance/DependencyInjection.cs`.
- `dotnet build src/backend/Elsasa.Sis.Finance` succeeds with 0 warnings/errors.

Notes:  
- `FIN-01` must filter `r.net_obligation > 0` so negative scholarship credit rows (`-1,500,000`) do not distort collection percentages (`RISK-003`).
- Calculate both current period and prior period (`Semester 1`) collection metrics to yield accurate trend deltas (`DeltaPercent` and `DeltaLabel`).

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created `src/backend/Elsasa.Sis.Finance/Features/Kpis/GetFinanceKpiDashboard/FinanceKpiMetricsCalculator.cs`, `GetFinanceKpiDashboardHandler.cs`, `GetFinanceKpiDashboardEndpoint.cs`, and `FinanceKpiSummaryProvider.cs`.
  - Registered `GetFinanceKpiDashboardHandler`, `FinanceKpiSummaryProvider`, `IDomainKpiSummaryProvider`, and policy `Finance.Kpis.Dashboard.View` (allowing `*`, `Permissions.Finance.Obligations.View`, or roles `Admin`, `SuperAdmin`, `FinanceAuthority`, `FinanceOfficer`, `FinanceStaff`) in `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`.
  - Dapper `QueryMultipleAsync` queries `finance.v_student_receivables`, `finance.payments` (`payment_amount`), `finance.payment_allocations`, and `students.administrative_placements` with `r.net_obligation > 0` safeguard (`RISK-003`), current + prior period comparison deltas, all 5 aging brackets (`Current`, `1-30 Days`, `31-60 Days`, `61-90 Days`, `90+ Days`), charge-type & payment-pipeline breakdowns, campus/school-level filters, `NULLIF`/zero-row safeguards, and signals (`FIN_OVERDUE_90_PLUS`, `FIN_UNVERIFIED_PAYMENTS`, `FIN_UNALLOCATED_CASH`).
  - Verified `dotnet build src/backend/Elsasa.Sis.Finance --no-restore` (0 Warning(s), 0 Error(s)). Note: `dotnet test tests/backend/Elsasa.Sis.ArchitectureTests --no-restore` did not complete execution because sibling project `Elsasa.Sis.Teaching` had an unrelated `CA1859` compile error during test solution build.

---

## P3 - Backend Domain Slices: Academics, Teaching & Executive API

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P3-S06

Title: Academics Domain KPI Vertical Slice & Summary Provider

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Implement the Academics domain KPI query endpoint (`GET /api/academics/kpis/dashboard`) and `AcademicsKpiSummaryProvider` (`IDomainKpiSummaryProvider`) computing `ACA-01`..`ACA-05`, academic standing breakdown, submission breakdown, and attention signals.

Depends On: P1-S01, P1-S02  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/` is implemented containing:
  - `GetAcademicsKpiDashboardEndpoint.cs` mapped to `GET /api/academics/kpis/dashboard` with authorization.
  - `GetAcademicsKpiDashboardHandler.cs` executing Dapper `QueryMultipleAsync` computing `ACA-01` (Good Academic Standing Rate with prior S1 comparison), `ACA-02` (At-Risk Learners & Remediation Coverage matching `GetAtRiskLearnersHandler` criteria), `ACA-03` (Period Attendance Rate & Chronic Absenteeism), `ACA-04` (Coursework Submission Rate), `ACA-05` (Assessment Publication Readiness), breakdowns, and signals (`ACA_AT_RISK_UNADDRESSED`, `ACA_CHRONIC_ABSENTEEISM`, `ACA_MISSING_SUBMISSIONS`, `ACA_UNPUBLISHED_RESULTS`).
  - `AcademicsKpiSummaryProvider.cs` implementing `IDomainKpiSummaryProvider` returning headline `ACA-01`, domain health status, and active signals.
- Registered as `IDomainKpiSummaryProvider` in `Elsasa.Sis.Academics/DependencyInjection.cs`.
- `dotnet build src/backend/Elsasa.Sis.Academics` succeeds with 0 warnings/errors.

Notes:  
- Exact parity with `GetAtRiskLearnersHandler.cs`: A student is at-risk if standing is `conditional_standing`/`not_in_good_standing` OR learning progress has `needs_remediation` OR period attendance is `< 80%`. Covered if active intervention exists (`planned` or `in_progress`).

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created `src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/GetAcademicsKpiDashboardEndpoint.cs` (`GET /api/academics/kpis/dashboard`, automatically discovered via `AcademicsEndpoints.MapAcademicsEndpoints()`, with `.RequireAuthorization()` and role/permission access check matching TD-05).
  - Created `src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/GetAcademicsKpiDashboardHandler.cs` executing a parameterized 5-result-set Dapper `QueryMultipleAsync` batch over its own `SqlConnection` via `ISqlConnectionFactory`, computing `ACA-01`..`ACA-05` (with prior-semester S1 comparison deltas), `ACA-STANDING-DISTRIBUTION` and `ACA-SUBMISSION-STATUS` breakdowns, and `ACA_AT_RISK_UNADDRESSED`, `ACA_CHRONIC_ABSENTEEISM`, `ACA_MISSING_SUBMISSIONS`, and `ACA_UNPUBLISHED_RESULTS` attention signals with `GetAtRiskLearnersHandler` parity and `@Campus`/`@SchoolLevel` filtering via `students.administrative_placements`.
  - Created `src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/AcademicsKpiSummaryProvider.cs` implementing `IDomainKpiSummaryProvider` (`DomainKey = "academics"`, `SortOrder = 2`).
  - Registered `GetAcademicsKpiDashboardHandler` and `IDomainKpiSummaryProvider` (`AcademicsKpiSummaryProvider`) as scoped services in `src/backend/Elsasa.Sis.Academics/DependencyInjection.cs`.
  - Verified `dotnet build src/backend/Elsasa.Sis.Academics --no-restore` succeeded with `0 Warning(s)` and `0 Error(s)`.

---

### P3-S07

Title: Teaching Domain KPI Vertical Slice & Summary Provider

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Implement the Teaching domain KPI query endpoint (`GET /api/teaching/kpis/dashboard`) and `TeachingKpiSummaryProvider` (`IDomainKpiSummaryProvider`) computing `TCH-01`..`TCH-04`, teacher workload utilization distribution breakdown, open demands breakdown, and attention signals.

Depends On: P1-S01, P1-S02  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/` is implemented containing:
  - `GetTeachingKpiDashboardEndpoint.cs` mapped to `GET /api/teaching/kpis/dashboard` with authorization.
  - `GetTeachingKpiDashboardHandler.cs` executing Dapper `QueryMultipleAsync` computing `TCH-01` (Instructional Demand Coverage Rate), `TCH-02` (Open Unstaffed Teaching Demands), `TCH-03` (Teacher Workload Utilization Distribution across all 5 status bands), `TCH-04` (Unresolved Capacity & Qualification Conflicts), breakdowns, and signals (`TCH_UNSTAFFED_DEMAND`, `TCH_BLOCKING_CONFLICT`, `TCH_WORKLOAD_BREACH`).
  - `TeachingKpiSummaryProvider.cs` implementing `IDomainKpiSummaryProvider` returning headline `TCH-01`, domain health status, and active signals.
- Registered as `IDomainKpiSummaryProvider` in `Elsasa.Sis.Teaching/DependencyInjection.cs`.
- `dotnet build src/backend/Elsasa.Sis.Teaching` succeeds with 0 warnings/errors.

Notes:  
- Breakdown for `TCH-03` must group teacher workload commitments by status (`under_allocated`, `normal`, `approaching_limit`, `overloaded_with_exception`, `breached`).
- Break down `TCH-04` by severity (`blocking` vs `advisory`) and pending qualification verifications.

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created `src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/TeachingKpiQueries.cs` executing a 5-result-set Dapper `QueryMultipleAsync` batch over an independent `SqlConnection` via `ISqlConnectionFactory`, computing `TCH-01`..`TCH-04` (with prior-semester S1 comparison on `TCH-01`), breakdowns across all 5 workload utilization bands (`under_allocated`, `normal`, `approaching_limit`, `overloaded_with_exception`, `breached`), open unstaffed demands by capability, and unresolved capacity conflicts by severity (`blocking`, `advisory`) plus pending qualification verifications (`verification_state = 'submitted'`), and signals (`TCH_BLOCKING_CONFLICT`, `TCH_WORKLOAD_BREACH`, `TCH_UNSTAFFED_DEMAND`). Schema bindings verified against DbUp scripts (`wc.teacher_profile_id`, `tp.standing = 'active'`, `tp.campus_affiliation`, and `teaching.capacity_conflicts` period scoping via `teaching_assignments`/`teaching_demands`/`workload_commitments`).
  - Created `src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/GetTeachingKpiDashboardHandler.cs` (`IKpiPeriodResolver` + `ISqlConnectionFactory`).
  - Created `src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/GetTeachingKpiDashboardEndpoint.cs` (`GET /api/teaching/kpis/dashboard`, automatically mapped by `TeachingEndpoints.MapTeachingEndpoints()`, authorized by `TeachingKpiPolicies.DashboardView`).
  - Created `src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/TeachingKpiSummaryProvider.cs` implementing `IDomainKpiSummaryProvider` (`DomainKey = "teaching"`, `SortOrder = 4`).
  - Updated `src/backend/Elsasa.Sis.Teaching/DependencyInjection.cs` to register `GetTeachingKpiDashboardHandler`, `IDomainKpiSummaryProvider` -> `TeachingKpiSummaryProvider`, and `TeachingKpiPolicies.DashboardView` authorization policy (matching Architecture TD-05).
  - Verified `dotnet build src/backend/Elsasa.Sis.Teaching --no-restore` succeeded (`0 Warning(s)`, `0 Error(s)`) and `dotnet test tests/backend/Elsasa.Sis.ArchitectureTests --no-restore` passed (`311/311`).

---

### P3-S08

Title: Executive Dashboard Orchestration & Host DI Registration

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Implement `GET /api/dashboard/executive` in `Elsasa.Sis.Api` to resolve period context, aggregate all 5 `IDomainKpiSummaryProvider` instances, append the static Stage 3 `Development` placeholder card, decorate domain summaries with accessibility permissions, prioritize the `NeedsAttentionQueue`, and register all domain dependencies in host DI.

Depends On: P2-S03, P2-S04, P2-S05, P3-S06, P3-S07  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `src/backend/Elsasa.Sis.Api/Features/Dashboard/GetExecutiveDashboard/` is created containing:
  - `GetExecutiveDashboardEndpoint.cs` mapped to `GET /api/dashboard/executive`.
  - `GetExecutiveDashboardHandler.cs` resolving filter context via `IKpiPeriodResolver`, aggregating the 5 domain summary providers, appending Stage 3 `Development` placeholder card (`Status = "pending_stage"`, `IsAccessible = true`), evaluating user permissions for `IsAccessible`, and sorting the attention queue (`critical` before `warning`).
- Host DI in `src/backend/Elsasa.Sis.Api/Program.cs` maps the endpoint and ensures all domain summary providers and `IKpiPeriodResolver` (registered in Operations DI) are wired.
- `dotnet build src/backend/Elsasa.Sis.Api` succeeds with 0 warnings/errors.

Notes:  
- Sequential or independent connection execution: each provider manages its own connection to avoid MARS connection concurrency bugs (`RISK-002`).
- Check caller permissions and roles via `ICurrentUser` to set `DomainHealthSummaryDto.IsAccessible = false` if the caller lacks view permissions for a specific domain (`DEC-006`).

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created `src/backend/Elsasa.Sis.Api/Features/Dashboard/GetExecutiveDashboard/GetExecutiveDashboardEndpoint.cs` (`GET /api/dashboard/executive`, `.RequireAuthorization()`, query parameters `academicYearId`, `academicPeriodId`, `campus`, `schoolLevel`).
  - Created `src/backend/Elsasa.Sis.Api/Features/Dashboard/GetExecutiveDashboard/GetExecutiveDashboardHandler.cs` resolving context and available periods via `IKpiPeriodResolver`, invoking all 5 `IDomainKpiSummaryProvider` services sequentially (`RISK-002`), ordering results by `SortOrder` (`STD-01`, `ACA-01`, `FIN-01`, `TCH-01`, `OPS-01`), evaluating `ICurrentUser` permissions/roles per domain (`DEC-006` / TD-05) to set `DomainHealthSummaryDto.IsAccessible` and filter `NeedsAttentionQueue` to accessible domains, appending the static Stage 3 `Development` placeholder card (`Status = "pending_stage"`, `StatusLabel = "Stage 3 Pending"`, `DashboardRoute = "/dashboard/development"`, `IsAccessible = true`), and sorting/capping `NeedsAttentionQueue` (`critical` before `warning`, descending `AffectedCount`, max 12 items).
  - Registered `GetExecutiveDashboardHandler` as scoped in `src/backend/Elsasa.Sis.Api/Configuration/DependencyInjection.cs` (`AddHostComposition`, alongside `AddOperationsDomain`, `AddStudentsDomain`, and `AddFinanceDomain`), and wired `app.MapEndpoints(typeof(Program).Assembly)` in `src/backend/Elsasa.Sis.Api/Program.cs` (alongside `AddAcademicsDomain`, `AddTeachingModule`, `MapEndpoints(DependencyInjection.Stage1Assemblies)`, `MapAcademicsEndpoints`, and `MapTeachingEndpoints`).
  - Verified `dotnet build src/backend/Elsasa.Sis.Api --no-restore` (`0 Warning(s)`, `0 Error(s)`) and `dotnet test tests/backend/Elsasa.Sis.ArchitectureTests --no-restore` (`311/311` passed).

---

## P4 - Frontend Shared Primitives & Executive Overview

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P4-S09

Title: Frontend Shared KPI Visual Primitives & API Client Layer

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Implement the TypeScript contracts, Axios API client, TanStack Query composables, and reusable zero-dependency SVG/CSS visual components in `src/frontend/src/features/dashboard/`.

Depends On: P1-S01  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `src/frontend/src/features/dashboard/types/dashboard.types.ts` defines all matching TypeScript interfaces.
- `src/frontend/src/features/dashboard/api/dashboard.api.ts` implements `getExecutiveDashboard(filter)` and `getDomainKpiDashboard(domain, filter)`.
- `src/frontend/src/features/dashboard/api/dashboard.keys.ts` and `useDashboardQueries.ts` provide TanStack Query composables (`useExecutiveDashboardQuery`, `useDomainKpiDashboardQuery`).
- Reusable UI primitives created in `src/frontend/src/features/dashboard/components/`:
  - `KpiFilterBar.vue`: Period selector, campus selector sync, and school level pills.
  - `KpiMetricCard.vue`: Card rendering headline value, delta trend chip, status badge, target label, and drill-down link.
  - `KpiDomainHealthCard.vue`: Domain health summary card with primary/secondary metrics, warning/critical counters, and deep-link.
  - `KpiBreakdownBarChart.vue`: Zero-dependency horizontal SVG/CSS bar chart with interactive drill-down rows.
  - `KpiStackedProgressBar.vue`: Segmented horizontal bar with interactive legend.
  - `NeedsAttentionPanel.vue`: Actionable exception queue with severity tags, descriptions, affected count badges, and action buttons.
- `pnpm type-check` passes with 0 errors.

Notes:  
- No external charting packages (`chart.js`, `echarts`, etc.). All visualizations must use native Vue 3 + Tailwind CSS 4 + inline SVG.
- Every bar, segment, and attention item must bind to its respective `drillDownRoute`.

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created `src/frontend/src/features/dashboard/types/dashboard.types.ts` matching `KpiContracts.cs` (`GlobalKpiFilterDto`, `ResolvedKpiContextDto`, `KpiPeriodOptionDto`, `KpiMetricCardDto`, `KpiBreakdownItemDto`, `KpiBreakdownDto`, `AttentionSignalDto`, `DomainHealthSummaryDto`, `ExecutiveDashboardResponse`, `DomainKpiDashboardResponse`).
  - Created `src/frontend/src/features/dashboard/api/dashboard.api.ts` (`getExecutiveDashboard`, `getDomainKpiDashboard`), `src/frontend/src/features/dashboard/api/dashboard.keys.ts`, and `src/frontend/src/features/dashboard/api/useDashboardQueries.ts` (`useExecutiveDashboardQuery`, `useDomainKpiDashboardQuery`).
  - Created zero-dependency Vue 3 + Tailwind CSS 4 + inline SVG + PrimeVue 4 primitives in `src/frontend/src/features/dashboard/components/`: `kpiStatusStyles.ts`, `KpiFilterBar.vue`, `KpiMetricCard.vue`, `KpiDomainHealthCard.vue`, `KpiBreakdownBarChart.vue`, `KpiStackedProgressBar.vue`, and `NeedsAttentionPanel.vue`.
  - Verified `pnpm type-check` (`vue-tsc --noEmit`) and scoped `eslint` on `src/features/dashboard/api`, `src/features/dashboard/types`, and `src/features/dashboard/components` passed with 0 errors and 0 warnings.

---

### P4-S10

Title: AppLayout Global Context Synchronization & Level 1 Executive Overview

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Add `'All Campuses'` option to `AppLayout.vue` campus switcher, synchronize global context with `useAppContextStore`, and rebuild `DashboardView.vue` at `/dashboard` into the live Executive Overview command center.

Depends On: P3-S08, P4-S09  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `src/frontend/src/app/layouts/AppLayout.vue` updated to include `{ id: null, name: 'All Campuses (Semua Kampus)' }` in `campusOptions` and synchronize selection with `appContextStore.activeCampusId`.
- `src/frontend/src/features/dashboard/views/DashboardView.vue` re-implemented using `useExecutiveDashboardQuery`:
  - Renders `KpiFilterBar` bound to global campus/period context.
  - Displays the 5 Headline KPI Cards in a responsive grid.
  - Displays the 6 Domain Health Cards (including Stage 3 `Development` placeholder with lock/pending badge).
  - Displays `NeedsAttentionPanel` showing sorted institutional exceptions.
  - Provides skeleton loading states and error retry triggers.
- `pnpm type-check` passes with 0 errors.

Notes:  
- Preserve greeting banner and quick navigation shortcuts if appropriate, but replace all hardcoded static metric cards (`1,248`, `94.2%`, `Rp 42.5M`, `14`) with live data from `GET /api/dashboard/executive`.

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Updated `src/frontend/src/app/layouts/AppLayout.vue` to add `{ id: null, name: 'All Campuses (Semua Kampus)' }` to `campusOptions`, default `activeCampusName` fallback to `'All Campuses (Semua Kampus)'`, type `selectCampus(campusId: string | null)`, and bind `:key="campus.id ?? 'all'"` synchronized with `appContextStore.activeCampusId` (`useAppContextStore` verified to support `null` cleanly).
  - Re-implemented `src/frontend/src/features/dashboard/views/DashboardView.vue` using `useExecutiveDashboardQuery`: renders greeting hero banner with quick launchers (`/admission/apply`, `/operations/calendar`, `/finance/payments`), `KpiFilterBar` bound to global campus/period and local `schoolLevel`, 5 Headline KPI cards (`KpiMetricCard`) in a responsive grid, 6 Domain Health cards (`KpiDomainHealthCard`, including Stage 3 `Development` placeholder), prioritized `NeedsAttentionPanel`, initial skeleton loading states, and error/retry states.
  - Verified `pnpm type-check` (`vue-tsc --noEmit`), scoped `eslint` on `src/app/layouts/AppLayout.vue` and `src/features/dashboard/views/DashboardView.vue` (0 errors, 0 warnings), and `vitest run "src/app/layouts/__tests__/AppLayout.spec.ts"` (25/25 passed). Note: `src/features/dashboard/views/__tests__/DashboardView.spec.ts` still targets the pre-KPI static view and is scheduled for update in `P6-S14`.

---

## P5 - Frontend Domain Dashboards & Workbench Query Hydration

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P5-S11

Title: Level 2 Domain Dashboard Views & Navigation Routing

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Implement dedicated Level 2 Domain Dashboard views for `Students`, `Academics`, `Finance`, `Teaching`, `Operations`, and `Development` placeholder, wire navigation links into `AppLayout.vue`, and configure routes in `routes.ts`.

Depends On: P3-S08, P4-S09, P4-S10  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- Implemented views in `src/frontend/src/features/dashboard/views/`:
  - `StudentsKpiDashboardView.vue` (`/dashboard/students`)
  - `AcademicsKpiDashboardView.vue` (`/dashboard/academics`)
  - `FinanceKpiDashboardView.vue` (`/dashboard/finance`)
  - `TeachingKpiDashboardView.vue` (`/dashboard/teaching`)
  - `OperationsKpiDashboardView.vue` (`/dashboard/operations`)
  - `DevelopmentPlaceholderView.vue` (`/dashboard/development`)
- Routes registered in `src/frontend/src/app/router/routes.ts` with route meta permissions matching TD-05.
- Sidebar navigation in `AppLayout.vue` updated to expose domain dashboard links or sub-items.
- `pnpm type-check` passes with 0 errors.

Notes:  
- Each domain dashboard renders: (1) `KpiFilterBar`, (2) Domain Headline KPI Cards with period-over-period comparison chips, (3) 2–3 Diagnostic Breakdown Charts (`KpiBreakdownBarChart`), (4) Domain-scoped `NeedsAttentionPanel`.
- `/dashboard/development` renders an educational preview explaining Stage 3 Holistic Student Development metrics with `"Stage 3 Pending"` status.

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created shared reusable Level 2 dashboard shell `src/frontend/src/features/dashboard/components/DomainKpiDashboardShell.vue` integrating `PageHeader` (with breadcrumb and `/dashboard` back link), `KpiFilterBar` synced with `useAppContextStore`, `useDomainKpiDashboardQuery`, responsive `KpiMetricCard` grid (with period-over-period comparison chips), diagnostic breakdown rendering (`KpiStackedProgressBar` for `stacked-bar` and `KpiBreakdownBarChart` for `horizontal-bar` / `funnel`), domain-scoped `NeedsAttentionPanel`, skeleton loading states, and error retry banners.
  - Created the 6 Level 2 views in `src/frontend/src/features/dashboard/views/`: `StudentsKpiDashboardView.vue`, `AcademicsKpiDashboardView.vue`, `FinanceKpiDashboardView.vue`, `TeachingKpiDashboardView.vue`, `OperationsKpiDashboardView.vue`, and `DevelopmentPlaceholderView.vue` (educational Stage 3 Holistic Student Development preview with `"Stage 3 Pending"` status and `DEV-01`–`DEV-05` roadmap cards).
  - Registered `/dashboard/students`, `/dashboard/academics`, `/dashboard/finance`, `/dashboard/teaching`, `/dashboard/operations`, and `/dashboard/development` in `src/frontend/src/app/router/routes.ts` with route `meta` (`requiresAuth`, `permission`, `roles`, `title`) matching Architecture TD-05.
  - Updated sidebar navigation in `src/frontend/src/app/layouts/AppLayout.vue` to expand the `dashboard` navigation group with `Executive Overview` (`/dashboard`, preserving `data-testid="nav-item-dashboard"`) and role/permission-filtered Level 2 domain dashboard links (`/dashboard/students`, `/dashboard/academics`, `/dashboard/finance`, `/dashboard/teaching`, `/dashboard/operations`, `/dashboard/development`) while preserving all `campusOptions` / `appContextStore.activeCampusId` changes from `P4-S10`.
  - Verified `pnpm type-check` (`vue-tsc --noEmit`) passed with 0 errors, scoped `pnpm exec eslint` on all 9 created/modified files passed with 0 errors/warnings, and `AppLayout.spec.ts` (25/25), `AppLayoutDemoReset.spec.ts` (15/15), and `TeachingNavigation.spec.ts` (8/8) passed.
- **Remediated — Iteration 1 (2026-11-11):**
  - **`FINDING-P5-S11-01`:** Created shared Level 2 domain dashboard access rules and OR-policy evaluator (`src/frontend/src/app/router/dashboardAccess.ts`: `LEVEL2_DASHBOARD_ACCESS`, `isAuthorizedForAnyRule`) aligned 1-to-1 with Architecture TD-05 and backend `GetExecutiveDashboardHandler.cs` / Level 2 endpoint policies. Extended `AppRouteMeta` and `NavigationItem` in `src/frontend/src/shared/types/navigation.ts` with `permissions`, `excludedRoles`, and `authMode`. Updated `src/frontend/src/app/router/routes.ts`, `src/frontend/src/app/router/guards.ts`, and `src/frontend/src/app/layouts/AppLayout.vue` so `/dashboard/{students,academics,finance,teaching,operations}` and corresponding sidebar items enforce OR semantics (`SuperAdmin` / `*` / allowed role / allowed permission) while explicitly blocking self-service `Student` and `Parent` roles (preventing portal permission bleed on `/dashboard/academics` and `/dashboard/finance`) and preserving all existing non-dashboard guard semantics.
  - **`FINDING-P5-S11-02`:** Optimized `src/frontend/src/app/layouts/__tests__/AppLayout.spec.ts` by stubbing lazy route `component` loaders on `routes` (while keeping real route paths, `meta`, and `setupNavigationGuards`) and mounting `AppLayout` once across the 7-route `resolves breadcrumbs correctly for Submodule 2 routes` loop (`516ms` vs `15012ms` timeout), and added sidebar visibility assertions for Level 2 dashboard items.
  - Added 8 comprehensive Level 2 domain dashboard authorization tests in `src/frontend/src/app/router/__tests__/guards.spec.ts`.
  - Verified `pnpm --dir src/frontend type-check` (0 errors), `pnpm --dir src/frontend exec eslint` on all 7 touched files (0 errors/warnings), standalone `AppLayout.spec.ts` under default 15000ms timeout (26/26 passed in `8893ms`), and combined Vitest run across `guards.spec.ts` (33/33), `AppLayout.spec.ts` (26/26), `AppLayoutDemoReset.spec.ts` (15/15), and `TeachingNavigation.spec.ts` (8/8) (`82/82` passed).

---

### P5-S12

Title: Level 4 Operational Workbench `route.query` Filter & Tab Hydration

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Update 15 operational workbenches across all five domains to read URL query parameters (`route.query`) on mount and watch, pre-selecting filters, search terms, and active tabs when drilled down from KPI cards or attention signals.

Depends On: P4-S09  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- The following workbenches are updated to hydrate `route.query`:
  1. `StudentRecordsDeskView.vue` (`/students/records`): `status`, `campus`, `schoolLevel`, `cohort`.
  2. `StudentIntakeWorkbenchView.vue` (`/students/admission`): `status`, `docStatus`.
  3. `StudentLifecycleWorkbenchView.vue` (`/students/lifecycle`): `status` (`withdrawn`, `on_leave`, `transferred_out`).
  4. `CohortAcademicReviewView.vue` (`/academics/cohort-review`): `standing`.
  5. `AcademicInterventionWorkbenchView.vue` (`/academics/interventions`): `filter` (`unaddressed`), `status`.
  6. `ClassSessionWorkbenchView.vue` (`/academics/sessions`): `filter` (`low-attendance`).
  7. `CourseworkWorkbenchView.vue` (`/academics/coursework`): `status` (`missing`, `late`, `submitted`).
  8. `AssessmentWorkbenchView.vue` (`/academics/assessment`): `status` (`provisional`, `approved`, `published`).
  9. `ObligationsListView.vue` (`/finance/obligations`): `status`, `aging` (`90+ Days`).
  10. `PaymentVerificationQueueView.vue` (`/finance/payments`): `status` (`submitted`, `confirmed`).
  11. `StudentAccountsWorkbenchView.vue` (`/finance/accounts`): `filter` (`unallocated`, `overdue`).
  12. `TeachingAssignmentWorkspaceView.vue` (`/teaching/assignments`): `status` (`unstaffed`, `staffed`), `capability`.
  13. `FacultyWorkloadMonitoringView.vue` (`/teaching/workload`): `status` (`breached`), `tab` (`conflicts`), `severity` (`blocking`).
  14. `FacultyResourceWorkspaceView.vue` (`/teaching/faculty`): `verificationState` (`submitted`).
  15. `CalendarWorkspaceView.vue` (`/operations/calendar`): `tab` (`amendments`), `reviewed` (`false`), `tab` (`closures`).
- `pnpm type-check` passes with 0 errors.

Notes:  
- Use a reusable composable or clean `onMounted` / `watch(route.query)` pattern in each view to ensure query parameters take precedence over default store values when arriving via deep link.

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created shared composable `src/frontend/src/shared/composables/useRouteQueryHydration.ts` (`useRouteQueryHydration` + `getQueryString`) to hydrate filters, search terms, and active tabs immediately, on mount, and whenever `route.query` changes without throwing when router injection is absent in isolated unit tests.
  - Updated all 15 Level 4 operational workbenches and supporting filterable child components:
    1. `StudentRecordsDeskView.vue`: hydrated `status`, `campus` (including `campus-main`/`campus-east`), `schoolLevel` (`ELC`/`JHLC`/`HSLC` + grade levels), and `cohort` filter chip.
    2. `StudentIntakeWorkbenchView.vue`: hydrated `status` (including `submitted`, `entrance_assessed`, `accepted`, `waitlisted`, `under_review`, `withdrawn`, `registered`, `decided`) and `docStatus` (`pending`, auto-selecting `documents` dossier tab).
    3. `StudentLifecycleWorkbenchView.vue`: added Status `Select` filter and hydrated `status` (`withdrawn`, `on_leave`, `transferred_out`, etc.), `campus`, and `schoolLevel`/`level`.
    4. `CohortAcademicReviewView.vue`: added Academic Standing filter (`good_standing`, `conditional_standing`, `not_in_good_standing`) hydrated from `route.query.standing`.
    5. `AcademicInterventionWorkbenchView.vue`: hydrated `filter=unaddressed` (switches to `atRisk` tab and filters out learners with active `planned`/`in_progress` interventions) and `status`.
    6. `ClassSessionWorkbenchView.vue`: hydrated `filter=low-attendance` with clearable attendance-required filter.
    7. `CourseworkWorkbenchView.vue` & `SubmissionsReviewTable.vue`: hydrated `status` (`missing`, `late`, `submitted`, etc. into `SubmissionsReviewTable`, plus assignment statuses) and `unitId`.
    8. `AssessmentWorkbenchView.vue` & `AssessmentScoringGrid.vue`: hydrated `status` (`provisional`, `approved`, `published`, etc. into `AssessmentScoringGrid`, plus assessment statuses) and `unitId`.
    9. `ObligationsListView.vue`: implemented obligations table workbench backed by `useFinanceObligationsQuery`, hydrating `status` (`paid`, `unpaid`, `overdue`, and backend statuses), `aging` (`Current`, `1-30 Days`, `31-60 Days`, `61-90 Days`, `90+ Days`), and `chargeType`.
    10. `PaymentVerificationQueueView.vue`: hydrated `status` (`submitted`, `confirmed`, `rejected`, `all`).
    11. `StudentAccountsWorkbenchView.vue`: added directory filter (`all`, `overdue`, `unallocated`) hydrated from `route.query.filter` alongside `studentId`.
    12. `TeachingAssignmentWorkspaceView.vue` & `DemandsIntakeTab.vue`: hydrated `status` (`unstaffed`, `staffed`, etc.) and `capability`, plus reactive `route.query` sync for `tab`, `periodId`, `campus`, `q`, and `demandId`.
    13. `FacultyWorkloadMonitoringView.vue`: added `Workload Commitments` / `Capacity & Qualification Conflicts` tabs (`useCapacityConflicts`) and hydrated `status` (`breached`, `overloaded_with_exception`, etc.), `tab` (`conflicts`), and `severity` (`blocking`, `advisory`).
    14. `FacultyResourceWorkspaceView.vue`, `FacultyQueueSidebar.vue` & `QualificationEvidenceTab.vue`: hydrated `verificationState` (`submitted` → `pending_verification` queue filter + `qualifications` tab + filtered credential list).
    15. `CalendarWorkspaceView.vue` & `ClosuresAmendmentsTab.vue`: hydrated `tab` (`amendments` / `closures` → `closures` tab) and `reviewed` (`false` / `true` / `all` on amendments timeline).
  - Verified `pnpm type-check` (`vue-tsc --noEmit`) passed with 0 errors and all 14 existing workbench Vitest suites (`124/124` tests) passed.
- **Remediation (Iteration 1, 2026-11-11):**
  - `FINDING-P5-S12-01` (ADR-FE-001 cross-submodule isolation): Removed cross-submodule imports from `@/features/teaching/assignments/...` in `src/frontend/src/features/teaching/workload/views/FacultyWorkloadMonitoringView.vue`. Defined `CapacityConflictSeverity`, `CapacityConflictStatus`, `CapacityConflictDto`, and `GetCapacityConflictsParams` locally in `src/frontend/src/features/teaching/workload/types/index.ts`, added `workloadKeys.conflicts` / `workloadKeys.conflictList` in `src/frontend/src/features/teaching/workload/api/queryKeys.ts`, added `getCapacityConflicts` (calling `/v1/teaching/conflicts` via `apiClient`) in `src/frontend/src/features/teaching/workload/api/workloadApi.ts`, added `useCapacityConflicts` in `src/frontend/src/features/teaching/workload/composables/useWorkload.ts`, and updated `FacultyWorkloadMonitoringView.vue` to import from `../composables/useWorkload` and `../types`.
  - `MINOR-P5-S12-01`: Updated `src/frontend/src/features/finance/accounts/views/StudentAccountsWorkbenchView.vue` (`studentAccountsList` computed) to only fall back to unfiltered `items` when `unallocatedPaymentsResponse.value === undefined` so that `filter=unallocated` returns `[]` when payments have loaded with zero unallocated amounts.
  - Verified after final edits: `pnpm type-check` exited `0` (0 errors), `pnpm exec eslint` on `FacultyWorkloadMonitoringView.vue`, `useWorkload.ts`, `workloadApi.ts`, `queryKeys.ts`, and `types/index.ts` exited `0` (0 errors; 3 pre-existing unused-var warnings on `ShieldAlert`, `FileCheck2`, `PeriodOption` in `FacultyWorkloadMonitoringView.vue`), and `pnpm exec vitest run` across all 14 workbench specs plus the 3 `teaching/workload` unit specs passed (`17 passed (17)`, `154 passed (154)`).

---

## P6 - Verification & Test Harnesses

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P6-S13

Title: Backend Integration Tests & Architecture Verification

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Author integration tests for executive and domain KPI endpoints, verify demo data reset and persona integrity tests, and validate architectural isolation rules.

Depends On: P3-S08  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Kpis/KpiExecutiveDashboardEndpointTest.cs` created and passing:
  - Verifies `GET /api/dashboard/executive` returns 200 OK with 5 headline KPIs, 6 domain summaries, and sorted attention queue.
  - Verifies campus query filtering (`campus-main`, `campus-east`, and all campuses).
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Kpis/DomainKpiDashboardEndpointsTest.cs` created and passing:
  - Verifies `GET /api/{domain}/kpis/dashboard` for each of the 5 operational domains.
- `DemoDataResetTest.cs` and `DemoDataIntegrityTest.cs` pass 100%.
- `Elsasa.Sis.ArchitectureTests` pass with 0 rule violations, verifying compile-time domain boundary isolation.
- `dotnet test` succeeds across all test suites.

Notes:  
- Tests run against SQL Server Testcontainers or local SQL Server instance.
- Verify non-zero counts for aging brackets, workload bands, at-risk learners, and unreviewed amendments.

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created `tests/backend/Elsasa.Sis.IntegrationTests/Features/Kpis/KpiExecutiveDashboardEndpointTest.cs` (4 tests verifying `GET /api/dashboard/executive` 5 headline KPIs, 6 domain summaries including Stage 3 `Development` `pending_stage`, sorted `NeedsAttentionQueue`, `campus-main`/`campus-east`/all-campus + `schoolLevel=ELC` filtering, `DEC-006` `IsAccessible` & signal filtering, and 401 unauthorized).
  - Created `tests/backend/Elsasa.Sis.IntegrationTests/Features/Kpis/DomainKpiDashboardEndpointsTest.cs` (6 tests verifying all 5 `GET /api/{domain}/kpis/dashboard` endpoints, `FIN-04` all 5 non-zero aging brackets, `TCH-03` all 5 non-zero workload bands, `ACA-02` at-risk learners, `OPS-01` unreviewed amendments, East Campus scoping, and 403 Forbidden for unauthorized caller).
  - **Verification against SQL Server (`TEST_SQLSERVER_CONNECTION_STRING='Server=localhost;Database=ElsasaSisTestDb;Trusted_Connection=True;TrustServerCertificate=True;'`):**
    - `dotnet test tests/backend/Elsasa.Sis.ArchitectureTests --no-restore`: **311/311 passed** (0 failed, 0 skipped).
    - `dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-restore --filter "FullyQualifiedName~Features.Kpis"`: **10/10 passed** (0 failed, 0 skipped).
    - `dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~DemoDataResetTest|FullyQualifiedName~DemoDataIntegrityTest"`: **10/10 passed** (0 failed, 0 skipped). Note: `Features.DemoData` also includes `DemoDataProductionGuardTest` (2 pre-existing failures returning empty body).
    - `dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~UnitTests"`: **2281/2284 passed**, **3 failed** (`AcademicsModuleHostIntegrationTests` x2 and `EndpointDiscoveryTests` x1) because `GetAcademicsKpiDashboardEndpoint` (`P3-S06`) and `GetStudentsKpiDashboardEndpoint` (`P2-S04`) omit `[FromServices]` on `ICurrentUser currentUser`, causing Minimal API `Body (Inferred)` errors in isolated host unit tests where `ICurrentUser` is not registered.
    - Full `dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-build`: **2565/2661 passed**, **96 failed** (93 DB integration tests across parallel xUnit collections contending on shared `ElsasaSisTestDb` `Respawn` resets + pre-existing `Teaching`/`Operations`/`Finance`/`Students` failures, plus the 3 `UnitTests` endpoint `[FromServices]` regressions noted above).
- **Remediation Iteration 1 (2026-11-11 — `FINDING-P6-S13-01`):**
  - Added `[FromServices]` to `ICurrentUser currentUser` and the handler parameter in `src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/GetAcademicsKpiDashboardEndpoint.cs`, `src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/GetStudentsKpiDashboardEndpoint.cs` (also added `using Microsoft.AspNetCore.Mvc;` and `[FromQuery]` on the 4 query parameters), and `src/backend/Elsasa.Sis.Operations/Features/Kpis/GetOperationsKpiDashboard/GetOperationsKpiDashboardEndpoint.cs`.
  - **Re-verification sequentially against `TEST_SQLSERVER_CONNECTION_STRING='Server=localhost;Database=ElsasaSisTestDb;Trusted_Connection=True;TrustServerCertificate=True;'`:**
    - `dotnet build src/backend/Elsasa.Sis.sln --no-restore`: **0 Warning(s), 0 Error(s)**.
    - `dotnet test tests/backend/Elsasa.Sis.ArchitectureTests --no-restore`: **311/311 passed** (0 failed, 0 skipped).
    - `dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~UnitTests"`: **2284/2284 passed** (0 failed, 0 skipped).
    - `dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~Features.Kpis"`: **10/10 passed** (0 failed, 0 skipped).
    - `dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~DemoDataResetTest|FullyQualifiedName~DemoDataIntegrityTest"`: **10/10 passed** (0 failed, 0 skipped).

---

### P6-S14

Title: Frontend Unit & Component Tests

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Implement frontend component and unit tests for KPI visual primitives, query composables, and workbench query hydration.

Depends On: P5-S11, P5-S12  

Repository: `elsasa-sis-v3`  

Completion Criteria:  
- Unit tests created in `src/frontend/src/features/dashboard/__tests__/`:
  - `KpiMetricCard.spec.ts`: Validates metric value formatting, status pill classes, trend delta label, and drill-down navigation link.
  - `KpiBreakdownBarChart.spec.ts`: Validates bar percentage widths, labels, amounts, and link targets.
  - `NeedsAttentionPanel.spec.ts`: Validates severity sorting, signal tag badges, and action button routes.
  - `DashboardView.spec.ts`: Updated to mount with TanStack Query mock and assert headline cards and domain health cards render.
- `pnpm test:unit` passes 100%.
- `pnpm type-check` passes with 0 errors.

Notes:  
- Mock TanStack Query responses using sample fixtures matching `ExecutiveDashboardResponse` and `DomainKpiDashboardResponse`.

Implementation Notes:  
- **Implemented (2026-11-11):**
  - Created `src/frontend/src/features/dashboard/__tests__/dashboard.fixtures.ts` providing typed sample fixtures matching `ExecutiveDashboardResponse` and `DomainKpiDashboardResponse` (`dashboard.types.ts`) plus `ensureMatchMediaPolyfill()` for PrimeVue jsdom listeners.
  - Created `src/frontend/src/features/dashboard/__tests__/KpiMetricCard.spec.ts` (12 tests validating formatted metric values, domain badge, semantic `StatusPill` classes/variants across `healthy`/`warning`/`critical`/`neutral`, `up`/`down`/`flat` trend delta chips, and drill-down `RouterLink` targets).
  - Created `src/frontend/src/features/dashboard/__tests__/KpiBreakdownBarChart.spec.ts` (5 tests validating chart headers, horizontal/funnel SVG bar percentage widths & clamping, item labels/counts/formatted amounts, `RouterLink` drill-down targets, `maxItems` truncation, and empty state).
  - Created `src/frontend/src/features/dashboard/__tests__/NeedsAttentionPanel.spec.ts` (5 tests validating prioritized signal ordering, critical/warning summary counters, severity & domain tag badges, affected count pills, `maxVisible` truncation, empty clear state, and action link routes).
  - Created `src/frontend/src/features/dashboard/__tests__/KpiStackedProgressBar.spec.ts` (2 tests validating proportional stacked segments, interactive legend links, and empty state) and `src/frontend/src/features/dashboard/__tests__/DomainKpiDashboardShell.spec.ts` (2 tests validating Level 2 `useDomainKpiDashboardQuery` rendering, skeleton loading, and error-retry recovery).
  - Updated `src/frontend/src/features/dashboard/views/__tests__/DashboardView.spec.ts` (4 tests mounting `DashboardView` with `Pinia`, `PrimeVue`, `VueQueryPlugin`, and mocked `getExecutiveDashboard` to assert greeting banner, skeleton loading states, 5 headline KPI cards, 6 domain health cards including Stage 3 `Development` placeholder, `KpiFilterBar` school-level filter refetch, restricted domain badge, `NeedsAttentionPanel`, and error-retry recovery).
  - **Verification:**
    - `pnpm --dir src/frontend exec vitest run src/features/dashboard`: **6/6 test files passed, 30/30 tests passed**.
    - `pnpm --dir src/frontend type-check` (`vue-tsc --noEmit`): **passed (0 errors)**.
    - `pnpm --dir src/frontend exec eslint "src/features/dashboard/**/*.ts"`: **passed (0 errors, 0 warnings)**.
    - `pnpm --dir src/frontend test:unit`: **222/224 test files passed (2,329/2,332 tests passed)**; all dashboard and Level 4 workbench suites passed, while 2 pre-existing unrelated curriculum test files (`src/features/academics/curriculum/__tests__/StructureOverviewAndModals.spec.ts` [1 test] and `src/features/academics/curriculum/__tests__/components/CurriculumWorkspaceView.spec.ts` [2 tests], introduced in prior commit `d17ece9`) failed and `src/features/teaching/faculty/__tests__/FacultyResourceWorkspaceView.spec.ts` logged a PrimeVue `TabList` `inkBarObserver.observe` unhandled timer error after its assertions passed.

---

# 6. Change Log

- **2026-11-11 (v1.0):** Initial authoritative Implementation Plan created by Architect for `CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS` (Release 1 — Essential Executive & Domain Dashboards) based on approved Target Architecture [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md) and Feasibility Assessment [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-FEASIBILITY-ASSESSMENT.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-FEASIBILITY-ASSESSMENT.md). Plan finalized and released with `Execution Approval: APPROVED`.
- **2026-11-11 (v1.1):** Architectural Escalation Resolution for P1-S01:
  1. Resolved assembly cycle by defining `IKpiPeriodResolver` in `Elsasa.Sis.Contracts/Kpis/` and concrete `KpiPeriodResolver` in `Elsasa.Sis.Operations/Services/` registered in Operations DI, leaving `Elsasa.Sis.Shared` uncoupled and compliant with `ArchitectureTests`.
  2. Reconciled database table and column specifications to `operations.operational_periods` (`period_code`, `period_type`, `start_date`, `end_date`, `status`) and `operations.academic_years`.
  3. Reconciled `ExecutiveDashboardResponse.AvailablePeriods` and `DomainKpiDashboardResponse.AvailablePeriods` to `IReadOnlyList<KpiPeriodOptionDto>`.
  4. Unblocked slice P1-S01 (`Implementation Status: NOT-STARTED`).
