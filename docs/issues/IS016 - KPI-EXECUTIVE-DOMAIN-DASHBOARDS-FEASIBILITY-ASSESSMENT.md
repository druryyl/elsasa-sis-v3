# FEASIBILITY ASSESSMENT

## Metadata
- **Artifact Type:** FEASIBILITY-ASSESSMENT
- **Artifact ID:** FA-KPI-EXECUTIVE-DOMAIN-DASHBOARDS
- **Related Issue ID:** `CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS` (`docs/issues/IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ISSUE.md`)
- **Feature ID:** `FEAT-KPI-EXECUTIVE-DOMAIN-DASHBOARDS` (Release 1 — Essential Executive & Domain Dashboards)
- **Domain ID(s):**
  - `DOM-OPERATIONS` (`operations.*` — Calendar & Periods)
  - `DOM-STUDENTS` (`students.*` — Admission, Enrollment & Identity, Placement & Status)
  - `DOM-ACADEMICS` (`academics.*` — Learning Structure, Participation & Activities, Assessment & Results, Standing & Progression)
  - `DOM-FINANCE` (`finance.*` — Obligations & Billing, Payments & Receivables)
  - `DOM-TEACHING` (`teaching.*` — Teacher Resources & Qualification, Assignment & Workload)
  - `DOM-DEVELOPMENT` (`development.*` — Stage 3 Pending Placeholder)
- **Author:** Analyst / Architect
- **Assessment Status:** READY-FOR-PLANNING
- **Assessment Outcome:** FEASIBLE-WITH-CONDITIONS
- **Version:** 1.0
- **Last Updated:** 2026-11-11

---

## 1. Assessment Summary

### Feature Summary
`CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS` delivers **Release 1 (Essential Executive & Domain Dashboards)** of the Elsasa SIS V3 KPI & Institutional Intelligence layer across the 4-level decision-oriented information architecture defined in `docs/kpi/KPI-DASHBOARD-IA.md`:
1. **Level 1 — Executive Overview (`/dashboard`)**: Replaces the static hard-coded placeholder cards in `src/frontend/src/features/dashboard/views/DashboardView.vue` with a live, filter-aware institutional health command center featuring **5 headline KPI cards** (`STD-01`, `ACA-01`, `FIN-01`, `TCH-01`, `OPS-01`), **6 Domain Health Summary cards** (including the Stage 3 `Development` placeholder card), and a unified **Needs Attention Queue** aggregating up to 12 actionable operational signals across domains (`GET /api/dashboard/executive`).
2. **Level 2 — Domain Dashboards (`/dashboard/students`, `/dashboard/academics`, `/dashboard/finance`, `/dashboard/teaching`, `/dashboard/operations`, plus `/dashboard/development` placeholder)**: Provides 3–5 headline KPI cards with prior-period comparison deltas, 2–3 diagnostic breakdown visualizations (aging buckets, standing breakdowns, admission funnel, workload bands, attendance/submission trends) built with lightweight zero-dependency SVG/CSS primitives, and a domain-scoped exception queue (`GET /api/{domain}/kpis/dashboard`).
3. **Level 3 → Level 4 — Actionable Drill-Down Navigation**: Connects every KPI card, chart bar/segment, and attention signal to the actual operational workbench routes in `src/frontend/src/app/router/routes.ts` with URL query-parameter filter/tab hydration (`route.query`).
4. **Demo Sample Data Generator Enrichment (`DemoDataOrchestrator` & Domain Seeders)**: Enhances the five existing domain seeders deterministically so that every Release 1 KPI, Semester 2 vs. Semester 1 delta, diagnostic distribution chart, multi-campus filter (`campus-main` and `campus-east`), and Attention Signal renders realistic, non-zero institutional conditions while preserving all existing persona test invariants (`DemoDataIntegrityTest` and `DemoDataResetTest`).

### Feasibility Conclusion
The change request is **FEASIBLE-WITH-CONDITIONS** under a **Pragmatic, Explicit, and Minimalist** implementation approach:
- **Zero New DDL / Schema Migrations Required**: All 5 operational domains (`operations.*`, `students.*`, `academics.*`, `finance.*`, `teaching.*`) and `finance.v_student_receivables` are already provisioned in `src/backend/Elsasa.Sis.Database/Scripts/`. By executing direct parameterized Dapper `QueryMultipleAsync` batches inside each domain's vertical slice and composing Level 1 via `IDomainKpiSummaryProvider` in `Elsasa.Sis.Contracts`, we avoid adding unnecessary SQL helper views or schema migrations.
- **Explicit Schema & Formula Reconciliation**: Several conceptual column names and enum literals in `docs/kpi/KPI-ARCHITECTURE.md` and `docs/kpi/KPI-CATALOG.md` differed from the actual DbUp migrations (documented in `GAP-001`). Binding queries directly to the provisioned SQL columns and status values eliminates any need for database alterations.
- **Targeted Sample Data Enrichment**: Code inspection of the six Demo Data Seeders revealed why several dashboard charts and signals would currently render as `0` (e.g., `31-60`, `61-90`, and `90+ Days` overdue receivables = `0`; unverified payments = `0`; unallocated cash = `0`; 45 of 48 students missing `academic_standings` and `learning_progress` rows; student attrition = `0%`; unresolved teaching capacity conflicts = `0`; unreviewed calendar amendments = `0`; campus mismatch `'Kampus Utama'` vs `'campus-main'`). Enriching the existing domain seeders deterministically resolves all visual data gaps within the `< 5000ms` reset budget.

### Key Conditions or Blockers
All 6 identified gaps (`GAP-001` through `GAP-006`) and 6 open questions (`OQ-001` through `OQ-006`) were resolved with the stakeholder during the feasibility review on `2026-11-11`. All gap-closure conditions are `CLOSED`, and the issue is granted **READY-FOR-PLANNING**.

---

## 2. Input Artifacts Reviewed

| Artifact Type | Artifact ID | Title | Version / Status | Relevance to Assessment |
| --- | --- | --- | --- | --- |
| ISSUE | `CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS` | `docs/issues/IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ISSUE.md` | Draft / Intake Complete | Authoritative scope and acceptance criteria for Release 1 KPI Dashboards |
| KPI-SPEC | `KPI-CATALOG` | `docs/kpi/KPI-CATALOG.md` | v1.0 | Conceptual definitions, formulas, and thresholds for the 21 Release 1 KPIs |
| KPI-SPEC | `KPI-DASHBOARD-IA` | `docs/kpi/KPI-DASHBOARD-IA.md` | v1.0 | 4-level dashboard hierarchy, screen wireframes, attention signals, and drill-down map |
| KPI-SPEC | `KPI-ARCHITECTURE` | `docs/kpi/KPI-ARCHITECTURE.md` | v1.0 | Proposed backend/frontend technical architecture and DTO contracts |
| KPI-SPEC | `KPI-FEASIBILITY` | `docs/kpi/KPI-FEASIBILITY.md` | v1.0 | Initial conceptual schema mapping across domains |
| KPI-SPEC | `KPI-ROADMAP` | `docs/kpi/KPI-IMPLEMENTATION-ROADMAP.md` | v1.0 | Release 1 scope boundaries (Phases 1.1–1.4) |
| ARCHITECTURE | `ARC-BACKEND` | `docs/architecture/backend-architecture.md` | v1.0 / Approved | Modular Monolith VSA, compile-time domain isolation, Dapper, selective cross-schema read `JOIN`s |
| ARCHITECTURE | `ARC-DEMO-SAMPLE-DATA` | `docs/issues/IS011 - DEMO-SAMPLE-DATA-GENERATOR-ARCHITECTURE.md` | v1.2 / Approved | `IDemoDataSeeder` / `DemoDataOrchestrator` architecture, deterministic personas, and dynamic date anchoring |
| CODEBASE | `Elsasa.Sis.Database` | `src/backend/Elsasa.Sis.Database/Scripts/*.sql` | Provisioned | Authoritative SQL Server tables, constraints, and `finance.v_student_receivables` view |
| CODEBASE | `DemoDataSeeders` | `Operations`, `Students`, `Finance`, `Academics`, `Teaching` seeders & `DemoDataIntegrityTest.cs` | Implemented | Actual seeded record distributions, persona IDs, and integration test guardrails |

---

## 3. Current System Baseline

### Relevant Existing Capabilities
1. **Database Schemas & Views (`src/backend/Elsasa.Sis.Database/Scripts/`)**:
   - `operations.*`: `academic_years`, `academic_periods`, `operational_periods`, `school_calendars`, `calendar_events`, `calendar_amendments`, `school_closures`.
   - `students.*`: `students`, `applicants`, `admission_applications`, `applicant_documents`, `admission_decisions`, `enrollments`, `administrative_placements`, `student_statuses`, `student_lifecycle_events`.
   - `academics.*`: `learning_structures`, `learning_units`, `academic_participations`, `learning_activities`, `attendance_records`, `assignments`, `submissions`, `assessments`, `academic_results`, `academic_standings`, `learning_progress`, `promotion_recommendations`, `academic_interventions`, `academic_achievements`, `report_cards`, `transcripts`.
   - `finance.*`: `financial_obligations`, `billing_documents`, `payments`, `payment_allocations`, `student_accounts`, and `finance.v_student_receivables` (`20261104_0301_Finance_PaymentsReceivables_Tables.sql`).
   - `teaching.*`: `teacher_profiles`, `qualification_evidence`, `teaching_authorizations`, `teaching_demands`, `teaching_assignments`, `teacher_availabilities`, `workload_policies`, `workload_commitments`, `workload_exceptions`, `capacity_conflicts`, `teaching_substitutions`, `teaching_reassignments`.
2. **Existing Analytical Read Precedent**:
   - `GetAtRiskLearnersHandler` (`src/backend/Elsasa.Sis.Academics/Features/AcademicInterventions/GetAtRiskLearners/GetAtRiskLearnersHandler.cs`) already implements a cross-schema Dapper read query joining `academics.academic_participations`, `students.students`, `academics.academic_standings`, `academics.learning_progress`, `academics.attendance_records`, and `academics.academic_interventions`.
3. **Frontend Shell, Context Store & Router**:
   - `AppLayout.vue` (`src/frontend/src/app/layouts/AppLayout.vue`) provides the collapsible sidebar, campus selector (`campus-main`, `campus-east`), academic period display, and `Reset Demo Data` dialog.
   - `useAppContextStore` (`src/frontend/src/app/stores/appContext.store.ts`) persists `activeCampusId` and `activeAcademicPeriodId` in `localStorage`.
   - `DashboardView.vue` (`src/frontend/src/features/dashboard/views/DashboardView.vue`) currently displays 4 hard-coded static summary numbers (`1,248`, `94.2%`, `Rp 42.5M`, `14`) and 5 static module cards.

### Relevant Architectural and Technical Constraints
- **Compile-Time Domain Isolation (`NetArchTest`)**: Domain projects (`Elsasa.Sis.Operations`, `Elsasa.Sis.Students`, `Elsasa.Sis.Academics`, `Elsasa.Sis.Finance`, `Elsasa.Sis.Teaching`) must **never** reference each other. Cross-domain orchestration for Level 1 (`/api/dashboard/executive`) must reside in `Elsasa.Sis.Api` consuming interfaces declared in `Elsasa.Sis.Contracts`.
- **Selective Cross-Schema Read JOINs**: Per `docs/architecture/backend-architecture.md` (§1.5 & §4.2.4), read-only Dapper queries inside a domain slice may selectively `JOIN` external schemas (specifically `students.administrative_placements` and `operations.academic_periods`) to filter or group by `campus`, `school_level`, `grade`, or `academic_year_id`.
- **Demo Reset Performance & Test Guardrails**: `DemoDataResetTest.cs` enforces that `/api/operations/demo-data/reset` completes in `< 5000ms` with `0` orphaned foreign keys and within bounded seed counts (`TotalStudentsSeeded` between `41` and `59`, `TotalInvoicesSeeded < 200`, `TotalAttendanceSeeded` between `1001` and `1999`, `TotalResultsSeeded < 200`, `TotalReportCardsSeeded < 100`).

---

## 4. Feasibility Analysis

### 4.1 Business and Functional Fit
- The 21 Release 1 KPIs (`STD-01..03, 06, 07`, `ACA-01..05`, `FIN-01..05`, `TCH-01..04`, `OPS-01..02`) and 12 Attention Signals directly answer the operational questions of the Principal, VP Academics, Finance Head, Admissions Officer, and Operations Admin.
- Every KPI can be computed deterministically from existing transactional tables once the conceptual column/status names in `docs/kpi/*.md` are mapped to the actual provisioned SQL schema (see Table 4.2 below).

### 4.2 Authoritative Release 1 KPI & Attention Signal Schema Mapping (`GAP-001` Resolution)
The table below establishes the authoritative binding between each Release 1 KPI / Signal and the **actual DbUp SQL Server schema**:

| KPI / Signal Code | Authoritative SQL Tables & Columns | Exact Status / Filter Binding |
| --- | --- | --- |
| **`STD-01`** Active Enrolled Students | `students.students s` + `students.student_statuses ss` (`is_current = 1`) + `students.administrative_placements ap` (`is_current = 1`) | `ss.status = 'active'` AND `ap.is_current = 1` (grouped by `ap.campus`, `ap.school_level`, `ap.grade`, `ap.cohort`; filtered by `ap.academic_year`) |
| **`STD-02`** Admission Pipeline Conversion & Gate Backlog | `students.admission_applications aa` + `students.admission_decisions ad` + `students.applicant_documents doc` + `students.enrollments e` | Funnel stages via `aa.status IN ('submitted','under_review','waitlisted','accepted','rejected','withdrawn')` + enrolled check via `students.enrollments` (`application_id`). **Gate Backlog (`STD_ENROLLMENT_BLOCKED`)**: `aa.status = 'accepted'` AND `NOT EXISTS (SELECT 1 FROM students.enrollments e WHERE e.application_id = aa.id)` |
| **`STD-03`** Admission Document Verification Rate | `students.admission_applications aa` + `students.applicant_documents doc` | `aa.status IN ('submitted','under_review','accepted')`; verified where `doc.is_mandatory = 1 AND doc.submission_status = 'verified'` vs pending `doc.submission_status IN ('not_submitted','submitted','rejected')` |
| **`STD-06`** Student Attrition & Withdrawal Rate | `students.student_statuses ss` + `students.student_lifecycle_events sle` + `students.administrative_placements ap` | Exits where `ss.is_current = 1 AND ss.status IN ('withdrawn','transferred_out')` (or `sle.event_type IN ('withdrawal','transfer_out')` within period date window). **Leave Expired (`STD_LEAVE_EXPIRED`)**: `ss.is_current = 1 AND ss.status = 'on_leave'` with expired return date in `sle.metadata` / `effective_date` |
| **`STD-07`** Cohort & Section Fill Utilization | `students.administrative_placements ap` (`is_current = 1`) + `students.student_statuses ss` (`status = 'active'`) | Headcount per `(ap.campus, ap.school_level, ap.grade, ap.cohort)` divided by standard cohort target capacity (`20` students per cohort section default) |
| **`ACA-01`** Students in Good Academic Standing Rate | `academics.academic_standings ast` + `students.administrative_placements ap` (`is_current = 1`) | `ast.academic_period_id = @PeriodId`; Good Standing: `ast.standing = 'good_standing'`; Conditional/At-Risk: `ast.standing IN ('conditional_standing','not_in_good_standing')` |
| **`ACA-02`** At-Risk Learners & Remediation Coverage | `academics.academic_participations p` + `academics.academic_standings ast` + `academics.learning_progress lp` + `academics.attendance_records ar` + `academics.academic_interventions ai` | Reuses `GetAtRiskLearnersHandler` logic: learner is at-risk if `ast.standing != 'good_standing'` OR `lp.status = 'needs_remediation'` OR attendance rate `< 80%`. Covered if `EXISTS (SELECT 1 FROM academics.academic_interventions ai WHERE ai.student_id = s.id AND ai.academic_period_id = @PeriodId AND ai.status IN ('planned','in_progress'))`. Unaddressed count feeds **`ACA_AT_RISK_UNADDRESSED`** |
| **`ACA-03`** Period Attendance Rate & Chronic Absenteeism | `academics.attendance_records ar` + `academics.learning_activities la` + `academics.learning_units lu` + `academics.learning_structures ls` | Filtered by `ls.academic_period_id = @PeriodId` (or `la.activity_date` in period). Attended: `ar.status IN ('present','late','excused_absence')` vs Total `ar.status IN ('present','late','excused_absence','absent','partial')`. Chronic Absenteeism (`ACA_CHRONIC_ABSENTEEISM`): learner attendance `< 85%` (warning) / `< 80%` (critical) |
| **`ACA-04`** Coursework Submission Completion Rate | `academics.assignments a` + `academics.learning_activities la` + `academics.submissions sub` | Active assignments: `a.status IN ('released','closed')` in period. On-time/completed: `sub.status IN ('submitted','excused','exempted')`; Late: `sub.status = 'late'`; Missing (`ACA_MISSING_SUBMISSIONS`): `sub.status = 'missing'` |
| **`ACA-05`** Assessment Result Publication Readiness | `academics.assessments a` + `academics.academic_results ar` | Published: `ar.is_final = 1 AND ar.result_status = 'published'`; Approved pending publish: `ar.result_status = 'approved'`; Provisional / Awaiting grading (`ACA_UNPUBLISHED_RESULTS`): `ar.result_status = 'provisional'` |
| **`FIN-01`** Net Tuition Collection Rate | `finance.v_student_receivables r` + `students.administrative_placements ap` (`is_current = 1`) | For positive assessed obligations (`r.net_obligation > 0`): `SUM(r.total_paid) / NULLIF(SUM(r.total_paid + r.outstanding_amount), 0) * 100` (accurately accounting for scholarship credit rows without negative-sum distortion) |
| **`FIN-02`** Total Outstanding Receivables | `finance.v_student_receivables r` | `SUM(r.outstanding_amount)` and `COUNT(DISTINCT r.student_id)` where `r.outstanding_amount > 0` |
| **`FIN-03`** Overdue Receivable Rate & Delinquent Accounts | `finance.v_student_receivables r` | Overdue amount: `SUM(CASE WHEN r.is_overdue = 1 THEN r.outstanding_amount ELSE 0 END)`; Delinquent accounts: `COUNT(DISTINCT CASE WHEN r.is_overdue = 1 THEN r.student_id END)` |
| **`FIN-04`** Receivables Aging Distribution (`0-30`, `31-60`, `61-90`, `90+`) | `finance.v_student_receivables r` | Grouped by `r.aging_bracket IN ('Current','1-30 Days','31-60 Days','61-90 Days','90+ Days')`. Bucket `'90+ Days'` feeds **`FIN_OVERDUE_90_PLUS`** |
| **`FIN-05`** Unverified Payment Backlog & Unallocated Cash | `finance.payments p` + `finance.payment_allocations pa` (`status = 'active'`) | **Unverified (`FIN_UNVERIFIED_PAYMENTS`)**: `p.confirmation_status = 'submitted'`. **Unallocated Cash (`FIN_UNALLOCATED_CASH`)**: `p.confirmation_status = 'confirmed'` where `p.amount - ISNULL(SUM(pa.allocated_amount), 0) > 0` |
| **`TCH-01`** Instructional Demand Coverage Rate | `teaching.teaching_demands td` | Covered: `td.status = 'staffed'`; Total active demands: `td.status IN ('received','accepted_for_staffing','changed','staffed')` filtered by `td.academic_period_id` and `td.campus_scope` |
| **`TCH-02`** Open Unstaffed Teaching Demands | `teaching.teaching_demands td` | Open demands (`TCH_UNSTAFFED_DEMAND`): `td.status IN ('received','accepted_for_staffing','changed')`, grouped by `td.required_capability_code` and `td.campus_scope` |
| **`TCH-03`** Teacher Workload Utilization Distribution | `teaching.workload_commitments wc` + `teaching.teacher_profiles tp` | Grouped by `wc.status IN ('under_allocated','normal','approaching_limit','overloaded_with_exception','breached')`. Breached/Overloaded feeds **`TCH_WORKLOAD_BREACH`** |
| **`TCH-04`** Unresolved Capacity & Qualification Conflicts | `teaching.capacity_conflicts cc` + `teaching.qualification_evidence qe` | Open conflicts: `cc.status = 'unresolved'` by `cc.severity IN ('blocking','advisory')`. Blocking conflicts feed **`TCH_BLOCKING_CONFLICT`**. Pending qualifications: `qe.verification_state = 'submitted'` |
| **`OPS-01`** Academic Calendar & Period Readiness Index | `operations.school_calendars sc` + `operations.academic_periods ap` + `operations.calendar_amendments ca` | Published calendars (`sc.publication_status = 'published'`), configured periods (`ap.status IN ('planned','active','closed')`), and unreviewed amendments (`ca.downstream_impact_reviewed = 0` → **`OPS_UNREVIEWED_AMENDMENT`**) |
| **`OPS-02`** Unplanned School Closure Days | `operations.school_closures cl` | `cl.status IN ('authorized','extended','lifted')`, grouped by `cl.reason_category` and `cl.affected_campus`; active closures feed **`OPS_ACTIVE_CLOSURE`** |

### 4.3 Technical and Architectural Fit
- **Domain Vertical Slices (`Features/Kpis/`)**:
  - Each of the 5 operational domain assemblies (`Elsasa.Sis.Students`, `Elsasa.Sis.Academics`, `Elsasa.Sis.Finance`, `Elsasa.Sis.Teaching`, `Elsasa.Sis.Operations`) exposes:
    1. Its own Level 2 endpoint: `GET /api/{domain}/kpis/dashboard` (`Get{Domain}KpiDashboardEndpoint` + `Get{Domain}KpiDashboardHandler`).
    2. An implementation of `IDomainKpiSummaryProvider` (defined in `Elsasa.Sis.Contracts/Kpis/`) returning the domain's Level 1 headline KPI, domain health card summary, and active attention signals.
- **Executive Aggregation (`Elsasa.Sis.Api/Features/Dashboard/GetExecutiveDashboard/`)**:
  - `GET /api/dashboard/executive` resolves the active/selected `AcademicPeriodId`, `AcademicYearId`, and prior comparison `AcademicPeriodId` (e.g., matching `Semester 2` with `Semester 1` of the same academic year), invokes all registered `IDomainKpiSummaryProvider` implementations, appends the static Stage 3 `Development` placeholder card, sorts the `NeedsAttentionQueue` by severity (`critical` first, then `warning`), and returns `ExecutiveDashboardResponse`.
- **Single-Roundtrip Multi-Result Queries (`QueryMultipleAsync`)**:
  - Because `Microsoft.Data.SqlClient` does not permit concurrent commands on the same connection without MARS, each domain handler opens its own `IDbConnection` via `ISqlConnectionFactory.CreateConnectionAsync()` and executes a single batched T-SQL script via Dapper `QueryMultipleAsync`.

### 4.4 Demo Sample Data Generator Assessment (`GAP-004` Resolution)
To satisfy the requirement that dashboards represent realistic operational conditions across all cards, charts, filters, and signals, the existing Demo Data Seeders will be enhanced deterministically as follows (while keeping all existing flagship persona records and test count bounds intact):

| Domain Seeder | Current State (Why Charts/Signals Show 0) | Deterministic Enrichment for Release 1 Dashboards |
| --- | --- | --- |
| **`OperationsDemoDataSeeder`** | Only 1 calendar for `campus-main`; 1 amendment (`AMD-CAL-2026-001`) with `DownstreamImpactReviewed = true` (`OPS_UNREVIEWED_AMENDMENT = 0`). | 1. Add a second calendar amendment (`AMD-CAL-2026-002`) with `DownstreamImpactReviewed = false` (`OPS-01` = ~85%, `OPS_UNREVIEWED_AMENDMENT = 1`).<br>2. Seed a published school calendar for `campus-east` so campus filtering shows valid readiness on both campuses. |
| **`StudentsDemoDataSeeder`** | 48 enrolled students all `'active'` in `'campus-main'` (`STD-06` Attrition = `0%`, `STD_LEAVE_EXPIRED = 0`, `campus-east = 0`). | 1. Keep the 48 enrolled students (preserving `DemoDataResetTest` `40 < TotalStudentsSeeded < 60`).<br>2. Assign 8 non-flagship students to `'campus-east'` (`ELC-B` / `JHLC-B`) so multi-campus bar charts and campus switcher have realistic data.<br>3. Set 2 non-flagship students to `'withdrawn'` / `'transferred_out'` with `student_lifecycle_events` (`STD-06` Attrition = ~4.2%) and 1 non-flagship student to `'on_leave'` with an expired return date (`STD_LEAVE_EXPIRED = 1`).<br>4. Add 1 additional accepted applicant awaiting mandatory document verification alongside Esther Nugroho (`STD-02` Gate Backlog = 2). |
| **`FinanceTransactionsDemoDataSeeder`** | S2 overdue only on Grace Santoso within `< 30 days` (`31-60`, `61-90`, `90+` aging buckets = `0`); 0 `'submitted'` unverified payments (`FIN-05 = 0`); 0 unallocated cash (`FIN_UNALLOCATED_CASH = 0`). | 1. Spread overdue due dates across non-flagship S2 obligations so `v_student_receivables` populates all 4 overdue aging brackets: `1-30 Days` (4 students), `31-60 Days` (3 students), `61-90 Days` (2 students), and `90+ Days` (2 students, triggering `FIN_OVERDUE_90_PLUS`).<br>2. Seed 4 payments with `confirmation_status = 'submitted'` (2 aged `> 3 days`, triggering `FIN-05` & `FIN_UNVERIFIED_PAYMENTS`).<br>3. Seed 2 `'confirmed'` payments with partial/zero allocation in `finance.payment_allocations` (triggering `FIN_UNALLOCATED_CASH`). |
| **`AcademicsActivitiesDemoDataSeeder`** | Only 3 of 48 students have `academic_standings` and `learning_progress` rows; all coursework `submissions` are `'submitted'` (`0` `'late'` or `'missing'`). | 1. Seed `academic_standings` (both S1 and S2) and summary `learning_progress` across the remainingenrolled students: ~83% `'good_standing'`, ~11% `'conditional_standing'`, ~6% `'not_in_good_standing'` (with slightly improved S2 vs S1 rate for realistic trend delta).<br>2. Ensure 4–5 at-risk learners in S2: 2 covered by active `academic_interventions` and 2–3 unaddressed (triggering `ACA_AT_RISK_UNADDRESSED`).<br>3. Seed realistic coursework `submissions` mix: ~82% `'submitted'`, ~10% `'late'`, ~8% `'missing'` (triggering `ACA_MISSING_SUBMISSIONS`), while keeping Budi Wijaya's `'submitted'` record intact. |
| **`TeachingDemoDataSeeder` & `TeachingAssignmentsDemoDataSeeder`** | Uses `'Kampus Utama'` instead of `'campus-main'`; only 4 teachers (`2 normal`, `1 overloaded_with_exception`, `0 under_allocated`, `0 approaching_limit`, `0 breached`); `0` `'unresolved'` capacity conflicts (`TCH-04 = 0`). | 1. Standardize `_campusMain` to `'campus-main'` (and assign 1 teacher/demand to `'campus-east'`).<br>2. Add 2 additional teacher personas/commitments so all workload utilization bands (`under_allocated`, `normal`, `approaching_limit`, `overloaded_with_exception`, `breached`) are represented in `TCH-03`.<br>3. Seed 1 `'unresolved'` `'blocking'` capacity conflict and 1 `'unresolved'` `'advisory'` conflict on Hendra/new teacher (`TCH-04 = 2`, triggering `TCH_BLOCKING_CONFLICT`), plus 2 open unstaffed demands (`DEM-2026-0005` + 1 more). |

---

## 5. Gaps Identified

### GAP-001
- **Category:** Data | Scope
- **Description:** Several SQL column names and enum literals referenced in `docs/kpi/KPI-ARCHITECTURE.md`, `docs/kpi/KPI-CATALOG.md`, and `docs/kpi/KPI-FEASIBILITY.md` do not match the actual provisioned DbUp SQL scripts (e.g., `students.administrative_placements` uses `is_current = 1` instead of `placement_status = 'active'`; `students.enrollments` is only created post-enrollment so gate-blocked applicants live in `students.admission_applications` with `status = 'accepted'`; `academics.academic_standings` uses `standing IN ('good_standing','conditional_standing','not_in_good_standing')`; `teaching.teaching_demands` uses `expected_workload_hours`, `campus_scope`, and `status IN ('received','accepted_for_staffing','changed','staffed')`).
- **Impact:** High — Queries written against the conceptual column names in `KPI-ARCHITECTURE.md` would fail at runtime with `Invalid column name` SQL errors.
- **Severity:** High
- **Recommended Resolution:** Bind all 21 Release 1 KPI queries and 12 Attention Signals directly to the authoritative DbUp schema columns and enum values documented in Section 4.2, without adding redundant alias columns or new SQL views.
- **Status:** CLOSED
- **Decision:** Bind all 21 Release 1 KPIs and 12 Attention Signals strictly to actual DbUp schema columns/statuses and reuse the existing `GetAtRiskLearnersHandler` criteria for `ACA-02` / `ACA_AT_RISK_UNADDRESSED`, executing direct Dapper `QueryMultipleAsync` queries without new SQL DDL views.
- **Rationale:** Pragmatic, explicit, and minimalist—avoids schema churn and aligns KPI reporting with the exact columns written by domain command handlers.
- **Architecture Impact:** The `ARCHITECTURE` artifact (`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md`) must use the Section 4.2 SQL column/status bindings as the authoritative query specification.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

### GAP-002
- **Category:** Integration | UX
- **Description:** The Level 4 operational drill-down paths in `docs/kpi/KPI-DASHBOARD-IA.md` (§5) do not match the actual Vue Router paths in `src/frontend/src/app/router/routes.ts` (e.g., `/students/directory` vs `/students/records`, `/students/admissions` vs `/students/admission`, `/students/lifecycle-workbench` vs `/students/lifecycle`, `/finance/receivables` vs `/finance/obligations`, `/academics/progression` vs `/academics/cohort-review` & `/academics/interventions`, `/teaching/teachers` vs `/teaching/faculty`, `/operations/calendars` vs `/operations/calendar`), and existing workbench views do not read `route.query` on mount to pre-hydrate filters.
- **Impact:** High — Clicking KPI cards, chart segments, or attention signals would navigate to 404 routes or unfiltered default views.
- **Severity:** High
- **Recommended Resolution:** Map all Level 4 `drillDownRoute` URLs to the actual routes in `routes.ts`, build lightweight zero-dependency SVG/CSS chart components with clickable segments, and add `route.query` filter/tab hydration to the target workbench views.
- **Status:** CLOSED
- **Decision:** Build lightweight zero-dependency SVG/CSS chart components with clickable segments, map all Level 4 deep links to actual `routes.ts` paths, and add `route.query` filter/tab hydration to the target workbench views.
- **Rationale:** Preserves existing navigation structure without router bloat or heavy third-party charting dependencies (`chart.js`/`echarts`).
- **Architecture Impact:** Define the exact Level 4 route & query-parameter contract in the `ARCHITECTURE` artifact and include workbench query-hydration in the implementation slices.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

### GAP-003
- **Category:** Integration | Data
- **Description:** Global context and campus naming are inconsistent across domains: `Operations` and `Students` seed `'campus-main'`, whereas `Teaching` seeds `'Kampus Utama'`. In addition, `AppLayout.vue` lists `'campus-main'` and `'campus-east'` without an `'All Campuses'` option, `'campus-east'` has `0` seeded records, and `useAppContextStore` stores `activeCampusId` and `activeAcademicPeriodId` without `academicYearId` or `schoolLevel`.
- **Impact:** High — Filtering by `'campus-main'` would exclude Teaching KPIs (`0` teachers/demands), switching to `'campus-east'` would show empty dashboards, and period-over-period comparisons (`S2` vs `S1`) require resolving the academic year and prior period.
- **Severity:** High
- **Recommended Resolution:** Standardize campus identifiers to `'campus-main'` and `'campus-east'` across all seeders (seeding a realistic secondary cohort/teacher subset in `'campus-east'`), add an `'All Campuses'` (`null`) option in the campus selector, synchronize `Campus` and `AcademicPeriodId` with `useAppContextStore`, and auto-resolve `AcademicYearId` and the prior comparison `AcademicPeriodId` (`Semester 1`) on the backend.
- **Status:** CLOSED
- **Decision:** Standardize campus codes to `'campus-main'` / `'campus-east'` across seeders (with secondary-campus sample data in `'campus-east'`), add `'All Campuses'` option, sync `Campus` + `AcademicPeriodId` with `appContextStore`, and auto-resolve `AcademicYear` + prior-period (`S1`) comparison on the backend.
- **Rationale:** Ensures consistent cross-domain filtering and makes the global header context selector immediately useful across both Executive and Domain dashboards.
- **Architecture Impact:** `GlobalKpiFilterDto` and backend period-resolution helper must support nullable `Campus` (`All Campuses`), `AcademicPeriodId`, `AcademicYearId`, and `SchoolLevel`.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

### GAP-004
- **Category:** Data | Operational
- **Description:** The current Demo Sample Data Generator (`OperationsDemoDataSeeder`, `StudentsDemoDataSeeder`, `FinanceTransactionsDemoDataSeeder`, `AcademicsActivitiesDemoDataSeeder`, `TeachingDemoDataSeeder`, `TeachingAssignmentsDemoDataSeeder`) leaves several Release 1 KPIs, breakdown buckets, and Attention Signals at `0` (specifically: `31-60`, `61-90`, and `90+ Days` overdue receivables = `0`; unverified payment backlog = `0`; unallocated cash = `0`; 45 of 48 students lack `academic_standings` and `learning_progress`; `late` and `missing` coursework submissions = `0`; student attrition = `0%`; `on_leave` expired = `0`; unresolved capacity conflicts = `0`; unreviewed calendar amendments = `0`).
- **Impact:** High — Without enriching the sample data generator, the Executive and Domain dashboards would display flat or empty charts and missing attention signals.
- **Severity:** High
- **Recommended Resolution:** Enhance all 5 domain seeders deterministically (as specified in Table 4.4) while preserving all existing deterministic persona IDs and staying strictly within `DemoDataIntegrityTest` and `DemoDataResetTest` assertions (`< 5000ms` execution, `0` orphans, bounded record counts).
- **Status:** CLOSED
- **Decision:** Enrich all 5 domain seeders deterministically (preserving existing flagship persona IDs/assertions) so every KPI, S1-vs-S2 delta, aging/standing/workload distribution chart, and Attention Signal has realistic non-zero data.
- **Rationale:** Modifies existing domain seeders in-place rather than introducing a separate synthetic KPI seeder, ensuring that clicking any dashboard signal or chart bar drills down into genuine operational records in the Level 4 workbenches.
- **Architecture Impact:** Include a dedicated Demo Sample Data Enrichment slice in the Architecture and Implementation Plan with explicit integration test verification.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

### GAP-005
- **Category:** Architecture
- **Description:** `Elsasa.Sis.ArchitectureTests` enforces strict compile-time isolation between domain assemblies (`Operations`, `Students`, `Academics`, `Finance`, `Teaching` cannot reference each other), whereas Level 1 (`/api/dashboard/executive`) requires aggregating headline KPIs, domain health summaries, and attention signals from all 5 domains.
- **Impact:** Medium — Direct cross-domain references would fail `NetArchTest` build verification.
- **Severity:** Medium
- **Recommended Resolution:** Define `IDomainKpiSummaryProvider` and shared KPI summary DTOs in `Elsasa.Sis.Contracts/Kpis/`, implement one provider per domain assembly, and compose them inside `Elsasa.Sis.Api/Features/Dashboard/GetExecutiveDashboard/`.
- **Status:** CLOSED
- **Decision:** Use `IDomainKpiSummaryProvider` in `Elsasa.Sis.Contracts` implemented by each domain assembly and composed in `Elsasa.Sis.Api`.
- **Rationale:** Adheres 100% to `docs/architecture/backend-architecture.md` dependency direction rules.
- **Architecture Impact:** Document `IDomainKpiSummaryProvider` in `Elsasa.Sis.Contracts` and `GetExecutiveDashboardHandler` in `Elsasa.Sis.Api`.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

### GAP-006
- **Category:** Security / Compliance
- **Description:** Staff demo accounts in `LoginEndpoint.cs` include both full-access leadership (`admin`/`superadmin` with `Permissions: ["*"]`) and domain-scoped officers (`admission`, `finance`, `coordinator`, `supervisor`). A clear authorization policy is needed for Level 1 (`/dashboard`) and Level 2 (`/dashboard/{domain}`) without proliferating new permissions across existing accounts.
- **Impact:** Medium — Unclear permission guards could either lock staff out of `/dashboard` after login or expose unauthorized Level 2 financial/HR details.
- **Severity:** Medium
- **Recommended Resolution:** Allow all authenticated staff roles to access Level 1 (`/dashboard`) with domain drill-down actions badged/enabled according to their existing domain `View` permissions, and guard each Level 2 endpoint (`GET /api/{domain}/kpis/dashboard`) and route (`/dashboard/{domain}`) using the domain's existing `View` permissions.
- **Status:** CLOSED
- **Decision:** Allow all authenticated staff to view Level 1 (`/dashboard`) with domain cards/signals filtered or badged by their existing domain view permissions, and guard each Level 2 (`/dashboard/{domain}`) endpoint and route with the existing domain `View` permissions (no new RBAC permission proliferation).
- **Rationale:** Minimalist and pragmatic—works seamlessly with all existing demo accounts in `LoginEndpoint.cs` without inventing new `Permissions.Kpi.*` claims.
- **Architecture Impact:** Document the exact existing permission claims guarding each Level 2 endpoint and route in the `ARCHITECTURE` artifact.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

---

## 6. Open Questions and Ambiguities

### OQ-001
- **Question:** How should backend KPI read queries and cross-domain executive aggregation be structured?
- **Why It Matters:** Determines whether new DbUp SQL migration scripts (`v_active_student_roster`, etc.) are required or if direct Dapper `QueryMultipleAsync` queries in domain slices suffice.
- **Affected Area:** Backend Architecture & Database
- **Owner:** Architect
- **Target Resolution Stage:** Before Architecture Finalization
- **Status:** CLOSED
- **Decision:** Direct Dapper `QueryMultipleAsync` in domain vertical slices + `IDomainKpiSummaryProvider` in `Elsasa.Sis.Contracts`, reusing existing `finance.v_student_receivables` without adding new SQL views (zero new DDL migrations).
- **Rationale:** Minimizes database objects and migration maintenance while keeping SQL explicit and co-located with each domain handler.
- **Impact:** Zero new `.sql` migration scripts needed for Release 1.
- **Architecture Impact:** Each domain assembly implements `Features/Kpis/Get{Domain}KpiDashboard/` and `IDomainKpiSummaryProvider`.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

### OQ-002
- **Question:** How should we reconcile the KPI formulas and Attention Signals with the actual database schema?
- **Why It Matters:** Resolves column/status discrepancies between `docs/kpi/*.md` and the actual SQL Server tables.
- **Affected Area:** Domain KPI Calculation Logic
- **Owner:** Analyst / Architect
- **Target Resolution Stage:** Before Architecture Finalization
- **Status:** CLOSED
- **Decision:** Bind all 21 Release 1 KPIs and 12 Attention Signals strictly to actual DbUp schema columns/statuses and reuse the existing `GetAtRiskLearnersHandler` criteria for `ACA-02` / `ACA_AT_RISK_UNADDRESSED`.
- **Rationale:** Ensures 100% consistency between the Academic Interventions workbench (`GetAtRiskLearnersHandler`) and the Academics KPI dashboard (`ACA-02`).
- **Impact:** All KPI counts match the underlying operational workbenches when drilled into.
- **Architecture Impact:** Formalize the Section 4.2 query mapping table in the Architecture specification.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

### OQ-003
- **Question:** How should we modify the Demo Sample Data Generator to produce realistic dashboard conditions?
- **Why It Matters:** Ensures that aging charts, academic standing distributions, attrition metrics, workload bands, and attention signals are populated with realistic non-zero data out of the box.
- **Affected Area:** Demo Sample Data Seeders (`Operations`, `Students`, `Finance`, `Academics`, `Teaching`)
- **Owner:** Architect
- **Target Resolution Stage:** Before Architecture Finalization
- **Status:** CLOSED
- **Decision:** Enrich all 5 domain seeders deterministically (preserving existing flagship persona IDs/assertions) so every KPI, S1-vs-S2 delta, aging/standing/workload distribution chart, and Attention Signal has realistic non-zero data.
- **Rationale:** Real domain records ensure that Level 3/Level 4 drill-downs display the exact records counted by the Level 1/Level 2 KPIs.
- **Impact:** Modifies existing seeders in-place while respecting `DemoDataIntegrityTest` and `DemoDataResetTest` invariants.
- **Architecture Impact:** Specify exact seeder deltas per domain in the Architecture artifact.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

### OQ-004
- **Question:** How should campus identifiers, multi-campus seeding, and global filter synchronization work?
- **Why It Matters:** Aligns `Teaching` (`'Kampus Utama'`) with `Students`/`Operations` (`'campus-main'`), provides data for `'campus-east'`, and links the dashboard filter bar with `useAppContextStore`.
- **Affected Area:** Seeders, Global Context Store (`appContext.store.ts`), and Backend Filter Binding
- **Owner:** Architect
- **Target Resolution Stage:** Before Architecture Finalization
- **Status:** CLOSED
- **Decision:** Standardize campus codes to `'campus-main'` / `'campus-east'` across seeders (with small secondary-campus sample data in `'campus-east'`), add `'All Campuses'` option, sync `Campus` + `AcademicPeriodId` with `appContextStore`, and auto-resolve `AcademicYear` + prior-period (`S1`) comparison on the backend.
- **Rationale:** Gives users a cohesive filter experience across the top header bar and all dashboard views.
- **Impact:** Updates `TeachingDemoDataSeeder` / `TeachingAssignmentsDemoDataSeeder` campus constant from `'Kampus Utama'` to `'campus-main'`, seeds `'campus-east'` records, and adds `'All Campuses'` to `AppLayout.vue`.
- **Architecture Impact:** Document global filter state synchronization and backend period resolution in the Architecture artifact.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

### OQ-005
- **Question:** How should we approach frontend chart rendering and Level 4 operational workbench drill-downs?
- **Why It Matters:** Determines whether third-party charting libraries are added and how Level 4 routes receive drill-down filter context.
- **Affected Area:** Frontend Dashboard Views & Operational Workbenches
- **Owner:** Architect
- **Target Resolution Stage:** Before Architecture Finalization
- **Status:** CLOSED
- **Decision:** Build lightweight zero-dependency SVG/CSS chart components with clickable segments, map all Level 4 deep links to actual `routes.ts` paths, and add `route.query` filter/tab hydration to the target workbench views.
- **Rationale:** Zero bundle overhead, crisp Tailwind styling, native click-to-drill-down on every bar/segment, and reliable Playwright E2E testability.
- **Impact:** Maps all drill-down routes to existing `routes.ts` paths and hydrates initial filter/tab state from `useRoute().query` in target workbenches.
- **Architecture Impact:** Specify shared UI primitives (`KpiMetricCard.vue`, `KpiBreakdownBarChart.vue`, `KpiStackedProgressBar.vue`, `NeedsAttentionPanel.vue`, `KpiFilterBar.vue`) and the Level 4 route hydration table in the Architecture artifact.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

### OQ-006
- **Question:** How should authorization and role-based visibility be enforced for Level 1 and Level 2 dashboards?
- **Why It Matters:** Ensures domain officers (`admission`, `finance`, `coordinator`, `supervisor`) and institutional administrators (`admin`/`superadmin`) have appropriate access without breaking existing login flows.
- **Affected Area:** Backend Endpoint Authorization & Frontend Router Guards
- **Owner:** Architect
- **Target Resolution Stage:** Before Architecture Finalization
- **Status:** CLOSED
- **Decision:** Allow all authenticated staff to view Level 1 (`/dashboard`) with domain cards/signals filtered or badged by their existing domain view permissions, and guard each Level 2 (`/dashboard/{domain}`) endpoint and route with the existing domain `View` permissions (no new RBAC permission proliferation).
- **Rationale:** Reuses existing permission claims in `LoginEndpoint.cs` without adding RBAC complexity.
- **Impact:** `GET /api/dashboard/executive` requires `.RequireAuthorization()` (staff roles) and filters/annotates domain summaries by user permissions; each `GET /api/{domain}/kpis/dashboard` checks the domain's existing view permission/role.
- **Architecture Impact:** Include the authorization matrix in the Architecture artifact.
- **Resolved By:** Stakeholder (`User`)
- **Resolved Date:** 2026-11-11

---

## 7. Risks and Concerns

| Risk ID | Description | Likelihood | Impact | Mitigation / Handling Strategy |
| --- | --- | --- | --- | --- |
| `RISK-001` | **Demo Data Reset Exceeding 5-Second SLA or Breaking Existing Persona Assertions**: Adding standings, progress, overdue obligations, and secondary campus data could increase reset latency or alter counts checked by `DemoDataIntegrityTest` / `DemoDataResetTest`. | Medium | High | Keep total enrolled students at `48` (re-allocating existing non-flagship students across `'campus-main'`/`'campus-east'` and lifecycle statuses rather than inflating student count), use batched inserts for standings/progress, and never alter flagship persona records (`Jonathan`, `Grace`, `Budi`, `Rina`, `Timotius`, `Ruth`, `Daniel`, `Esther`). |
| `RISK-002` | **Parallel SQL Connection Contention in Executive Dashboard**: Calling `IDomainKpiSummaryProvider` concurrently on a shared `SqlConnection` would throw `InvalidOperationException` (MARS disabled). | Low | High | Each `IDomainKpiSummaryProvider` creates and disposes its own `IDbConnection` from `ISqlConnectionFactory`, or `GetExecutiveDashboardHandler` executes providers sequentially/with independent connections (total latency `< 150ms` for ~50 students). |
| `RISK-003` | **Negative Scholarship Adjustments Distorting `FIN-01` Collection Rate**: `FinanceTransactionsDemoDataSeeder` seeds pastoral scholarships as negative `original_amount` rows (`-1,500,000`), while `v_student_receivables` clamps `outstanding_amount` at `0`. | High | Medium | Explicitly compute `FIN-01` using positive obligation rows or `SUM(total_paid) / NULLIF(SUM(total_paid + outstanding_amount), 0) * 100` so collection rate is strictly bounded between `0%` and `100%`. |
| `RISK-004` | **Division-by-Zero on Empty Filter Combinations**: Selecting a `SchoolLevel` or `Campus` with zero matching rows could cause SQL divide-by-zero exceptions. | Medium | Medium | Guard all SQL percentage denominators with `NULLIF(denominator, 0)` and coalesce null ratios to `0.0m` with status `'neutral'`. |

---

## 8. Options and Trade-Offs

### Option 1: Direct Dapper `QueryMultipleAsync` in Domain Slices + In-Place Seeder Enrichment + Lightweight SVG/CSS Charts (Recommended & Selected)
- **Description:** Implement Level 2 KPI endpoints inside each domain assembly using parameterized Dapper `QueryMultipleAsync` over existing tables and `finance.v_student_receivables`; aggregate Level 1 via `IDomainKpiSummaryProvider` in `Elsasa.Sis.Contracts`; enrich the 5 existing domain seeders deterministically; and render charts with zero-dependency Vue/SVG/CSS components linked to actual `routes.ts` workbenches.
- **Benefits:**
  - **Minimalist**: Zero new DbUp DDL migrations, zero new npm chart libraries, zero new RBAC permissions.
  - **Explicit**: Every KPI SQL formula is co-located with its domain slice handler and directly bound to actual table columns.
  - **Pragmatic**: Enriching real domain seeders guarantees 1-to-1 numerical parity between dashboard KPIs and Level 4 operational workbenches.
- **Trade-Offs:** Requires updating existing domain seeders carefully to preserve `DemoDataIntegrityTest` and `DemoDataResetTest` invariants.
- **When Suitable:** Ideal for Elsasa SIS V3's Modular Monolith VSA and current scale.

### Option 2: New Cross-Schema SQL Helper Views + Third-Party Chart Library (`Chart.js`) + Standalone KPI Seeder
- **Description:** Add a new DbUp migration creating 4 new SQL views (`v_active_student_roster`, `v_learner_period_risk_summary`, `v_teacher_period_workload`, `v_calendar_period_readiness`), install `chart.js`/`vue-chartjs`, and add a 7th `KpiDemoDataSeeder`.
- **Benefits:** Pre-joins some columns at the database view layer.
- **Trade-Offs:** Increases migration maintenance, introduces canvas rendering friction in Playwright E2E tests, and risks data drift if a synthetic KPI seeder inserts rows that bypass domain conventions.
- **When Suitable:** Rejected in favor of Option 1.

---

## 9. Recommended Direction

- **Recommended Approach:** Proceed with **Option 1**:
  1. **Shared Contracts (`Elsasa.Sis.Contracts/Kpis/`)**: Define `GlobalKpiFilterDto`, `KpiMetricCardDto`, `KpiBreakdownDto`, `AttentionSignalDto`, `DomainHealthSummaryDto`, `DomainKpiDashboardResponse`, `ExecutiveDashboardResponse`, and `IDomainKpiSummaryProvider`.
  2. **Demo Sample Data Generator Enrichment (`GAP-003`, `GAP-004`)**: Enhance `OperationsDemoDataSeeder`, `StudentsDemoDataSeeder`, `FinanceTransactionsDemoDataSeeder`, `AcademicsActivitiesDemoDataSeeder`, `TeachingDemoDataSeeder`, and `TeachingAssignmentsDemoDataSeeder` deterministically so all 21 KPIs, S1-vs-S2 comparisons, multi-campus filters (`campus-main` & `campus-east`), and 12 Attention Signals have realistic non-zero distributions.
  3. **Backend Domain KPI Vertical Slices & Executive Endpoint**: Implement `GET /api/{domain}/kpis/dashboard` and `IDomainKpiSummaryProvider` in each of the 5 active domains (`Students`, `Academics`, `Finance`, `Teaching`, `Operations`) plus `GET /api/dashboard/executive` in `Elsasa.Sis.Api`.
  4. **Frontend Executive & Domain Dashboards + Level 4 Query Hydration**: Replace static `DashboardView.vue` with the live Level 1 Executive Overview, add the 5 Level 2 Domain Dashboard views + Stage 3 `Development` placeholder, build reusable SVG/CSS KPI visual primitives, and add `route.query` filter hydration to target Level 4 workbenches.
- **Why This Direction Is Best:** Directly honors the user's preference for a **Pragmatic, Explicit, and Minimalist** solution while delivering a rich, realistic dashboard experience backed by genuine domain data.

---

## 10. Required Decisions Before Architecture Finalization

All required decisions have been finalized with the stakeholder:

| Decision ID | Decision Needed | Owner | Impact if Unresolved | Status |
| --- | --- | --- | --- | --- |
| `DEC-001` | Backend KPI Read & Composition Strategy (`OQ-001`) | Stakeholder / Architect | Cannot finalize backend slice structure | **DECIDED** — Direct Dapper `QueryMultipleAsync` + `IDomainKpiSummaryProvider` (zero new SQL views) |
| `DEC-002` | Schema & Formula Reconciliation (`OQ-002`) | Stakeholder / Architect | Runtime SQL column/status mismatches | **DECIDED** — Bind strictly to actual DbUp schema & reuse `GetAtRiskLearnersHandler` logic |
| `DEC-003` | Demo Sample Data Seeder Scope (`OQ-003`) | Stakeholder / Architect | Empty aging charts and zero attention signals | **DECIDED** — Enrich all 5 domain seeders deterministically in-place |
| `DEC-004` | Campus Standardization & Context Sync (`OQ-004`) | Stakeholder / Architect | Broken campus filtering across Teaching vs Students | **DECIDED** — Standardize `'campus-main'`/`'campus-east'`, add `'All Campuses'`, sync with `appContextStore` |
| `DEC-005` | Frontend Charting & Level 4 Deep Links (`OQ-005`) | Stakeholder / Architect | 404 drill-down routes or heavy dependency bloat | **DECIDED** — Zero-dependency SVG/CSS charts + actual `routes.ts` paths with `route.query` hydration |
| `DEC-006` | Role-Based Visibility & Authorization (`OQ-006`) | Stakeholder / Architect | Unauthorized access or broken staff login landing | **DECIDED** — All staff view Level 1 (filtered/badged by permissions); existing domain `View` permissions guard Level 2 |

---

## 11. Impact on Downstream Artifacts

### DOMAIN Impact
- **None**: Existing domain business rules and invariants remain unchanged; KPI slices are strictly read-only projections (plus deterministic sample data enrichment in DemoDataSeeders).

### FEATURE Impact
- Formalizes the realization of `CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS` across Level 1 (`/dashboard`), Level 2 (`/dashboard/{domain}`), Level 3 (`NeedsAttentionPanel`), and Level 4 (`route.query` deep-link hydration).

### ARCHITECTURE Impact
- Create `docs/issues/IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md` defining:
  1. `Elsasa.Sis.Contracts/Kpis/` DTOs and `IDomainKpiSummaryProvider` contract.
  2. Authoritative SQL query specifications per domain (using Table 4.2 bindings).
  3. Exact Demo Data Seeder enrichment specifications (Table 4.4) and test guardrails.
  4. Frontend component hierarchy (`src/frontend/src/features/dashboard/`), navigation updates (`AppLayout.vue` and `routes.ts`), and Level 4 `route.query` hydration map.

### IMPLEMENTATION-PLAN Impact
- Structure the implementation plan into clean, dependency-ordered vertical slices:
  - Slice 1: Shared KPI Contracts (`Elsasa.Sis.Contracts/Kpis/`) & Demo Sample Data Generator Enrichment (`Operations`, `Students`, `Finance`, `Academics`, `Teaching` seeders + integration test verification).
  - Slice 2: `Operations`, `Students`, and `Finance` KPI Vertical Slices (`GET /api/{domain}/kpis/dashboard` + `IDomainKpiSummaryProvider`).
  - Slice 3: `Academics` and `Teaching` KPI Vertical Slices (`GET /api/{domain}/kpis/dashboard` + `IDomainKpiSummaryProvider`) + Executive Dashboard Aggregation (`GET /api/dashboard/executive`).
  - Slice 4: Frontend Shared KPI Primitives, Global Filter Sync (`AppLayout.vue` / `appContext.store.ts`), and Level 1 Executive Overview (`DashboardView.vue`).
  - Slice 5: Frontend Level 2 Domain Dashboards (`Students`, `Academics`, `Finance`, `Teaching`, `Operations`, `Development` placeholder) + Level 4 Workbench `route.query` Hydration & E2E/Component Verification.

---

## 12. Readiness Determination

- **Current Status:** `READY-FOR-PLANNING`
- **Outcome:** `FEASIBLE-WITH-CONDITIONS`
- **Justification:** All technical, schema, sample-data, routing, and authorization gaps have been thoroughly investigated against the live codebase and resolved with explicit stakeholder decisions.
- **Conditions for Moving Forward:**
  1. Carry forward the authoritative SQL column/status bindings in Section 4.2 into `IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md`.
  2. Carry forward the deterministic Demo Data Seeder enrichments in Section 4.4 while preserving all `DemoDataIntegrityTest` and `DemoDataResetTest` assertions.
  3. Map all Level 4 deep links to actual `routes.ts` paths with `route.query` filter hydration.

---

## 13. Traceability

### Upstream References
- **Issue ID:** `CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS` (`docs/issues/IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ISSUE.md`)
- **KPI Reference Suite:** `docs/kpi/KPI-CATALOG.md`, `docs/kpi/KPI-DASHBOARD-IA.md`, `docs/kpi/KPI-ARCHITECTURE.md`, `docs/kpi/KPI-FEASIBILITY.md`, `docs/kpi/KPI-IMPLEMENTATION-ROADMAP.md`
- **Reference Architecture:** `docs/architecture/backend-architecture.md`, `docs/issues/IS011 - DEMO-SAMPLE-DATA-GENERATOR-ARCHITECTURE.md`

### Downstream References
- **Target Architecture Artifact:** `docs/issues/IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md`
- **Target Implementation Plan:** `docs/issues/IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md`

---

## 14. Assumptions

1. Standard cohort section capacity for `STD-07` (Cohort & Section Fill Utilization) defaults to `20` students per cohort section (`ELC-A`, `JHLC-A`, `HSLC-A`, `ELC-B`, `JHLC-B`) since no dedicated room-capacity column exists on `students.administrative_placements`.
2. When `AcademicPeriodId` is set to the active `Semester 2` period, the backend automatically resolves the closed `Semester 1` period of the same `AcademicYearId` as the prior-period comparison baseline for KPI trend deltas (`DeltaPercent` / `DeltaLabel`).
3. Stage 3 `Development` (`Holistic Student Development`) remains an explicit non-clickable/informational `"Stage 3 Pending"` placeholder card on Level 1 and `/dashboard/development` until the `development.*` schema is implemented in a future release.

---

## 15. Approval and Handoff

- **Prepared By:** Analyst / Architect
- **Reviewed By:** Stakeholder (`User`)
- **Approved By:** Architect / Stakeholder (`User`)
- **Approval Date:** 2026-11-11
- **Handoff To:** Architect (`ics-architecture-creation` / `ics-implementation-plan`)
- **Handoff Notes:** All 6 gaps (`GAP-001`..`GAP-006`) and 6 open questions (`OQ-001`..`OQ-006`) are `CLOSED`. Proceed to create `docs/issues/IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md` and `docs/issues/IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md`.
