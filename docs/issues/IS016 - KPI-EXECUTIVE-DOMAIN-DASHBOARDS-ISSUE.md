# ISSUE

## Metadata

ID: CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Release 1 — Essential Executive & Domain Dashboards (Immediate Delivery)

## Source

Reported By: Drury
Reported Date: 2026-11-11

## Description

Implement Release 1 of the Management KPI & Dashboard system as formally specified in the SIS Management KPI Roadmap (`docs/kpi/KPI-IMPLEMENTATION-ROADMAP.md` §2).

The requested scope replaces the current static dashboard placeholder with a live **Level 1 Executive Overview (Management Attention Center)** and launches the first release of the **Level 2 Domain Dashboards** across five operational domains, supported by 18 essential `READY` KPIs and 12 management attention signals without introducing breaking changes to existing database schemas:

1. **Level 1 Executive Overview (Management Attention Center):**
   - Provide school leadership with a single unified screen answering overall school performance, domain health, urgent operational exceptions, and period-over-period movement.
   - Display a prioritized Cross-Domain Critical Management Attention Strip highlighting urgent items across domains (e.g., severe receivables delinquency, unaddressed at-risk learners, unstaffed teaching demands, unverified payment receipts).
   - Display six domain attention cards (`Students`, `Academics`, `Finance`, `Teaching`, `Operations`, and a clean Stage 3 readiness placeholder for `Development`), each presenting key headline metrics, health status chips, and primary status indicators.
   - Provide one-line scannable domain summaries with direct navigation into Level 2 domain dashboards.

2. **Level 2 Domain Dashboards:**
   - **Student Management Dashboard (`/dashboard/students`):** Surface active student population (`STD-01`), new student enrollment realization (`STD-02`), admission funnel conversion & yield (`STD-03`), student withdrawal & attrition rate (`STD-06`), and population distribution across grade/program/campus (`STD-07`).
   - **Academic Performance Dashboard (`/dashboard/academics`):** Surface academic standing distribution & unsatisfactory rate (`ACA-01`), at-risk learners requiring intervention (`ACA-02`), learning activity attendance rate (`ACA-03`), assignment submission & missing work rate (`ACA-04`), and assessment grading & result publication rate (`ACA-05`).
   - **Financial Health Dashboard (`/dashboard/finance`):** Surface total outstanding receivables (`FIN-01`), net fee collection rate (`FIN-02`), overdue receivables & delinquency rate (`FIN-03`), receivables aging distribution (`FIN-04`), and unverified payment confirmation backlog (`FIN-05`).
   - **Teaching Performance Dashboard (`/dashboard/teaching`):** Surface teaching demand staffing coverage rate (`TCH-01`), unassigned open teaching demands (`TCH-02`), faculty workload distribution & limit breach rate (`TCH-03`), and unresolved capacity conflicts (`TCH-04`).
   - **School Operations Dashboard (`/dashboard/operations`):** Surface operational calendar & period publication readiness (`OPS-01`), and unscheduled school closure days & unreviewed amendments (`OPS-02`).
   - **Student Development Dashboard (`/dashboard/development`):** Present a graceful `Stage 3 — Not Yet Available` state in accordance with phased release planning.

3. **Level 4 Operational Deep-Link Integration:**
   - Provide pre-hydrated navigation from attention signals and KPI breakdown rows to existing operational domain workbenches (`SCR-STD-*`, `SCR-ACA-*`, `SCR-FIN-*`, `SCR-TCH-*`, `SCR-OPS-*`) to enable immediate investigation and action.

## Desired Outcome

1. School leadership, campus principals, academic directors, and finance officers have real-time visibility into executive and domain-level health from a centralized dashboard.
2. Cross-domain operational bottlenecks and compliance risks are surfaced proactively via the Management Attention Center rather than discovered reactively.
3. Users can filter KPI data by global academic context (`academicYearId`, `academicPeriodId`, `campusId`).
4. Users can seamlessly drill down from high-level indicators and attention signals into relevant operational workbenches with search/filter contexts pre-hydrated.
5. Immediate decision-support value is delivered using existing transactional data and completed Stage 1–2 domain modules without blocking on unimplemented Stage 2/3 transactional capabilities.

## Current Situation

1. The frontend dashboard route (`/dashboard`) renders a static launcher in [`DashboardView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/dashboard/views/DashboardView.vue) without live operational metrics, attention signals, or domain dashboard integration.
2. School leadership has no consolidated overview to track key student enrollment, academic at-risk, fee collection, teaching coverage, or operational calendar readiness metrics.
3. Although underlying transactional tables and views exist for students, admissions, finance, learning activities, assessments, academic standings, and teaching assignments/workload, there are no consolidated read endpoints or KPI presentation views delivering executive decision support.

## Evidence

- Roadmap specification: [`docs/kpi/KPI-IMPLEMENTATION-ROADMAP.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/kpi/KPI-IMPLEMENTATION-ROADMAP.md) (§2 Release 1 — Essential Executive & Domain Dashboards)
- KPI definitions and formulas: [`docs/kpi/KPI-CATALOG.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/kpi/KPI-CATALOG.md)
- Information architecture & visual design specifications: [`docs/kpi/KPI-DASHBOARD-IA.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/kpi/KPI-DASHBOARD-IA.md)
- Technical architecture & read-side composition patterns: [`docs/kpi/KPI-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/kpi/KPI-ARCHITECTURE.md)
- Technical feasibility and schema readiness assessment: [`docs/kpi/KPI-FEASIBILITY.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/kpi/KPI-FEASIBILITY.md)
- Existing static placeholder view: [`src/frontend/src/features/dashboard/views/DashboardView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/dashboard/views/DashboardView.vue)

## Notes

- This issue captures intake exclusively for **Release 1 (Immediate Delivery)**. Subsequent phases—Release 2 (historical snapshot worker, Level 3 KPI detail drawer, management policy thresholds) and Release 3 (Stage 2/3 transactional module activations)—are documented in the roadmap and will be executed under separate issues when their prerequisites are satisfied.
