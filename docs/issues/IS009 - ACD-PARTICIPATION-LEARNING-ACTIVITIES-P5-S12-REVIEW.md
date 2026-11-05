# Review Record: P5-S12 Missing Work Dynamic Projection & Student Workspace Slices

- **Slice ID:** P5-S12
- **Title:** Missing Work Dynamic Projection & Student Workspace Slices
- **Review Date:** 2026-11-05
- **Review Decision:** GO
- **Review Iteration:** 1

---

## 1. Scope & Objective Verification

| Requirement / Objective | Evaluation | Status |
|---|---|---|
| `GetAssignmentSubmissions` query & endpoint | `GET /api/v1/academics/assignments/{id}/submissions` protected by `Permissions.Academics.Submissions.View`. Verifies assignment exists (returns 404 if not found). Executes SQL CTE query joining enrolled active participants (`status = 'in_progress'`) in the assignment's unit/section with their latest submission via `LEFT JOIN`. Evaluates display status dynamically: `s.status` if submission exists, `'missing'` if `due_date < CAST(SYSUTCDATETIME() AS DATE)`, `'pending'` otherwise (TD-06, SCR-ACD-02). Returns student roster with display status, submission timestamp, reference link, resubmission flag, and creator metadata. | SATISFIED |
| `GetStudentCourseworkWorkspace` query & endpoint | `GET /api/v1/portal/learning/coursework` protected by `Permissions.Academics.Assignments.View`. Determines target student from `ICurrentUser.StudentId`. Enforces student authorization guard preventing horizontal privilege escalation (Section 9.1), while allowing privileged teacher/admin role override with required `studentIdOverride`. Supports filter `statusFilter` (`all`, `upcoming`, `due_soon`, `submitted`, `missing`). Queries assignments across all units where the student has an active participation (`p.status = 'in_progress'`) for released and closed assignments. Dynamically projects display status (`pending`, `submitted`, `late`, `missing`, `excused`, `exempted`) and countdown days to due date (`DATEDIFF(day, CAST(SYSUTCDATETIME() AS DATE), a.due_date)`). Returns list of coursework tasks matching SCR-SELF-03. | SATISFIED |
| Dynamic Missing Work Projection Architecture | Evaluates missing status on the fly via SQL `CASE` without background scheduler daemons, write locking, or synthetic row bloat (TD-06, GAP-007, OQ-005). Instantaneous responsiveness to due date updates. | SATISFIED |
| Student Authorization Guard | Verifies `ICurrentUser.StudentId` matches requested workspace, returning 403 Forbidden on horizontal privilege escalation attempts. Allows teachers and administrators with privileged roles to query on behalf of a student. | SATISFIED |
| Dependency Injection Registration | Both `GetAssignmentSubmissionsHandler` and `GetStudentCourseworkWorkspaceHandler` registered as scoped services in `Elsasa.Sis.Academics/DependencyInjection.cs`. Both endpoints implement `IEndpoint` and are mapped via Minimal API discovery. | SATISFIED |
| Automated Unit Testing | 40 unit tests authored in `MissingWorkDynamicProjectionTests.cs` verifying dynamic status evaluation rules, countdown days calculation, workspace filtering across all filter permutations, and student authorization guards. All 40 pass. | SATISFIED |
| Architecture & Quality Gates | Solution compiles cleanly with 0 warnings and 0 errors under `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. All 36 architecture rules pass in `Elsasa.Sis.ArchitectureTests`. | SATISFIED |

---

## 2. Architecture & Code Quality Verification

- **Architecture Compliance:**
  - Conforms strictly to ADR-001 (Domain Boundaries), ADR-002 (Logical references for cross-domain IDs), ADR-004 (Vertical Slice Architecture with Dapper), ADR-005 (Minimal APIs with `IEndpoint`), and ADR-009 (Role-Based Authorization Policies).
  - Fully implements TD-06 (On-the-Fly Missing Submission Projection & Separation of Submission from Grading).
  - Aligns with UI specifications for `SCR-ACD-02` (Teacher Review Submissions Table) and `SCR-SELF-03` (Student Learning Workspace).
- **Security & Authorization:**
  - `GetAssignmentSubmissions`: Protected by `Permissions.Academics.Submissions.View`.
  - `GetStudentCourseworkWorkspace`: Protected by `Permissions.Academics.Assignments.View`.
  - Horizontal privilege escalation prevention: Rejects access when a student attempts to inspect another student's coursework workspace.
- **Performance & Read Scalability:**
  - Zero synthetic rows or background workers required to evaluate missing coursework.
  - Queries leverage covering and composite indexes on participations, activities, assignments, and submissions.
  - Dapper parameterized SQL queries with `DapperTypeHandlers.RegisterTypeHandlers()` for clean DateOnly mapping.
- **Error Handling:**
  - Returns standard RFC 7807 problem details (`404 NotFound`, `400 BadRequest`, `403 Forbidden`).

---

## 3. Findings

No BLOCKER, MAJOR, or MINOR findings.

- **[NOTE-01]:** `GetStudentCourseworkWorkspaceHandler` includes a secondary filter on `CountdownDays >= 0 && CountdownDays <= 7` for `due_soon` filter, allowing the student workspace to highlight urgent tasks with clean color coding.

---

## 4. Remediation History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 1 | 2026-11-05 | GO | Initial review: All completion criteria satisfied for `GetAssignmentSubmissions` and `GetStudentCourseworkWorkspace`, dynamic missing work SQL projection verified (TD-06), student authorization guards verified, 40 unit tests passing (40/40), 36 architecture tests passing (36/36), and clean compilation with zero warnings. |

---

## 5. Final Review Decision

**GO**
