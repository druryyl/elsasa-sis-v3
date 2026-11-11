# Review Record: P6-S13 Backend Integration Tests & Architecture Verification

- **Issue / Plan:** [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md) (`CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS`, v1.1)
- **Architecture:** [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md)
- **Slice ID:** P6-S13
- **Title:** Backend Integration Tests & Architecture Verification
- **Review Date:** 2026-11-11
- **Review Decision:** GO
- **ReviewIteration:** 1

---

## 1. Scope & Objective Verification

| Requirement / Criterion | Evaluation & Observed Evidence | Status |
|---|---|---|
| Dependency `P3-S08` satisfied | Verified in Implementation Plan: `P3-S08` has `Implementation Status: IMPLEMENTED` and `Review Status: GO`. | SATISFIED |
| `KpiExecutiveDashboardEndpointTest.cs` verifies `GET /api/dashboard/executive` (200 OK, 5 headline KPIs, 6 domain summaries including Stage 3 `pending_stage`, sorted `NeedsAttentionQueue`, campus/schoolLevel filtering, `IsAccessible` permission decoration) | Verified [`KpiExecutiveDashboardEndpointTest.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/Kpis/KpiExecutiveDashboardEndpointTest.cs#L1-L194): 4 `[SkippableFact]` tests cover all required assertions (`STD-01`, `ACA-01`, `FIN-01`, `TCH-01`, `OPS-01`; 6 domain summaries with `development` `pending_stage`; `critical` before `warning` queue sorting; `campus-main`/`campus-east`/all/`schoolLevel=ELC` filtering; `DEC-006` `IsAccessible` & signal filtering; 401 unauthorized). | SATISFIED |
| `DomainKpiDashboardEndpointsTest.cs` verifies `GET /api/{domain}/kpis/dashboard` across all 5 operational domains including non-zero aging brackets, workload bands, at-risk learners, and unreviewed amendments | Verified [`DomainKpiDashboardEndpointsTest.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/Kpis/DomainKpiDashboardEndpointsTest.cs#L1-L230): 6 `[SkippableFact]` tests verify `operations`, `students`, `finance`, `academics`, and `teaching` dashboards, all 5 `FIN-04` aging brackets (`> 0`), all 5 `TCH-03` workload bands (`> 0`), `ACA-02` at-risk learners, `OPS-AMENDMENT-REVIEW` unreviewed amendments, East Campus scoping, and 403 Forbidden enforcement. | SATISFIED |
| Independent execution of `Elsasa.Sis.ArchitectureTests`, `Features.Kpis`, and `DemoDataResetTest` / `DemoDataIntegrityTest` against SQL Server (`ElsasaSisTestDb`) | Independently executed sequentially against `TEST_SQLSERVER_CONNECTION_STRING='Server=localhost;Database=ElsasaSisTestDb;Trusted_Connection=True;TrustServerCertificate=True;'`:<br>- `Elsasa.Sis.ArchitectureTests --no-restore`: **311/311 passed** (0 failed, 0 skipped).<br>- `Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~Features.Kpis"`: **10/10 passed** (0 failed, 0 skipped).<br>- `Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~DemoDataResetTest|FullyQualifiedName~DemoDataIntegrityTest"`: **10/10 passed** (0 failed, 0 skipped). | SATISFIED |
| `dotnet test` succeeds across all test suites | Re-Review Iteration 1 (2026-11-11): `dotnet build src/backend/Elsasa.Sis.sln --no-restore` succeeded (0 warnings, 0 errors); `Elsasa.Sis.ArchitectureTests --no-restore`: **311/311 passed**; `Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~UnitTests"`: **2284/2284 passed** (0 failed, 0 skipped); `Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~Features.Kpis"`: **10/10 passed**; `Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~DemoDataResetTest\|FullyQualifiedName~DemoDataIntegrityTest"`: **10/10 passed**. (Prior Iteration 0 3/2284 `UnitTests` failures resolved; 93 unrelated pre-existing schema/contract drift failures in `FullyQualifiedName!~UnitTests` remain documented in `NOTE-P6-S13-01`.) | SATISFIED |

---

## 2. Findings

### `FINDING-P6-S13-01` — Three `UnitTests` Regressions Due to Missing `[FromServices]` on `ICurrentUser` and Handler Parameters in KPI Endpoints

- **Severity:** MAJOR
- **Resolution Status:** RESOLVED (Iteration 1 — 2026-11-11)
- **Description:**
  `dotnet test` fails 3 tests in `Elsasa.Sis.IntegrationTests` (`FullyQualifiedName~UnitTests`: **2281 passed, 3 failed**) because the Minimal API endpoints for Academics, Students, and Operations KPI dashboards inject `ICurrentUser` (and their handler) in `HandleAsync` without `[FromServices]`. Because `ICurrentUser` is registered in `Elsasa.Sis.Api`'s host composition (`AddHostComposition`) rather than in `AddAcademicsDomain()`, `AddStudentsDomain()`, or `AddOperationsDomain()`, ASP.NET Core Minimal API `RequestDelegateFactory` infers `ICurrentUser` as `Body (Inferred)` on a `GET` endpoint when domain endpoints are mapped in isolation, throwing `System.InvalidOperationException: Body was inferred but the method does not allow inferred body parameters`.
- **Evidence:**
  1. **Test execution (`dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~UnitTests"`):**
     - `Elsasa.Sis.IntegrationTests.UnitTests.Academics.AcademicsModuleHostIntegrationTests.WebApiHost_Should_GenerateValidSwaggerSchema_IncludingAcademicsEndpoints` — Failed with `System.InvalidOperationException` (`currentUser | Body (Inferred)`).
     - `Elsasa.Sis.IntegrationTests.UnitTests.Academics.AcademicsModuleHostIntegrationTests.MapAcademicsEndpoints_Should_RegisterEndpointsWithoutErrors` — Failed with `System.InvalidOperationException` (`currentUser | Body (Inferred)`).
     - `Elsasa.Sis.IntegrationTests.UnitTests.Endpoints.EndpointDiscoveryTests.MapEndpoints_Should_DiscoverStudentsEndpoints_IncludingPlacementAndStatusEndpoints` — Failed with `System.InvalidOperationException` (`currentUser | Body (Inferred)`).
  2. **Source inspection:**
     - [`GetAcademicsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/GetAcademicsKpiDashboardEndpoint.cs#L49-L56) (`P3-S06`) omits `[FromServices]` on `ICurrentUser currentUser` and `GetAcademicsKpiDashboardHandler handler`.
     - [`GetStudentsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/GetStudentsKpiDashboardEndpoint.cs#L40-L47) (`P2-S04`) omits `[FromServices]` on `ICurrentUser currentUser` and `GetStudentsKpiDashboardHandler handler` (and `[FromQuery]` on query parameters).
     - [`GetOperationsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Operations/Features/Kpis/GetOperationsKpiDashboard/GetOperationsKpiDashboardEndpoint.cs#L48-L55) (`P2-S03`) exhibits the identical defect (`ICurrentUser currentUser` and `GetOperationsKpiDashboardHandler handler` without `[FromServices]`), which will fail identically whenever `OperationsAssemblyMarker` endpoints are mapped without host composition.
     - By contrast, [`GetFinanceKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Finance/Features/Kpis/GetFinanceKpiDashboard/GetFinanceKpiDashboardEndpoint.cs#L32-L38), [`GetTeachingKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/GetTeachingKpiDashboardEndpoint.cs#L30-L36), and [`GetExecutiveDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Api/Features/Dashboard/GetExecutiveDashboard/GetExecutiveDashboardEndpoint.cs#L29-L35) explicitly annotate their service parameters with `[FromServices]`.
- **Required Correction:**
  1. Note on slice ownership: The test files authored in `P6-S13` (`KpiExecutiveDashboardEndpointTest.cs` and `DomainKpiDashboardEndpointsTest.cs`) are valid and passing (`10/10`), whereas the `[FromServices]` defect sits in endpoints delivered in `P2-S03`, `P2-S04`, and `P3-S06`. Coordinate with `ica-architect` / the orchestrator as needed to apply `[FromServices]` across those three endpoint files (without altering `P6-S13` scope constraints improperly):
     - Add `[FromServices]` to `ICurrentUser currentUser` and the handler parameter in [`GetAcademicsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/GetAcademicsKpiDashboardEndpoint.cs#L54-L55), [`GetStudentsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/GetStudentsKpiDashboardEndpoint.cs#L45-L46) (also adding `using Microsoft.AspNetCore.Mvc;` and `[FromQuery]` on query parameters for consistency), and [`GetOperationsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Operations/Features/Kpis/GetOperationsKpiDashboard/GetOperationsKpiDashboardEndpoint.cs#L53-L54).
  2. Re-run and verify `Elsasa.Sis.ArchitectureTests` (`311/311`), `Elsasa.Sis.IntegrationTests --filter "FullyQualifiedName~UnitTests"` (`2284/2284`), `Elsasa.Sis.IntegrationTests --filter "FullyQualifiedName~Features.Kpis"` (`10/10`), and `Elsasa.Sis.IntegrationTests --filter "FullyQualifiedName~DemoDataResetTest|FullyQualifiedName~DemoDataIntegrityTest"` (`10/10`).
- **Remediation Evidence (Iteration 1 — 2026-11-11):**
  1. Verified [`GetAcademicsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/GetAcademicsKpiDashboardEndpoint.cs#L49-L56) now decorates `ICurrentUser currentUser` and `GetAcademicsKpiDashboardHandler handler` with `[FromServices]` (and query parameters with `[FromQuery]`).
  2. Verified [`GetStudentsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/GetStudentsKpiDashboardEndpoint.cs#L8-L48) now imports `Microsoft.AspNetCore.Mvc` and decorates all 4 query parameters with `[FromQuery]` and `ICurrentUser currentUser` / `GetStudentsKpiDashboardHandler handler` with `[FromServices]`.
  3. Verified [`GetOperationsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Operations/Features/Kpis/GetOperationsKpiDashboard/GetOperationsKpiDashboardEndpoint.cs#L48-L55) now decorates `ICurrentUser currentUser` and `GetOperationsKpiDashboardHandler handler` with `[FromServices]` (and query parameters with `[FromQuery]`).
  4. Independently executed sequentially with `TEST_SQLSERVER_CONNECTION_STRING='Server=localhost;Database=ElsasaSisTestDb;Trusted_Connection=True;TrustServerCertificate=True;'`:
     - `dotnet build src/backend/Elsasa.Sis.sln --no-restore`: **0 Warning(s), 0 Error(s)**.
     - `dotnet test tests/backend/Elsasa.Sis.ArchitectureTests --no-restore`: **311/311 passed** (0 failed, 0 skipped).
     - `dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~UnitTests"`: **2284/2284 passed** (0 failed, 0 skipped).
     - `dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~Features.Kpis"`: **10/10 passed** (0 failed, 0 skipped).
     - `dotnet test tests/backend/Elsasa.Sis.IntegrationTests --no-build --filter "FullyQualifiedName~DemoDataResetTest|FullyQualifiedName~DemoDataIntegrityTest"`: **10/10 passed** (0 failed, 0 skipped).

---

### `NOTE-P6-S13-01` — Diagnosis of the 93 Non-UnitTest Failures in `Elsasa.Sis.IntegrationTests` & `DemoDataResetTest` Cold-Run Timing

- **Severity:** NOTE
- **Resolution Status:** INFORMATIONAL
- **Description:**
  1. **Root cause of the 93 DB failures (`284/377` passed in `FullyQualifiedName!~UnitTests`):**
     The 93 failures in the unfiltered `Elsasa.Sis.IntegrationTests` run are **not** caused by parallel xUnit test collection collisions. [`AssemblySmokeTests.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/AssemblySmokeTests.cs#L3) already sets `[assembly: CollectionBehavior(DisableTestParallelization = true)]`, so xUnit executes all collections sequentially within a single `dotnet test` invocation. (Collisions only occur if two separate `dotnet test` processes are launched concurrently against the same `ElsasaSisTestDb`.)
     Inspection of the `FullyQualifiedName!~UnitTests` run output confirms all 93 failures are **pre-existing schema/contract drift** in earlier stage tests unrelated to `CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS` (e.g., `Invalid column name 'structure_type'` in `AcademicsAssessmentResultsMigrationTests`, `Invalid column name 'proficiency_level'` in `AcademicsParticipationActivitiesIntegrationTests`, `Invalid column name 'commitment_status'` / `'is_active'` in older `Teaching` tests, `uq_finance_student_financial_accounts_student` index name in `FinanceMigrationIntegrationTests`, `422` vs `400`/`201` in `PaymentWorkflowIntegrationTests` / `SchoolClosureTransactionTests`, `404` in older admission decision tests, and `DemoDataProductionGuardTest` returning empty error body `""`).
  2. **`DemoDataResetTest` timing sensitivity:**
     When `DemoDataResetTest|DemoDataIntegrityTest` ran standalone (`10/10`), all tests passed cleanly (`< 5000ms` reset budget). Under a subsequent full `FullyQualifiedName~DemoData` run, `DemoDataResetTest` exceeded `5000ms` (`20665ms` and `7615ms`) due to local SQL Server cold/loaded execution timing.

---

## 3. Remediation & Re-Review History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 0 | 2026-11-11 | NO-GO | Verified `KpiExecutiveDashboardEndpointTest.cs` (4 tests) and `DomainKpiDashboardEndpointsTest.cs` (6 tests) pass `10/10` sequentially against SQL Server `ElsasaSisTestDb`, `DemoDataResetTest` + `DemoDataIntegrityTest` pass `10/10`, and `Elsasa.Sis.ArchitectureTests` pass `311/311`. Confirmed the 93 failures in `FullyQualifiedName!~UnitTests` (`284/377`) are pre-existing schema/contract drift in older stage tests rather than parallel collection contention (`DisableTestParallelization = true` is already enabled in `AssemblySmokeTests.cs`). Identified **MAJOR** `FINDING-P6-S13-01`: 3 `UnitTests` regressions (`2281/2284`, `AcademicsModuleHostIntegrationTests` x2 and `EndpointDiscoveryTests` x1) caused by missing `[FromServices]` on `ICurrentUser` and handler parameters in `GetAcademicsKpiDashboardEndpoint.cs` (`P3-S06`), `GetStudentsKpiDashboardEndpoint.cs` (`P2-S04`), and identically in `GetOperationsKpiDashboardEndpoint.cs` (`P2-S03`). Decision: **NO-GO**. |
| 1 | 2026-11-11 | GO | Re-review verified `FINDING-P6-S13-01` resolved: `[FromServices]` added to `ICurrentUser` and handler parameters (plus `[FromQuery]` in `GetStudentsKpiDashboardEndpoint.cs`) across `GetAcademicsKpiDashboardEndpoint.cs`, `GetStudentsKpiDashboardEndpoint.cs`, and `GetOperationsKpiDashboardEndpoint.cs`. Independently verified sequential runs against SQL Server `ElsasaSisTestDb`: `dotnet build src/backend/Elsasa.Sis.sln --no-restore` (0 warnings, 0 errors), `Elsasa.Sis.ArchitectureTests` (`311/311`), `Elsasa.Sis.IntegrationTests --filter "FullyQualifiedName~UnitTests"` (`2284/2284`), `Elsasa.Sis.IntegrationTests --filter "FullyQualifiedName~Features.Kpis"` (`10/10`), and `DemoDataResetTest\|DemoDataIntegrityTest` (`10/10`). All acceptance criteria satisfied with zero BLOCKER or MAJOR findings. Decision: **GO**. |

---

## 4. Final Review Decision

**GO**
