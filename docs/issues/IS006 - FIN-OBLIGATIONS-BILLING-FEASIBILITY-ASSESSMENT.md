---
Title: Feasibility Assessment - Stage 1 Obligations & Billing Full Scope
Code: FIN-OBLIGATIONS-BILLING
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-04
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Feature being assessed: Stage 1 "Obligations & Billing — full scope: Financial Policy & Charge Rules, Student Financial Account, Obligation Assessment & Issuance, Billing & Statement Communication" under the Finance domain, as formally captured in change request intake `CR-FIN-OBLIGATIONS-BILLING` (`docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-ISSUE.md`).

Referenced artifacts:
- **ISSUE:** `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-ISSUE.md`
- **DOMAIN BLUEPRINT:** `docs/domains/finance-domain.md` (Sections: Financial Policy and Charge Rules lines 58–91; Student Financial Account lines 93–115; Financial Obligation and Charge lines 116–138; Billing and Statements lines 139–155; Due Schedules lines 157–183; Indonesian School Context & Operational Principles)
- **ROADMAP & CRITERIA:** `docs/sub-modul-and-development-phases.md` (Stage 1 Module: Obligations & Billing; Criteria 6, 7, 10; Section E GAP-01 & GAP-06 resolutions)
- **WORKFLOWS & USE CASES:** `docs/operational-workflow-discovery.md` (`FIN-WF-01: Define or Adopt Financial Policy`, `FIN-WF-02: Assess and Issue Financial Obligation`, `FIN-WF-04: Issue Bill or Financial Statement`, `FIN-WF-13: Confirm Enrollment Payment Gate`; Section 6 GAP-06 Resolution), `docs/use-cases.md` (`UC-FIN-10: Establish Financial Policy`, `UC-FIN-02: Assess Financial Obligations`, `UC-FIN-03: Issue Billing Statement`), `docs/actors.md` (Consolidated Finance Officer, Finance Authority, Zero Discretionary Spending)
- **DATA SPECIFICATIONS:** `docs/entities/finance-entities.md` (Submodule 1 — Obligations & Billing; Resolved Decisions 1, 2, 5, 6, 7; Assumptions), `docs/entities/finance-entities-tables.md` (Submodule 1: `financial_policies`, `charge_rules`, `student_financial_accounts`, `financial_obligations`, `obligation_due_schedules`, `billing_documents`, `billing_document_items`, lines 23–270)
- **SCREEN INVENTORY:** `docs/screen-inventory.md` (`SCR-FIN-04: Financial Policy Workspace`, `SCR-FIN-01: Billing Cycle Workspace`, `SCR-FIN-03: Student Accounts Workbench (Stage 1 initial scope)`, `SCR-SELF-02: Family Tasks & Requests Portal`)
- **ARCHITECTURE CONSTRAINTS:** `docs/architecture/adr.md` (ADR-001 through ADR-010)

## Objective

Assess the technical, architectural, and operational feasibility, system impact, persistence schema requirements, invariant enforcement mechanisms, query projection requirements, and implementation readiness for delivering the Stage 1 Obligations & Billing submodules.

This assessment strictly embodies a **Pragmatic**, **Minimalist**, and **Explicit** approach to reconcile the existing baseline codebase with authoritative specifications, resolving all architectural design decisions through a structured collaborative interview.

---

# 2. Current State

## Existing Behavior

1. **Backend Implementation (`Elsasa.Sis.Finance`):**
   - The Finance assembly contains only `FinanceAssemblyMarker.cs`, `DependencyInjection.cs`, and a stub query service `FinanceModule.cs`.
   - `FinanceModule.IsEnrollmentFeePaidAsync` always returns `Result.Success(false)` by default, intentionally keeping the enrollment payment gate closed until actual Finance ledger tables are provisioned.
   - There are zero domain models, MediatR handlers, vertical slice endpoints, or business rules for creating/approving policies, configuring charge rules, maintaining student financial accounts, assessing obligations, or compiling billing documents.
   - `Elsasa.Sis.Contracts` defines domain event records `ObligationAssessedEvent` and `PaymentConfirmedEvent`, but neither is emitted or consumed by any active handler.

2. **Database Persistence (`Elsasa.Sis.Database`):**
   - Migration `20261101_0301_Finance_Policies.sql` provisioned an empty, rudimentary stub table `finance.policies` with only `[id]`, `[academic_year_id]`, `[policy_code]`, `[name]`, `[is_active]`, `[created_at]`, and `[created_by]`.
   - None of the 7 authoritative tables defined in `docs/entities/finance-entities-tables.md` for Submodule 1 exist:
     - `finance.financial_policies`
     - `finance.charge_rules`
     - `finance.student_financial_accounts`
     - `finance.financial_obligations`
     - `finance.obligation_due_schedules`
     - `finance.billing_documents`
     - `finance.billing_document_items`
   - Existing enrolled students in `students.students` (provisioned via `CR-STU-ENROLLMENT-IDENTITY` and `CR-STU-PLACEMENT-STATUS`) do not possess corresponding ledger accounts in Finance.

3. **Frontend Implementation (`src/frontend`):**
   - `src/frontend/src/features/finance/obligations/views/ObligationsListView.vue` is a static placeholder shell returning `<p>Student Obligations & Billing</p>`.
   - `src/frontend/src/app/router/routes.ts` maps only `/finance/obligations` to this placeholder and `/finance/payments` to `PaymentDeskView.vue`.
   - None of the required Stage 1 Finance UI workspaces exist:
     - `SCR-FIN-04`: Financial Policy Workspace (Policy & charge rules governance for Finance Authority)
     - `SCR-FIN-01`: Billing Cycle Workspace (Batch assessment, exception validation, statement issuance for Finance Officer)
     - `SCR-FIN-03`: Student Accounts Workbench (Initial inquiry, account balance summary, ad-hoc charges)
     - `SCR-SELF-02`: Family Tasks & Requests Portal (Billing & invoice viewing tab for parents/payers)

## Existing Constraints

1. **Modular Monolith Architecture (ADR-001):** `Elsasa.Sis.Finance` cannot reference other domain assemblies (`Students`, `Operations`). All cross-domain collaboration occurs via `Elsasa.Sis.Contracts` or asynchronous domain events.
2. **Database Schema Isolation (ADR-002):** Schema isolation between `finance`, `students`, and `operations` is mandatory. Logical references (`student_id`, `academic_period_id`) are stored as explicit UUIDs without physical cross-schema foreign keys. Intra-schema foreign keys within `finance` must be fully enforced. Read JOINs across schemas are permitted strictly in CQRS query slices.
3. **Cross-Domain Communication (ADR-003):** Domain events (e.g. `ObligationAssessedEvent`, `StudentEnrolledEvent`) published via MediatR must be triggered strictly **after** primary database transactions commit.
4. **Data Access & Endpoint Patterns (ADR-004, ADR-005):** Explicit Dapper queries using C# raw string literals, `Result<T>` domain outcomes, and native Minimal APIs with FluentValidation endpoint filters.
5. **Monetary Precision & Base Currency (Resolved Decision 6):** All monetary amounts must be stored as `BIGINT` in Indonesian Rupiah (IDR, zero decimal places), accompanied by an explicit `currency_code VARCHAR NOT NULL DEFAULT 'IDR'` column.
6. **Student-Centric Ledger Anchor (Resolved Decision 5):** Financial accounts must maintain a strict 1:1 relationship with `students.students`. Account balance must not be an independently editable column; it is dynamically derived from obligations, payments, credits, and adjustments.
7. **Zero-Discretionary Spending (ADR-009, Actors Resolution 3.2):** Standard fee assessment and statement issuance are routine operations executed by the `Finance Officer`. However, policy creation, policy versioning, and formal approvals require the `Finance Authority`.
8. **Stage 1 Scope Boundary:** Payments, allocations, receipts, receivables overdue management, payment arrangements, discounts, scholarships, waivers, refunds, and financial clearance belong to companion Stage 1 issues or Stages 2/3 and must not be conflated with Obligations & Billing.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | **Missing Persistence Schema for Obligations & Billing:** The rudimentary stub table `finance.policies` is obsolete and none of the 7 Submodule 1 tables specified in `docs/entities/finance-entities-tables.md` (`financial_policies`, `charge_rules`, `student_financial_accounts`, `financial_obligations`, `obligation_due_schedules`, `billing_documents`, `billing_document_items`) exist. |
| GAP-002 | CRITICAL | **Missing Student Financial Account Lifecycle & Backfill:** No mechanism exists to provision a `student_financial_accounts` record when an applicant is enrolled (`STD-WF-03` / `StudentEnrolledEvent`), and existing enrolled students have no financial ledger account. |
| GAP-003 | CRITICAL | **Missing Obligation Assessment Engine (`FIN-WF-02`, `UC-FIN-02`):** No backend slice exists to assess and issue financial obligations, calculate due dates, enforce duplicate prevention, record audit logs, attach due schedules, or emit `ObligationAssessedEvent`. |
| GAP-004 | CRITICAL | **Missing Due Date Calculation & Due Schedules Model:** No calculation logic exists to derive settlement due dates from charge rule parameters (`days_after_issuance`, `fixed_day_of_month`, `fixed_calendar_date`) and populate initial `obligation_due_schedules` records. |
| GAP-005 | CRITICAL | **Missing Billing Document Compilation & Itemization (`FIN-WF-04`, `UC-FIN-03`):** No backend slice exists to compile outstanding obligations into an authorized point-in-time bill or periodic financial statement, freeze line items into `billing_document_items`, resolve recipient contacts from Students, and track delivery status. |
| GAP-006 | MAJOR | **Missing Frontend Workspaces per Screen Inventory (`SCR-FIN-04`, `SCR-FIN-01`, `SCR-FIN-03`):** The frontend has only a stub `ObligationsListView.vue`. Dedicated workspaces for policy governance (`SCR-FIN-04`), billing cycles (`SCR-FIN-01`), and student accounts (`SCR-FIN-03`) are missing. |
| GAP-007 | MAJOR | **Missing Initial GAP-06 Core Charge Types Seed Data:** The system lacks seeded active financial policies and charge rules for `APPLICATION_FEE`, `ENROLLMENT_FEE`, and `TUITION_FEE`, preventing immediate out-of-the-box fulfillment of Stage 1 Completion Criterion 6. |
| GAP-008 | MINOR | **Missing Finance Domain Permissions Hierarchy:** Permissions for Finance policies, obligations, billing, and accounts are not yet defined in `Elsasa.Sis.Finance/Permissions.cs`. |

---

# 4. Open Questions

| ID | Question | Impact | Status |
|---|---|---|---|
| OQ-001 | How should the database persistence schema and migration strategy be structured for Stage 1 Obligations & Billing, given the existing stub 'finance.policies'? | Governs database migration script design, table structures, and data integrity. | CLOSED |
| OQ-002 | How should 'finance.student_financial_accounts' be provisioned and maintained for new and existing students? | Governs event handling, cross-domain coordination with Students enrollment, and existing student account continuity. | CLOSED |
| OQ-003 | How should the Obligation Assessment engine be designed to support both bulk billing cycles (SCR-FIN-01) and single student charges? | Governs API endpoint structure, candidate filtering, preview-and-post workflow, and duplicate prevention. | CLOSED |
| OQ-004 | How should due date calculation and initial 'obligation_due_schedules' generation be handled during obligation assessment? | Governs rule evaluation, due schedule generation, and milestone tracking. | CLOSED |
| OQ-005 | How should Billing Document compilation, itemization, and communication (FIN-WF-04, UC-FIN-03) be implemented? | Governs unified document modeling, frozen line-item preservation, recipient resolution, and multi-channel delivery status. | CLOSED |
| OQ-006 | How should the frontend workspaces and routes be organized to satisfy SCR-FIN-04, SCR-FIN-01, and SCR-FIN-03? | Governs Vue component architecture, route configuration, and user workflow separation. | CLOSED |
| OQ-007 | How should the initial GAP-06 core charge types (Application Fee, Enrollment Fee, Tuition Fee) be provisioned? | Governs initial seed configuration and Stage 1 Completion Criterion 6 readiness. | CLOSED |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | All monetary calculations in Stage 1 operate in Indonesian Rupiah (IDR) using exact `BIGINT` integer representation (zero decimal places). |
| ASM-002 | The active academic period context is supplied by Operations (`operations.academic_years` and `operations.operational_periods`) via UUID logical references. |
| ASM-003 | The primary financial recipient for billing documents is resolved from `students.student_guardian_relationships` (`is_financial_guarantor = 1` or `is_primary_contact = 1`), falling back to the student record if unassigned. |
| ASM-004 | In Stage 1, billing documents delivered via `portal` or `printed` are immediately inspectable on-screen; external email transmission records delivery status (`issued`, `sent`) without requiring live external SMTP gateway infrastructure during development or testing. |
| ASM-005 | A student's financial account balance is strictly a dynamically derived calculation (Total Assessed Obligations - Total Allocated Payments + Credits/Adjustments); no denormalized editable balance column is maintained on `student_financial_accounts`. |
| ASM-006 | Financial policies and charge rules must be in `active` status before they can be utilized by the obligation assessment engine. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Duplicate obligation assessment for the same student, charge rule, and billing period during repeated billing cycle runs. | High | Enforce database-level uniqueness or query-level candidate filtering checking existing obligations for `(student_account_id, charge_rule_id, academic_period_id)` or cycle label prior to insertion. |
| RISK-002 | Account desynchronization for students enrolled prior to Submodule 1 deployment. | High | Include an idempotent SQL backfill script in migration `20261104_0201` to provision `student_financial_accounts` for all existing students in `students.students`, plus an auto-provisioning fallback on ad-hoc assessment. |
| RISK-003 | Reissuing or modifying billing documents rewriting underlying financial transaction history. | High | Treat `billing_documents` and `billing_document_items` as frozen, append-only point-in-time communication snapshots; never update historical item amounts when reissuing. |
| RISK-004 | Premature infrastructure overhead from background cron schedulers for monthly billing. | Medium | Adopt on-demand, planned billing cycles executed by Finance Officers via `SCR-FIN-01` (Preview -> Validate Exceptions -> Post Obligations) per `screen-inventory.md`. |
| RISK-005 | Cross-schema write leakage between Finance and Students domains. | High | Enforce strict ADR-001/ADR-002 boundaries: Finance writes strictly to `finance.*`; read-only JOINs to `students.*` are restricted to query projection handlers. |

---

# 7. Recommendations

## Evaluated Architectural Approaches

### Approach 1: Pragmatic, Minimalist, and Explicit Domain Alignment (Selected)
- **Database Schema:** Deliver migration `20261104_0201_Finance_ObligationsBilling_Tables.sql` that cleanly drops the empty stub `finance.policies` and provisions all 7 Submodule 1 tables:
  1. `finance.financial_policies`: Policy governance, versions, effective dates, approval authority.
  2. `finance.charge_rules`: Concrete charging criteria, base amounts (`BIGINT` IDR), recurrence patterns, due rule parameters.
  3. `finance.student_financial_accounts`: 1:1 student ledger anchor (`account_number`, `account_status`, `opened_date`).
  4. `finance.financial_obligations`: Authoritative liability records (`obligation_number`, `original_amount`, `due_date`, `status`, `authorized_by`).
  5. `finance.obligation_due_schedules`: Initial installment/due date schedule attached upon issuance.
  6. `finance.billing_documents`: Unified point-in-time bill and periodic statement header (`document_type`, `recipient_name`, `total_billed_amount`, `delivery_status`).
  7. `finance.billing_document_items`: Frozen snapshot line items.
- **Initial Seed Data:** Idempotent seed script within migration `20261104_0201` establishing an approved policy `FP-2026-001` and 3 core charge rules per GAP-06 (`APPLICATION_FEE`, `ENROLLMENT_FEE`, `TUITION_FEE`).
- **Account Provisioning:** Asynchronous event handler `StudentEnrolledEventHandler` in Finance reacting to `StudentEnrolledEvent` (ADR-003) to provision `student_financial_accounts`, backed by migration backfill for existing students and auto-provisioning fallback.
- **Assessment Engine:** Dual-mode assessment:
  - Batch Billing Cycle (`POST /api/finance/obligations/batch-assess`): Filters candidate students by rule criteria, previews assessable charges, validates duplicate exceptions, and posts obligations in bulk.
  - Single-Student Ad-Hoc (`POST /api/finance/obligations`): Direct charge creation for ad-hoc or individual assessments.
  - Both modes compute due dates from rule parameters, attach sequence-1 `obligation_due_schedules`, and emit `ObligationAssessedEvent`.
- **Billing Document Engine:**
  - Batch invoice generation (`POST /api/finance/billing-documents/generate`): Compiles open obligations for a student/cycle into a `bill`, freezes line items in `billing_document_items`, resolves primary guardian contact, and marks delivery status `issued`.
  - Statement generation (`POST /api/finance/billing-documents/statement`): Compiles periodic ledger statement.
  - Document query endpoints (`GET /api/finance/billing-documents/{id}`, `GET /api/finance/billing-documents`) for on-screen viewing, printing, and portal display.
- **Frontend Architecture:** Scaffold three explicit feature workspaces in `src/frontend/src/features/finance/`:
  - `policies/views/FinancialPolicyWorkspaceView.vue` (`SCR-FIN-04` at `/finance/policies`)
  - `billing/views/BillingCycleWorkspaceView.vue` (`SCR-FIN-01` at `/finance/billing-cycles`)
  - `accounts/views/StudentAccountsWorkbenchView.vue` (`SCR-FIN-03` at `/finance/accounts`)
  - Add billing statement inspection component in `SCR-SELF-02` (Family Portal).

### Approach 2: Monolithic Single View & Background Cron Daemon (Rejected)
- Attempt to automate monthly tuition generation via a background Quartz.NET/cron daemon and combine all policies, billing cycles, and account queries into a single Vue component.
- **Verdict:** Rejected. Adds unnecessary background worker infrastructure for Stage 1; violates the documented human-in-the-loop operational rhythm ("Billing is a planned cycle... SCR-FIN-01 is the periodic billing cycle: assess obligations, review exceptions, issue statements"); single-screen UI becomes bloated and unmaintainable.

### Approach 3: Full-Domain 25-Table Schema Provisioning Upfront (Rejected)
- Provision all 5 Finance submodules (Benefits, Payments, Clearance, Refunds, Adjustments) in a single massive database script.
- **Verdict:** Rejected. Violates phased implementation discipline; introduces premature unexercised tables that risk schema churn before Stage 2/3 requirements are fully analyzed.

---

# 8. Gap Closure

## GAP-001: Missing Persistence Schema for Obligations & Billing

### Decision
Deliver migration `src/backend/Elsasa.Sis.Database/Scripts/20261104_0201_Finance_ObligationsBilling_Tables.sql` that drops the unused empty stub `finance.policies` and provisions all 7 explicit Submodule 1 tables: `financial_policies`, `charge_rules`, `student_financial_accounts`, `financial_obligations`, `obligation_due_schedules`, `billing_documents`, and `billing_document_items` exactly matching `docs/entities/finance-entities-tables.md`.

### Rationale
Provides clean, authoritative storage for financial policies, charge rules, student accounts, obligations, due schedules, and billing documents with native SQL Server types, `BIGINT` IDR monetary precision, intra-schema foreign keys, and logical UUID cross-schema references per ADR-002.

### Impact
Defines the authoritative database foundation for the Finance domain.

### Architecture Impact
Requires new DbUp migration script in `Elsasa.Sis.Database`.

### Resolved By
Drury & Antigravity (Collaborative Alignment)

### Resolved Date
2026-11-04

---

## GAP-002: Missing Student Financial Account Lifecycle & Backfill

### Decision
Implement `StudentEnrolledEventHandler` in `Elsasa.Sis.Finance` handling `StudentEnrolledEvent` (ADR-003) to provision `finance.student_financial_accounts` with status `active` and deterministic account number format `ACC-{Year}-{StudentNumber/Seq}`. Complement with an idempotent `INSERT ... SELECT` backfill in migration `20261104_0201` for existing students, and an auto-provisioning fallback on ad-hoc obligation assessment.

### Rationale
Guarantees the strict 1:1 relationship between students and financial accounts (Resolved Decision 5) across both real-time intake and legacy/migrated records without coupling write operations across domain boundaries.

### Impact
Every enrolled student is guaranteed an authoritative financial ledger account.

### Architecture Impact
Requires MediatR notification handler in `Elsasa.Sis.Finance` and backfill in migration `20261104_0201`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-003: Missing Obligation Assessment Engine (`FIN-WF-02`, `UC-FIN-02`)

### Decision
Implement dual-mode assessment vertical slices:
1. `BatchAssessObligations` (`POST /api/finance/obligations/batch-assess`): Filters eligible students by academic placement, previews candidates and net charge values, validates against existing obligations, and posts obligations in bulk.
2. `AssessAdHocObligation` (`POST /api/finance/obligations`): Assesses an individual charge for a specific student account.
Both modes enforce immutable audit logging (`actor_id`, timestamp, charge payload) and publish `ObligationAssessedEvent`.

### Rationale
Directly fulfills `SCR-FIN-01 Billing Cycle Workspace` requirements and `UC-FIN-02`, supporting both bulk termly billing and individual ad-hoc charges while preventing duplicate charges.

### Impact
Enables Finance Officers to assess core fees (application, enrollment, tuition) against student accounts.

### Architecture Impact
New feature slices under `Elsasa.Sis.Finance/Features/Obligations/`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-004: Missing Due Date Calculation & Due Schedules Model

### Decision
Calculate obligation `due_date` automatically from charge rule parameters (`days_after_issuance`, `fixed_day_of_month`, `fixed_calendar_date`) during assessment, while allowing manual override on ad-hoc charges. Automatically generate initial `obligation_due_schedules` records (defaulting to 100% in installment sequence 1, or proportional installment intervals when defined on the rule).

### Rationale
Maintains explicit due date determinism without manual calculation errors and provides the structural basis for future overdue tracking and installment management.

### Impact
Every assessed obligation is born with an authoritative settlement timetable.

### Architecture Impact
Domain calculation logic in `Elsasa.Sis.Finance/Features/Obligations/`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-005: Missing Billing Document Compilation & Itemization (`FIN-WF-04`, `UC-FIN-03`)

### Decision
Implement unified billing document vertical slices:
1. `GenerateBillingDocument` (`POST /api/finance/billing-documents/generate`): Compiles open obligations for a student/cycle into a `bill`, resolves primary guardian from Students guardian relationships, and freezes itemized entries in `billing_document_items`.
2. `GenerateStatement` (`POST /api/finance/billing-documents/statement`): Compiles periodic financial statement across a date range.
3. Query slices (`GET /api/finance/billing-documents`, `GET /api/finance/billing-documents/{id}`): Expose structured invoice snapshots for on-screen viewing, printing, and family portal access.

### Rationale
Satisfies Resolved Decision 1 (Unified `Billing Document` model) and Stage 1 Completion Criterion 7 ("A bill or statement communicating obligations is produced for an authorized recipient"). Preserves immutable financial snapshots without rewriting historical items.

### Impact
Provides parents, payers, and officers with clear, auditable point-in-time invoices and periodic statements.

### Architecture Impact
New feature slices under `Elsasa.Sis.Finance/Features/Billing/`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-006: Missing Frontend Workspaces per Screen Inventory (`SCR-FIN-04`, `SCR-FIN-01`, `SCR-FIN-03`)

### Decision
Scaffold three dedicated feature workspaces under `src/frontend/src/features/finance/`:
1. `SCR-FIN-04` at `/finance/policies`: `FinancialPolicyWorkspaceView.vue` for policy and charge rule configuration, versioning, and formal approval by Finance Authority.
2. `SCR-FIN-01` at `/finance/billing-cycles`: `BillingCycleWorkspaceView.vue` for running guided billing cycles (Select Rule/Period -> Preview Candidates -> Assess & Post -> Generate Bills).
3. `SCR-FIN-03` at `/finance/accounts`: `StudentAccountsWorkbenchView.vue` for student account inquiry, balance cards, obligations list, ad-hoc fee modal, and billing document viewing modal.
4. Update router configuration in `src/frontend/src/app/router/routes.ts` with explicit permissions and titles.

### Rationale
Aligns 100% with the authoritative `screen-inventory.md`, separating planning rhythms (`SCR-FIN-04`, `SCR-FIN-01`) from continuous account workbenches (`SCR-FIN-03`) with clear actor boundaries.

### Impact
Delivers comprehensive, intuitive UI for Finance Authorities, Finance Officers, and families.

### Architecture Impact
New Vue views, components, API client services, and route definitions.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-007: Missing Initial GAP-06 Core Charge Types Seed Data

### Decision
Include idempotent seed data in migration `20261104_0201` creating:
- Active Financial Policy `FP-2026-001` ("Kebijakan Biaya Pendidikan TP 2026/2027", category `tuition`, version `1.0`, status `active`, approved by `Direktur Keuangan`).
- Three charge rules per GAP-06:
  1. `CR-2026-APP` (`APPLICATION_FEE`, Rp 500.000, one_time, days_after_issuance = 14)
  2. `CR-2026-ENR` (`ENROLLMENT_FEE`, Rp 15.000.000, one_time, days_after_issuance = 14)
  3. `CR-2026-SPP-SD` (`TUITION_FEE`, Rp 750.000, monthly, fixed_day_of_month = 10, applicable to SD Reguler)

### Rationale
Guarantees immediate out-of-the-box readiness for testing and satisfies Stage 1 Completion Criterion 6 without requiring manual bootstrap steps.

### Impact
Enables automated and manual integration tests to assess obligations immediately upon migration execution.

### Architecture Impact
Included in DbUp migration script `20261104_0201`.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

## GAP-008: Missing Finance Domain Permissions Hierarchy

### Decision
Define authoritative permission constants in `Elsasa.Sis.Finance/Permissions.cs`:
- Policies: `Permissions.Finance.Policies.View`, `Permissions.Finance.Policies.Manage`, `Permissions.Finance.Policies.Approve`
- Obligations: `Permissions.Finance.Obligations.View`, `Permissions.Finance.Obligations.Assess`
- Billing: `Permissions.Finance.Billing.View`, `Permissions.Finance.Billing.Generate`
- Accounts: `Permissions.Finance.Accounts.View`
Assign policy approval to `Finance Authority` and billing assessments to `Finance Officer`.

### Rationale
Enforces ADR-009 policy-based authorization and actor separation of duties.

### Impact
Protects sensitive financial mutation endpoints with explicit claims.

### Architecture Impact
New `Permissions.cs` in `Elsasa.Sis.Finance` and registration in authorization policies.

### Resolved By
Drury & Antigravity

### Resolved Date
2026-11-04

---

# 9. Architecture Applicability

**Architecture Applicability Decision:** `ARCHITECTURE-REQUIRED`

**Criteria Assessment:**
- [x] **New or changed components:** Introduces core Finance domain services, MediatR event handlers, and vertical slice Minimal API endpoints.
- [x] **New integration boundaries:** Formalizes event-driven consumption of `StudentEnrolledEvent` from Students domain and read-only projection joins against `students.students` and `students.guardians`.
- [x] **Persistence/data ownership changes:** Provisions 7 new database tables in schema `finance`, establishes strict 1:1 student financial account invariants, and defines frozen snapshot structures for billing documents.
- [x] **Significant technical decisions:** Explicit rule-driven due date engine, dual-mode assessment (batch cycle vs ad-hoc), unified billing document itemization, and integer IDR monetary precision.
- [x] **Architectural constraints:** ADR-001 through ADR-010 adherence (schema isolation, raw string literal Dapper queries, Result pattern, JWT authorization).

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps (GAP-001 through GAP-008) have been identified and formally analyzed.
- [x] All blocking open questions (OQ-001 through OQ-007) have been resolved with explicit decisions and rationales.
- [x] Alignment with domain blueprint (`finance-domain.md`), entity definitions (`finance-entities.md`), and database tables (`finance-entities-tables.md`) has been fully validated.
- [x] Cross-domain interactions and stage boundaries with Students (`CR-STU-ENROLLMENT-IDENTITY`, `CR-STU-PLACEMENT-STATUS`) and Operations (`CR-OPS-CALENDAR-PERIODS`) are explicitly preserved.
- [x] Stage 1 Completion Criteria (6, 7, 10) are directly supported and achievable.
- [x] Architectural applicability has been established as `ARCHITECTURE-REQUIRED`.

## Determination

**Status:** `READY-FOR-PLANNING`

All feasibility questions, gaps, and technical decisions for delivering Stage 1 Obligations & Billing under a **Pragmatic**, **Minimalist**, and **Explicit** approach have been completely resolved. The next step in the knowledge-centric SDLC is to finalize the Target Architecture update (`CR-FIN-OBLIGATIONS-BILLING-ARCHITECTURE.md`) or proceed to Implementation Planning.
