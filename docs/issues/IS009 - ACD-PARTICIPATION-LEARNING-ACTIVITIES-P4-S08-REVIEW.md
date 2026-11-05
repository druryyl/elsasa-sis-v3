# Review Record: P4-S08 Attendance Roll-Call Recording Slice (`RecordSessionAttendance` with 24-Hour Grace Window)

- **Slice ID:** P4-S08
- **Title:** Attendance Roll-Call Recording Slice (`RecordSessionAttendance` with 24-Hour Grace Window)
- **Review Date:** 2026-11-05
- **Review Decision:** GO
- **Review Iteration:** 0

---

## 1. Scope & Objective Verification

| Requirement / Objective | Evaluation | Status |
|---|---|---|
| `RecordSessionAttendance` endpoint & authorization | `POST /api/v1/academics/learning-activities/{id:guid}/attendance` mapped in `RecordSessionAttendanceEndpoint.cs` and protected by `Permissions.Academics.Attendance.Record`. Returns `200 OK`, `404 NotFound`, `409 Conflict`, or `422 UnprocessableEntity`. | SATISFIED |
| Request contract & deserialization | `RecordSessionAttendanceRequest` supports student attendance entries with `StudentId`, `Status`, optional `Evidence`, and `Notes`. Custom `RecordSessionAttendanceRequestJsonConverter` enables deserialization of both root JSON arrays `[...]` and wrapped JSON objects `{ "entries": [...] }`. | SATISFIED |
| FluentValidation rules | `RecordSessionAttendanceValidator` verifies non-null/non-empty entries collection, unique student IDs in payload, valid status literals (`present`, `absent`, `late`, `excused_absence`, `partial`), required student IDs, and text length limits on evidence and notes. | SATISFIED |
| Activity existence & status check | `RecordSessionAttendanceHandler` queries `academics.learning_activities`. Returns `DomainError.NotFound` if activity is missing. Returns `DomainError.Conflict` if activity status is `cancelled`. | SATISFIED |
| 24-Hour Grace Window enforcement | Evaluates `activity.activity_date >= today || (SYSUTCDATETIME() - activity.created_at) <= 24 hours` using `TimeProvider`. Rejects expired recording requests with `DomainError.Conflict("Academic.AttendanceGraceWindowExpired", "The 24-hour grace window for direct attendance recording has expired. Use the auditable correction endpoint.")`. | SATISFIED |
| Atomic upsert in SQL transaction | Encloses upserts in a database transaction. Executes `MERGE academics.attendance_records WITH (HOLDLOCK) AS target ...` inserting new attendance records or updating existing entries for the same activity and student. Stamped with `recorded_by = currentUser.Username` and `recorded_date = CAST(SYSUTCDATETIME() AS DATE)`. | SATISFIED |
| Post-commit domain event | Publishes `AttendanceRecordedEvent` post-commit via MediatR `IPublisher` containing `LearningActivityId`, `RecordedDate`, aggregated totals (`TotalPresent`, `TotalAbsent`, `TotalLate`, `TotalExcused`), `RecordedBy`, and timestamp. | SATISFIED |
| Aggregated summary response | Computes and returns `RecordSessionAttendanceResponse` containing `TotalRecorded`, `TotalPresent`, `TotalAbsent`, `TotalLate`, `TotalExcused`, `TotalPartial`, `RecordedDate`, and `RecordedBy`. | SATISFIED |
| Dependency Injection & registration | `RecordSessionAttendanceHandler` registered as scoped service in `DependencyInjection.cs`. Validator registered via `AddValidatorsFromAssemblyContaining`. | SATISFIED |
| Unit testing coverage | 18 unit tests authored in `AttendanceValidatorTests.cs` verifying request deserialization, status case-insensitivity, duplicate student rejection, and required field rules. | SATISFIED |
| Quality & Architecture tests | 0 warnings, 0 errors under `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. All 36 architecture rules pass in `Elsasa.Sis.ArchitectureTests`. All 664 unit tests pass in `Elsasa.Sis.IntegrationTests`. | SATISFIED |

---

## 2. Architecture & Code Quality Verification

- **Architecture Compliance:**
  - Conforms to ADR-001 (Domain Boundaries), ADR-003 (Domain Events via MediatR post-commit), ADR-004 (Vertical Slice Architecture), ADR-005 (Minimal APIs with `IEndpoint`), and ADR-009 (Role-Based Authorization Policies).
  - Aligns with specifications in `IS009 - ACD-PARTICIPATION-LEARNING-ACTIVITIES-ARCHITECTURE.md` Section 5.4 (`Attendance` Feature Slice), Section 7 (`AttendanceRecordedEvent`), and Section 9 (Permissions).
- **Concurrency & Transaction Safety:**
  - Uses `MERGE` with `(HOLDLOCK)` under an explicit database transaction to prevent race conditions during concurrent roll calls.
  - Domain events are dispatched only after transaction commit succeeds.
- **Security & Authorization:**
  - Endpoint requires `Permissions.Academics.Attendance.Record`.
  - Auditing correctly tracks actor identity via `ICurrentUser.Username ?? currentUser.Email ?? "system"`.

---

## 3. Findings

No BLOCKER, MAJOR, or MINOR findings.

- **[NOTE-01]:** `RecordSessionAttendanceRequestJsonConverter` provides transparent backward and forward compatibility for clients submitting either an object with an `entries` array or a plain root array.

---

## 4. Remediation History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 0 | 2026-11-05 | GO | Initial review: Feature slice implemented and fully verified against completion criteria, grace window rule enforced, atomic transaction and merge logic validated, unit tests and architecture tests passing with 0 errors. |

---

## 5. Final Review Decision

**GO**
