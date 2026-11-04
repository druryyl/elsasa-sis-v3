---
Title: Feasibility Assessment — Stage 1 Payments & Receivables Core Submodules
Code: CR-FIN-PAYMENTS-RECEIVABLES
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-04
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Implementation of the core submodules of Stage 1 "Payments & Receivables" under the Finance domain as specified in [IS007 - FIN-PAYMENTS-RECEIVABLES-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS007%20-%20FIN-PAYMENTS-RECEIVABLES-ISSUE.md) and the SIS implementation roadmap ([sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)).

Referenced artifacts:
- **DOMAIN:** [finance-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/finance-domain.md)
- **FEATURE:** Payments & Receivables Core (`FIN-WF-05`, `FIN-WF-06`, `FIN-WF-13`)

## Objective

Evaluate the technical feasibility, architectural consistency, codebase readiness, and structural impacts of implementing Stage 1 Payments & Receivables:
1. Payer Management & Payment Receipt / Confirmation (`FIN-WF-05`, `UC-FIN-01`, `UC-FIN-04`, `SCR-FIN-02`, `SCR-SELF-02`).
2. Payment Allocation & Reallocation (`FIN-WF-06`, `UC-FIN-05`, `SCR-FIN-02`, `SCR-FIN-03`).
3. Enrollment Payment Inquiry Gate (`FIN-WF-13`, `UC-FIN-09`, `GAP-01`, `SCR-STU-01`).

Deliver the assessment using a **Pragmatic**, **Minimalist**, and **Explicit** approach while strictly honoring the boundaries of the Finance domain and upstream dependencies.

---

# 2. Current State

## Existing Behavior

1. **Database Schema:**
   - The database contains provisioned tables for Submodule 1: `finance.financial_policies`, `finance.charge_rules`, `finance.student_financial_accounts`, `finance.financial_obligations`, `finance.obligation_due_schedules`, `finance.billing_documents`, and `finance.billing_document_items`.
   - The tables specified for Submodule 3 in [finance-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/finance-entities-tables.md) (`finance.payers`, `finance.payments`, `finance.payment_allocations`) and the dynamic view `v_student_receivables` are **not provisioned**.
2. **Backend Domain & Services:**
   - `src/backend/Elsasa.Sis.Finance` contains handlers for Policies, Charge Rules, Account queries, Batch/Ad-hoc Obligation Assessment, and Billing Documents.
   - `FinanceModule.cs` implements `IFinanceModule.IsEnrollmentFeePaidAsync(Guid applicantId, Guid academicYearId)` as a hardcoded stub returning `Result.Success(false)`.
   - No feature handlers, repository queries, validators, commands, or Minimal API endpoints exist for payers, payment recording, payment confirmation, payment allocation, or enrollment fee queries.
   - Cross-domain consumer `EnrollAcceptedApplicantHandler.cs` in Students domain is already wired to query `IFinanceModule.IsEnrollmentFeePaidAsync` when `checkFinanceModule = true`.
3. **Frontend Implementation:**
   - `SCR-FIN-02` (Payment Verification Queue): Not implemented.
   - `SCR-FIN-03` (Student Accounts Workbench): Exists as an initial shell without payment receipts or allocation breakdown.
   - `SCR-SELF-02` (Family Tasks & Requests Portal): `FamilyBillingTab.vue` displays billing statements, but does not support fee payment submission or receipt downloading.
   - `SCR-STU-01` (Student Intake Workbench): `EnrollApplicantModal.vue` is wired to invoke enrollment with payment confirmation or waiver overrides, but cannot receive confirmation from Finance.

## Existing Constraints

1. **Domain Isolation (ADR-001, ADR-002, ADR-010):**
   - Finance domain assembly references only `Shared` and `Contracts`. It must never reference `Students` or `Operations`.
   - Compile-time isolation is verified by NetArchTest rules in `Elsasa.Sis.ArchitectureTests`. Cross-schema foreign keys in SQL Server are prohibited. Logical UUID references must be used.
2. **Single Base Currency (IDR) (Resolved Decision 6):**
   - All transactions operate strictly in Indonesian Rupiah (`IDR`) with integer precision (BIGINT columns, zero decimal places).
3. **Segregation of Duties & Actor Authority (Actors Resolution 3.1 & 3.2):**
   - Single consolidated `Finance Officer` actor performs billing assessments, payment recording, confirmations, and allocations.
   - `Finance Authority` approval is reserved for financial adjustments, refunds, and policy changes (zero discretionary spending). Routine cashiering and allocations do not require `Finance Authority` escalation.
4. **Historical Immutability (finance-domain.md):**
   - Adjust history, never rewrite history. Reallocations must supersede or reverse prior allocations with preserved audit references (`superseded_by_id`, `reversal_reason`). Receipts and audit logs are append-only.
5. **Enrollment Gate Policy (GAP-01):**
   - Enrolling an accepted applicant requires confirmed payment and full allocation by default. Hard block can be overridden only with an explicit logged justification.

---

# 3. Gap Analysis

| ID | Severity | Gap | Status | Resolution |
|------|------|------|:---:|------|
| `GAP-001` | CRITICAL | **Pre-Enrollment Candidate Reference (Applicant vs. Student):** `FIN-WF-13` and `GAP-01` require verifying payment for an accepted applicant *before* enrollment. However, accounts and obligations previously required `student_id NOT NULL`. | **CLOSED** | Adopted Dual Reference Pattern (GAP-07 precedent) supporting `applicant_id` and graduating to `student_id` upon enrollment. |
| `GAP-002` | CRITICAL | **Missing Submodule 3 Database Tables:** Tables `finance.payers`, `finance.payments`, and `finance.payment_allocations` and projection `v_student_receivables` are not yet created in SQL Server migrations. | **OPEN** | To be provisioned by Architecture / Implementation Slice. |
| `GAP-003` | MAJOR | **Missing Backend Vertical Slices:** No commands, queries, validators, handlers, or endpoints exist for: Payer creation/lookup, Payment submission/recording, Payment confirmation & receipt generation, Payment allocation/reallocation, or Ledger-backed `IsEnrollmentFeePaidAsync`. | **OPEN** | To be structured by Architecture in implementation plan. |
| `GAP-004` | MAJOR | **Missing Frontend Workspaces:** `SCR-FIN-02` (Payment Verification Queue) is completely missing. `SCR-SELF-02` lacks payment submission dialog and receipt viewer. `SCR-FIN-03` lacks payment/allocation transaction tabs. | **OPEN** | To be structured by Architecture in implementation plan. |
| `GAP-005` | MAJOR | **Payer Resolution Mechanism:** Need an explicit, minimalist mechanism to resolve `payer_id` when parents submit payments online vs. when cashier records counter payments. | **CLOSED** | Adopted Auto-Match & On-Demand Creation via NIK / email / phone lookup with manual sponsor creation. |
| `GAP-006` | CRITICAL | **Stubbed Enrollment Payment Gate:** `IFinanceModule.IsEnrollmentFeePaidAsync` returns static `false`, blocking student enrollment unless administratively waived. | **CLOSED** | Adopted Mandatory Obligation Settlement Check query in `FinanceModule`. |

---

# 4. Open Questions

| ID | Question | Impact | Status | Decision Summary |
|------|------|------|:---:|------|
| `OQ-001` | How should Finance model and store financial accounts, obligations, and payments for an accepted applicant prior to student enrollment? | Directly dictates table schema, foreign key constraints, account provisioning lifecycle, and query implementation of `IsEnrollmentFeePaidAsync`. | **CLOSED** | Dual Reference Pattern with nullable `applicant_id` and `student_id`, graduating account on `StudentEnrolledEvent`. |
| `OQ-002` | How should `payer_id` be resolved when a payment is submitted via self-service portal (`SCR-SELF-02`) or recorded manually by a Cashier (`SCR-FIN-02`)? | Determines payer identity lifecycle, deduplication rules, and data entry flow. | **CLOSED** | Auto-match by NIK/email/phone; auto-create `parent_guardian` if not found; manual sponsor entry for cashiers. |
| `OQ-003` | What is the allocation workflow mechanics upon payment confirmation (automatic rule-based allocation vs. cashier explicit allocation)? | Affects operator efficiency, partial payment handling, and complexity of allocation logic. | **CLOSED** | Explicit cashier allocation with 1-click Auto-Suggest (mandatory fees first, then oldest tuition FIFO). Excess funds remain unallocated on payment. |
| `OQ-004` | How should payment receipts be numbered and issued upon confirmation? | Determines numbering sequence format, concurrency safety, and voucher immutability. | **CLOSED** | Dual path: portal submissions enter as `submitted`; counter cash payments verified & confirmed atomically. Sequential `REC-{Year}-{Sequence:D6}` voucher generated on confirmation. |
| `OQ-005` | What exact criteria define "Enrollment Fee Paid" in `IsEnrollmentFeePaidAsync`? | Determines gate unblocking logic for student intake. | **CLOSED** | Mandatory Obligation Settlement Check: true if no mandatory enrollment obligations exist, or if all are fully settled (`outstanding_amount == 0`). |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| `ASM-001` | All monetary amounts are non-negative integers in Indonesian Rupiah (IDR). |
| `ASM-002` | An applicant accepted in `STD-WF-02` has an assessed mandatory `ENROLLMENT_FEE` obligation before `STD-WF-03` is executed. |
| `ASM-003` | Payer records are independent entities that may be associated with multiple students (e.g. parents with siblings or corporate sponsors). |
| `ASM-004` | Reallocations are auditable corrections performed through replacement records (`superseded_by_id`) without altering original allocation history. |
| `ASM-005` | Receivables status (overdue, settled, outstanding) is dynamically calculated via database view / Dapper projection, not maintained as a separate mutable column. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| `RISK-001` | Schema mismatch between pre-enrollment applicant obligations and post-enrollment student accounts causes orphaned records or failed intake. | High | Adopt dual reference pattern (`student_id` NULL / `applicant_id` NULL with CHECK constraint) matching established `GAP-07` precedent. |
| `RISK-002` | Concurrent allocation race condition causes over-allocation exceeding payment amount or obligation net liability. | High | Enforce database-level transactions with row-level locking (`UPDLOCK, HOLDLOCK`) and mathematical check assertions. |
| `RISK-003` | Duplicate payment receipt numbers generated during high-concurrency cashiering. | Medium | Enforce unique constraint on `receipt_number` with transactional sequence generation pattern. |
| `RISK-004` | Payer duplication occurs due to slight variations in parent name spelling. | Low | Provide searchable payer lookup with auto-match on phone/email/NIK. |

---

# 7. Recommendations

## Pre-Enrollment Account Reference Architecture (GAP-001 / OQ-001)

### Option A: Dual Reference Pattern (Matching GAP-07 in Academics) — Recommended
- Add nullable `applicant_id UNIQUEIDENTIFIER NULL` to `finance.student_financial_accounts`, `finance.financial_obligations`, and `finance.payments`.
- Apply table constraint: `CHECK ((student_id IS NOT NULL AND applicant_id IS NULL) OR (student_id IS NULL AND applicant_id IS NOT NULL))` or allow `student_id` to be populated later upon enrollment while retaining `applicant_id`.
- When an applicant is accepted, `student_financial_accounts` and `financial_obligations` are provisioned using `applicant_id`.
- When `StudentEnrolledEvent` fires, `StudentEnrolledEventHandler` updates the account: setting `student_id = @StudentId`.

#### Advantages
- **Pragmatic & Minimalist:** Uses the exact same single ledger and table structures for applicants and enrolled students.
- **Explicit:** Proven in the codebase (mirrors `GAP-07` referential integrity constraint).
- **Seamless Continuity:** Once enrolled, all historical admission payments, receipts, and allocations remain intact on the exact same student account.

#### Disadvantages
- Requires modifying migration scripts or adding an alter migration for `student_financial_accounts` and `financial_obligations` to support nullable `student_id` and optional `applicant_id`.

---

### Option B: Standalone Applicant Pre-Payment Table
- Keep `student_financial_accounts` strictly `student_id NOT NULL`.
- Create a separate `finance.applicant_payments` table for pre-enrollment fees.

#### Advantages
- Leaves `student_financial_accounts` strictly student-only.

#### Disadvantages
- Creates schema duplication, dual payment flows, and requires data copying upon enrollment. Violates minimalist and unified ledger principles.

---

# 8. Gap Closure

## GAP-001 / OQ-001: Pre-Enrollment Candidate Reference (Applicant vs. Student)

### Status
CLOSED

### Decision
Adopt the **Dual Reference Pattern** established by `GAP-07`:
1. Add nullable `applicant_id UNIQUEIDENTIFIER NULL` alongside `student_id UNIQUEIDENTIFIER NULL` across `finance.student_financial_accounts`, `finance.financial_obligations`, and `finance.payments`.
2. Enforce table-level check constraint on `finance.student_financial_accounts`:
   `CHECK (student_id IS NOT NULL OR applicant_id IS NOT NULL)`
   (allowing an account to be opened initially for an applicant, and subsequently updated with `student_id` upon enrollment while preserving `applicant_id` for historical audit).
3. Upon admission decision acceptance (`decision_type = 'accepted'`), Finance provisions the account using `applicant_id` and assesses the mandatory `ENROLLMENT_FEE` obligation.
4. When `StudentEnrolledEvent` fires upon enrollment execution (`STD-WF-03`), `StudentEnrolledEventHandler` updates the existing financial account, populating `student_id = @StudentId`.

### Rationale
- **Pragmatic & Minimalist:** Eliminates the need for duplicate "applicant ledger" or "pre-payment" tables while ensuring full transaction continuity from admission through graduation.
- **Explicit:** Mirrors the proven referential integrity mechanism implemented in `GAP-07` for applicant entrance assessments.
- **Single Source of Truth:** Preserves unified account numbering (`ACC-{Year}-{Prefix}`), payment vouchers, and allocation lineages without copying or migrating data.

### Impact
- Database migration script for Submodule 3 will include schema adjustments for `finance.student_financial_accounts` and `finance.financial_obligations` to support nullable `student_id` with `applicant_id`.
- `StudentEnrolledEventHandler` will update the existing account's `student_id` if an account already exists for `applicant_id`, rather than only inserting on `student_id`.

### Architecture Impact
- Requires ARCHITECTURE update to document the dual candidate foreign key relationship in the Finance schema specification and domain event handler contracts.

### Resolved By
Drury (User via /grill-me)

### Resolved Date
2026-11-04

---

## GAP-005 / OQ-002: Payer Resolution & Provisioning Strategy

### Status
CLOSED

### Decision
Adopt **Auto-Match & On-Demand Creation**:
1. When a payment is submitted via `SCR-SELF-02` (Family Portal) or recorded by a Finance Officer in `SCR-FIN-02`:
   - System searches `finance.payers` for an existing match using identification number (NIK/NPWP), contact email, or phone number.
   - If an existing payer matches, their `payer_id` is reused.
   - If no match exists, the system automatically creates a new `finance.payers` record (default `payer_type = 'parent_guardian'`) using the payer name and contact details provided in the submission.
2. In `SCR-FIN-02`, Finance Officers can also explicitly register, search, or update payers (including corporate sponsors, embassies, or foundations) with standard payer numbers (`PYR-{Year}-{Sequence:D4}`).
3. Default bank account details for refunds (`bank_name`, `bank_account_number`, `bank_account_name`) are captured on the payer record for future Stage 2 refund workflows.

### Rationale
- **Pragmatic & Minimalist:** Zero friction for parents submitting school fees online; avoids blocking payment intake with redundant administrative pre-registration steps.
- **Explicit:** Maintains the shared, independent `Payer` entity specified in Resolved Decision 4, allowing corporate sponsors and multi-student parents to remain cleanly centralized without data duplication.

### Impact
- Backend command handlers for payment submission (`SubmitPaymentCommand`) and counter payment recording (`RecordPaymentCommand`) will incorporate the `PayerResolver` service.
- Database table `finance.payers` will maintain indexes on `identification_number`, `email`, and `phone_number`.

### Architecture Impact
- Architectural specification will define the `IPayerResolver` service contract and payer deduplication rules within `Elsasa.Sis.Finance`.

### Resolved By
Drury (User via /grill-me)

### Resolved Date
2026-11-04

---

## OQ-004: Payment Submission, Verification, and Receipt Generation Lifecycle

### Status
CLOSED

### Decision
Adopt **Dual Submission Paths with Atomic Counter Confirmation**:
1. **Self-Service Portal Path (`SCR-SELF-02`, `UC-FIN-01`):**
   - Parents/payers submit payment information with external transaction references (bank transfer code, gateway reference) and upload documentary evidence.
   - Initial state is set to `confirmation_status = 'submitted'`, `receipt_number = NULL`.
   - The transaction enters the `SCR-FIN-02` Payment Verification Queue for Cashier review.
2. **Cashier / Counter Path (`SCR-FIN-02`, `UC-FIN-04`):**
   - Finance Officers recording verified payments (cash, POS EDC machine, verified bank clearance) can execute an atomic "Record & Confirm" action.
   - Status transitions immediately to `confirmation_status = 'confirmed'`, assigning the official `receipt_number` within the same transaction.
3. **Receipt Generation & Immutability:**
   - Upon confirmation, a sequential receipt code is assigned using the format `REC-{Year}-{Sequence:D6}` (e.g., `REC-2026-000108`) with concurrency-safe locking (`UPDLOCK, HOLDLOCK`) and an explicit database unique index.
   - Confirmed records permanently freeze payment monetary amounts and capture `receipt_issued_at` and `confirmed_by`.
   - Rejections set `confirmation_status = 'rejected'` and require entering a mandatory `rejection_reason`.

### Rationale
- **Pragmatic & Minimalist:** Streamlines physical cashier counter operations by removing an artificial second click for transactions already verified in person, while strictly enforcing verification queue rigor for unverified remote portal submissions.
- **Explicit:** Implements the domain rule distinguishing submission from confirmation, with explicit timestamps, actors, and voucher numbering.

### Impact
- Backend will provide two dedicated command handlers:
  - `SubmitPaymentCommand`: For self-service family submissions (`submitted` state).
  - `ConfirmPaymentCommand` and `RecordAndConfirmPaymentCommand`: For cashier operations (`confirmed` state with receipt numbering).
- `finance.payments` table enforces `UNIQUE (receipt_number)` where `receipt_number IS NOT NULL`.

### Architecture Impact
- Architectural specification will define the state machine for `payments.confirmation_status` (`submitted` → `confirmed` / `rejected` → `reversed`).

### Resolved By
Drury (User via /grill-me)

### Resolved Date
2026-11-04

---

## OQ-003: Payment Allocation Mechanics & Excess Funds Handling

### Status
CLOSED

### Decision
Adopt **Explicit Allocation with Smart Auto-Suggest**:
1. **Explicit Allocation Model (`SCR-FIN-02`, `UC-FIN-05`):**
   - Allocations are explicitly confirmed by the Finance Officer, preventing unintended automated misallocations.
   - The UI provides an intelligent "Auto-Suggest Allocation" feature that calculates proposed line-item distributions according to school policy priorities:
     1. Mandatory admission/enrollment fee obligations (unblocking intake).
     2. Outstanding mandatory tuition obligations ordered by oldest due date (FIFO).
     3. Non-mandatory or upcoming period fees.
   - The Finance Officer can modify any proposed amount before committing.
2. **Cardinality Support:**
   - One-to-one (single payment settles single obligation).
   - One-to-many (split payment across multiple obligations).
   - Many-to-one (multiple partial payments settling a single obligation).
   - Partial settlements (reducing `outstanding_amount` on `v_student_receivables`).
3. **Excess / Unallocated Funds Handling:**
   - Unallocated value is dynamically computed on each payment as `payment_amount - SUM(allocated_amount WHERE status = 'active')`.
   - Remaining unallocated funds remain available on the student financial account for future obligation settlement without requiring separate Stage 2 `credit_balances` tables in Stage 1.
4. **Reallocation & Correction Protocol (`FIN-WF-06`):**
   - Correcting an erroneous allocation creates new replacement `payment_allocations` rows and marks prior rows as `status = 'superseded'` and `superseded_by_id = @NewAllocationId`.
   - A mandatory audit justification (`reversal_reason`) must be supplied. Historical allocation records are never physically updated or deleted.

### Rationale
- **Pragmatic & Minimalist:** High cashier productivity via smart auto-fill while maintaining strict human-in-the-loop verification. Avoids prematurely implementing Stage 2 credit ledger tables while cleanly accounting for unallocated money.
- **Explicit:** Satisfies the core domain principle: distinct payment confirmation, explicit allocation, and immutable audit history.

### Impact
- Backend command handler `AllocatePaymentCommand` enforces transactional row-level locks on obligations and payments, asserting that the total active allocations do not exceed the payment amount or the net liability of any obligation.
- Dynamic view `v_student_receivables` aggregates active allocations (`status = 'active'`) to compute settled and outstanding amounts.

### Architecture Impact
- Architectural specification will define the allocation validation invariants and reallocation state transition graph.

### Resolved By
Drury (User via /grill-me)

### Resolved Date
2026-11-04

---

## GAP-006 / OQ-005: Enrollment Payment Gate Inquiry Evaluation

### Status
CLOSED

### Decision
Adopt **Mandatory Obligation Settlement Check**:
1. `IFinanceModule.IsEnrollmentFeePaidAsync(applicantId, academicYearId)` executes the following evaluation:
   - Identify the financial account for `applicantId` in `finance.student_financial_accounts`.
   - Query all active financial obligations (`status != 'cancelled'`) linked to this account/applicant with `charge_type IN ('enrollment_fee', 'admission_fee')` and `is_mandatory = 1` for the specified `academicYearId`.
   - **Case 1 (No mandatory enrollment obligations):** If no mandatory enrollment obligations are assessed for this applicant, return `Result.Success(true)` (no fee gate required).
   - **Case 2 (Mandatory enrollment obligations exist):** Return `Result.Success(true)` **if and only if** every mandatory enrollment obligation is completely settled (`outstanding_amount == 0` via confirmed active allocations).
   - **Case 3 (Unsettled or partially settled obligations):** Return `Result.Success(false)`, maintaining the hard block on `STD-WF-03: Enroll Accepted Applicant` until fees are confirmed paid or an authorized administrative override is recorded.
2. In accordance with `GAP-01`, `SCR-STU-01` (`EnrollApplicantModal.vue`) retains the ability for an authorized School Leadership / Bursar role to execute an administrative override by supplying a mandatory justification string (`waiverReference`), which is audited in `students.enrollments`.

### Rationale
- **Pragmatic & Minimalist:** Relies directly on the single-source-of-truth obligation and allocation records without creating synthetic clearance tables or redundant gate-tracking flags.
- **Explicit:** Perfectly aligns with `GAP-01` ("Hard block by default with explicit administrative override") and `FIN-WF-13`.

### Impact
- Replaces the stub `FinanceModule.IsEnrollmentFeePaidAsync` with an efficient, parameterized SQL query executed via `ISqlConnectionFactory`.
- Guarantees seamless unblocking of `STD-WF-03` intake immediately upon cashier payment confirmation and allocation in `SCR-FIN-02`.

### Architecture Impact
- Architectural specification will document the query contract and test specification for `IFinanceModule.IsEnrollmentFeePaidAsync`.

### Resolved By
Drury (User via /grill-me)

### Resolved Date
2026-11-04

---

# 9. Architecture Applicability

## Decision
ARCHITECTURE-REQUIRED

## Rationale
Implementing Stage 1 Payments & Receivables introduces:
1. New core database tables (`payers`, `payments`, `payment_allocations`) and dynamic view `v_student_receivables`.
2. Cross-domain query resolution for `IFinanceModule.IsEnrollmentFeePaidAsync`.
3. Account lifecycle extension supporting pre-enrollment applicant obligations.
4. New vertical slice endpoints, commands, queries, and UI workspaces.

---

# 10. Planning Readiness

## Readiness Checklist
- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated

## Status
READY-FOR-PLANNING

## Notes
All critical gaps (`GAP-001`, `GAP-005`, `GAP-006`) and design questions (`OQ-001` through `OQ-005`) have been formally resolved through the structured `/grill-me` session. Status is maintained as `NOT-READY` pending handoff to the Architect (`ica-architect`), who will review the decisions and grant `READY-FOR-PLANNING` before creating the ARCHITECTURE artifact and slice plan.

---

# 11. References

Referenced artifacts:
- [finance-domain.md](file:///d:/Project.Private/elsasa-sis-v3/docs/domains/finance-domain.md)
- [finance-entities.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/finance-entities.md)
- [finance-entities-tables.md](file:///d:/Project.Private/elsasa-sis-v3/docs/entities/finance-entities-tables.md)
- [operational-workflow-discovery.md](file:///d:/Project.Private/elsasa-sis-v3/docs/operational-workflow-discovery.md)
- [use-cases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/use-cases.md)
- [actors.md](file:///d:/Project.Private/elsasa-sis-v3/docs/actors.md)
- [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v3/docs/screen-inventory.md)
- [sub-modul-and-development-phases.md](file:///d:/Project.Private/elsasa-sis-v3/docs/sub-modul-and-development-phases.md)
- [IS007 - FIN-PAYMENTS-RECEIVABLES-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS007%20-%20FIN-PAYMENTS-RECEIVABLES-ISSUE.md)

Referenced codebase locations:
- `src/backend/Elsasa.Sis.Finance/`
- `src/backend/Elsasa.Sis.Contracts/Finance/IFinanceModule.cs`
- `src/backend/Elsasa.Sis.Students/Features/Enrollment/EnrollAcceptedApplicant/EnrollAcceptedApplicantHandler.cs`
- `src/frontend/src/features/finance/`
- `src/frontend/src/features/portal/FamilyTasksView.vue`
- `src/frontend/src/features/students/admission/components/EnrollApplicantModal.vue`
