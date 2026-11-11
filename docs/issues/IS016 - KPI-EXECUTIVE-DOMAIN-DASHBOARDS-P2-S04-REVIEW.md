---
Title: Review Report — P2-S04 Students Domain KPI Vertical Slice & Summary Provider
Code: CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS
Slice: P2-S04
Artifact: REVIEW
ReviewIteration: 0
Decision: GO
Date: 2026-11-11
---

# Review Summary: P2-S04 — Students Domain KPI Vertical Slice & Summary Provider

- **Decision:** `GO`
- **ReviewIteration:** `0`
- **Dependencies Checked:** `P1-S01` (`IMPLEMENTED`, `GO`), `P1-S02` (`IMPLEMENTED`, `GO`)
- **Plan Status Updated:** `P2-S04` `Review Status` set to `GO` in [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md). Plan-level `Status` left unchanged (`NOT-STARTED`).

---

# Verification Against Completion Criteria & Architecture

1. **Endpoint (`GetStudentsKpiDashboardEndpoint.cs`):**
   - Verified [`GetStudentsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/GetStudentsKpiDashboardEndpoint.cs) implements `IEndpoint`, automatically discovered via `DependencyInjection.Stage1Assemblies` (`StudentsAssemblyMarker`) and `app.MapEndpoints(...)` in [`Program.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Api/Program.cs#L123).
   - Mapped to `GET /api/students/kpis/dashboard` with `.RequireAuthorization()` and explicit `ICurrentUser` authentication (`401`) and authorization (`403`) checks allowing wildcard `*`, `Permissions.Students.Admission.View`, `Permissions.Students.Records.View`, or roles `Admin`, `SuperAdmin`, `AdmissionOfficer`/`Admission Officer`, `StudentAdminOfficer`/`Student Administration Officer`, `SchoolLeadership`/`School Leadership` in alignment with Architecture TD-05 (`DEC-006`).
   - Query parameters (`academicYearId`, `academicPeriodId`, `campus`, `schoolLevel`) bind cleanly to `GlobalKpiFilterDto`.

2. **Handler & SQL Schema Bindings (`GetStudentsKpiDashboardHandler.cs`):**
   - Verified [`GetStudentsKpiDashboardHandler.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/GetStudentsKpiDashboardHandler.cs) resolves period context and available periods via `IKpiPeriodResolver`, opens its own `SqlConnection` via `ISqlConnectionFactory.CreateConnectionAsync(cancellationToken)` (`RISK-002`), and executes a 5-result-set parameterized Dapper `QueryMultipleAsync` batch using `CommandDefinition` with `cancellationToken`.
   - Verified SQL table and column bindings against DbUp scripts ([`20261101_0202_Students_AdmissionApplications.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261101_0202_Students_AdmissionApplications.sql), [`20261103_0201_Students_Admission_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261103_0201_Students_Admission_Tables.sql), [`20261103_0301_Students_EnrollmentIdentity_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261103_0301_Students_EnrollmentIdentity_Tables.sql), [`20261104_0101_Students_PlacementStatus_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261104_0101_Students_PlacementStatus_Tables.sql)) and [`StudentsDemoDataSeeder.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/Features/DemoData/StudentsDemoDataSeeder.cs):
     - `STD-01` & `STD-06`: Queries `students.students`, `students.student_statuses` (`is_current = 1`), and `students.administrative_placements` (`is_current = 1`) with `@Campus` (`ap.campus`) and `@SchoolLevel` (`ap.school_level`) filters, computing active count (`status = 'active'`), attrition count (`status IN ('withdrawn', 'transferred_out')`), and attrition rate with `NULLIF(..., 0)` + `COALESCE(..., 0.0)`.
     - `STD-02` & `STD_ENROLLMENT_BLOCKED`: Queries `students.admission_applications aa LEFT JOIN students.enrollments e ON e.application_id = aa.id` scoped to `aa.academic_year_id = @AcademicYearId`, `@Campus` (`aa.requested_campus`), and `@SchoolLevel` (supporting both exact level literals and `Grade 1..6` → `ELC`, `Grade 7..9` → `JHLC`, `Grade 10..12` → `HSLC` mapping matching seeded `requested_entry_level` values), with `NULLIF(..., 0)` conversion rate safeguard.
     - `STD-03`: Queries `students.applicant_documents` joined to `students.admission_applications` (`status IN ('submitted', 'under_review', 'accepted')`) without referencing non-existent `is_mandatory` column, computing `verified` vs pending (`not_submitted`, `submitted`, `rejected`) with `NULLIF(..., 0)`.
     - `STD-07`: Groups active learners by `(ap.campus, ap.school_level, Grade, Cohort)` against `_standardSectionCapacity = 20` students/section (`STD-COHORT-FILL` breakdown and `STD-07` utilization metric).
     - `STD_LEAVE_EXPIRED`: Queries `students.student_statuses` (`status = 'leave_expired'` or `status = 'on_leave'`) with `OUTER APPLY` against the provisioned `students.administrative_history_entries` table (`event_type = 'leave_of_absence'`, `is_correction = 0`, `> 14 days` threshold), correctly replacing the prose reference `student_lifecycle_events`.
   - Verified breakdowns (`STD-FUNNEL`, `STD-COHORT-FILL`), signals (`STD_ENROLLMENT_BLOCKED`, `STD_LEAVE_EXPIRED`), thresholds, `"neutral"` empty-filter fallbacks (`RISK-004`), and Level 4 drill-down routes match TD-03 and TD-06.

3. **Summary Provider & DI (`StudentsKpiSummaryProvider.cs`, `DependencyInjection.cs`):**
   - Verified [`StudentsKpiSummaryProvider.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/StudentsKpiSummaryProvider.cs) implements `IDomainKpiSummaryProvider` (`DomainKey => "students"`, `SortOrder = 1`) returning headline `STD-01`, `DomainHealthSummaryDto` (`Active Students` / `Attrition Rate`, `/dashboard/students`), and active `AttentionSignals`.
   - Verified [`DependencyInjection.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/DependencyInjection.cs#L104-L106) registers `GetStudentsKpiDashboardHandler` and `IDomainKpiSummaryProvider` -> `StudentsKpiSummaryProvider` as scoped services.

4. **Build & Architecture Tests Verification:**
   - Executed `dotnet build src/backend/Elsasa.Sis.Students --no-restore`: **Build succeeded. 0 Warning(s), 0 Error(s).**
   - Executed `dotnet test tests/backend/Elsasa.Sis.ArchitectureTests --no-restore`: **Passed! - Failed: 0, Passed: 311, Skipped: 0, Total: 311.**

---

# Findings

No `BLOCKER`, `MAJOR`, or `MINOR` findings.

| ID | Severity | Description | Evidence |
| :--- | :--- | :--- | :--- |
| `NOTE-P2-S04-01` | `NOTE` | `GetStudentsKpiDashboardHandler` adapts `STD-03` (`students.applicant_documents` has no `is_mandatory` column) and `STD_LEAVE_EXPIRED` (`students.administrative_history_entries` with `event_type = 'leave_of_absence'` instead of `student_lifecycle_events`) to match the provisioned DbUp schema, and maps `requested_entry_level` grades (`Grade 1`–`Grade 12`) to `ELC`/`JHLC`/`HSLC` for consistent admission filtering. | [`GetStudentsKpiDashboardHandler.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/Features/Kpis/GetStudentsKpiDashboard/GetStudentsKpiDashboardHandler.cs#L67-L153) |
