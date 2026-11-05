# Review Record: P8-S21 Coursework & Assessment Workbench View, Assignment Modals & Submissions Table

- **Slice ID:** P8-S21
- **Title:** `SCR-ACD-02` Coursework & Assessment Workbench View, Assignment Modals & Submissions Table (`/academics/coursework`)
- **Review Date:** 2026-11-05
- **Review Decision:** GO
- **Review Iteration:** 1

---

## 1. Scope & Objective Verification

| Requirement / Objective | Evaluation | Status |
|---|---|---|
| `CourseworkWorkbenchView.vue` (`src/frontend/src/features/academics/coursework/views/`) | Left pane features learning unit search and reactive list with active structure resolution, along with status filter pills (`all`, `draft`, `released`, `closed`) and counts. Middle pane renders assignment cards displaying assignment title, parent learning activity name/date, release date, due date, and submission progress bar (`X / Y submitted`). Header actions provide context-aware "Release" (draft state), "Close" (released state), and "Create Assignment" with role-based permission checks (`COURSEWORK_PERMISSIONS`). Responsive 12-column grid layout (3/4/5 cols) with breadcrumbs, PageHeader, EmptyState, and LoadingSpinner. | SATISFIED |
| `CreateAssignmentDrawer.vue` (`src/frontend/src/features/academics/coursework/components/`) | Right-side slide-over PrimeVue Drawer with parent learning activity selection dropdown (querying `useUnitActivitiesQuery` and filtering out cancelled activities), assignment title input (max 200 chars), multiline description textarea (max 2000 chars), release date (defaulting to today's date), optional due date, and internal teacher notes. Form validation backed by VeeValidate and Zod schema enforcing `dueDate >= releaseDate`. Emits `created` event upon successful mutation. | SATISFIED |
| `SubmissionsReviewTable.vue` (`src/frontend/src/features/academics/coursework/components/`) | PrimeVue DataTable rendering enrolled participant roster. Displays dynamic status pills (`submitted`, `late`, `missing` highlighted in rose/red with warning ring, `excused`, `exempted`, `pending`). Renders formatted submitted date, deliverable previews (URL link with external link badge, uploaded file badge with filename, inline plain text), and warning tag for resubmission attempts. Includes row action buttons for inspecting deliverables and granting policy exceptions. Search filter and status dropdown filter with summary metrics bar (Total, Submitted, Missing, Exceptions). | SATISFIED |
| `GrantExceptionModal.vue` (`src/frontend/src/features/academics/coursework/components/`) | PrimeVue Dialog modal allowing teachers/coordinators to select an enrolled student, specify exception type (`excused` for medical/approved absence, `exempted` for curriculum waiver/transfer), and provide mandatory justification reason. Form validated via VeeValidate and `recordExceptionSchema` from `coursework.schema.ts` (enforcing min 5 characters). Submits via `useRecordExceptionMutation` and emits `saved`. | SATISFIED |
| `SubmissionDetailDrawer.vue` (`src/frontend/src/features/academics/coursework/components/`) | Inspection drawer displaying student participant identity, dynamic status pill, and timeliness banners (specifically highlighting overdue missing projection without database row locks, late submission warnings, or on-time verification). Inspects student response: clickable URL button, file download card with filename and download trigger, or formatted text block. Displays timestamp, resubmission history, internal notes, and direct shortcut to grant policy exception. | SATISFIED |
| Visual Dynamic Missing Indicator | Missing work is calculated dynamically on-the-fly without database write lock overhead (`GAP-007`, `TD-06`). `SubmissionsReviewTable.vue` renders `missing` status in prominent rose/red with warning ring and highlights the count in the summary bar. `SubmissionDetailDrawer.vue` presents a dedicated overdue alert banner explaining the dynamic projection mechanism. | SATISFIED |
| Feature Barrel Exports (`index.ts`) | `src/frontend/src/features/academics/coursework/components/index.ts` exports all 4 components. `src/frontend/src/features/academics/coursework/index.ts` cleanly exports components, views (`CourseworkWorkbenchView`), API endpoints, composables, permission constants, Zod schemas, and TypeScript types. | SATISFIED |
| Quality Checks & TypeScript Compilation | Ran `vue-tsc --noEmit` via `pnpm typecheck`, compiling cleanly with 0 errors. All 23 test suites and 307 unit tests across `src/features/academics` pass with 100% success. | SATISFIED |

---

## 2. Architecture & Code Quality Verification

- **Architecture Compliance (`SCR-ACD-02`, `GAP-007`, `GAP-008`, `OQ-005`, `TD-06`, `TD-10`):**
  - **Dynamic Missing Work:** Strictly adheres to the zero-lock dynamic projection pattern. Missing work is identified dynamically when `CURRENT_TIMESTAMP > due_date` without persistent table bloat or scheduled daemon write locks.
  - **Permission Enforcement:** View actions and header buttons adhere to client permissions defined in `COURSEWORK_PERMISSIONS` (`academics.assignments.manage`, `academics.assignments.release`, `academics.assignments.close`, `academics.submissions.exception`).
  - **Component Quality & PrimeVue 4 Usage:** Utilizes PrimeVue `Drawer`, `Dialog`, `DataTable`, `Column`, `Button`, `InputText`, `Textarea`, `Select`, `ProgressBar`, and `Tag` components with unstyled/Tailwind semantic design tokens and accessibility attributes.
  - **Validation & Form State:** `CreateAssignmentDrawer.vue` and `GrantExceptionModal.vue` leverage `vee-validate` paired with `@vee-validate/zod` and domain schemas, ensuring date order validations (`due_date >= release_date`) and audit reason length boundaries (`min 5`).
  - **Reactivity & TanStack Query Integration:** Reactivity is properly managed with `computed`, `watch`, and `ref`. Cache invalidation rules correctly trigger `refetchAssignments()` and `refetchSubmissions()` on assignment release, assignment closure, and exception recording.

---

## 3. Findings

No BLOCKER, MAJOR, or MINOR findings.

- **[NOTE-01]:** In `CourseworkWorkbenchView.vue`, the submissions progress bar accurately aggregates both submitted/late submissions and administrative exceptions (`excused`/`exempted`) toward the completion metric, properly reflecting that exempted/excused students do not penalize unit progress.

---

## 4. Remediation History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 1 | 2026-11-05 | GO | Initial review: All completion criteria satisfied for Coursework & Assessment Workbench View (`CourseworkWorkbenchView.vue`), assignment creation drawer, submission review table with dynamic missing indicator, exception modal, and submission detail inspection drawer. Zero typecheck errors and 100% passing Academics test suites. |

---

## 5. Final Review Decision

**GO**
