---
Title: Feasibility Assessment - Stage 1 Calendar & Periods Full Scope
Code: OPS-CALENDAR-PERIODS
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-02
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Feature being assessed: Stage 1 "Calendar & Periods — full scope" under the Operational domain as requested in change request intake `CR-OPS-CALENDAR-PERIODS` (`docs/issues/IS001 - OPS-CALENDAR-PERIODS-ISSUE.md`).

Referenced artifacts:
- DOMAIN: `docs/domains/operational-domain.md`
- FEATURE: `docs/sub-modul-and-development-phases.md` (Stage 1), `docs/operational-workflow-discovery.md` (`OPS-WF-01`, `OPS-WF-10`, `OPS-WF-12`), `docs/use-cases.md` (`UC-OPS-11`, `UC-OPS-12`, `UC-OPS-13`)

## Objective

Assess the technical and architectural feasibility, system impact, domain dependencies, data model requirements, and implementation readiness for delivering the full Stage 1 Calendar & Periods module. This assessment reconciles the existing POC implementation with authoritative domain specifications using a Pragmatic, Minimalist, and Explicit approach.

---

# 2. Current State

## Existing Behavior

1. **Backend Implementation:**
   - `src/backend/Elsasa.Sis.Operations` contains a single initialization proof-of-concept vertical slice: `CreateAcademicYear` (`Features/CalendarPeriods/CreateAcademicYear`).
   - The handler writes directly to `operations.academic_years` with minimal columns (`id`, `code`, `name`, `start_date`, `end_date`, `is_active`, `created_at`, `created_by`).
   - No endpoints or domain logic exist for:
     - Academic Year lifecycle state transitions (`draft` -> `approved` -> `active` -> `completed` -> `archived`).
     - Operational Periods (`operational_periods`) management (semesters, terms, admission/assessment windows).
     - School Calendar container (`school_calendars`) and publication lifecycle.
     - Calendar Entries (`calendar_entries`) for holidays, instructional days, and special dates.
     - Campus Closure Coordination (`school_closures`).
     - Calendar Amendments (`calendar_amendments`) and version tracking.
     - Auditable Operations Record Corrections (`operations_record_corrections`).

2. **Database Migrations:**
   - Migration `20261101_0101_Operations_AcademicYears.sql` creates only `operations.academic_years` with `is_active BIT`.
   - The remaining 5 tables specified in `docs/entities/operational-entities-tables.md` (`operational_periods`, `school_calendars`, `calendar_entries`, `school_closures`, `calendar_amendments`) and the correction audit table (`operations_record_corrections`) do not exist.

3. **Frontend Implementation:**
   - `src/frontend/src/features/operations/calendar/views/CalendarWorkspaceView.vue` is a verification screen with a live `/health` check and a basic `CreateAcademicYear` form.
   - It does not implement the Schedule Management workspace pattern (`SCR-OPS-01`) specified in `docs/screen-inventory.md`.

4. **Cross-Domain Contracts:**
   - `src/backend/Elsasa.Sis.Contracts` does not declare any shared interfaces or query models for Operations (e.g., retrieving active academic year or valid admission window dates).

## Existing Constraints

1. **Modular Monolith Architecture (ADR-001):** Domain assemblies (`Operations`, `Students`, `Finance`) are strictly isolated and must never reference each other directly.
2. **Database Isolation & Logical Foreign Keys (ADR-002):** Schema isolation is enforced in SQL Server; foreign keys across domain schemas are logical only (via UUIDs), not physical FK constraints.
3. **Cross-Domain Communication (ADR-003):** Synchronous inquiries from other domains (Students, Finance) must pass through strongly-typed C# contracts in `Elsasa.Sis.Contracts`.
4. **Data Access & Endpoint Patterns (ADR-004, ADR-005):** Explicit Dapper queries using C# raw string literals, `Result<T>` domain outcomes, and native Minimal APIs.
5. **Stage 1 Functional Boundary:** Campus closures in Stage 1 update calendar dates and non-instructional designations only. Automated timetable disruption analysis and bulk class session rescheduling are deferred to Stage 2 (`SCR-OPS-02`).

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | **Schema & Lifecycle Inconsistency in Academic Years:** The existing `operations.academic_years` table uses `is_active BIT` instead of the authoritative lifecycle state machine (`draft`, `approved`, `active`, `completed`, `archived`) and lacks `approved_by`, `approved_date`, and `notes`. |
| GAP-002 | CRITICAL | **Missing Operational Periods Infrastructure:** No data persistence, domain logic, or API endpoints exist for managing terms, semesters, quarters, or specialized operational windows (admission, assessment). |
| GAP-003 | CRITICAL | **Missing School Calendar & Calendar Entries:** No capability to create and publish school calendars, or to manage granular date entries (instructional days, statutory holidays, staff-only days). |
| GAP-004 | MAJOR | **Missing Closure Coordination & Calendar Sync (`UC-OPS-12`):** No capability to declare campus closures and automatically reflect them on calendar entries and versioned amendments. |
| GAP-005 | MAJOR | **Missing Cross-Domain Query Service:** `Elsasa.Sis.Contracts` lacks `IOperationsCalendarQueryService` needed by Student Admission and Finance Billing to query open periods and active academic years. |
| GAP-006 | MAJOR | **Frontend Incomplete for `SCR-OPS-01`:** `CalendarWorkspaceView.vue` is a POC form rather than the full Schedule Management workspace supporting visual calendar matrix, period tree, and closure workflows. |
| GAP-007 | MINOR | **Missing Auditable Record Corrections (`UC-OPS-13`):** No mechanism to record justified historical corrections to approved Academic Years and Operational Periods. |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| OQ-001 | How should the existing `operations.academic_years` table and lifecycle statuses be reconciled? | Affects existing POC integration tests and database migration strategy. |
| OQ-002 | How should Operational Periods be structured and validated within an Academic Year? | Affects data model nesting, validation logic, and query complexity. |
| OQ-003 | How should calendar days, instructional days, and holidays be represented in `calendar_entries`? | Affects database row volume, calendar grid generation, and date calculation logic. |
| OQ-004 | How should Campus Closures interact with School Calendars and Calendar Amendments in Stage 1? | Affects automation between closures, calendar entries, and amendment auditing. |
| OQ-005 | How should the `SCR-OPS-01` Calendar & Periods Workspace UI be organized? | Affects frontend component hierarchy, navigation, and user workflow ergonomics. |
| OQ-006 | How should authorized corrections (`UC-OPS-13`) to Academic Years, Operational Periods, and Published Calendars be audited? | Affects audit table schema and concurrency/correction API design. |
| OQ-007 | How should initial terms and operational periods be populated during setup? | Affects onboarding UX and operational efficiency for administrative staff. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | Stage 1 operates primarily on a single institutional calendar (`scope_type = 'institution'`, `scope_reference = 'All'`), while maintaining table support for campus-specific scopes. |
| ASM-002 | Standard weekdays (Monday through Friday) falling within active term dates that have no non-instructional calendar entries (holidays, closures, staff days) are dynamically evaluated as instructional days. |
| ASM-003 | Automated timetable rescheduling and instructional occurrence impacts due to campus closures are strictly deferred to Stage 2 (`SCR-OPS-02`); Stage 1 handles calendar date designations only. |
| ASM-004 | Operations Authority is the primary approving and publishing role for academic years, calendars, and closures; Operations Coordinator can draft and view records. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Schema migration to `operations.academic_years` breaks existing POC integration tests. | Medium | Update existing integration tests (`CreateAcademicYearTests.cs`) and request/response DTOs in lockstep with the migration. |
| RISK-002 | Date boundary anomalies (e.g., overlapping terms, child periods exceeding parent or academic year range). | High | Enforce strict hierarchical date boundary validation in FluentValidation validators and domain query logic. |
| RISK-003 | Accidental unversioned alterations to published school calendars. | High | Enforce database and API guard: modifying an approved/published calendar requires creating a versioned `calendar_amendments` record. |

---

# 7. Recommendations

## Evaluated Architectural Options

### Approach 1: Full Lifecycle & Domain Model Alignment (Selected)
- Evolve `operations.academic_years` to full status lifecycle (`draft`, `approved`, `active`, `completed`, `archived`), deprecating `is_active`.
- Create all 5 required tables (`operational_periods`, `school_calendars`, `calendar_entries`, `school_closures`, `calendar_amendments`) plus `operations_record_corrections`.
- Implement hierarchical operational periods with explicit boundary validation.
- Exception-based calendar entry management with dynamic weekday instructional evaluation.
- Tabbed Schedule Management workspace for `SCR-OPS-01`.

### Approach 2: Lightweight Hybrid / Dual-State
- Maintain `is_active BIT` alongside a new `status` column for backwards-compatibility.
- Create flat operational periods without parent hierarchy.
- **Verdict:** Rejected. Introduces redundant state flags, potential desynchronization, and fails to support nested operational windows needed by Admission.

---

# 8. Gap Closure

## GAP-001 / OQ-001: Academic Year Schema & Lifecycle Reconciliation
### Decision
Execute a new DbUp migration (`20261102_0101_Operations_CalendarPeriods_Tables.sql`) that:
1. Alters `operations.academic_years` to add `status NVARCHAR(32) NOT NULL DEFAULT 'draft'`, `approved_by NVARCHAR(100) NULL`, `approved_date DATE NULL`, and `notes NVARCHAR(MAX) NULL`.
2. Migrates existing active records (`is_active = 1`) to `status = 'active'`, then drops `is_active`.
3. Creates remaining 5 domain tables matching `docs/entities/operational-entities-tables.md` plus `operations_record_corrections`.
### Rationale
Guarantees full alignment with authoritative domain artifacts and eliminates ambiguous boolean flags in favor of explicit state transitions.
### Impact
Existing POC vertical slice handlers and tests must be updated to use the new status fields.
### Architecture Impact
Standardizes the domain entity model according to ADR-002 and ADR-006.

---

## GAP-002 / OQ-002: Operational Periods Hierarchy & Validation
### Decision
Implement `operations.operational_periods` supporting self-referencing `parent_period_id` (UUID nullable). Validation rules:
- An operational period's `start_date` and `end_date` must fall within the parent `academic_years` date range.
- If `parent_period_id` is specified, child period dates must fall strictly within the parent period's date range.
- Lifecycle: `draft` -> `approved` -> `active` -> `closed`.
### Rationale
Supports both primary terms (Semester 1, Semester 2) and nested operational windows (e.g., Admission Window 1 inside Semester 1) cleanly.
### Impact
Enables downstream Student Admission and Finance Billing to associate their workflows with specific periods.

---

## GAP-003 / OQ-003: Calendar Entries & Holiday Modeling
### Decision
Model calendar entries as milestone/exception-based records in `operations.calendar_entries` with `entry_date` and optional `end_date` for multi-day spans.
- Weekdays between term start and end dates that lack non-instructional entries are dynamically evaluated as instructional days.
- Designations supported: `instructional_day`, `holiday`, `school_closure`, `staff_only_day`, `exam_day`, `significant_date`, `domain_window`.
### Rationale
Pragmatic and storage-efficient. Avoids generating 365 physical database rows per year while offering full fidelity for school schedules.
### Impact
API provides a calendar matrix projection query that computes the monthly/term calendar status by overlaying entries onto the date grid.

---

## GAP-004 / OQ-004: Campus Closure Coordination & Amendment Automation
### Decision
Implement `school_closures` (`UC-OPS-12`). When a closure is authorized:
1. A closure record is stored in `operations.school_closures`.
2. A corresponding non-instructional entry is automatically created in `operations.calendar_entries` (`designation = 'school_closure'`, `closure_id = closure.id`).
3. If the calendar is currently `published`, an auditable record is logged in `operations.calendar_amendments`, and the calendar's minor version is incremented (e.g., `1.0` -> `1.1`).
### Rationale
Satisfies Stage 1 Closure Coordination and audit requirements without requiring manual duplicate entry by administrators.
### Impact
Guarantees calendar consistency and auditability across emergency and planned closures.

---

## GAP-005: Cross-Domain Consumption Contract
### Decision
Declare `IOperationsCalendarQueryService` in `Elsasa.Sis.Contracts` and implement it in `Elsasa.Sis.Operations`.
- Methods: `GetActiveAcademicYearAsync()`, `GetOperationalPeriodByIdAsync()`, `GetActiveAdmissionWindowsAsync(DateTime date)`.
- Register the implementation in DI for in-process consumption by `Students` and `Finance`.
- Expose corresponding Minimal API endpoints for frontend consumption.
### Rationale
Complies with ADR-001 (assembly isolation) and ADR-003 (strongly-typed synchronous contracts).

---

## GAP-006 / OQ-005: Frontend Workspace Layout (`SCR-OPS-01`)
### Decision
Implement `SCR-OPS-01` in `CalendarWorkspaceView.vue` as a tabbed Schedule Management workspace:
- **Top Header:** Academic Year selector, lifecycle/publication status badge (`Draft`, `Approved`, `Published v1.0`), and action buttons (`Approve`, `Publish`, `Declare Closure`).
- **Tab 1: Visual Calendar Matrix & Entries:** Interactive monthly grid displaying instructional days, statutory holidays, exam windows, and closures, with entry authoring modals.
- **Tab 2: Academic Years & Operational Periods:** Master-detail tree/table managing the Academic Year lifecycle and hierarchical periods.
- **Tab 3: Closures & Amendment History:** List of declared campus closures and versioned audit history of calendar amendments.
### Rationale
Preserves single-screen inventory compliance (`SCR-OPS-01`), avoids fragmented routing, and provides intuitive information hierarchy.

---

## GAP-007 / OQ-006: Auditable Record Corrections (`UC-OPS-13`)
### Decision
Create `operations.operations_record_corrections` table to capture authorized corrections to approved/active Academic Years and Operational Periods (`id`, `academic_year_id`, `target_table_name`, `target_record_id`, `field_name`, `old_value`, `new_value`, `justification_reason`, `authorized_by`, `authorized_at`).
- Calendar entry changes to published calendars continue using `calendar_amendments`.
### Rationale
Delivers complete traceability for historical operational records as mandated by `UC-OPS-13` and `OPS-WF-12`.

---

## OQ-007: Setup Acceleration via Term Presets
### Decision
Provide an optional preset in the UI/API (e.g., Indonesian standard 2-semester template: Semester 1 Jul–Dec, Semester 2 Jan–Jun) when creating an Academic Year, while retaining full capability for custom period creation.
### Rationale
Significantly improves user onboarding ergonomics while maintaining domain flexibility.

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

This change introduces:
1. Significant schema expansion: 5 new domain tables and 1 cross-cutting audit table in SQL Server.
2. Modification to existing persistence structure (`operations.academic_years` lifecycle refactor).
3. New public integration contracts (`IOperationsCalendarQueryService` in `Elsasa.Sis.Contracts`) establishing synchronous boundaries for Students and Finance domains.
4. Formal state machines for Academic Years (`draft` -> `approved` -> `active` -> `completed` -> `archived`), Operational Periods (`draft` -> `approved` -> `active` -> `closed`), and School Calendars (`draft` -> `published` -> `withdrawn`).
5. Cross-cutting versioning and audit mechanisms (`calendar_amendments` and `operations_record_corrections`).

An ARCHITECTURE update is required to document these component boundaries, data models, state machines, and API contracts before implementation planning begins.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated based on approved decisions

## Status

**READY-FOR-PLANNING**

## Notes

All feasibility gaps, open questions, and architectural design choices have been thoroughly explored, interviewed, and resolved in accordance with a Pragmatic, Minimalist, and Explicit approach.

Target architecture [`IS001 - OPS-CALENDAR-PERIODS-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS001%20-%20OPS-CALENDAR-PERIODS-ARCHITECTURE.md) has been established by ica-architect. The READY-FOR-PLANNING gate is granted. Implementation planning may proceed.

---

# 11. References

Referenced artifacts:
- Issue Intake: [`IS001 - OPS-CALENDAR-PERIODS-ISSUE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS001%20-%20OPS-CALENDAR-PERIODS-ISSUE.md)
- Operational Domain Blueprint: [`operational-domain.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/operational-domain.md)
- Operational Entities: [`operational-entities.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/operational-entities.md)
- Operational Entity Tables: [`operational-entities-tables.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/operational-entities-tables.md)
- Operational Workflow Discovery: [`operational-workflow-discovery.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md)
- Use Cases: [`use-cases.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md)
- Screen Inventory: [`screen-inventory.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md)
- Implementation Roadmap: [`sub-modul-and-development-phases.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)
- Architectural Decision Records: [`adr.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md)

Referenced codebase locations:
- `src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/`
- `src/backend/Elsasa.Sis.Database/Scripts/`
- `src/backend/Elsasa.Sis.Contracts/`
- `src/frontend/src/features/operations/calendar/`
- `tests/backend/Elsasa.Sis.IntegrationTests/Operations/`
