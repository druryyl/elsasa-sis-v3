---
Title: Review Report — P3-S06 Academics Domain KPI Vertical Slice & Summary Provider
Code: CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS
Slice: P3-S06
Artifact: REVIEW
ReviewIteration: 0
Decision: GO
Date: 2026-11-11
---

# Review Summary: P3-S06 — Academics Domain KPI Vertical Slice & Summary Provider

- **Decision:** `GO`
- **ReviewIteration:** `0`
- **Dependencies Checked:** `P1-S01` (`IMPLEMENTED`, `GO`), `P1-S02` (`IMPLEMENTED`, `GO`)
- **Plan Status Updated:** `P3-S06` `Review Status` set to `GO` in [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md). Plan-level `Status` left unchanged (`NOT-STARTED`).

---

# Verification Against Completion Criteria & Architecture

1. **Endpoint (`GetAcademicsKpiDashboardEndpoint.cs`):**
   - Verified `GET /api/academics/kpis/dashboard` is implemented via `IEndpoint` in [`GetAcademicsKpiDashboardEndpoint.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/GetAcademicsKpiDashboardEndpoint.cs) and automatically discovered by [`AcademicsEndpoints.MapAcademicsEndpoints()`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/AcademicsEndpoints.cs#L35-L43), which is wired in [`Program.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Api/Program.cs#L124).
   - Verified `.RequireAuthorization()` and `HasAccess(currentUser)` enforce authentication and TD-05 (`DEC-006`) access rules: wildcard `*`, permission `Permissions.Academics.Participations.View` (`academics.participations.view`) or `"Permissions.Academics.Coursework.View"`, or roles `Admin`, `SuperAdmin`, `AcademicAdmin`, `AcademicCoordinator` (`Academic Coordinator`), `AcademicAuthority` (`Academic Authority`), `Teacher`.
   - Verified query parameter bindings (`academicYearId`, `academicPeriodId`, `campus`, `schoolLevel`) map cleanly to `GlobalKpiFilterDto`.

2. **Handler & SQL Verification (`GetAcademicsKpiDashboardHandler.cs`):**
   - Verified [`GetAcademicsKpiDashboardHandler.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/GetAcademicsKpiDashboardHandler.cs) resolves period context and available semester options via `IKpiPeriodResolver`, opens its own `SqlConnection` via `ISqlConnectionFactory.CreateConnectionAsync(cancellationToken)` (`RISK-002`), and executes a parameterized 5-result-set Dapper `QueryMultipleAsync` batch using `CommandDefinition` with `cancellationToken`.
   - Verified all SQL table and column bindings against DbUp scripts ([`20261105_0401_Academics_ParticipationActivities_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261105_0401_Academics_ParticipationActivities_Tables.sql), [`20261105_0402_Academics_AssessmentResults_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261105_0402_Academics_AssessmentResults_Tables.sql), [`20261107_0401_Academics_StandingProgression_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261107_0401_Academics_StandingProgression_Tables.sql), [`20261104_0101_Students_PlacementStatus_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261104_0101_Students_PlacementStatus_Tables.sql), [`20261102_0101_Operations_CalendarPeriods_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261102_0101_Operations_CalendarPeriods_Tables.sql)), [`GetAtRiskLearnersHandler.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/AcademicInterventions/GetAtRiskLearners/GetAtRiskLearnersHandler.cs), and [`AcademicsActivitiesDemoDataSeeder.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/DemoData/AcademicsActivitiesDemoDataSeeder.cs):
     - `academics.academic_standings`: `student_id`, `academic_period_id`, `standing` (`'good_standing'`, `'conditional_standing'`, `'not_in_good_standing'`).
     - `academics.academic_participations`: `student_id`, `academic_period_id`.
     - `academics.learning_progress`: `student_id`, `academic_period_id`, `status` (`'needs_remediation'`).
     - `academics.academic_interventions`: `student_id`, `academic_period_id`, `status` (`'planned'`, `'in_progress'`).
     - `academics.attendance_records` + `academics.learning_activities`: `student_id`, `learning_activity_id`, `status` (`'present'`, `'late'`, `'excused_absence'`, `'absent'`, `'partial'`), `activity_date`, `la.status <> 'cancelled'`.
     - `academics.submissions` + `academics.assignments`: `student_id`, `assignment_id`, `sub.status` (`'submitted'`, `'excused'`, `'exempted'`, `'late'`, `'missing'`), `a.status` (`'released'`, `'closed'`), `a.learning_activity_id`.
     - `academics.academic_results` + `academics.assessments`: `student_id`, `assessment_id`, `is_final = 1`, `result_status` (`'published'`, `'approved'`, `'provisional'`), `asm.assessment_date`, `res.evaluated_date`.
     - `operations.operational_periods`: `id`, `start_date`, `end_date`.
     - `students.administrative_placements`: `student_id`, `is_current = 1`, `campus`, `school_level` (enforced single active row per student via unique filtered index `ux_administrative_placements_student_active`).
   - Verified KPI formulas, prior-semester (`Semester 1`) comparison deltas, thresholds, breakdowns (`ACA-STANDING-DISTRIBUTION`, `ACA-SUBMISSION-STATUS`), divide-by-zero/empty-filter `neutral` safeguards (`RISK-004`), and signals (`ACA_AT_RISK_UNADDRESSED`, `ACA_CHRONIC_ABSENTEEISM`, `ACA_MISSING_SUBMISSIONS`, `ACA_UNPUBLISHED_RESULTS`) match Architecture TD-03.2 and `GetAtRiskLearnersHandler` parity criteria.

3. **Summary Provider & DI (`AcademicsKpiSummaryProvider.cs`, `DependencyInjection.cs`):**
   - Verified [`AcademicsKpiSummaryProvider.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/AcademicsKpiSummaryProvider.cs) implements `IDomainKpiSummaryProvider` (`DomainKey => "academics"`, `SortOrder = 2`), returning headline `ACA-01`, `DomainHealthSummaryDto` (`HeadlineValue` = `ACA-01`, `SecondaryValue` = `ACA-03`, `DashboardRoute = "/dashboard/academics"`), and ordered `AttentionSignals`.
   - Verified `GetAcademicsKpiDashboardHandler` and `IDomainKpiSummaryProvider` (`AcademicsKpiSummaryProvider`) are registered as scoped services in [`DependencyInjection.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/DependencyInjection.cs#L320-L322).

4. **Build Verification:**
   - Executed `dotnet build src/backend/Elsasa.Sis.Academics --no-restore`: **Build succeeded. 0 Warning(s), 0 Error(s).**

---

# Findings

No `BLOCKER`, `MAJOR`, or `MINOR` findings.

| ID | Severity | Description | Evidence |
| :--- | :--- | :--- | :--- |
| `NOTE-P3-S06-01` | `NOTE` | `ParticipatingStudents` in `ACA-02` queries `academics.academic_participations` by `academic_period_id` without filtering `p.status = 'active'` because `CK_academics_academic_participations_status` only allows `('in_progress', 'paused', 'withdrawn', 'completed')`. This accurately handles both `Semester 2` (`in_progress`) and `Semester 1` (`completed`) participations. | [`GetAcademicsKpiDashboardHandler.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/GetAcademicsKpiDashboardHandler.cs#L54-L64), [`20261105_0401_Academics_ParticipationActivities_Tables.sql`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Database/Scripts/20261105_0401_Academics_ParticipationActivities_Tables.sql#L70-L71) |
| `NOTE-P3-S06-02` | `NOTE` | `ACA-03`, `ACA-04`, and `ACA-05` scope `learning_activities` and `assessments` to the resolved period via `op.start_date` and `op.end_date` (which matches `OperationsDemoDataSeeder` and `AcademicsActivitiesDemoDataSeeder` timelines since those tables have no `academic_period_id` column). | [`GetAcademicsKpiDashboardHandler.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/Kpis/GetAcademicsKpiDashboard/GetAcademicsKpiDashboardHandler.cs#L120-L186) |
