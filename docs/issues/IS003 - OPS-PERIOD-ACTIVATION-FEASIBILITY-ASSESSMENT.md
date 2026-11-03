---
Title: Feasibility Assessment - Operational Period Activation and Academic Year Cascading
Code: OPS-PERIOD-ACTIVATION
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-03
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Feature being assessed: Operational Period Activation and Academic Year Cascading in the Operations Calendar domain, requested under Change Request `CR-OPS-PERIOD-ACTIVATION` (`docs/issues/IS003 - OPS-PERIOD-ACTIVATION-ISSUE.md`).

Referenced artifacts:
- ISSUE: `docs/issues/IS003 - OPS-PERIOD-ACTIVATION-ISSUE.md`
- DOMAIN: `docs/domains/operational-domain.md`, `docs/domains/student-domain.md`
- FEATURE / WORKFLOW: `docs/operational-workflow-discovery.md` (`OPS-WF-01`), `docs/use-cases.md` (`UC-OPS-04`, `UC-OPS-07`, `UC-OPS-08`), `docs/screen-inventory.md` (`SCR-OPS-01`)
- ARCHITECTURE: `docs/issues/IS001 - OPS-CALENDAR-PERIODS-ARCHITECTURE.md`, `docs/architecture/backend-architecture.md`

## Objective

Assess the technical and domain feasibility, system impact, state machine consistency, cross-domain dependencies, and implementation readiness for enabling operational period activation. This assessment establishes an explicit, pragmatic, and minimalist resolution ensuring operational periods seamlessly transition to active status both via parent academic year cascading and explicit user interface actions in `SCR-OPS-01`, unblocking downstream consumers (notably prospective student registration in the Admission Application Portal).

---

# 2. Current State

## Existing Behavior

1. **Operational Period Creation & Initial Status:**
   - In `CreateOperationalPeriodHandler` (`src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/OperationalPeriods/CreateOperationalPeriod/CreateOperationalPeriodHandler.cs`, lines 175, 192), operational periods are persisted with `status = 'draft'`.
   - In `ApplyAcademicYearPresetHandler` (`src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/AcademicYears/ApplyAcademicYearPreset/ApplyAcademicYearPresetHandler.cs`, lines 152, 166), Indonesian 2-semester preset records (Semester 1 and Semester 2) are generated with `status = 'draft'`.

2. **Academic Year Lifecycle & Activation Scope:**
   - In `ActivateAcademicYearHandler` (`src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/AcademicYears/ActivateAcademicYear/ActivateAcademicYearHandler.cs`, lines 74-90), activating an approved Academic Year executes within a Dapper transaction:
     - Demotes any currently active academic year to `'completed'`.
     - Updates the target academic year status to `'active'`.
   - Activating an Academic Year does **not** update, promote, or synchronize child records in `operations.operational_periods`. As a result, all operational periods under the activated year remain in `'draft'` status.

3. **Schedule Management Workspace (`SCR-OPS-01`) Periods Tree Table:**
   - In `PeriodsTreeTab.vue` (`src/frontend/src/features/operations/calendar/components/tabs/PeriodsTreeTab.vue`, lines 539-575), row-level actions for operational periods are limited to:
     - "Correct Record" (`handleCorrectPeriod` / `UC-OPS-13`) for authorized data correction.
     - "Close Period" (`handleClosePeriod` calling `POST /api/operations/periods/{id}/close`).
   - There is no endpoint, composable mutation, or UI action to approve or activate an individual operational period.

4. **Downstream Cross-Domain Validation & Impact:**
   - In `RegisterApplicationHandler` (`src/backend/Elsasa.Sis.Students/Features/Admission/RegisterApplication/RegisterApplicationHandler.cs`, lines 188-196), application registration verifies the target operational period via `IOperationsCalendarQueryService.GetOperationalPeriodByIdAsync`:
     ```csharp
     var isPeriodUsable = string.Equals(period.Status, "active", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(period.Status, "approved", StringComparison.OrdinalIgnoreCase);
     if (!isPeriodUsable)
     {
         return DomainError.Conflict(
             "Admission.PeriodNotActive",
             $"Admission period '{period.Name}' is not active or approved (current status: '{period.Status}').");
     }
     ```
   - In `OperationsCalendarQueryService.cs` (`src/backend/Elsasa.Sis.Operations/Services/OperationsCalendarQueryService.cs`, lines 54-58), `GetActiveAdmissionWindowsAsync` queries `operations.operational_periods WHERE period_type = 'admission_window' AND status = 'active'`.
   - Because newly created or preset operational periods remain stuck in `'draft'`, portal registrations fail with a 409 Conflict.
   - Integration tests in `ApplicationRegistrationIntegrationTests.cs` (line 51) bypass domain endpoints with a direct SQL statement: `UPDATE operations.operational_periods SET status = 'active' WHERE id = @Id;`.

## Existing Constraints

1. **Modular Monolith Schema Isolation (ADR-001, ADR-002):** Schema boundaries (`operations`, `students`) are isolated. Cross-domain queries occur via strongly-typed contracts (`IOperationsCalendarQueryService`).
2. **Endpoint & Domain Result Conventions (ADR-004, ADR-005):** CQRS vertical slices using FastEndpoints/Minimal APIs, Dapper parameterized SQL, and `Result<T>` domain outcomes.
3. **Database Schema Stability:** `operations.operational_periods` already contains `status NVARCHAR(32)`, `updated_at DATETIMEOFFSET`, and `updated_by NVARCHAR(100)` columns. No DDL alterations or database migrations are required to support `'active'` status.
4. **Single Active Academic Year:** At any time, exactly one Academic Year has `status = 'active'`, while multiple operational periods (e.g. Semester 1, concurrent admission window, assessment window) may concurrently be `'active'`.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | **Missing Operational Period Activation Endpoint & Slice:** There is no backend endpoint or vertical slice (`POST /api/operations/periods/{id}/activate`) allowing authorized users to transition an operational period from `draft` (or `approved`) to `active`. |
| GAP-002 | CRITICAL | **Academic Year Activation Does Not Cascade to Child Periods:** Activating an Academic Year via `ActivateAcademicYearHandler` only touches `operations.academic_years`; it leaves child operational periods in `draft` status, breaking downstream operational workflows. |
| GAP-003 | MAJOR | **SCR-OPS-01 Tree Table Missing Activation Action:** `PeriodsTreeTab.vue` lacks a row action to activate operational periods, leaving users with no interface capability to make periods operational. |
| GAP-004 | MAJOR | **Orphaned Active Periods on Academic Year Demotion:** When an Academic Year is demoted from `active` to `completed`, its active child operational periods are not demoted to `closed`, potentially causing ambiguous active period queries. |
| GAP-005 | MINOR | **Test Workaround Bypassing Domain Invariants:** Integration tests (`ApplicationRegistrationIntegrationTests.cs`, line 51) execute raw SQL updates (`UPDATE operations.operational_periods SET status = 'active'`) instead of relying on domain business operations. |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| OQ-001 | How should the lifecycle transition and UI action for individual Operational Periods be structured? | Affects backend endpoint design, UI button behavior, and governance complexity. |
| OQ-002 | What exact cascading scope should apply to child operational periods when an Academic Year is activated? | Affects atomic transaction logic in `ActivateAcademicYearHandler`. |
| OQ-003 | When a new Operational Period is created (or presets applied) under an ALREADY ACTIVE Academic Year, what initial status should it receive? | Affects `CreateOperationalPeriodHandler` and `ApplyAcademicYearPresetHandler`. |
| OQ-004 | What permission should be required to activate an Operational Period via API and UI? | Affects endpoint authorization and UI action visibility policies. |
| OQ-005 | What parent Academic Year status constraints should be enforced when activating an individual Operational Period? | Affects validation rules and early admission window readiness. |
| OQ-006 | How should hierarchical (parent-child) operational periods behave during activation? | Affects recursive vs target-only state transition logic. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | The existing database schema for `operations.operational_periods` already provisions `status NVARCHAR(32)`, `updated_at`, and `updated_by` with a default of `'draft'`. No database schema migration or table alteration is required. |
| ASM-002 | Downstream consumers (e.g. `RegisterApplicationHandler`, `GetActiveAdmissionWindowsAsync`) already evaluate date ranges (`start_date <= @Date AND end_date >= @Date`) in addition to checking `status = 'active'`. Activating periods makes them immediately operational when within valid date spans. |
| ASM-003 | Operational period closure remains terminal: once a period transitions to `closed`, it cannot be re-activated directly. Any erroneous closure must be resolved through authorized record corrections (`UC-OPS-13`). |
| ASM-004 | Multiple operational periods of different types (e.g., a semester and an admission window) may legitimately be `active` at the same time within the same academic year. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Premature activation of unreviewed child periods when applying presets to an active year. | Operational windows become active before dates are customized. | Preserve `draft` as initial creation status even under active years. Activation requires an explicit click or academic year activation cascade. |
| RISK-002 | Cascading activation during Academic Year activation could reactivate previously closed periods. | Closed periods mistakenly reopened. | Restrict cascading updates strictly to periods with `status IN ('draft', 'approved')`. Never touch `status = 'closed'` periods. |
| RISK-003 | Broken integration tests or regression in existing test suites due to behavioral changes in `ActivateAcademicYearHandler`. | Test suite failures. | Run end-to-end integration tests; update test expectations to assert period auto-activation; eliminate raw SQL workarounds. |

---

# 7. Recommendations

## Option A: Direct Activation & Full Lifecycle Cascade (Recommended)

1. Introduce vertical slice `ActivateOperationalPeriod` (`POST /api/operations/periods/{id}/activate`) requiring `Permissions.Operations.Calendar.Manage`. Transitions target period from `draft` (or `approved`) to `active`.
2. Enhance `ActivateAcademicYearHandler` to execute full lifecycle cascading within its existing Dapper transaction:
   - Promote child operational periods of the activated year with `status IN ('draft', 'approved')` to `active`.
   - Demote child operational periods of the demoted active year with `status = 'active'` to `closed`.
3. In `SCR-OPS-01` (`PeriodsTreeTab.vue`), add an "Activate Period" button in the Actions column for rows with `status IN ('draft', 'approved')` wired to TanStack mutation `useActivateOperationalPeriodMutation`.
4. Keep period creation initial status as `draft` for explicit administrator control.

### Advantages
- Minimalist, pragmatic, and explicit: aligns directly with user decisions.
- Completely avoids database schema migrations or additional audit columns.
- Prevents orphaned `draft` periods in active years and lingering `active` periods in completed years.
- Cleanly solves downstream Admission Portal 409 Conflict errors without manual interventions.

### Disadvantages
- None identified; directly addresses the reported problem within existing architecture.

---

## Option B: Multi-Step Governance (Approve + Activate) with Recursive Cascade

Introduce separate `/approve` and `/activate` endpoints for operational periods, add `approved_by` / `approved_date` columns via a database migration, and recursively cascade period activation down all tree levels.

### Advantages
- Enforces strict two-man rule governance at the period level.

### Disadvantages
- Adds unnecessary ceremony and administrative friction for routine operational windows.
- Requires database migration and schema alters.
- Over-complicates sub-period scheduling where future assessment windows should not be auto-activated alongside semesters.

---

# 8. Gap Closure

## GAP-001: Missing Operational Period Activation Endpoint & Backend Lifecycle Handler

### Decision
Implement vertical slice `ActivateOperationalPeriod` under `src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/OperationalPeriods/ActivateOperationalPeriod/`:
- Endpoint: `POST /api/operations/periods/{id:guid}/activate`
- Permission: `Permissions.Operations.Calendar.Manage`
- Status Transition: `draft` | `approved` -> `active`
- Response: `ActivateOperationalPeriodResponse` (Id, PeriodCode, Name, Status = "active", UpdatedAt)

### Rationale
Provides a first-class, standard REST Minimal API endpoint adhering to ADR-004 and ADR-005.

### Impact
Adds 4 new files: `ActivateOperationalPeriodEndpoint.cs`, `ActivateOperationalPeriodHandler.cs`, `ActivateOperationalPeriodResponse.cs`, and endpoint registration.

### Architecture Impact
None. Fits existing CQRS vertical slice pattern.

### Resolved By
User & Architect alignment during Feasibility Assessment.

### Resolved Date
2026-11-03

---

## GAP-002: Academic Year Activation Does Not Cascade to Child Periods

### Decision
Update `ActivateAcademicYearHandler.cs` to atomically promote all child operational periods belonging to the activating Academic Year having `status IN ('draft', 'approved')` to `status = 'active'` within the existing transaction:
```sql
UPDATE operations.operational_periods
SET status = 'active',
    updated_at = @UpdatedAt,
    updated_by = @UpdatedBy
WHERE academic_year_id = @Id
  AND status IN ('draft', 'approved');
```

### Rationale
Ensures that when an Academic Year is officially activated, all configured operational coordination periods (semesters, terms, admission windows) immediately become operational without requiring repetitive manual activation.

### Impact
Modifies `ActivateAcademicYearHandler.cs` within the existing database transaction.

### Architecture Impact
None. Transactional scope remains entirely within the `operations` schema.

### Resolved By
User & Architect alignment during Feasibility Assessment.

### Resolved Date
2026-11-03

---

## GAP-003: SCR-OPS-01 Tree Table Missing Activation Action

### Decision
Add an "Activate Period" button in `PeriodsTreeTab.vue` Actions column:
- Visible when: `canManage && (node.data.status === 'draft' || node.data.status === 'approved')`
- Visual styling: Emerald icon button (`CheckCircle2` / `Play`), tooltip "Activate operational period"
- Wiring: Calls `activatePeriodMutation.mutate(node.data.id)`
- Composable & API: Add `activatePeriod` in `operationalPeriod.api.ts` and `useActivateOperationalPeriodMutation` in `useOperationalPeriodsQuery.ts`, invalidating `operationalPeriodKeys.all` and `calendarKeys.matrixAll`.

### Rationale
Gives administrators and operations coordinators direct visual agency to activate periods on demand.

### Impact
Updates `operationalPeriod.api.ts`, `useOperationalPeriodsQuery.ts`, and `PeriodsTreeTab.vue`.

### Architecture Impact
None. Follows existing TanStack Query invalidation and component conventions.

### Resolved By
User & Architect alignment during Feasibility Assessment.

### Resolved Date
2026-11-03

---

## GAP-004: Orphaned Active Periods on Academic Year Demotion

### Decision
In `ActivateAcademicYearHandler.cs`, atomically demote any child operational periods with `status = 'active'` belonging to the previously active academic year to `status = 'closed'` within the same transaction:
```sql
UPDATE operations.operational_periods
SET status = 'closed',
    updated_at = @UpdatedAt,
    updated_by = @UpdatedBy
WHERE status = 'active'
  AND academic_year_id IN (
      SELECT id FROM operations.academic_years WHERE status = 'active'
  );
```
*(Executed immediately before demoting the active academic year to 'completed').*

### Rationale
Maintains clean temporal boundary discipline, ensuring completed academic years do not leave active operational periods lingering in database queries.

### Impact
Adds demotion SQL step in `ActivateAcademicYearHandler.cs`.

### Architecture Impact
None. Internal state consistency improvement.

### Resolved By
User & Architect alignment during Feasibility Assessment.

### Resolved Date
2026-11-03

---

## GAP-005: Downstream Consumer Test Workaround Bypassing Operational Domain Boundaries

### Decision
Update `ApplicationRegistrationIntegrationTests.cs` to eliminate the raw SQL update (`UPDATE operations.operational_periods SET status = 'active'`). Rely on `POST /api/operations/periods/{id}/activate` (or academic year activation cascading) to verify the integration workflow through sanctioned domain endpoints.

### Rationale
Removes fragile test workarounds and guarantees tests validate production behavior end-to-end.

### Impact
Simplifies and cleans up `ApplicationRegistrationIntegrationTests.cs`.

### Architecture Impact
None.

### Resolved By
User & Architect alignment during Feasibility Assessment.

### Resolved Date
2026-11-03

---

## OQ-001: Lifecycle transition and endpoint design for Operational Periods

### Decision
Direct Activation (`draft` / `approved` -> `active`): Implement a single `POST /api/operations/periods/{id}/activate` endpoint and a direct "Activate" row action in `SCR-OPS-01`.

### Rationale
Operational periods belong to an already approved/active Academic Year. Adding a distinct approval ceremony for every term, semester, or admission window would introduce unnecessary administrative overhead.

### Impact
Minimalist API design; no extra approval state machine overhead.

### Architecture Impact
None.

### Resolved By
User Decision (Question 1)

### Resolved Date
2026-11-03

---

## OQ-002: Academic Year Activation Cascading Scope

### Decision
Full Lifecycle Cascading: When an Academic Year is activated, atomically promote all its child periods in `draft` or `approved` status to `active`, and demote all active child periods of the outgoing active Academic Year to `closed`.

### Rationale
Guarantees operational alignment across the school year boundary without leaving orphaned drafts or lingering active periods.

### Impact
Atomic execution inside `ActivateAcademicYearHandler`.

### Architecture Impact
None.

### Resolved By
User Decision (Question 2)

### Resolved Date
2026-11-03

---

## OQ-003: Initial status of newly created operational periods under active academic year

### Decision
Always create as `draft`: Operational periods created manually or via preset under an already active Academic Year always start with `status = 'draft'`.

### Rationale
Preserves administrator control to review, verify date ranges, and add notes before explicitly activating the period.

### Impact
No change to `CreateOperationalPeriodHandler` or `ApplyAcademicYearPresetHandler` initial status logic.

### Architecture Impact
None.

### Resolved By
User Decision (Question 3)

### Resolved Date
2026-11-03

---

## OQ-004: Authorization and permission required for operational period activation

### Decision
Require `Permissions.Operations.Calendar.Manage`.

### Rationale
Matches the existing permissions for creating, modifying, and closing operational periods, avoiding operational bottlenecks while preserving `Permissions.Operations.Calendar.Approve` for high-level Academic Year governance.

### Impact
Configured on `ActivateOperationalPeriodEndpoint` and evaluated via `canManage` in `PeriodsTreeTab.vue`.

### Architecture Impact
None.

### Resolved By
User Decision (Question 4)

### Resolved Date
2026-11-03

---

## OQ-005: Parent Academic Year status validation constraint for period activation

### Decision
Require parent Academic Year to be in `approved` or `active` status.

### Rationale
Allows early admission windows or setup periods for upcoming approved academic years to become active before the school year formally begins, while preventing period activation within unapproved (`draft`) or completed/archived academic years.

### Impact
Validation check in `ActivateOperationalPeriodHandler`.

### Architecture Impact
None.

### Resolved By
User Decision (Question 5)

### Resolved Date
2026-11-03

---

## OQ-006: Hierarchical parent-child sub-period activation behavior

### Decision
Target-only activation with parent check: Activating a period activates that specific period only (no downward cascade). Activating a child period requires its parent period (if one exists) to be in `active` or `approved` status.

### Rationale
Sub-periods (such as assessment or reporting windows) have specialized, independent timeframes and should not be force-activated when a semester opens. Requiring the parent period to be active/approved prevents orphaned sub-periods.

### Impact
Validation check in `ActivateOperationalPeriodHandler` against `parent_period_id`.

### Architecture Impact
None.

### Resolved By
User Decision (Question 6)

### Resolved Date
2026-11-03

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-NOT-REQUIRED**

## Rationale

1. **No Schema Changes:** The existing database schema (`operations.operational_periods`, `operations.academic_years`) already supports the `status = 'active'` and `status = 'closed'` states, along with audit tracking (`updated_at`, `updated_by`). No DDL migrations or alterations are needed.
2. **Preserved Boundaries:** The changes reside strictly within the existing `Operations` bounded context (`Elsasa.Sis.Operations`) and its client workspace (`SCR-OPS-01`). No cross-domain contracts or inter-domain interfaces are modified or introduced.
3. **Established Patterns:** The solution introduces a standard CQRS vertical slice (`ActivateOperationalPeriod`) following ADR-004 / ADR-005, and enhances `ActivateAcademicYearHandler` within its existing Dapper transaction.
4. **Complete Specification:** The decisions approved in Section 8 provide complete, unambiguous technical and domain specifications sufficient to proceed directly to Implementation Planning.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture Applicability determined (ARCHITECTURE-NOT-REQUIRED)
- [x] Implementation Plan can be created directly from approved feasibility decisions

## Status

**READY-FOR-PLANNING**

## Notes

All architectural, domain, and interface decisions have been explicitly resolved and recorded with the user. The next stage in the SDLC workflow is creating the executable implementation plan (`IS003 - OPS-PERIOD-ACTIVATION-IMPLEMENTATION-PLAN.md`).

---

# 11. References

Referenced artifacts:
- ISSUE: `docs/issues/IS003 - OPS-PERIOD-ACTIVATION-ISSUE.md`
- DOMAIN: `docs/domains/operational-domain.md`
- USE CASES: `docs/use-cases.md` (`UC-OPS-04`, `UC-OPS-07`, `UC-OPS-08`, `UC-OPS-13`)
- SCREEN INVENTORY: `docs/screen-inventory.md` (`SCR-OPS-01`)
- ARCHITECTURE: `docs/issues/IS001 - OPS-CALENDAR-PERIODS-ARCHITECTURE.md`

Referenced codebase locations:
- `src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/AcademicYears/ActivateAcademicYear/ActivateAcademicYearHandler.cs`
- `src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/OperationalPeriods/CreateOperationalPeriod/CreateOperationalPeriodHandler.cs`
- `src/backend/Elsasa.Sis.Operations/Features/CalendarPeriods/OperationalPeriods/CloseOperationalPeriod/CloseOperationalPeriodHandler.cs`
- `src/backend/Elsasa.Sis.Students/Features/Admission/RegisterApplication/RegisterApplicationHandler.cs`
- `src/frontend/src/features/operations/calendar/components/tabs/PeriodsTreeTab.vue`
- `src/frontend/src/features/operations/calendar/api/operationalPeriod.api.ts`
- `src/frontend/src/features/operations/calendar/composables/useOperationalPeriodsQuery.ts`
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Students/Admission/ApplicationRegistrationIntegrationTests.cs`
