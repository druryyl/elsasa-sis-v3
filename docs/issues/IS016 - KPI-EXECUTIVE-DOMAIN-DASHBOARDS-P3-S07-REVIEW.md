---
Title: Review Report — P3-S07 Teaching Domain KPI Vertical Slice & Summary Provider
Code: CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS
Slice: P3-S07
Artifact: REVIEW
ReviewIteration: 0
Decision: GO
Date: 2026-11-11
---

# Review Summary: P3-S07 — Teaching Domain KPI Vertical Slice & Summary Provider

- **Decision:** `GO`
- **ReviewIteration:** `0`
- **Dependencies Checked:** `P1-S01` (`IMPLEMENTED`, `GO`), `P1-S02` (`IMPLEMENTED`, `GO`)
- **Plan Status Updated:** `P3-S07` `Review Status` set to `GO` in [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md). Plan-level `Status` left unchanged (`NOT-STARTED`).

---

# Verification Against Completion Criteria & Architecture

1. **Endpoint & Discovery (`GetTeachingKpiDashboardEndpoint.cs`, `TeachingEndpoints.cs`, `Program.cs`):**
   - Verified `GET /api/teaching/kpis/dashboard` is mapped via `IEndpoint` in [`GetTeachingKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/GetTeachingKpiDashboardEndpoint.cs#L14-L49), automatically discovered by [`TeachingEndpoints.MapTeachingEndpoints()`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/TeachingEndpoints.cs#L19-L31), and wired in [`Program.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Api/Program.cs#L125).
   - Verified `.RequireAuthorization(TeachingKpiPolicies.DashboardView)` enforces the TD-05 (`DEC-006`) policy registered in [`DependencyInjection.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/DependencyInjection.cs#L307-L321) for wildcard `*`, `Permissions.Teaching.Faculty.View`, `Permissions.Teaching.Workload.View`, and roles `Admin`, `SuperAdmin`, `AcademicCoordinator`, `AcademicAuthority`, `TeachingAuthority` (`Teaching Authority`), `TeachingCoordinator` (`Teaching Coordinator`), and `Teacher`.
   - Verified query parameter bindings (`academicYearId`, `academicPeriodId`, `campus`, `schoolLevel`) map to `GlobalKpiFilterDto`.

2. **Handler & SQL Verification (`GetTeachingKpiDashboardHandler.cs`, `TeachingKpiQueries.cs`):**
   - Verified [`GetTeachingKpiDashboardHandler.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/GetTeachingKpiDashboardHandler.cs#L11-L43) resolves period context and available semesters via `IKpiPeriodResolver` and delegates to [`TeachingKpiQueries.QueryAsync`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/TeachingKpiQueries.cs#L91-L151).
   - Verified `TeachingKpiQueries.QueryAsync` opens and disposes its own `DbConnection` via `ISqlConnectionFactory.CreateConnectionAsync(cancellationToken)` (`RISK-002`) and executes a 5-result-set Dapper `QueryMultipleAsync` batch using `CommandDefinition` with `cancellationToken`.
   - Verified all SQL table/column/status bindings against DbUp scripts ([`20261110_0501_Teaching_TeacherProfile_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261110_0501_Teaching_TeacherProfile_Tables.sql), [`20261110_0502_Teaching_AssignmentWorkload_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261110_0502_Teaching_AssignmentWorkload_Tables.sql)) and seeders ([`TeachingDemoDataSeeder.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/DemoData/TeachingDemoDataSeeder.cs), [`TeachingAssignmentsDemoDataSeeder.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/DemoData/TeachingAssignmentsDemoDataSeeder.cs)):
     - `teaching.teaching_demands`: `academic_period_id`, `status` (`'received'`, `'accepted_for_staffing'`, `'changed'`, `'staffed'`), `expected_workload_hours`, `campus_scope`, `required_capability_code`.
     - `teaching.workload_commitments` + `teaching.teacher_profiles`: `wc.teacher_profile_id = tp.id`, `wc.academic_period_id`, `wc.status` across all 5 bands (`'under_allocated'`, `'normal'`, `'approaching_limit'`, `'overloaded_with_exception'`, `'breached'`), `tp.standing = 'active'`, `tp.campus_affiliation`.
     - `teaching.capacity_conflicts`: `severity` (`'blocking'`, `'advisory'`), `status = 'unresolved'`, joined to `teaching.teacher_profiles` (`campus_affiliation`), `teaching.teaching_assignments`, and `teaching.teaching_demands` (since `capacity_conflicts` has no direct `academic_period_id` column).
     - `teaching.qualification_evidence`: `verification_state = 'submitted'` joined to `teaching.teacher_profiles` (`campus_affiliation`).
   - Verified `TCH-01` (with prior S1 comparison delta), `TCH-02`, `TCH-03`, `TCH-04`, breakdowns (`TCH-03-WORKLOAD-BANDS` covering all 5 bands, `TCH-02-OPEN-DEMANDS`, `TCH-04-CONFLICTS-QUALIFICATIONS`), divide-by-zero/empty-filter `neutral` safeguards (`RISK-004`), signals (`TCH_BLOCKING_CONFLICT`, `TCH_WORKLOAD_BREACH`, `TCH_UNSTAFFED_DEMAND`), and TD-06 drill-down routes.

3. **Summary Provider & DI (`TeachingKpiSummaryProvider.cs`, `DependencyInjection.cs`):**
   - Verified [`TeachingKpiSummaryProvider.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/TeachingKpiSummaryProvider.cs#L10-L62) implements `IDomainKpiSummaryProvider` (`DomainKey => "teaching"`, `SortOrder = 4`), returning headline `TCH-01`, `DomainHealthSummaryDto` (`DashboardRoute = "/dashboard/teaching"`), and active `AttentionSignals`.
   - Verified [`DependencyInjection.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/DependencyInjection.cs#L183-L184) registers `GetTeachingKpiDashboardHandler` and `IDomainKpiSummaryProvider` (`TeachingKpiSummaryProvider`) as scoped services.

4. **Build & Architecture Test Verification:**
   - `dotnet build src/backend/Elsasa.Sis.Teaching --no-restore`: **Build succeeded. 0 Warning(s), 0 Error(s).**
   - `dotnet test tests/backend/Elsasa.Sis.ArchitectureTests --no-restore`: **Passed! Failed: 0, Passed: 311, Skipped: 0, Total: 311.**

---

# Findings

No `BLOCKER`, `MAJOR`, or `MINOR` findings.

| ID | Severity | Description | Evidence |
| :--- | :--- | :--- | :--- |
| `NOTE-P3-S07-01` | `NOTE` | `TeachingKpiQueries` accurately resolves `wc.teacher_profile_id`, `tp.standing = 'active'`, `tp.campus_affiliation`, and joins `capacity_conflicts` through `teaching_assignments`/`teaching_demands`/`workload_commitments` instead of prose column names in TD-03.4, adhering to `DEC-002` and DbUp DDL. | [`TeachingKpiQueries.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/TeachingKpiQueries.cs#L54-L82), [`20261110_0502_Teaching_AssignmentWorkload_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261110_0502_Teaching_AssignmentWorkload_Tables.sql#L273-L303) |
| `NOTE-P3-S07-02` | `NOTE` | In `capacity_conflicts` period scoping, the `OR EXISTS (SELECT 1 FROM teaching.workload_commitments wc ...)` fallback is currently accurate because seeded commitments exist only for Semester 2, though if multi-period commitments are added later, scoping that fallback to `cc.teaching_assignment_id IS NULL AND cc.teaching_demand_id IS NULL` would prevent cross-period leakage. | [`TeachingKpiQueries.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/Kpis/GetTeachingKpiDashboard/TeachingKpiQueries.cs#L73-L82) |
