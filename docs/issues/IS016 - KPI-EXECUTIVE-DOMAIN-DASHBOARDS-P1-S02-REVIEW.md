# Review Record: P1-S02 Deterministic Demo Data Generator Enrichment Across All 5 Domains

- **Issue / Plan:** [`IS016 - KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS016%20-%20KPI-EXECUTIVE-DOMAIN-DASHBOARDS-IMPLEMENTATION-PLAN.md) (`CR-KPI-EXECUTIVE-DOMAIN-DASHBOARDS`)
- **Slice ID:** P1-S02
- **Title:** Deterministic Demo Data Generator Enrichment Across All 5 Domains
- **Review Date:** 2026-11-11
- **Review Decision:** GO
- **ReviewIteration:** 1

---

## 1. Scope & Objective Verification

| Requirement / Criterion | Evaluation & Observed Evidence | Status |
|---|---|---|
| `OperationsDemoDataSeeder.cs`: published `CAL-2026-EAST` (`campus-east`) + unreviewed amendment `AMD-CAL-2026-002` (`downstream_impact_reviewed = 0`) | Verified in [`OperationsDemoDataSeeder.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Operations/Features/DemoData/OperationsDemoDataSeeder.cs#L230-L482) and queried in SQL Server after `--seed-demo`: 2 published calendars (`campus-main` and `CAL-2026-EAST` with `scope_reference = 'campus-east'`), plus `AMD-CAL-2026-001` (`downstream_impact_reviewed = 1`) and `AMD-CAL-2026-002` (`downstream_impact_reviewed = 0`). | SATISFIED |
| `StudentsDemoDataSeeder.cs`: 8 students on `'campus-east'`, 2 exits (`withdrawn`, `transferred_out`), 1 expired leave (`on_leave` > 14 days ago), 2 uncompleted accepted admissions, ~78% doc verification rate, total students = 48 | Verified in [`StudentsDemoDataSeeder.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Students/Features/DemoData/StudentsDemoDataSeeder.cs#L1500-L1968) and queried in SQL Server: 48 total students (40 `campus-main`, 8 `campus-east`), 45 `active`, 1 `withdrawn` (18d ago), 1 `transferred_out` (20d ago), 1 `on_leave` (`leave_of_absence` 21d ago), 2 `accepted` un-enrolled applications (`Esther`, `Ruth`), 14/18 (`77.78%`) verified applicant documents. | SATISFIED |
| `FinanceTransactionsDemoDataSeeder.cs`: unpaid obligations across all aging brackets, 3 submitted payments, 2 unallocated cash payments | Verified in [`FinanceTransactionsDemoDataSeeder.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Finance/Features/DemoData/FinanceTransactionsDemoDataSeeder.cs#L199-L823) and queried `finance.v_student_receivables` & `finance.payments`: `Current` (48 rows), `1-30 Days` (18), `31-60 Days` (12), `61-90 Days` (10), `90+ Days` (11); 3 `submitted` payments; 2 `confirmed` payments with 500,000 IDR unallocated balance; Jonathan `INV-SEM2-0042` (`1,500,000`, `unpaid`), Grace overdue, and pastoral waiver preserved. | SATISFIED |
| `AcademicsActivitiesDemoDataSeeder.cs`: S1/S2 standings & learning progress for all 45 active students (+4.4% delta), 5 unaddressed at-risk, 3 chronic absenteeism (<80%), 6 missing submissions, 3 provisional results | Verified in [`AcademicsActivitiesDemoDataSeeder.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Academics/Features/DemoData/AcademicsActivitiesDemoDataSeeder.cs#L414-L1880) and queried in SQL Server: S1 36/45 `good_standing` (80.0%) vs S2 38/45 `good_standing` (84.44%, `+4.44%` delta); 7 at-risk learners (2 covered by `in_progress` interventions, 5 unaddressed); 3 learners at `66.67%` S2 attendance (`< 80%`); 6 `missing` submissions; 3 `provisional` results (`is_final = 1`). | SATISFIED |
| `TeachingDemoDataSeeder.cs` & `TeachingAssignmentsDemoDataSeeder.cs`: campus codes `'campus-main'`/`'campus-east'`, 2 additional teachers spanning all 5 workload bands (incl. 1 breached), 1 blocking & 1 advisory conflict, 2 open demands | Verified in [`TeachingDemoDataSeeder.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/DemoData/TeachingDemoDataSeeder.cs#L20-L1365), [`TeachingAssignmentsDemoDataSeeder.cs`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Teaching/Features/DemoData/TeachingAssignmentsDemoDataSeeder.cs#L19-L1592), and SQL Server: 5 `campus-main` + 1 `campus-east` teachers; all 5 bands (`under_allocated`, `normal`, `approaching_limit`, `overloaded_with_exception`, `breached`); 1 `unresolved` `blocking` + 1 `unresolved` `advisory` conflict; 2 open demands (`DEM-2026-0005`, `DEM-2026-0006`). | SATISFIED |
| Flagship GUIDs preserved, count bounds respected, 0 FK orphans, `dotnet build`, `ArchitectureTests`, `--seed-demo` < 5000ms | `DemoDataConstants.cs` unchanged; 48 students, 96 invoices, 1199 attendance, 163 results, 48 report cards, 0 FK orphans verified via SQL; `dotnet build src/backend/Elsasa.Sis.sln` succeeded (0 warnings, 0 errors); `Elsasa.Sis.ArchitectureTests` passed 311/311; `--seed-demo` completed in `3692ms` and `3100ms` (see note on 1 transient spike). | SATISFIED |
| `DemoDataResetTest` and `DemoDataIntegrityTest` pass 100% (< 5000ms), and existing Teaching integration tests remain green | Verified in Iteration 1 re-review against SQL Server (`ElsasaSisTestDb`): `DemoDataResetTest` (2/2), `DemoDataIntegrityTest` (8/8), and Teaching DemoData integration tests (`DemoDataSeederAndPurgerCycle` & `DemoDataResetCycle`, 2/2) all pass (12/12, 100%). All observed reset+seed durations (`1093ms`–`2766ms`) are strictly `< 5000ms`. | SATISFIED |

---

## 2. Findings

### [FINDING-P1-S02-01]
- **ID:** FINDING-P1-S02-01
- **Severity:** MAJOR
- **Status:** RESOLVED
- **Resolution Evidence (Iteration 1):** Updated [`TeachingAuthorizationsAndModuleIntegrationTests.cs#L556`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/Teaching/TeachingAuthorizationsAndModuleIntegrationTests.cs#L556) to `seedResult.TeacherProfilesCreated.Should().Be(6);` and verified `DemoDataSeederAndPurgerCycle_Should_SeedAllPersonas_And_PurgeCleanlyInReverseOrder` passes against `ElsasaSisTestDb`.
- **Description:** P1-S02 added 2 additional teacher profiles (`Dewi Lestari` and `Yohanes Prabowo`) in `TeachingDemoDataSeeder.cs`, increasing `TeacherProfilesCreated` from `4` to `6`, which directly breaks the existing integration test `TeachingAuthorizationsAndModuleIntegrationTests.DemoDataSeederAndPurgerCycle_Should_SeedAllPersonas_And_PurgeCleanlyInReverseOrder`.
- **Evidence:**
  Executing `dotnet test tests/backend/Elsasa.Sis.IntegrationTests/Elsasa.Sis.IntegrationTests.csproj --filter "FullyQualifiedName~DemoData"` with `TEST_SQLSERVER_CONNECTION_STRING` set outputs:
  ```text
  Failed Elsasa.Sis.IntegrationTests.Features.Teaching.TeachingAuthorizationsAndModuleIntegrationTests.DemoDataSeederAndPurgerCycle_Should_SeedAllPersonas_And_PurgeCleanlyInReverseOrder [746 ms]
    Error Message:
     Expected seedResult.TeacherProfilesCreated to be 4, but found 6.
  ```
  Source assertion at [`TeachingAuthorizationsAndModuleIntegrationTests.cs#L556`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/Teaching/TeachingAuthorizationsAndModuleIntegrationTests.cs#L556):
  `seedResult.TeacherProfilesCreated.Should().Be(4);`
- **Required Correction:**
  Update [`TeachingAuthorizationsAndModuleIntegrationTests.cs#L556`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/Teaching/TeachingAuthorizationsAndModuleIntegrationTests.cs#L556) to expect `6` teacher profiles (and optionally assert the enriched `Dewi Lestari` / `Yohanes Prabowo` profiles) so the Teaching seeder/purger integration test passes cleanly against SQL Server.

### [FINDING-P1-S02-02]
- **ID:** FINDING-P1-S02-02
- **Severity:** MAJOR
- **Status:** RESOLVED
- **Resolution Evidence (Iteration 1):** Updated [`DemoDataResetTest.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataResetTest.cs) and [`DemoDataIntegrityTest.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataIntegrityTest.cs) to set `Factory.CurrentUser.Roles = ["SuperAdmin"]` in each class's `InitializeAsync()` override (leaving global `Fixtures/` unchanged) and aligned stale schema/table/column/count assertions (`PurgedSchemas == 5`, `SeededDomains == 8`, `finance.student_financial_accounts`, `academics.submissions`, `students.admission_applications`, `finance.billing_document_items`, `ap.school_level`, `delivery_status`, `confirmation_status`, `finance.v_student_receivables.aging_bracket`, `long` `total_billed_amount`, Grace retake `replaces_result_id` match). Re-running against `ElsasaSisTestDb` passed 2/2 (`DemoDataResetTest`) and 8/8 (`DemoDataIntegrityTest`) with reset durations `1093ms`–`2766ms` (`< 5000ms`). *(Note: Unrelated `DemoDataProductionGuardTest` still has 2/4 pre-existing failures due to default `Admin` role in `TestCurrentUser`; unchanged and outside `P1-S02` criteria.)*
- **Description:** P1-S02 Completion Criterion #6 (`DemoDataResetTest and DemoDataIntegrityTest pass with 100% success and reset duration < 5000ms`) is not satisfied when executed against SQL Server. Both test classes fail 100% (2/2 failed in `DemoDataResetTest`, 8/8 failed in `DemoDataIntegrityTest`) due to pre-existing test drift predating P1-S02.
- **Evidence:**
  Executing `dotnet test tests/backend/Elsasa.Sis.IntegrationTests/Elsasa.Sis.IntegrationTests.csproj --filter "FullyQualifiedName~DemoData"` against `ElsasaSisTestDb` outputs `403 Forbidden` on all 10 tests in `DemoDataResetTest` and `DemoDataIntegrityTest`:
  ```text
  Reason: These requirements were not met:
  RolesAuthorizationRequirement:User.IsInRole must be true for one of the following roles: (SuperAdmin)
  ```
  Additionally, code inspection of [`DemoDataResetTest.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataResetTest.cs#L45-L91) and [`DemoDataIntegrityTest.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataIntegrityTest.cs#L46-L418) confirms pre-existing schema/count drift:
  1. `Factory.CurrentUser` (`TestCurrentUser`) defaults to `Roles = ["Admin"]` rather than `"SuperAdmin"` required by [`DemoDataEndpoints.cs#L31`](file:///d:/Project.Private/elsasa-sis-v3/src/backend/Elsasa.Sis.Api/Features/DemoData/DemoDataEndpoints.cs#L31).
  2. `DemoDataResetTest.cs` asserts `PurgedSchemas == 4` and `SeededDomains == 6` (stale pre-Teaching counts; `DemoDataOrchestrator` now purges `5` schemas and seeds `8` domains) and queries `finance.student_accounts` instead of `finance.student_financial_accounts`.
  3. `DemoDataIntegrityTest.cs` queries non-existent tables/columns (`students.students.first_name`, `academics.coursework_submissions`, `students.applications`, `finance.billing_documents.financial_obligation_id`, `students.administrative_placements.level`, `finance.payments.status`).
  *(Note: Direct `--seed-demo` execution and SQL verification confirm P1-S02 seeder logic itself has 0 FK orphans, satisfies all count bounds, and preserves all flagship persona invariants.)*
- **Required Correction:**
  Either:
  1. Remediate the pre-existing drift in [`DemoDataResetTest.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataResetTest.cs) and [`DemoDataIntegrityTest.cs`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/DemoData/DemoDataIntegrityTest.cs) (set `SuperAdmin` role on `Factory.CurrentUser` and align SQL queries / domain counts to the current schema) so Completion Criterion #6 passes 100%, **or**
  2. Escalate to `ica-architect` if fixing pre-existing `DemoDataResetTest` / `DemoDataIntegrityTest` drift is intended to remain exclusively in `P6-S13` rather than blocking `P1-S02`.

### [FINDING-P1-S02-03]
- **ID:** FINDING-P1-S02-03
- **Severity:** NOTE
- **Status:** RESOLVED (Item 1 RESOLVED; Item 2 NOTE)
- **Resolution Evidence (Iteration 1):**
  1. Updated [`TeachingWorkloadAndSubstitutionsIntegrationTests.cs#L759`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/Teaching/TeachingWorkloadAndSubstitutionsIntegrationTests.cs#L759) to filter `WHERE display_name IN ('Budi Hartono, M.Pd.', 'Siti Nurhaliza, S.Pd.')` and verified `DemoDataResetCycle_Should_ExecuteBothPhases_And_PurgerCleansAll24TeachingTables` passes against `ElsasaSisTestDb`.
  2. Across all 12 SQL Server integration resets in Iteration 1 re-review, `ElapsedMilliseconds` remained consistently between `1093ms` and `2766ms` (`< 5000ms`).
- **Description:** Two additional observations during SQL Server integration test and CLI `--seed-demo` verification:
  1. Pre-existing test query bug in [`TeachingWorkloadAndSubstitutionsIntegrationTests.cs#L759`](file:///d:/Project.Private/elsasa-sis-v3/tests/backend/Elsasa.Sis.IntegrationTests/Features/Teaching/TeachingWorkloadAndSubstitutionsIntegrationTests.cs#L759): `DemoDataResetCycle_Should_ExecuteBothPhases_And_PurgerCleansAll24TeachingTables` filters `WHERE full_name IN ('Budi Hartono, M.Pd.', 'Siti Nurhaliza, S.Pd.')`, whereas `TeachingDemoDataSeeder` stores those titles in `display_name` (`full_name` is `'Budi Hartono'` / `'Siti Nurhaliza'`).
  2. While CLI `--seed-demo` met the `< 5000ms` target on two runs (`3692ms` and `3100ms`), one intermediate run spiked to `14772ms` (`AcademicsActivitiesDemoDataSeeder` took ~11.8s vs ~0.6s normally), likely due to SQL Server log/data file auto-growth or checkpoint contention.

---

## 3. Remediation History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 0 | 2026-11-11 | NO-GO | All 6 seeder files satisfy domain enrichment criteria, count bounds, flagship GUID preservation, 0 FK orphans, `dotnet build` (0 warnings/errors), and `ArchitectureTests` (311/311). However, `TeachingAuthorizationsAndModuleIntegrationTests.DemoDataSeederAndPurgerCycle` fails due to P1-S02 increasing `TeacherProfilesCreated` from 4 to 6 ([FINDING-P1-S02-01]), and Completion Criterion #6 (`DemoDataResetTest` and `DemoDataIntegrityTest` pass 100%) is unmet due to pre-existing test drift ([FINDING-P1-S02-02]). Returned for remediation / architect clarification. |
| 1 | 2026-11-11 | GO | Re-reviewed after remediation: `FINDING-P1-S02-01`, `FINDING-P1-S02-02`, and `FINDING-P1-S02-03` (item 1) verified resolved. `dotnet build Elsasa.Sis.sln` succeeded (0 warnings, 0 errors); `Elsasa.Sis.ArchitectureTests` passed 311/311; `DemoDataResetTest` (2/2), `DemoDataIntegrityTest` (8/8), and Teaching DemoData integration tests (2/2) passed 12/12 against `ElsasaSisTestDb` with reset durations `1093ms`–`2766ms` (`< 5000ms`). All 6 seeders and `DemoDataConstants.cs` remain compliant. Decision: **GO**. |

---

## 4. Final Review Decision

**GO**
