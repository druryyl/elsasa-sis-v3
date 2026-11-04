# Review Record: P4-S12 End-to-End Finance Workflows Integration Tests (Testcontainers SQL Server 2022)

- **Slice ID:** P4-S12
- **Title:** End-to-End Finance Workflows Integration Tests (Testcontainers SQL Server 2022)
- **Review Date:** 2026-11-04
- **Review Decision:** GO
- **ReviewIteration:** 1

---

## 1. Scope & Objective Verification

| Requirement / Objective | Evaluation | Status |
|---|---|---|
| `StudentAccountLifecycleIntegrationTests.cs`: `StudentEnrolledEvent` triggers account provisioning | Verified account provisioning, active status, deterministic account number formatting, idempotency on repeated event publishing, and query endpoint. | SATISFIED |
| `FinancialPolicyWorkflowIntegrationTests.cs`: Policy draft, approval as Finance Authority, and charge rule creation | Policy draft creation, Finance Authority approval workflow, attaching charge rules to active policies, and rejection of charge rules on draft policies verified. | SATISFIED |
| `BatchAssessmentWorkflowIntegrationTests.cs`: Preview mode (dry run), commit mode (persistence + event dispatch), and duplicate skipping | Verified preview mode metrics without mutations, commit mode bulk generation with sequence-1 due schedules and event emission, and duplicate skipping on subsequent runs. | SATISFIED |
| `AdHocAssessmentWorkflowIntegrationTests.cs`: Ad-hoc fee assessment, sequence-1 due schedule, auto-provisioning missing account | Verified ad-hoc fee assessment, auto-provisioning missing accounts, sequence-1 due schedule generation, `ObligationAssessedEvent` publication, and existing account reuse with seeded active policy. | SATISFIED |
| `DerivedAccountBalanceIntegrationTests.cs`: Real-time balance derivation reflecting assessed, settled, cancelled obligations | Verified on-the-fly dynamic balance aggregation reflecting assessed, settled, and cancelled obligations. Invariant test verifying no denormalized balance column passes. | SATISFIED |
| `BillingDocumentWorkflowIntegrationTests.cs`: Bill compilation, guarantor resolution, frozen line items, status update to `issued` | Verified compilation of open obligations into immutable bills, primary financial guarantor resolution from Student domain, frozen line items, and obligation status transition to `issued`. | SATISFIED |
| Stage 1 Completion Criteria 6, 7, and 10 | Complete end-to-end integration test coverage for account lifecycle, policy governance, batch assessment, ad-hoc assessment, balance derivation, and billing documents. | SATISFIED |
| All integration tests pass cleanly | All 26 Finance integration tests and 127 unit tests pass cleanly against Microsoft SQL Server 2022 (`Elsasa.Sis.IntegrationTests`). | SATISFIED |

---

## 2. Findings

### Finding F-P4-S12-01

- **ID:** F-P4-S12-01
- **Severity:** BLOCKER
- **Description:** Multiple integration tests in `AdHocAssessmentWorkflowIntegrationTests`, `DerivedAccountBalanceIntegrationTests`, and `BillingDocumentWorkflowIntegrationTests` fail with HTTP 500 caused by `Microsoft.Data.SqlClient.SqlException: The INSERT statement conflicted with the FOREIGN KEY constraint "fk_finance_obligations_policy"`.
- **Evidence:**
  Executing `dotnet test tests/backend/Elsasa.Sis.IntegrationTests/Elsasa.Sis.IntegrationTests.csproj --filter "FullyQualifiedName~Finance"` against Microsoft SQL Server 2022 yields:
  ```text
  Microsoft.Data.SqlClient.SqlException (0x80131904): The INSERT statement conflicted with the FOREIGN KEY constraint "fk_finance_obligations_policy". The conflict occurred in database "ElsasaSisTestDb", table "finance.financial_policies", column 'id'.
     at Elsasa.Sis.Finance.Features.Obligations.AssessAdHocObligation.AssessAdHocObligationHandler.HandleAsync(AssessAdHocObligationRequest request, CancellationToken cancellationToken) in D:\Project.Private\elsasa-sis-v3\src\backend\Elsasa.Sis.Finance\Features\Obligations\AssessAdHocObligation\AssessAdHocObligationHandler.cs:line 228
  ```
  In `AssessAdHocObligationHandler.cs` (lines 210–216), when `request.PolicyId` is null, the handler queries `SELECT TOP (1) id FROM finance.financial_policies WHERE status = 'active'` and falls back to `_defaultSeedPolicyId = Guid.Parse("00000000-0000-0000-0002-000000000001")`. Because Respawn wipes domain tables in `finance` before each test, `finance.financial_policies` is empty, causing the insert into `finance.financial_obligations` to fail on the foreign key constraint.
- **Required Correction:**
  1. In `FinanceIntegrationTestHelpers.cs`, author a reusable helper (e.g. `EnsureActivePolicyAsync`) or ensure each test suite (`AdHocAssessmentWorkflowIntegrationTests`, `DerivedAccountBalanceIntegrationTests`, `BillingDocumentWorkflowIntegrationTests`) provisions an active policy before assessing obligations (or configure test harness seeding so that an active policy exists).
  2. Ensure all calls to `POST /api/finance/obligations` in integration tests succeed with `201 Created` and all subsequent workflow assertions execute cleanly.
- **Resolution Status:** RESOLVED
- **Remediation Evidence:**
  Authored `FinanceIntegrationTestHelpers.EnsureActivePolicyAsync` idempotently ensuring default active governing policy `FP-2026-001` exists. Wired this helper into `AdHocAssessmentWorkflowIntegrationTests`, `DerivedAccountBalanceIntegrationTests`, and `BillingDocumentWorkflowIntegrationTests`. All ad-hoc obligation creation requests return HTTP 201 Created and all downstream workflow assertions pass cleanly.

---

### Finding F-P4-S12-02

- **ID:** F-P4-S12-02
- **Severity:** MAJOR
- **Description:** `CreateChargeRuleHandler` allows attaching charge rules to `draft` financial policies, returning `201 Created` instead of `409 Conflict`, violating the business rule and causing `FinancialPolicyWorkflowIntegrationTests.CreateChargeRule_OnDraftPolicy_Should_ReturnConflictOrValidationFailure` to fail.
- **Evidence:**
  Executing `FinancialPolicyWorkflowIntegrationTests.CreateChargeRule_OnDraftPolicy_Should_ReturnConflictOrValidationFailure` produces:
  ```text
  Expected ruleResponse.StatusCode to be HttpStatusCode.Conflict {value: 409} because charge rules can only be attached to active policies, but found HttpStatusCode.Created {value: 201}.
     at Elsasa.Sis.IntegrationTests.Features.Finance.FinancialPolicyWorkflowIntegrationTests.CreateChargeRule_OnDraftPolicy_Should_ReturnConflictOrValidationFailure() in D:\Project.Private\elsasa-sis-v3\tests\backend\Elsasa.Sis.IntegrationTests\Features\Finance\FinancialPolicyWorkflowIntegrationTests.cs:line 169
  ```
- **Required Correction:**
  1. In `CreateChargeRuleHandler.cs`, query the policy status (e.g., `SELECT status FROM finance.financial_policies WHERE id = @PolicyId`).
  2. If the policy is not in `active` status, return `Result.Failure<CreateChargeRuleResponse>(DomainError.Conflict("Finance.Policy.NotActive", "Charge rules can only be attached to active financial policies."))`.
  3. Ensure `CreateChargeRule_OnDraftPolicy_Should_ReturnConflictOrValidationFailure` passes cleanly.
- **Resolution Status:** RESOLVED
- **Remediation Evidence:**
  Updated `CreateChargeRuleHandler.cs` (lines 78–97) to query policy status and enforce that only policies with `status = 'active'` can have charge rules attached; returns `DomainError.Conflict("Finance.Policy.NotActive", "Charge rules can only be attached to active financial policies.")` otherwise. Test `CreateChargeRule_OnDraftPolicy_Should_ReturnConflictOrValidationFailure` passes cleanly.

---

### Finding F-P4-S12-03

- **ID:** F-P4-S12-03
- **Severity:** NOTE
- **Description:** When running tests without Docker or `TEST_SQLSERVER_CONNECTION_STRING`, `SkipIfDatabaseUnavailable()` skips all 27 database integration tests, hiding runtime failures during local test execution unless explicitly pointed to a running SQL Server 2022 instance.
- **Evidence:**
  Running `dotnet test` without environment variable reported `Passed! - Failed: 0, Passed: 133, Skipped: 27`. Running with `$env:TEST_SQLSERVER_CONNECTION_STRING="Server=localhost;Database=ElsasaSisTestDb;Integrated Security=true;TrustServerCertificate=True;"` revealed 5 failing integration tests.
- **Required Correction:**
  Informational only. Always ensure tests are verified against an active SQL Server 2022 instance before reporting slice completion.
- **Resolution Status:** NOTED

---

## 3. Remediation History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 0 | 2026-11-04 | NO-GO | Initial review: 5 Finance integration tests fail against SQL Server 2022 due to unseeded active financial policy foreign key violations in ad-hoc assessment and `CreateChargeRule` allowing charge rule creation on draft policies. Returned for remediation. |
| 1 | 2026-11-04 | GO | Re-review Iteration 1: Remediations for F-P4-S12-01 (active policy helper) and F-P4-S12-02 (draft policy rejection) verified. All 26 Finance integration tests and 127 Finance unit tests pass cleanly against SQL Server 2022. No blocker or major findings remain. |

---

## 4. Final Review Decision

**GO**
