# Review Record: P8-S23 Route Registration, RBAC Navigation & AppLayout Integration

- **Slice ID:** P8-S23
- **Title:** Route Registration, RBAC Navigation & AppLayout Integration
- **Review Date:** 2026-11-05
- **Review Decision:** GO
- **Review Iteration:** 0

---

## 1. Scope & Objective Verification

| Requirement / Objective | Evaluation | Status |
|---|---|---|
| Route Registration (`src/frontend/src/app/router/routes.ts`) | Four Submodule 2 lazy-loaded routes registered under appropriate parent layout and domain router views: <br>1. `/academics/participation` &rarr; `ParticipationWorkbenchView.vue` with `requiresAuth: true`, `permission: 'academics.participations.view'`, and `title: 'Participation & Rosters'`.<br>2. `/academics/sessions` &rarr; `ClassSessionWorkbenchView.vue` with `requiresAuth: true`, `permission: 'academics.activities.view'`, and `title: 'Class Sessions'`.<br>3. `/academics/coursework` &rarr; `CourseworkWorkbenchView.vue` with `requiresAuth: true`, `permission: 'academics.assignments.view'`, and `title: 'Coursework'`.<br>4. `/portal/learning` &rarr; `StudentLearningWorkspaceView.vue` with `requiresAuth: true`, `roles: ['Student']`, and `title: 'My Learning & Coursework'`. | SATISFIED |
| AppLayout Academics Menu Integration (`src/frontend/src/app/layouts/AppLayout.vue`) | The "Academics" menu group in `masterNavItems` has been expanded with three Submodule 2 items: "Participation & Rosters" (`/academics/participation`), "Class Sessions" (`/academics/sessions`), and "Coursework" (`/academics/coursework`), each configured with distinct Lucide/PrimeIcons, route paths, permissions, and actor role arrays. | SATISFIED |
| AppLayout Student Portal Sidebar Integration (`src/frontend/src/app/layouts/AppLayout.vue`) | Added "Student Portal" navigation group (`id: 'portal'`, `label: 'Student Portal'`, `roles: ['Student']`) containing "My Learning & Coursework" (`to: '/portal/learning'`), visible strictly to authenticated users with the `Student` role. `portal` key added to `openGroups` for seamless accordion expansion. | SATISFIED |
| RBAC Navigation Filtering & Route Guards (`guards.ts` & `AppLayout.vue`) | 1. Extended `NavigationItem` and `AppRouteMeta` types in `src/frontend/src/shared/types/navigation.ts` with `roles?: string[]`.<br>2. `setupNavigationGuards` in `guards.ts` evaluates `to.meta.permission` against `authStore.hasPermission` (and role aliases for AcademicCoordinator, AcademicAuthority, Teacher, Admin), and checks `to.meta.roles` via `authStore.hasAnyRole`, redirecting unauthorized requests to `/403`.<br>3. `isNavAuthorized` and `filteredNavItems` in `AppLayout.vue` evaluate permissions and roles to dynamically hide menu items and empty groups from unauthorized actors. | SATISFIED |
| Dynamic Breadcrumbs Context Resolution | `resolvedBreadcrumbs` computed property in `AppLayout.vue` accurately parses route path segments, matches root segments to parent groups (e.g. `academics` &rarr; `Academics`, `portal` &rarr; `Student Portal`), and appends the active page title (e.g. `Participation & Rosters`, `Class Sessions`, `Coursework`, `My Learning & Coursework`) as current crumb. | SATISFIED |
| Quality Checks & TypeScript Compilation | Verified clean TypeScript compile via `pnpm --dir src/frontend run typecheck` (`vue-tsc --noEmit`), exiting with code 0 (0 errors, 0 warnings). Ran Vitest suite for guards and layout (`guards.spec.ts` and `AppLayout.spec.ts`) with 47/47 passing tests (100% success rate). | SATISFIED |

---

## 2. Architecture & Code Quality Verification

- **Architecture Compliance (`SCR-ACD-05`, `SCR-ACD-01`, `SCR-ACD-02`, `SCR-SELF-03`, `GAP-008`, `OQ-006`, `TD-10`):**
  - **Single Source of Truth for Route Metadata:** Route records declare explicit permissions and role claims in `meta`. `vue-router` type augmentation in `src/frontend/src/shared/types/navigation.ts` guarantees strict compile-time type validation across `routes.ts` and navigation components.
  - **Multi-Role RBAC Gating:** Academic Coordinators and Administrators access full roster, session, and coursework management; Teachers access instructional session conducting and coursework workbenches; Students are restricted exclusively to the Student Portal self-service workspace (`/portal/learning`).
  - **Graceful Permission Aliasing:** `guards.ts` and `AppLayout.vue` gracefully handle both dotted permission format (`academics.participations.view`) and legacy PascalCase permission strings (`Permissions.Academics.Participations.View`), preventing permission mismatch breakage across backend JWT token variations.
  - **Lazy Loading & Performance:** All feature views (`ParticipationWorkbenchView.vue`, `ClassSessionWorkbenchView.vue`, `CourseworkWorkbenchView.vue`, `StudentLearningWorkspaceView.vue`) utilize dynamic `import()` chunk loading to maintain optimal initial bundle payload size.

---

## 3. Findings

No BLOCKER, MAJOR, or MINOR findings.

- **[NOTE-01]:** In `AppLayout.vue`, `isNavAuthorized` checks both direct permissions and roles: if a navigation item specifies `roles`, an authenticated user possessing any of those roles is authorized even if fine-grained permission claims are not explicitly present in the token payload. This aligns cleanly with the transition model across Stage 1 and Stage 2 roles (`AcademicCoordinator`, `AcademicAuthority`, `Teacher`, `Student`).

---

## 4. Remediation History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 0 | 2026-11-05 | GO | Initial review: All completion criteria satisfied for route registration in `routes.ts`, menu and portal sidebar navigation integration in `AppLayout.vue`, role/permission RBAC filtering in `guards.ts`, dynamic breadcrumbs generation, type declarations in `navigation.ts`, clean `vue-tsc --noEmit` compilation, and 100% pass rate across 47 router and layout Vitest tests. |

---

## 5. Final Review Decision

**GO**
