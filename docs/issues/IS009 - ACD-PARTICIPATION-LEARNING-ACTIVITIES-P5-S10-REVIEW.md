# Review Record: P5-S10 Coursework Assignment Lifecycle Slices (Create, Release, Close, Queries)

- **Slice ID:** P5-S10
- **Title:** Coursework Assignment Lifecycle Slices (Create, Release, Close, Queries)
- **Review Date:** 2026-11-05
- **Review Decision:** GO
- **Review Iteration:** 1

---

## 1. Scope & Objective Verification

| Requirement / Objective | Evaluation | Status |
|---|---|---|
| `CreateAssignment` command & endpoint | `POST /api/v1/academics/learning-activities/{activityId}/assignments` protected by `Permissions.Academics.Assignments.Manage`. Validates parent learning activity existence and ensures it is not cancelled (TD-05, UC-ACD-04). Validates title, release date, and optional due date >= release date via `CreateAssignmentValidator`. Inserts row into `academics.assignments` with `status = 'draft'`. Returns `201 Created` with full assignment details. | SATISFIED |
| `ReleaseAssignment` command & endpoint | `POST /api/v1/academics/assignments/{id}/release` protected by `Permissions.Academics.Assignments.Release`. Verifies assignment exists and is in `draft` status. Updates `status = 'released'`, `updated_at`, `updated_by`. Publishes post-commit `AssignmentReleasedEvent` via MediatR `IPublisher` (ADR-003, TD-08). Returns `200 OK`. | SATISFIED |
| `CloseAssignment` command & endpoint | `POST /api/v1/academics/assignments/{id}/close` protected by `Permissions.Academics.Assignments.Close`. Verifies assignment exists and is in `released` status. Updates `status = 'closed'`, `updated_at`, `updated_by`. Returns `200 OK`. | SATISFIED |
| `GetAssignmentsByActivity` query & endpoint | `GET /api/v1/academics/learning-activities/{activityId}/assignments` protected by `Permissions.Academics.Assignments.View`. Verifies parent learning activity exists (returns 404 if not found). Returns assignments originating from the specified activity ordered by `release_date ASC, created_at ASC`. Returns `200 OK`. | SATISFIED |
| `GetAssignmentsByUnit` query & endpoint | `GET /api/v1/academics/learning-units/{unitId}/assignments` protected by `Permissions.Academics.Assignments.View`. Verifies learning unit exists (returns 404 if not found). Filters by optional `status`. Joins with `academics.learning_activities` to return assignments across the unit with activity title, type, and date context ordered by `release_date ASC, created_at ASC`. Returns `200 OK`. | SATISFIED |
| Strict curriculum hierarchy invariant | Enforces parent learning activity requirement (TD-05) at database foreign key level and slice logic level; prohibits assignment creation against cancelled learning activities. | SATISFIED |
| Dependency Injection & Endpoint Registration | All 5 feature handlers registered as scoped services in `DependencyInjection.cs`. Endpoints implement `IEndpoint` and are mapped into the pipeline via Minimal API scanning. `DapperTypeHandlers.RegisterTypeHandlers()` registered. | SATISFIED |
| Automated Unit Testing | 11 unit tests authored in `AssignmentValidatorTests.cs` verifying valid requests, null due dates, same-day due dates, empty titles, title length limits, invalid due date order, default release dates, and description/notes length boundaries. | SATISFIED |
| Architecture & Quality Gates | Solution compiles cleanly with 0 warnings and 0 errors under `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. All 36 architecture rules pass in `Elsasa.Sis.ArchitectureTests`. All 11 unit tests pass. | SATISFIED |

---

## 2. Architecture & Code Quality Verification

- **Architecture Compliance:**
  - Conforms strictly to ADR-001 (Domain Boundaries), ADR-003 (Domain Events via MediatR post-commit), ADR-004 (Vertical Slice Architecture), ADR-005 (Minimal APIs with `IEndpoint`), and ADR-009 (Role-Based Authorization Policies).
  - Complies with TD-05 (Strict Curriculum Hierarchy Invariant) preventing orphan assignments and validating parent activity status.
  - Matches technical specifications in `IS009 - ACD-PARTICIPATION-LEARNING-ACTIVITIES-ARCHITECTURE.md` Section 5.5 (`Assignments` Feature Slice), Section 7 (Domain Events), and Section 9 (Permissions).
- **Security & Authorization:**
  - Enforces policy authorization on all endpoints (`Assignments.Manage`, `Assignments.Release`, `Assignments.Close`, `Assignments.View`).
  - Parameterized Dapper queries prevent SQL injection.
  - Input sanitization and trimming across all request payloads.
- **Error Handling:**
  - All feature handlers return `Result<T>` and map to standard RFC 7807 problem details (`404 NotFound`, `409 Conflict`, `422 UnprocessableEntity`).
- **Post-Commit Eventing:**
  - `AssignmentReleasedEvent` published post-commit alerting downstream student coursework workspaces.

---

## 3. Findings

No BLOCKER, MAJOR, or MINOR findings.

- **[NOTE-01]:** `GetAssignmentsByUnitHandler` joins `assignments` with `learning_activities` to include originating activity context (`LearningActivityTitle`, `LearningActivityType`, `LearningActivityDate`), providing enriched metadata for unit-level coursework views.

---

## 4. Remediation History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 1 | 2026-11-05 | GO | Initial review: All 5 assignment lifecycle and query vertical slices verified against completion criteria, strict curriculum parentage enforced (TD-05), validator unit tests passing (11/11), architecture tests passing (36/36), and clean compilation with zero warnings. |

---

## 5. Final Review Decision

**GO**
