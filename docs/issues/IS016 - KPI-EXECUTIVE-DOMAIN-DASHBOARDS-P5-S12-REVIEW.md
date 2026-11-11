# Review Record: P5-S12 Level 4 Operational Workbench `route.query` Filter & Tab Hydration

- **Issue / Plan:** [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md) (`CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS`, v1.1)
- **Architecture:** [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-ARCHITECTURE.md)
- **Slice ID:** P5-S12
- **Title:** Level 4 Operational Workbench `route.query` Filter & Tab Hydration
- **Review Date:** 2026-11-11
- **Review Decision:** GO
- **ReviewIteration:** 1

---

## 1. Scope & Objective Verification

| Requirement / Criterion | Evaluation & Observed Evidence | Status |
|---|---|---|
| Dependency `P4-S09` satisfied | Verified in Implementation Plan: `P4-S09` has `Implementation Status: IMPLEMENTED` and `Review Status: GO`. | SATISFIED |
| Shared `route.query` hydration helper | Created [`useRouteQueryHydration.ts`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/shared/composables/useRouteQueryHydration.ts#L1-L55) providing `getQueryString` and `useRouteQueryHydration` (hydrates immediately via `{ immediate: true, deep: true }` watcher and `onMounted`, guarding against missing router injection). Passes `eslint` with 0 errors/warnings. | SATISFIED |
| 1. `StudentRecordsDeskView.vue` (`/students/records`): `status`, `campus`, `schoolLevel`, `cohort` | Verified [`StudentRecordsDeskView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/students/records/views/StudentRecordsDeskView.vue#L94-L171): hydrates `status`, `campus` (adds `campus-main`, `campus-east`), `schoolLevel` / `level` (adds `ELC`, `JHLC`, `HSLC`), and `cohort` (`active-cohort-chip`). See `NOTE-P5-S12-01` regarding backend `SearchStudentsHandler` lacking a `cohort` parameter. | SATISFIED |
| 2. `StudentIntakeWorkbenchView.vue` (`/students/admission`): `status`, `docStatus` | Verified [`StudentIntakeWorkbenchView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/students/admission/views/StudentIntakeWorkbenchView.vue#L127-L149): hydrates `status` (adds `submitted`, `entrance_assessed`, `waitlisted`, `accepted` pills matching backend drill-downs) and `docStatus` (displays `filter-doc-status-chip` and defaults dossier drawer tab to `'documents'` when `docStatus === 'pending'`). | SATISFIED |
| 3. `StudentLifecycleWorkbenchView.vue` (`/students/lifecycle`): `status` | Verified [`StudentLifecycleWorkbenchView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/students/lifecycle/views/StudentLifecycleWorkbenchView.vue#L28-L53): hydrates `status` (`withdrawn`, `on_leave`, `transferred_out`, etc.), `campus`, and `schoolLevel` / `level`, with `Select` control (`select-status-filter`). | SATISFIED |
| 4. `CohortAcademicReviewView.vue` (`/academics/cohort-review`): `standing` | Verified [`CohortAcademicReviewView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/academics/cohort-review/views/CohortAcademicReviewView.vue#L143-L178): hydrates `standing` (`good_standing`, `conditional_standing`, `not_in_good_standing`), selects `standingProgression` tab, and filters `combinedReviewRows`. | SATISFIED |
| 5. `AcademicInterventionWorkbenchView.vue` (`/academics/interventions`): `filter` (`unaddressed`), `status` | Verified [`AcademicInterventionWorkbenchView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/academics/interventions/views/AcademicInterventionWorkbenchView.vue#L105-L129): `filter=unaddressed` selects `atRisk` tab and filters out learners with `planned`/`in_progress` interventions; `status` hydrates `statusFilter`. | SATISFIED |
| 6. `ClassSessionWorkbenchView.vue` (`/academics/sessions`): `filter` (`low-attendance`) | Verified [`ClassSessionWorkbenchView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/academics/sessions/views/ClassSessionWorkbenchView.vue#L106-L144): hydrates `filter=low-attendance` with clearable `btn-clear-low-attendance-filter` and filters attendance-required sessions. | SATISFIED |
| 7. `CourseworkWorkbenchView.vue` (`/academics/coursework`): `status` (`missing`, `late`, `submitted`) | Verified [`CourseworkWorkbenchView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/academics/coursework/views/CourseworkWorkbenchView.vue#L89-L119) & [`SubmissionsReviewTable.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/academics/coursework/components/SubmissionsReviewTable.vue#L33-L61): hydrates `unitId` and `status` (`missing`, `late`, `submitted`, etc.) into `SubmissionsReviewTable` (`initialStatusFilter`). | SATISFIED |
| 8. `AssessmentWorkbenchView.vue` (`/academics/assessment`): `status` (`provisional`, `approved`, `published`) | Verified [`AssessmentWorkbenchView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/academics/assessment/views/AssessmentWorkbenchView.vue#L81-L115) & [`AssessmentScoringGrid.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/academics/assessment/components/AssessmentScoringGrid.vue#L38-L80): hydrates `unitId` and `status` (`provisional`, `approved`, `published`, etc.) into `AssessmentScoringGrid` (`initialStatusFilter`). | SATISFIED |
| 9. `ObligationsListView.vue` (`/finance/obligations`): `status`, `aging` (`90+ Days`) | Verified [`ObligationsListView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/finance/obligations/views/ObligationsListView.vue#L56-L222): rebuilt full table workbench backed by `useFinanceObligationsQuery`, hydrating `status` (`paid`, `unpaid`, `overdue`, and backend statuses), `aging` (`Current`, `1-30 Days`, `31-60 Days`, `61-90 Days`, `90+ Days`), and `chargeType`. Passes `eslint` with 0 errors/warnings. | SATISFIED |
| 10. `PaymentVerificationQueueView.vue` (`/finance/payments`): `status` (`submitted`, `confirmed`) | Verified [`PaymentVerificationQueueView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/finance/payments/views/PaymentVerificationQueueView.vue#L104-L115): hydrates `status` (`all`, `submitted`, `confirmed`, `rejected`) and resets pagination. | SATISFIED |
| 11. `StudentAccountsWorkbenchView.vue` (`/finance/accounts`): `filter` (`unallocated`, `overdue`) | Verified [`StudentAccountsWorkbenchView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/finance/accounts/views/StudentAccountsWorkbenchView.vue#L117-L173): hydrates `filter` (`unallocated`, `overdue`, `all`) and `studentId`. See `MINOR-P5-S12-01` regarding empty-result fallback when `unallocatedStudentIds.size === 0`. | SATISFIED |
| 12. `TeachingAssignmentWorkspaceView.vue` (`/teaching/assignments`): `status` (`unstaffed`, `staffed`), `capability` | Verified [`TeachingAssignmentWorkspaceView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/assignments/views/TeachingAssignmentWorkspaceView.vue#L84-L140) & [`DemandsIntakeTab.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/assignments/components/DemandsIntakeTab.vue#L50-L120): hydrates `status` (`unstaffed`, `staffed`, etc.), `capability`, `tab`, `periodId`, `campus`, `q`, and `demandId`. | SATISFIED |
| 13. `FacultyWorkloadMonitoringView.vue` (`/teaching/workload`): `status`, `tab` (`conflicts`), `severity` (`blocking`) | Verified [`FacultyWorkloadMonitoringView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/workload/views/FacultyWorkloadMonitoringView.vue#L22-L169): hydrates `status`, `tab` (`conflicts`/`workload`), `severity` (`blocking`/`advisory`/`all`), and `periodId`. Iteration 1 removed all cross-submodule imports from `@/features/teaching/assignments/...` in favor of local `teaching/workload` types, query keys, API client, and `useCapacityConflicts` composable (`FINDING-P5-S12-01` resolved). | SATISFIED |
| 14. `FacultyResourceWorkspaceView.vue` (`/teaching/faculty`): `verificationState` (`submitted`) | Verified [`FacultyResourceWorkspaceView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/faculty/views/FacultyResourceWorkspaceView.vue#L134-L159), [`FacultyQueueSidebar.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/faculty/components/FacultyQueueSidebar.vue#L68-L80), & [`QualificationEvidenceTab.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/faculty/components/QualificationEvidenceTab.vue#L102-L125): hydrates `verificationState=submitted` into `pending_verification` queue filter, `qualifications` tab, and filtered credential list. | SATISFIED |
| 15. `CalendarWorkspaceView.vue` (`/operations/calendar`): `tab` (`amendments`, `closures`), `reviewed` (`false`) | Verified [`CalendarWorkspaceView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/operations/calendar/views/CalendarWorkspaceView.vue#L84-L102) & [`ClosuresAmendmentsTab.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/operations/calendar/components/tabs/ClosuresAmendmentsTab.vue#L98-L120): hydrates `tab` (`amendments`/`closures` -> `'closures'`, `matrix`/`periods`) and `reviewed` (`false`/`true`/`all`). | SATISFIED |
| Independent `pnpm type-check`, `eslint`, and workbench + workload Vitest specs | Iteration 1 re-verification:<br>- `pnpm type-check` (`vue-tsc --noEmit`): exited `0` (0 errors)<br>- `pnpm exec eslint` on `FacultyWorkloadMonitoringView.vue`, `useWorkload.ts`, `workloadApi.ts`, `queryKeys.ts`, `api/types.ts`, and `types/index.ts`: exited `0` (0 errors; 3 pre-existing unused-var warnings in `FacultyWorkloadMonitoringView.vue`)<br>- `pnpm exec vitest run` across 14 workbench specs + 3 `teaching/workload` specs: `17 passed (17)` files, `185 passed (185)` tests. | SATISFIED |

---

## 2. Findings

### `FINDING-P5-S12-01` (MAJOR) — ADR-FE-001 Cross-Submodule Import Violation in `FacultyWorkloadMonitoringView.vue`

- **ID:** `FINDING-P5-S12-01`
- **Severity:** MAJOR
- **Resolution Status:** RESOLVED (Iteration 1)
- **Description:** `FacultyWorkloadMonitoringView.vue` (in submodule `teaching/workload`) imports a composable and type directly from sibling submodule `teaching/assignments`, violating frontend vertical-slice architectural isolation (`ADR-FE-001`) enforced by `eslint.config.js` (`no-restricted-imports`).
- **Evidence:**
  - [`FacultyWorkloadMonitoringView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/workload/views/FacultyWorkloadMonitoringView.vue#L22-L23) lines 22–23 (newly added in `P5-S12` diff):
    ```ts
    import { useCapacityConflicts } from '@/features/teaching/assignments/composables/useAssignments';
    import type { CapacityConflictDto } from '@/features/teaching/assignments/types';
    ```
  - [`eslint.config.js`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/eslint.config.js#L11-L75) configures `teaching: ['profiles', 'workload', 'assignments']` and prohibits cross-submodule imports between sibling submodules.
  - Independent `pnpm exec eslint` output on `FacultyWorkloadMonitoringView.vue`:
    - `22:1  error  '@/features/teaching/assignments/composables/useAssignments' import is restricted from being used by a pattern. Cross-submodule imports are strictly prohibited (ADR-FE-001). Submodule 'teaching/workload' cannot import from sibling submodule  no-restricted-imports`
    - `23:1  error  '@/features/teaching/assignments/types' import is restricted from being used by a pattern. Cross-submodule imports are strictly prohibited (ADR-FE-001). Submodule 'teaching/workload' cannot import from sibling submodule  no-restricted-imports`
- **Required Correction:**
  Remove the cross-submodule imports from `@/features/teaching/assignments/...` in `FacultyWorkloadMonitoringView.vue`. Define the capacity conflict DTO/params (`CapacityConflictDto`, `GetCapacityConflictsParams`) and query function/composable (`getCapacityConflicts` calling `/v1/teaching/conflicts`, `useCapacityConflicts`) inside the `teaching/workload` submodule (`features/teaching/workload/api/` and `features/teaching/workload/composables/useWorkload.ts`), keeping `teaching/workload` decoupled from `teaching/assignments`. Confirm `pnpm exec eslint src/features/teaching/workload/views/FacultyWorkloadMonitoringView.vue` (and any updated files in `features/teaching/workload/`) reports 0 errors.
- **Remediation Evidence (Iteration 1):**
  - Removed sibling submodule imports from [`FacultyWorkloadMonitoringView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/workload/views/FacultyWorkloadMonitoringView.vue#L22-L43); now imports `useCapacityConflicts` from `../composables/useWorkload` and `CapacityConflictDto` from `../types`.
  - Added `CapacityConflictSeverity`, `CapacityConflictStatus`, `CapacityConflictDto`, and `GetCapacityConflictsParams` in [`types/index.ts`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/workload/types/index.ts#L21-L171) (re-exported by [`api/types.ts`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/workload/api/types.ts#L1)), `conflicts` / `conflictList` keys in [`queryKeys.ts`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/workload/api/queryKeys.ts#L33-L37), `getCapacityConflicts` in [`workloadApi.ts`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/workload/api/workloadApi.ts#L161-L191), and `useCapacityConflicts` in [`useWorkload.ts`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/teaching/workload/composables/useWorkload.ts#L118-L134).
  - Independent `pnpm exec eslint` across all 6 `teaching/workload` files exited `0` (0 errors; 3 pre-existing unused-var warnings in `FacultyWorkloadMonitoringView.vue`).

### `MINOR-P5-S12-01` (MINOR) — `StudentAccountsWorkbenchView.vue` Unallocated Cash Filter Fallback

- **ID:** `MINOR-P5-S12-01`
- **Severity:** MINOR
- **Resolution Status:** RESOLVED (Iteration 1)
- **Description:** In [`StudentAccountsWorkbenchView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/finance/accounts/views/StudentAccountsWorkbenchView.vue#L163-L173), when `directoryFilter.value === 'unallocated'` and `unallocatedStudentIds.value.size === 0`, `studentAccountsList` returns all `items` instead of `[]` even after `unallocatedPaymentsResponse` has loaded with zero unallocated payments.
- **Evidence:** Line 169: `if (unallocatedStudentIds.value.size === 0) return items;`
- **Suggested Improvement:** Only skip filtering when `unallocatedPaymentsResponse.value === undefined` (not yet loaded / unmocked in isolated tests); when `unallocatedPaymentsResponse.value` is defined and `unallocatedStudentIds.value.size === 0`, return `[]`.
- **Remediation Evidence (Iteration 1):**
  - Updated [`StudentAccountsWorkbenchView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/finance/accounts/views/StudentAccountsWorkbenchView.vue#L168-L171) line 169 to `if (unallocatedPaymentsResponse.value === undefined) return items;`, returning `[]` when payments have loaded with zero unallocated items.

### `NOTE-P5-S12-01` (NOTE) — Backend `SearchStudentsHandler` Does Not Filter by `cohort`

- **Severity:** NOTE
- **Description:** [`StudentRecordsDeskView.vue`](file:///d:/Project.Private/elsasa-sis-v3/src/frontend/src/features/students/records/views/StudentRecordsDeskView.vue#L156-L171) hydrates `route.query.cohort` and renders `active-cohort-chip`, while `SearchStudentsHandler` (`GET /api/students`) only filters by `Status`, `Campus`, `Level`, and `SearchPattern` ([`SearchStudentsHandler.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/Features/Records/SearchStudents/SearchStudentsHandler.cs#L83-L110)). `campus`, `schoolLevel`, and `status` are actively passed in `searchParams`; `cohort` is visual-chip-only until backend student search supports `cohort`.

### `NOTE-P5-S12-02` (NOTE) — Pre-Existing ESLint Errors in Unmodified Lines of 4 Workbenches

- **Severity:** NOTE
- **Description:** Scoped `eslint` across all 22 P5-S12 files also reported 7 pre-existing errors in lines not modified by `P5-S12`:
  - `ClassSessionWorkbenchView.vue:191` (`no-explicit-any`)
  - `StudentAccountsWorkbenchView.vue:212,213,421` (`no-explicit-any`)
  - `StudentIntakeWorkbenchView.vue:47` (`no-restricted-imports` from `../../enrollment/constants/permissions`)
  - `FacultyResourceWorkspaceView.vue:103,115` (`no-undef` `window`)
  All newly created files (`useRouteQueryHydration.ts`, rebuilt `ObligationsListView.vue`) and the remaining 10 modified workbenches/components had 0 ESLint errors.

---

## 3. Review History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 0 | 2026-11-11 | NO-GO | Verified `pnpm type-check` (0 errors) and all 14 existing workbench Vitest specs (14/14 files, 125/125 tests passed), plus `route.query` hydration across 15 workbenches. Rejected due to `FINDING-P5-S12-01` (MAJOR): `FacultyWorkloadMonitoringView.vue` introduces 2 `no-restricted-imports` ESLint errors violating ADR-FE-001 cross-submodule boundaries by importing from `@/features/teaching/assignments/...`. |
| 1 | 2026-11-11 | GO | Verified `FINDING-P5-S12-01` resolved via local `teaching/workload` types, query keys, API client, and `useCapacityConflicts` composable (0 ADR-FE-001 violations), and `MINOR-P5-S12-01` resolved in `StudentAccountsWorkbenchView.vue`. Independently verified `pnpm type-check` (0 errors), `pnpm exec eslint` on `FacultyWorkloadMonitoringView.vue` + `teaching/workload/` files (exit 0, 0 errors), and 17 Vitest spec files (14 workbench + 3 workload: 17/17 files, 185/185 tests passed). |

---

## 4. Final Review Decision

**GO**
