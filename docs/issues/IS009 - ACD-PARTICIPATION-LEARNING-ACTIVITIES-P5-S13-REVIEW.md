# Review Record: P5-S13 Academics DI Registration, Endpoints Mapping & API Host Integration

- **Slice ID:** P5-S13
- **Title:** Academics DI Registration, Endpoints Mapping & API Host Integration
- **Review Date:** 2026-11-05
- **Review Decision:** GO
- **Review Iteration:** 1

---

## 1. Scope & Objective Verification

| Requirement / Objective | Evaluation | Status |
|---|---|---|
| `DependencyInjection.cs` Feature Handlers & Validators | Scoped registrations for all 23 Submodule 2 feature slice handlers: Academic Participations (BatchRegisterCohort, RegisterParticipation, GetParticipationsByUnit, GetStudentParticipations, UpdateParticipationStatus, RecognizePriorCredit, ConcludeParticipation), Learning Activities (PlanLearningActivity, ConductLearningActivity, CancelLearningActivity, GetActivitiesByUnit, GetActivityById, GetTeacherDailyActivities), Attendance (RecordSessionAttendance, CorrectAttendance, GetActivityAttendance, GetStudentAttendanceSummary), Coursework Assignments (CreateAssignment, ReleaseAssignment, CloseAssignment, GetAssignmentsByActivity, GetAssignmentsByUnit), and Submissions (SubmitCoursework, UploadSubmissionFile, RecordSubmissionException, GetAssignmentSubmissions, GetStudentCourseworkWorkspace). FluentValidation validators registered via assembly scanning with `AddValidatorsFromAssemblyContaining<AcademicsAssemblyMarker>()`. | SATISFIED |
| Policy-Based Authorization Requirements | Authorization policies mapped for all Submodule 2 permissions under `Permissions.Academics.*` (`Participations.View`, `Participations.Manage`, `Participations.RecognizePrior`, `Participations.Conclude`, `Activities.View`, `Activities.Manage`, `Activities.Conduct`, `Attendance.View`, `Attendance.Record`, `Attendance.Correct`, `Assignments.View`, `Assignments.Manage`, `Assignments.Release`, `Assignments.Close`, `Submissions.Submit`, `Submissions.Exception`, `Submissions.View`). Configured with role mappings adhering to ADR-009, granting access to `AcademicCoordinator`, `AcademicAuthority`, `Teacher`, and `Student` with administrator bypass. | SATISFIED |
| Coursework Submission Storage Services & Options | Configured `SubmissionStorageOptions` bound to configuration section `Academics:Submissions` (with fallback defaults: root `App_Data/Submissions`, max file size 25 MB). Registered `ISubmissionStorageService` with implementation `SubmissionStorageService` as a singleton. `UploadSubmissionFileHandler` refactored to consume `ISubmissionStorageService` and options cleanly. | SATISFIED |
| `AcademicsEndpoints.cs` Endpoint Discovery & Mapping | Minimal API mapping consolidated via `app.MapEndpoints(typeof(AcademicsAssemblyMarker).Assembly)`, dynamically discovering and registering all classes implementing `IEndpoint`. Maps routes under `/api/v1/academics/participations/*`, `/api/v1/academics/learning-activities/*`, `/api/v1/academics/attendance-records/*`, `/api/v1/academics/assignments/*`, and `/api/v1/portal/learning/*` (ADR-005, TD-02). | SATISFIED |
| Web API Host Integration (`Program.cs` & `appsettings.json`) | Physical coursework storage folder `App_Data/Submissions` ensured and created during host startup (`Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Submissions")`). Host DI pipeline registers Academics domain via `builder.Services.AddAcademicsDomain(builder.Configuration)`. Host endpoint pipeline registers Academics endpoints via `app.MapAcademicsEndpoints()`. `appsettings.json` specifies `Academics:Submissions` configuration. | SATISFIED |
| Automated Storage Unit Tests | 10 unit tests authored in `SubmissionStorageTests.cs` verifying storage options default values, physical file writing with path resolution, directory traversal sanitization, upload size limits, disallowed file formats, cross-student authorization guards, valid upload handling, and DI container resolution. All 10 tests pass (10/10). | SATISFIED |
| Clean Compilation & Quality Gates | Backend solution `src/backend/Elsasa.Sis.sln` compiles cleanly with 0 warnings and 0 errors under `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<Nullable>enable</Nullable>`. All 36 architecture rules pass in `Elsasa.Sis.ArchitectureTests`. All 751 backend unit tests pass in `Elsasa.Sis.IntegrationTests`. | SATISFIED |

---

## 2. Architecture & Code Quality Verification

- **Architecture Compliance:**
  - Fully conforms to ADR-001 (Domain Boundaries & Assembly Isolation), ADR-004 (Vertical Slice Architecture), ADR-005 (Minimal API Discovery via `IEndpoint`), ADR-006 (DbUp Host Separation), and ADR-009 (Role-Based Authorization Policies).
  - Implements TD-02 (Modular Minimal APIs for Submodule 2) and TD-07 (Local Hybrid Submission Storage Architecture).
- **Security & Authorization:**
  - Least privilege principle strictly enforced: Student access scoped only to own submissions (`Submissions.Submit`, `Submissions.View`, `Activities.View`, `Participations.View`).
  - Path traversal defense: `SubmissionStorageService.ResolvePhysicalPath` explicitly checks for `..` sequences, and `UploadSubmissionFileHandler` sanitizes file names via `Path.GetFileName` and alphanumeric whitelist character filtering.
  - File format and MIME type validation enforces allowed extensions (`.pdf`, `.jpg`, `.jpeg`, `.png`, `.docx`) and validates MIME headers.
- **Maintainability & Decoupling:**
  - `ISubmissionStorageService` abstracts filesystem access, allowing seamless transition to cloud object storage (e.g., S3/Blob storage) in future phases without altering command handlers.
  - Assembly scanning for `IEndpoint` and FluentValidation validators ensures zero boilerplate when adding new vertical slices.

---

## 3. Findings

No BLOCKER, MAJOR, or MINOR findings.

- **[NOTE-01]:** `SubmissionStorageOptions.StorageRootPath` defaults to relative path `App_Data/Submissions`, which `SubmissionStorageService` automatically anchors to `AppContext.BaseDirectory` when relative, ensuring consistent behavior across both IIS/Kestrel hosting and test harnesses.

---

## 4. Remediation History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 1 | 2026-11-05 | GO | Initial review: All completion criteria satisfied for Submodule 2 DI consolidation, endpoint assembly mapping, Web API host integration, file storage helper services, authorization policy configuration, 10 unit tests passing (10/10), 36 architecture rules passing (36/36), and clean compilation with zero warnings and zero errors. |

---

## 5. Final Review Decision

**GO**
