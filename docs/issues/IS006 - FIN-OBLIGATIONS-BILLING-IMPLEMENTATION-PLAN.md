---
Title: Implementation Plan — Stage 1 Obligations & Billing Full Scope
Code: FIN-OBLIGATIONS-BILLING
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-04
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Implement the complete Stage 1 "Obligations & Billing — full scope: Financial Policy & Charge Rules, Student Financial Account, Obligation Assessment & Issuance, Billing & Statement Communication" under the Finance domain for Elsasa SIS V3, fulfilling change request intake `CR-FIN-OBLIGATIONS-BILLING` ([`IS006 - FIN-OBLIGATIONS-BILLING-ISSUE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS006%20-%20FIN-OBLIGATIONS-BILLING-ISSUE.md)) and realizing the approved target architecture ([`IS006 - FIN-OBLIGATIONS-BILLING-ARCHITECTURE.md`](file:///d:/Project.Private/elsasa-sis-v3/docs/issues/IS006%20-%20FIN-OBLIGATIONS-BILLING-ARCHITECTURE.md)).

This implementation establishes:
1. **Authoritative Seven-Table Relational Persistence Schema:** Provisioning all 7 authoritative domain tables specified in `docs/entities/finance-entities-tables.md` (`financial_policies`, `charge_rules`, `student_financial_accounts`, `financial_obligations`, `obligation_due_schedules`, `billing_documents`, `billing_document_items`) while dropping the rudimentary stub table `finance.policies`.
2. **Student-Centric Financial Ledger Anchor & Lifecycle Synchronization:** Establishing `finance.student_financial_accounts` in a strict 1:1 relationship with `students.students`. Synchronizing account creation via asynchronous event consumption (`StudentEnrolledEvent`) and providing an idempotent backfill script for existing enrolled students. Account balances are dynamically derived from ledger entries rather than maintained in an independently mutable column.
3. **Monetary Precision & Base Currency Standard:** Enforcing exact `BIGINT` integer representation in Indonesian Rupiah (IDR, zero decimal places) across all monetary fields, coupled with explicit `currency_code VARCHAR(3) NOT NULL DEFAULT 'IDR'`.
4. **Dual-Mode Obligation Assessment Engine:** Delivering two vertical slices:
   - Guided batch billing cycle execution (`POST /api/finance/obligations/batch-assess`) with dry-run candidate preview, duplicate conflict validation, and bulk posting.
   - Single-student ad-hoc fee assessment (`POST /api/finance/obligations`).
   Both modes enforce rule-driven due date calculations, attach initial sequence-1 `obligation_due_schedules`, and emit `ObligationAssessedEvent` post-commit.
5. **Unified Point-in-Time Billing Document Pipeline:** Compiling open obligations into authorized bills (invoices) or periodic statements (`billing_documents`) with frozen, immutable line items (`billing_document_items`). Automatically resolving primary financial guarantors or emergency contacts from the Student domain.
6. **Out-of-the-Box Core Charge Rules Seed Data:** Seeding active Financial Policy `FP-2026-001` and three concrete charge rules per GAP-06 (`APPLICATION_FEE`, `ENROLLMENT_FEE`, and `TUITION_FEE`), immediately fulfilling Stage 1 Completion Criteria 6, 7, and 10.
7. **Actor Separation of Duties & Permissions Hierarchy:** Enforcing ADR-009 policy-based authorization with distinct permissions for the `Finance Authority` (policy creation, versioning, approvals) and the `Finance Officer` (operational assessment, billing cycle runs, statement dispatch).
8. **Operational Frontend Workspaces:** Scaffolding three dedicated Vue feature workspaces matching `screen-inventory.md`:
   - `SCR-FIN-04`: Financial Policy Workspace (`/finance/policies`)
   - `SCR-FIN-01`: Billing Cycle Workspace (`/finance/billing-cycles`)
   - `SCR-FIN-03`: Student Accounts Workbench (`/finance/accounts`)
   - Extending `SCR-SELF-02` (Family Tasks & Requests Portal) with a billing statement inspection tab.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE / ISSUE:
  - `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-ISSUE.md` (Intake `CR-FIN-OBLIGATIONS-BILLING`)
  - `docs/domains/finance-domain.md` (Financial Policy & Charge Rules lines 58–91, Student Financial Account lines 93–115, Financial Obligation & Charge lines 116–138, Billing & Statements lines 139–155, Due Schedules lines 157–183, Indonesian School Context & Operational Principles)
  - `docs/sub-modul-and-development-phases.md` (Stage 1 Module: Obligations & Billing; Criteria 6, 7, 10; Section E GAP-01 & GAP-06 resolutions)
  - `docs/operational-workflow-discovery.md` (`FIN-WF-01: Define or Adopt Financial Policy`, `FIN-WF-02: Assess and Issue Financial Obligation`, `FIN-WF-04: Issue Bill or Financial Statement`, `FIN-WF-13: Confirm Enrollment Payment Gate`)
  - `docs/use-cases.md` (`UC-FIN-10: Establish Financial Policy`, `UC-FIN-02: Assess Financial Obligations`, `UC-FIN-03: Issue Billing Statement`)
  - `docs/actors.md` (Finance Authority `ACT-03`, Finance Officer `ACT-04`)
  - `docs/screen-inventory.md` (`SCR-FIN-04: Financial Policy Workspace`, `SCR-FIN-01: Billing Cycle Workspace`, `SCR-FIN-03: Student Accounts Workbench`, `SCR-SELF-02: Family Tasks & Requests Portal`)
  - `docs/entities/finance-entities.md` & `docs/entities/finance-entities-tables.md` (Submodule 1 — Obligations & Billing; Tables 1.1 through 1.7)
- ARCHITECTURE:
  - `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-ARCHITECTURE.md` (Authoritative target capability architecture)
  - `docs/architecture/backend-architecture.md` (Modular Monolith & Vertical Slice specification)
  - `docs/architecture/frontend-architecture.md` (Modular Vue 3 SPA architecture)
  - `docs/architecture/adr.md` (ADR-001 through ADR-010)
  - `docs/architecture/coding-conventions.md` (C# 12, Dapper raw SQL, Minimal API conventions)
- FEASIBILITY-ASSESSMENT:
  - `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-FEASIBILITY-ASSESSMENT.md` (Status: `READY-FOR-PLANNING`)

Architecture Applicability: ARCHITECTURE-REQUIRED

This feature introduces the authoritative seven-table relational persistence schema for Finance, establishes event-driven ledger account provisioning, delivers a dual-mode obligation assessment engine, builds a point-in-time billing document compilation pipeline, configures domain authorization hierarchies, and scaffolds three major operational frontend workspaces. Authoritative target architecture `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-ARCHITECTURE.md` governs this plan.

---

# 2. Planning Scope

The planning scope encompasses all changes required across database persistence, backend vertical slices, cross-domain contracts, test harnesses, and frontend workspaces:

1. **Database Persistence & DbUp Migrations (`Elsasa.Sis.Database`):**
   - Author and register sequential forward DbUp migration script `20261104_0201_Finance_ObligationsBilling_Tables.sql` targeting Microsoft SQL Server 2022 ([`ADR-002`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L50-L75), [`ADR-006`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L152-L176)).
   - Drop the obsolete stub table `finance.policies`.
   - Create 7 authoritative domain tables in schema `finance`:
     - `finance.financial_policies`: Governance, versioning, status, approving authority, effective dates.
     - `finance.charge_rules`: Fee criteria, base amounts (`BIGINT` IDR), recurrence patterns, due rule parameters.
     - `finance.student_financial_accounts`: 1:1 student ledger anchor (`account_number`, `account_status`, `opened_date`).
     - `finance.financial_obligations`: Authoritative liability records (`obligation_number`, `original_amount`, `due_date`, `status`, `authorized_by`).
     - `finance.obligation_due_schedules`: Initial installment/due date schedule attached upon issuance.
     - `finance.billing_documents`: Unified point-in-time bill and periodic statement header (`document_type`, `recipient_name`, `total_billed_amount`, `delivery_status`).
     - `finance.billing_document_items`: Frozen snapshot line items.
   - Enforce database-level invariants via partial unique index `uq_finance_student_financial_accounts_student` on `student_id`.
   - Provision nonclustered query and composite indexes for sub-millisecond lookup and duplicate checks.
   - Implement an idempotent SQL backfill script for existing enrolled students in `students.students` (`GAP-002`).
   - Implement idempotent SQL seed data for active Financial Policy `FP-2026-001` and 3 core charge rules (`APPLICATION_FEE`, `ENROLLMENT_FEE`, `TUITION_FEE`) (`GAP-007`).

2. **Cross-Domain Contracts & Domain Events (`Elsasa.Sis.Contracts`):**
   - Ensure `ObligationAssessedEvent.cs` in `Elsasa.Sis.Contracts.Finance.Events` supports exact `long Amount` integer representation in IDR post-commit ([`ADR-003`](file:///d:/Project.Private/elsasa-sis-v3/docs/architecture/adr.md#L76-L98)).
   - Consume `StudentEnrolledEvent` from the Student domain.

3. **Backend Vertical Slices & Enhancements (`Elsasa.Sis.Finance`):**
   - **Permissions Hierarchy (`GAP-008`, TD-07):** Formal definition of `Permissions.Finance` in `Permissions.cs` distinguishing `Finance Authority` from `Finance Officer`.
   - **Student Account Lifecycle Event Handler (`GAP-002`, TD-03):** Asynchronous MediatR notification handler `StudentEnrolledEventHandler.cs` handling `StudentEnrolledEvent` to provision `finance.student_financial_accounts`.
   - **Financial Policy & Charge Rules Slices (`Features/Policies/`):**
     - `CreateFinancialPolicy` (`POST /api/finance/policies`): Creates policy draft (`FIN-WF-01`, `UC-FIN-10`).
     - `ApproveFinancialPolicy` (`POST /api/finance/policies/{id}/approve`): Transitions policy to `active` status (Finance Authority).
     - `CreateChargeRule` (`POST /api/finance/policies/{id}/rules`): Adds operational charge rule to approved policy.
     - `GetFinancialPolicies` (`GET /api/finance/policies`): Queries policies and associated charge rules.
     - `GetFinancialPolicyById` (`GET /api/finance/policies/{id}`): Detailed policy view.
   - **Obligation Assessment Slices (`Features/Obligations/`):**
     - `BatchAssessObligations` (`POST /api/finance/obligations/batch-assess`): Dual-mode billing cycle endpoint supporting `preview` (dry run candidate evaluation) and `commit` (bulk generation with sequence-1 due schedule and post-commit event emission) (`FIN-WF-02`, `UC-FIN-02`, `SCR-FIN-01`).
     - `AssessAdHocObligation` (`POST /api/finance/obligations`): Single-student charge issuance, auto-provisions missing account if needed, attaches sequence-1 due schedule, and emits event.
     - `GetObligations` (`GET /api/finance/obligations`): Queries obligations with filters (`studentId`, `status`, `academicPeriodId`).
   - **Billing Document & Statement Slices (`Features/Billing/`):**
     - `GenerateBillingDocument` (`POST /api/finance/billing-documents/generate`): Compiles open obligations into a `bill`, resolves primary guarantor contact, and freezes line items (`FIN-WF-04`, `UC-FIN-03`).
     - `GenerateStatement` (`POST /api/finance/billing-documents/statement`): Compiles periodic financial statement across a date range.
     - `GetBillingDocuments` (`GET /api/finance/billing-documents`): Queries billing documents.
     - `GetBillingDocumentById` (`GET /api/finance/billing-documents/{id}`): Projects complete invoice snapshot with frozen line items.
   - **Student Financial Accounts Slices (`Features/Accounts/`):**
     - `GetStudentFinancialAccount` (`GET /api/finance/accounts/{studentId}`): Queries account header, dynamically derived balance, open obligations, and recent billing documents (`SCR-FIN-03`).
     - `SearchStudentFinancialAccounts` (`GET /api/finance/accounts`): Paginated account search.
   - **Dependency Injection & Routing:** Register all feature handlers and Minimal API endpoints in `DependencyInjection.cs` and `FinanceEndpoints.cs`.

4. **Automated Testing Suite (`tests/backend`):**
   - Unit tests covering FluentValidation validators, due date calculation helper, and event records.
   - SQL Server 2022 Testcontainers integration tests verifying migration `20261104_0201` execution, table structures, constraints, partial unique index, account backfill, core charge rules seed, `StudentEnrolledEvent` account creation, policy approval flow, batch assessment dry-run and commit, duplicate prevention, ad-hoc assessment, derived balance calculation, and billing document compilation with immutable frozen items.

5. **Frontend State, API Clients & Validation Schemas (`src/frontend`):**
   - TypeScript interfaces, Zod validation schemas, and Axios client modules for policies, obligations, billing documents, and accounts.
   - TanStack Query hooks and cache invalidation composables (`useFinancePolicies`, `useFinanceObligations`, `useFinanceBilling`, `useFinanceAccounts`).
   - Shared IDR currency formatters and status badge helpers.

6. **Frontend Workspaces & UI Components (`src/frontend`):**
   - `SCR-FIN-04` Financial Policy Workspace (`/finance/policies`): Policy list, charge rules table, policy creation dialog, rule creation modal, and formal approval button for Finance Authority.
   - `SCR-FIN-01` Billing Cycle Workspace (`/finance/billing-cycles`): Guided wizard for Finance Officer to preview candidates, validate exceptions, post batch obligations, and issue billing statements.
   - `SCR-FIN-03` Student Accounts Workbench (`/finance/accounts`): Student search, account balance summary card, obligations table, invoice history table, ad-hoc charge modal, and invoice viewing dialog.
   - `SCR-SELF-02` Family Portal Extension: Payer-facing `FamilyBillingTab.vue` inspecting bills, due dates, and line item breakdowns.
   - Route registration in `src/frontend/src/app/router/routes.ts` with permission metadata.

7. **Frontend Automated Verification & Testing (`src/frontend`):**
   - Unit tests for Zod schemas, API clients, and currency formatters.
   - Component & integration tests using Vitest and Vue Test Utils for `FinancialPolicyWorkspaceView`, `BillingCycleWorkspaceView`, `StudentAccountsWorkbenchView`, and `FamilyBillingTab`.

---

# 3. Dependencies

### External System & Tooling Dependencies

- .NET 8 SDK (8.0.x)
- Docker / Docker Desktop / Podman (required for Testcontainers SQL Server 2022 integration tests)
- Microsoft SQL Server 2022 instance (for local Web API runtime)
- Node.js runtime (v22.x or LTS) and pnpm package manager (v9.x or v11.x)

### Internal Slice Dependencies

- `Depends On` declares implementation prerequisites between slices.
- Dependencies reference Slice IDs only.
- Dependency satisfaction requires the referenced slice to have implementation status `IMPLEMENTED`.
- Dependency satisfaction does not require review status `GO`.
- Slices with `Depends On: None` or whose dependencies are satisfied may be executed in parallel.

### Dependency Graph

```mermaid
flowchart TD
    P1_S01["P1-S01: DbUp Schema Migration, Backfill & GAP-06 Seed (Database)"]
    P1_S02["P1-S02: Domain Permissions & Event Contracts (Finance/Contracts)"]

    P2_S03["P2-S03: Student Account Lifecycle & Event Handler (Finance)"]
    P2_S04["P2-S04: Financial Policy & Charge Rules Slices (Finance)"]
    P2_S05["P2-S05: Student Financial Accounts & Balance Query (Finance)"]

    P3_S06["P3-S06: Batch Assessment & Due Date Engine (Finance)"]
    P3_S07["P3-S07: Ad-Hoc Assessment & Obligations Query (Finance)"]
    P3_S08["P3-S08: Billing Document Compilation & Itemization (Finance)"]
    P3_S09["P3-S09: Finance DI, Permissions & Endpoint Routing (Finance/Api)"]

    P4_S10["P4-S10: Backend Unit Tests (Tests)"]
    P4_S11["P4-S11: Database Schema & Migration Integration Tests (Tests)"]
    P4_S12["P4-S12: End-to-End Finance Workflows Integration Tests (Tests)"]

    P5_S13["P5-S13: TypeScript Types, Zod Schemas & API Clients (Frontend)"]
    P5_S14["P5-S14: TanStack Query & Mutation Composables (Frontend)"]

    P6_S15["P6-S15: Financial Policy Workspace UI SCR-FIN-04 (Frontend)"]
    P6_S16["P6-S16: Billing Cycle Workspace UI SCR-FIN-01 (Frontend)"]
    P6_S17["P6-S17: Student Accounts Workbench SCR-FIN-03 & Family Tab (Frontend)"]

    P7_S18["P7-S18: Vitest Unit Tests for Schemas & API Clients (Frontend)"]
    P7_S19["P7-S19: Vitest Component & Workspace Integration Tests (Frontend)"]

    %% Backend Dependencies
    P1_S01 --> P2_S03
    P1_S01 --> P2_S04
    P1_S02 --> P2_S04
    P1_S01 --> P2_S05
    P1_S02 --> P2_S05

    P1_S01 --> P3_S06
    P1_S02 --> P3_S06
    P2_S04 --> P3_S06

    P1_S01 --> P3_S07
    P1_S02 --> P3_S07
    P2_S03 --> P3_S07

    P1_S01 --> P3_S08
    P1_S02 --> P3_S08
    P3_S06 --> P3_S08
    P3_S07 --> P3_S08

    P2_S03 --> P3_S09
    P2_S04 --> P3_S09
    P2_S05 --> P3_S09
    P3_S06 --> P3_S09
    P3_S07 --> P3_S09
    P3_S08 --> P3_S09

    P3_S09 --> P4_S10
    P1_S01 --> P4_S11
    P3_S09 --> P4_S11
    P3_S09 --> P4_S12
    P4_S11 --> P4_S12

    %% Frontend Dependencies
    P3_S09 --> P5_S13
    P5_S13 --> P5_S14

    P5_S14 --> P6_S15
    P5_S14 --> P6_S16
    P5_S14 --> P6_S17
    P6_S15 --> P6_S17
    P6_S16 --> P6_S17

    P5_S13 --> P7_S18
    P5_S14 --> P7_S18
    P6_S15 --> P7_S19
    P6_S16 --> P7_S19
    P6_S17 --> P7_S19
    P7_S18 --> P7_S19
```

---

# 4. Progress Summary

Plan status values:
- NOT-STARTED
- IN-PROGRESS
- BLOCKED
- COMPLETED

Execution Approval values:
- PENDING
- APPROVED

Execution Approval is owned by the Architect. It is set to APPROVED when the plan structure is finalized and released for execution. Execution must not begin while Execution Approval is PENDING.

COMPLETED is a plan-level status only, set when every slice has implementation status IMPLEMENTED and review status GO.

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Persistence, Contracts & Permissions Foundation | IMPLEMENTED | GO | 2/2 |
| P2 - Student Account Lifecycle & Policy Governance Backend Slices | IMPLEMENTED | GO | 3/3 |
| P3 - Obligation Assessment & Billing Document Backend Slices | IMPLEMENTED | GO | 4/4 |
| P4 - Backend Automated Verification & Test Harness | IMPLEMENTED | GO | 3/3 |
| P5 - Frontend State, API Clients & Validation Schemas | IMPLEMENTED | GO | 2/2 |
| P6 - Frontend Workspaces & Portal UI Components | IMPLEMENTED | GO | 3/3 |
| P7 - Frontend Automated Verification & Component Testing | IMPLEMENTED | GO | 2/2 |

---

# 5. Phases

## P1 - Persistence, Contracts & Permissions Foundation

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P1-S01

Title: Database Schema Migration, Idempotent Backfill & GAP-06 Seed Data (`20261104_0201_Finance_ObligationsBilling_Tables.sql`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Author and register the sequential forward DbUp migration script `20261104_0201_Finance_ObligationsBilling_Tables.sql` in `Elsasa.Sis.Database/Scripts/` to drop stub table `finance.policies`, provision all 7 authoritative tables (`financial_policies`, `charge_rules`, `student_financial_accounts`, `financial_obligations`, `obligation_due_schedules`, `billing_documents`, `billing_document_items`), enforce indexes and constraints, execute idempotent student account backfill, and seed GAP-06 core policies and charge rules (`GAP-001`, `GAP-002`, `GAP-007`, TD-01, TD-02, TD-06).

Depends On: None

Repository: `src/backend`

Completion Criteria:
- Migration file `src/backend/Elsasa.Sis.Database/Scripts/20261104_0201_Finance_ObligationsBilling_Tables.sql` is authored and set to `EmbeddedResource` in `Elsasa.Sis.Database.csproj`.
- Drops obsolete stub table `finance.policies` via safe `IF OBJECT_ID(N'finance.policies', N'U') IS NOT NULL DROP TABLE [finance].[policies];`.
- Creates 7 authoritative tables in schema `finance` with exact column names, constraints, and data types specified in Section 8 of ARCHITECTURE:
  1. `finance.financial_policies`: `id`, `policy_code`, `name`, `category`, `business_purpose`, `applicable_context`, `effective_date`, `end_date`, `policy_version`, `status`, `approving_authority`, `approved_date`, `supersedes_policy_id`, `notes`, audit columns.
  2. `finance.charge_rules`: `id`, `rule_code`, `policy_id`, `name`, `charge_type`, `is_mandatory`, `determination_method`, `base_amount` (`BIGINT`), `currency_code` (`VARCHAR(3) DEFAULT 'IDR'`), `recurrence_pattern`, `due_rule_type`, `due_rule_value`, `applicable_grade`, `applicable_program`, `status`, `notes`, audit columns. Foreign key to `financial_policies(id)`.
  3. `finance.student_financial_accounts`: `id`, `account_number`, `student_id`, `currency_code`, `account_status`, `opened_date`, `closed_date`, `notes`, audit columns. Partial/unique indexes enforcing single 1:1 account per student (`uq_finance_student_financial_accounts_student`).
  4. `finance.financial_obligations`: `id`, `obligation_number`, `student_account_id`, `student_id`, `charge_rule_id` (NULL-able), `policy_id`, `description`, `charge_type`, `is_mandatory`, `original_amount` (`BIGINT`), `currency_code`, `issue_date`, `due_date`, `academic_period_id`, `academic_period_label`, `status`, `authorized_by`, `notes`, audit columns. Foreign keys to `student_financial_accounts`, `charge_rules`, `financial_policies`.
  5. `finance.obligation_due_schedules`: `id`, `obligation_id`, `installment_sequence`, `due_date`, `scheduled_amount` (`BIGINT`), `currency_code`, `status`, `notes`, audit columns. Foreign key to `financial_obligations(id)` with `ON DELETE CASCADE`. Unique index on `(obligation_id, installment_sequence)`.
  6. `finance.billing_documents`: `id`, `document_number`, `student_account_id`, `student_id`, `document_type`, `recipient_name`, `recipient_relationship`, `recipient_contact`, `billing_period_start`, `billing_period_end`, `issue_date`, `payment_due_date`, `total_billed_amount` (`BIGINT`), `currency_code`, `delivery_channel`, `delivery_status`, `sent_at`, `notes`, audit columns. Foreign key to `student_financial_accounts`.
  7. `finance.billing_document_items`: `id`, `billing_document_id`, `line_sequence`, `item_type`, `reference_entity_type`, `reference_entity_id`, `description`, `amount` (`BIGINT`), `currency_code`, `created_at`. Foreign key to `billing_documents(id)` with `ON DELETE CASCADE`. Unique index on `(billing_document_id, line_sequence)`.
- All monetary columns (`base_amount`, `original_amount`, `scheduled_amount`, `total_billed_amount`, `amount`) are `BIGINT NOT NULL`.
- Zero mutable balance columns exist on `finance.student_financial_accounts`.
- Nonclustered indexes created:
  - `ix_finance_financial_policies_category_status` on `([category], [status], [effective_date] DESC)`
  - `ix_finance_charge_rules_policy` on `([policy_id], [status])`
  - `ix_finance_student_financial_accounts_status` on `([account_status], [opened_date] DESC)`
  - `ix_finance_obligations_student_status` on `([student_id], [status], [due_date] ASC)`
  - `ix_finance_obligations_account_period` on `([student_account_id], [academic_period_id], [charge_rule_id])`
  - `ix_finance_due_schedules_due_date_status` on `([due_date] ASC, [status])`
  - `ix_finance_billing_documents_student` on `([student_id], [issue_date] DESC)`
  - `ix_finance_billing_documents_status` on `([delivery_status], [delivery_channel])`
  - `ix_finance_billing_items_reference` on `([reference_entity_type], [reference_entity_id])`
- Idempotent backfill script provisions accounts in `finance.student_financial_accounts` for all existing enrolled students in `students.students` who do not already have an account (`GAP-002`, TD-03).
- Idempotent seed data provisions active Financial Policy `FP-2026-001` and 3 core charge rules (`CR-2026-APP`, `CR-2026-ENR`, `CR-2026-SPP-SD`) (`GAP-007`, TD-06).
- Migration executes cleanly through DbUp within an atomic database transaction.

Implementation Notes:
- Authored sequential forward DbUp migration script `src/backend/Elsasa.Sis.Database/Scripts/20261104_0201_Finance_ObligationsBilling_Tables.sql`.
- Safely drops obsolete stub table `finance.policies`.
- Provisions all 7 authoritative tables in schema `finance` with exact constraints, `BIGINT` monetary fields in IDR, and no mutable balance column on `student_financial_accounts`.
- Created all specified nonclustered and composite indexes.
- Implemented idempotent SQL backfill into `finance.student_financial_accounts` for enrolled students.
- Implemented idempotent seed data for core financial policy `FP-2026-001` and 3 charge rules (`CR-2026-APP`, `CR-2026-ENR`, `CR-2026-SPP-SD`).
- Embedded resource automatically verified in `Elsasa.Sis.Database.dll` via wildcard pattern in `Elsasa.Sis.Database.csproj`.
- Built backend solution cleanly with 0 warnings and 0 errors; all 26 architecture tests passed.

Notes:
- Must adhere to SQL Server 2022 syntax, `DATETIMEOFFSET(7)` UTC defaults, and `NVARCHAR` for text.
- Must execute strictly after student placement tables (`20261104_0101`).

---

### P1-S02

Title: Domain Permissions Definition & Event Contracts Foundation

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Define the authoritative domain permissions hierarchy in `Elsasa.Sis.Finance/Permissions.cs` and ensure domain event contracts in `Elsasa.Sis.Contracts/Finance/Events/` conform to the integer IDR monetary standard and post-commit MediatR publishing requirements (`GAP-008`, TD-02, TD-07, ADR-003, ADR-009).

Depends On: None

Repository: `src/backend`

Completion Criteria:
- File `src/backend/Elsasa.Sis.Finance/Permissions.cs` is authored defining nested static classes under namespace `Elsasa.Sis.Finance`:
  - `Policies`: `View = "Permissions.Finance.Policies.View"`, `Manage = "Permissions.Finance.Policies.Manage"`, `Approve = "Permissions.Finance.Policies.Approve"`.
  - `Obligations`: `View = "Permissions.Finance.Obligations.View"`, `Assess = "Permissions.Finance.Obligations.Assess"`.
  - `Billing`: `View = "Permissions.Finance.Billing.View"`, `Generate = "Permissions.Finance.Billing.Generate"`.
  - `Accounts`: `View = "Permissions.Finance.Accounts.View"`.
- `ObligationAssessedEvent.cs` in `Elsasa.Sis.Contracts/Finance/Events/` is verified and updated:
  - Ensures primary constructor or overload supports `(Guid ObligationId, Guid StudentId, long Amount, DateTimeOffset OccurredOn) : IDomainEvent`.
  - Ensures convenience constructor defaults `OccurredOn` to `DateTimeOffset.UtcNow`.
- Code compiles cleanly in `Elsasa.Sis.Contracts` and `Elsasa.Sis.Finance` with zero warnings and zero errors.

Notes:
- `Finance Authority` possesses all permissions including `Policies.Approve`.
- `Finance Officer` possesses operational permissions (`Policies.View`, `Obligations.Assess`, `Billing.Generate`, `Accounts.View`).

Implementation Notes:
- Authored authoritative domain permissions hierarchy in `src/backend/Elsasa.Sis.Finance/Permissions.cs` defining `Policies`, `Obligations`, `Billing`, `Accounts`, and cross-domain `Finance.*` hierarchy aliases (GAP-008, TD-07, ADR-009).
- Updated `ObligationAssessedEvent.cs` in `src/backend/Elsasa.Sis.Contracts/Finance/Events/` with primary constructor supporting exact `long Amount` (BIGINT IDR) along with `DateTimeOffset.UtcNow` convenience constructor and backward-compatible decimal adapter overload (GAP-008, TD-02, ADR-003).
- Verified build and compilation across `Elsasa.Sis.Contracts` and `Elsasa.Sis.Finance` with 0 warnings and 0 errors, and all contract tests passing.
- Changed Files:
  - `src/backend/Elsasa.Sis.Finance/Permissions.cs`
  - `src/backend/Elsasa.Sis.Contracts/Finance/Events/ObligationAssessedEvent.cs`

---

## P2 - Student Account Lifecycle & Policy Governance Backend Slices

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P2-S03

Title: Student Financial Account Lifecycle & Event Handler (`StudentEnrolledEventHandler`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement MediatR notification handler `StudentEnrolledEventHandler.cs` in `Elsasa.Sis.Finance/Features/Accounts/Events/` to automatically and idempotently provision active `finance.student_financial_accounts` records upon student enrollment, and provide an internal account provisioning service for ad-hoc fallbacks (`GAP-002`, TD-03, ADR-003).

Depends On: P1-S01

Repository: `src/backend`

Completion Criteria:
- `StudentEnrolledEventHandler.cs` is authored in `Elsasa.Sis.Finance/Features/Accounts/Events/` implementing `INotificationHandler<StudentEnrolledEvent>`.
- Handler generates deterministic account number format: `ACC-{DateTime.UtcNow:yyyy}-{StudentId.ToString()[..8].ToUpperInvariant()}`.
- Handler performs an idempotent check: if `finance.student_financial_accounts` already contains a row for `StudentId`, no-op and complete successfully.
- Handler inserts an active account record with `id = Guid.NewGuid()`, `student_id = notification.StudentId`, `currency_code = 'IDR'`, `account_status = 'active'`, `opened_date = DateOnly.FromDateTime(DateTime.UtcNow)`, `created_by = 'SYSTEM_ENROLLMENT'`.
- Provide an internal service or helper method `GetOrCreateAccountAsync(Guid studentId, IDbConnection connection, IDbTransaction? transaction)` to guarantee account presence during ad-hoc obligation assessment.
- Registered in DI and verified with clean compilation.

Implementation Notes:
- Authored `src/backend/Elsasa.Sis.Finance/Features/Accounts/Events/StudentEnrolledEventHandler.cs` implementing `INotificationHandler<StudentEnrolledEvent>`.
- Generates deterministic account number with format `ACC-{DateTime.UtcNow:yyyy}-{StudentId.ToString()[..8].ToUpperInvariant()}`.
- Performs idempotent presence check prior to inserting active account with `opened_date = DateOnly.FromDateTime(DateTime.UtcNow)` and `created_by = 'SYSTEM_ENROLLMENT'`.
- Catches potential unique constraint races gracefully.
- Authored internal account provisioning service `IStudentAccountService` and `StudentAccountService` in `src/backend/Elsasa.Sis.Finance/Features/Accounts/Services/` exposing `GetOrCreateAccountAsync(Guid studentId, IDbConnection connection, IDbTransaction? transaction = null, ...)` returning `StudentFinancialAccountSummary` with implicit conversion to `Guid`.
- Registered services and notification handler in `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`.
- Authored unit test suite in `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/StudentAccountLifecycleTests.cs` verifying account number formatting, implicit Guid conversion, and DI resolution.
- Solution builds cleanly with 0 warnings and 0 errors; all 26 architecture tests and unit tests pass.

Changed Files:
- `src/backend/Elsasa.Sis.Finance/Elsasa.Sis.Finance.csproj`
- `src/backend/Elsasa.Sis.Finance/Features/Accounts/Events/StudentEnrolledEventHandler.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Accounts/Services/IStudentAccountService.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Accounts/Services/StudentAccountService.cs`
- `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/StudentAccountLifecycleTests.cs`

Notes:
- Relies on Dapper raw SQL parameterized queries.
- Catches potential unique constraint races gracefully via idempotent queries.

---

### P2-S04

Title: Financial Policy & Charge Rules Governance Slices (`Features/Policies/`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the vertical slices for creating, approving, querying, and managing financial policies and concrete charge rules in `Elsasa.Sis.Finance/Features/Policies/` (`FIN-WF-01`, `UC-FIN-10`, `SCR-FIN-04`, TD-04, TD-06, ADR-004, ADR-005, ADR-009).

Depends On: P1-S01, P1-S02

Repository: `src/backend`

Completion Criteria:
- Feature directory `src/backend/Elsasa.Sis.Finance/Features/Policies/` is created with dedicated vertical slices:
  1. `CreateFinancialPolicy`: Command + FluentValidation Validator + Handler (`POST /api/finance/policies`). Creates policy in `draft` status.
  2. `ApproveFinancialPolicy`: Command + Handler (`POST /api/finance/policies/{id}/approve`). Requires `Permissions.Finance.Policies.Approve` (`Finance Authority`). Validates policy exists and is in `draft` status; transitions status to `active`, sets `approved_date = DateOnly.FromDateTime(DateTime.UtcNow)`, and records `approving_authority`.
  3. `CreateChargeRule`: Command + FluentValidation Validator + Handler (`POST /api/finance/policies/{id}/rules`). Inserts charge rule attached to the active policy with `base_amount` (`BIGINT`), `recurrence_pattern`, `due_rule_type`, `due_rule_value`, and optional grade/program filters.
  4. `GetFinancialPolicies`: Query + Handler (`GET /api/finance/policies`). Supports query parameters for `status`, `category`, and includes associated charge rules.
  5. `GetFinancialPolicyById`: Query + Handler (`GET /api/finance/policies/{id}`). Returns detailed policy DTO with associated charge rules list.
- All handlers return `Task<Result<T>>` or `Task<Result>` per ADR-005.
- Validation checks enforce non-empty codes, base amounts >= 0, and valid due rule types (`days_after_issuance`, `fixed_day_of_month`, `fixed_calendar_date`).

Implementation Notes:
- Authored all five vertical slices under `src/backend/Elsasa.Sis.Finance/Features/Policies/`:
  1. `CreateFinancialPolicy`: Command, handler, validator (format `FP-YYYY-NNN`, name, category, effective/end date ordering), endpoint (`POST /api/finance/policies`) requiring `Permissions.Finance.Policies.Manage`.
  2. `ApproveFinancialPolicy`: Command, handler, validator, endpoint (`POST /api/finance/policies/{id}/approve`) requiring `Permissions.Finance.Policies.Approve`. Validates draft state, updates to `active`, records `approved_date` and `approving_authority`.
  3. `CreateChargeRule`: Command, handler, validator (rule code, name, charge type, base amount > 0, IDR currency, recurrence pattern, valid due rule types/values), endpoint (`POST /api/finance/policies/{id}/rules`) requiring `Permissions.Finance.Policies.Manage`.
  4. `GetFinancialPolicies`: Query, handler, endpoint (`GET /api/finance/policies`) requiring `Permissions.Finance.Policies.View`, supporting status and category filters, projecting policies with active charge rules.
  5. `GetFinancialPolicyById`: Query, handler, endpoint (`GET /api/finance/policies/{id}`) requiring `Permissions.Finance.Policies.View`, projecting full policy header details with associated charge rules list.
- Registered all feature handlers and configured domain authorization policies in `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`.
- Endpoints implement `IEndpoint` and are automatically discovered by host Minimal API composition via `DependencyInjection.Stage1Assemblies`.
- Built solution cleanly with zero errors and zero warnings; verified all 26 architecture tests passed.
- Changed Files:
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateFinancialPolicy/CreateFinancialPolicyRequest.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateFinancialPolicy/CreateFinancialPolicyResponse.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateFinancialPolicy/CreateFinancialPolicyValidator.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateFinancialPolicy/CreateFinancialPolicyHandler.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateFinancialPolicy/CreateFinancialPolicyEndpoint.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/ApproveFinancialPolicy/ApproveFinancialPolicyRequest.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/ApproveFinancialPolicy/ApproveFinancialPolicyResponse.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/ApproveFinancialPolicy/ApproveFinancialPolicyValidator.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/ApproveFinancialPolicy/ApproveFinancialPolicyHandler.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/ApproveFinancialPolicy/ApproveFinancialPolicyEndpoint.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateChargeRule/CreateChargeRuleRequest.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateChargeRule/CreateChargeRuleResponse.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateChargeRule/CreateChargeRuleValidator.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateChargeRule/CreateChargeRuleHandler.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateChargeRule/CreateChargeRuleEndpoint.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/GetFinancialPolicies/FinancialPolicyListItemDto.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/GetFinancialPolicies/GetFinancialPoliciesHandler.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/GetFinancialPolicies/GetFinancialPoliciesEndpoint.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/GetFinancialPolicyById/FinancialPolicyDetailResponse.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/GetFinancialPolicyById/GetFinancialPolicyByIdHandler.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Policies/GetFinancialPolicyById/GetFinancialPolicyByIdEndpoint.cs`
  - `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`

Notes:
- Data access uses Dapper raw string literals (`"""..."""`).
- Enforces ADR-009 authorization policies on mutation endpoints.

---

### P2-S05

Title: Student Financial Accounts & Derived Balance Query Slice (`Features/Accounts/`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the vertical slice `GetStudentFinancialAccount` and `SearchStudentFinancialAccounts` in `Elsasa.Sis.Finance/Features/Accounts/` projecting the 360-degree student financial position with dynamically derived balance calculation (`SCR-FIN-03`, TD-02, Resolved Decision 5).

Depends On: P1-S01, P1-S02

Repository: `src/backend`

Completion Criteria:
- Feature directory `src/backend/Elsasa.Sis.Finance/Features/Accounts/` is created with:
  1. `GetStudentFinancialAccount`: Query + Handler (`GET /api/finance/accounts/{studentId}`).
     - Projects student account header (`account_number`, `account_status`, `opened_date`).
     - Dynamically computes balance via SQL aggregation per TD-02:
       - `total_assessed = SUM(fo.original_amount WHERE fo.status != 'cancelled')`
       - `total_settled = SUM(fo.original_amount WHERE fo.status = 'settled')`
       - `current_balance = total_assessed - total_settled`
     - Returns open obligations list (`status IN ('assessed', 'issued')`) and recent billing documents.
  2. `SearchStudentFinancialAccounts`: Query + Handler (`GET /api/finance/accounts`).
     - Performs paginated search across student accounts, joining `students.students` via read-only SQL projection for student name, student number, and active grade/campus.
- Protected by `Permissions.Finance.Accounts.View`.
- Returns `DomainError.NotFound` when student account does not exist.

Notes:
- Zero mutable balance columns are read or written; balance is strictly derived.
- Cross-schema join to `students.students` is read-only and conforms to ADR-002.

Implementation Notes:
- Authored vertical slice `GetStudentFinancialAccount` (`GET /api/finance/accounts/{studentId}`) projecting account header, dynamic SQL aggregation of derived balance (`total_assessed`, `total_settled`, `current_balance`), open obligations list, and recent billing documents.
- Authored vertical slice `SearchStudentFinancialAccounts` (`GET /api/finance/accounts`) performing paginated lookup joined with read-only student demographics and current derived open balances.
- Both endpoints are secured with `Permissions.Finance.Accounts.View`.
- Queries utilize Dapper with parameterized raw T-SQL and return explicit `Result<T>` ProblemDetails mappings. Zero mutable balance columns queried.
- Registered handlers in `DependencyInjection.cs`.
- Verified build and compilation across the backend solution and passed all 26 architecture tests.
- Changed Files:
  - `src/backend/Elsasa.Sis.Finance/Features/Accounts/GetStudentFinancialAccount/OpenObligationDto.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Accounts/GetStudentFinancialAccount/RecentBillingDocumentDto.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Accounts/GetStudentFinancialAccount/GetStudentFinancialAccountResponse.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Accounts/GetStudentFinancialAccount/GetStudentFinancialAccountHandler.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Accounts/GetStudentFinancialAccount/GetStudentFinancialAccountEndpoint.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Accounts/SearchStudentFinancialAccounts/StudentFinancialAccountListItemDto.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Accounts/SearchStudentFinancialAccounts/SearchStudentFinancialAccountsResponse.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Accounts/SearchStudentFinancialAccounts/SearchStudentFinancialAccountsHandler.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Accounts/SearchStudentFinancialAccounts/SearchStudentFinancialAccountsEndpoint.cs`
  - `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`

---

## P3 - Obligation Assessment & Billing Document Backend Slices

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P3-S06

Title: Batch Obligation Assessment & Due Date Engine (`BatchAssessObligations`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the dual-action batch billing cycle vertical slice `BatchAssessObligations` in `Elsasa.Sis.Finance/Features/Obligations/BatchAssessObligations/` (`POST /api/finance/obligations/batch-assess`) supporting dry-run candidate preview, duplicate conflict validation, rule-driven due date derivation, sequence-1 due schedule attachment, and post-commit event publishing (`FIN-WF-02`, `UC-FIN-02`, `SCR-FIN-01`, `GAP-003`, `GAP-004`, TD-04, ADR-003, ADR-004).

Depends On: P1-S01, P1-S02, P2-S04

Repository: `src/backend`

Completion Criteria:
- Vertical slice folder `Elsasa.Sis.Finance/Features/Obligations/BatchAssessObligations/` is created with Command, Validator, Handler, Response DTOs.
- Supports payload: `ChargeRuleId`, `AcademicPeriodId`, `BillingCycleLabel`, `TargetCampus` (optional), `TargetGrade` (optional), `Mode` (`"preview"` | `"commit"`).
- **Rule Verification:** Validates `ChargeRuleId` exists and is `active`; resolves governing policy and due rule parameters.
- **Candidate Evaluation:** Queries active enrolled students from `students.students` and `students.administrative_placements` matching rule criteria (`applicable_program`, `applicable_grade`, `is_current = 1`).
- **Duplicate Detection:** Queries `finance.financial_obligations` to detect if candidates already have an obligation for `(student_account_id, charge_rule_id, academic_period_id)`.
- **Preview Mode (`Mode == "preview"`):**
  - Returns DTO with `TotalCandidatesCount`, `EligibleCandidatesCount`, `DuplicateCount`, `GrossAssessedAmount`, and candidate student items with `IsDuplicate` boolean flag.
  - Performs zero database writes.
- **Commit Mode (`Mode == "commit"`):**
  - Filters out duplicate candidates.
  - Computes rule-driven due date for each student:
    - `days_after_issuance`: `issue_date.AddDays(int.Parse(due_rule_value))`
    - `fixed_day_of_month`: Computes specified day in the period's month, clamped to the last day of the month.
    - `fixed_calendar_date`: Parses ISO-8601 date.
  - Inserts obligations into `finance.financial_obligations` (`obligation_number = OBL-{Year}-{Sequence:D6}`, `original_amount = rule.base_amount`, `status = 'assessed'`).
  - Inserts initial sequence-1 due schedule in `finance.obligation_due_schedules` (`installment_sequence = 1`, `scheduled_amount = original_amount`, `status = 'pending'`).
  - Executes all inserts inside a single atomic `SqlTransaction`.
  - Publishes `ObligationAssessedEvent` via MediatR `IPublisher` for each created obligation strictly **post-commit**.
- Returns `Result<BatchAssessObligationsResponse>` with summary counts and created obligation IDs.

Implementation Notes:
- Authored complete vertical slice `BatchAssessObligations` in `src/backend/Elsasa.Sis.Finance/Features/Obligations/BatchAssessObligations/`:
  1. `DueDateCalculationEngine.cs`: Derives rule-driven due dates supporting `days_after_issuance`, `fixed_day_of_month` (clamped to month bounds with optional period reference date), and `fixed_calendar_date`.
  2. `BatchAssessObligationsRequest.cs`: Accepts `ChargeRuleId`, `AcademicPeriodId`, `BillingCycleLabel`, optional `TargetCampus`, optional `TargetGrade`, and `Mode` (`preview` | `commit`).
  3. `BatchAssessObligationsValidator.cs`: FluentValidation rules for mandatory fields, string length bounds, and allowed modes.
  4. `BatchAssessCandidateDto.cs` & `BatchAssessObligationsResponse.cs`: Rich response models capturing candidate breakdowns, duplicate flags, monetary metrics, and created obligation UUIDs.
  5. `BatchAssessObligationsHandler.cs`:
     - Validates active charge rule and active governing policy.
     - Resolves operational period reference dates and display labels.
     - Queries enrolled students matching rule criteria and placement filters (`p.is_current = 1`).
     - Evaluates duplicate obligations using outer apply against `finance.financial_obligations`.
     - In `preview` mode: performs zero database writes and returns candidate eligibility metrics.
     - In `commit` mode: filters out duplicates, allocates sequence-locked traceable obligation numbers (`OBL-{Year}-{Sequence:D6}`), lazily provisions student financial accounts if missing, writes obligations and sequence-1 due schedules inside an atomic database transaction, and strictly post-commit publishes `ObligationAssessedEvent` via MediatR `IPublisher`.
  6. `BatchAssessObligationsEndpoint.cs`: Maps `POST /api/finance/obligations/batch-assess` requiring `Permissions.Finance.Obligations.Assess`.
- Registered `BatchAssessObligationsHandler` in `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`.
- Built solution with 0 warnings and 0 errors, and all 26 architecture tests passed.
- Changed Files:
  - `src/backend/Elsasa.Sis.Finance/Features/Obligations/BatchAssessObligations/DueDateCalculationEngine.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Obligations/BatchAssessObligations/BatchAssessObligationsRequest.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Obligations/BatchAssessObligations/BatchAssessCandidateDto.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Obligations/BatchAssessObligations/BatchAssessObligationsResponse.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Obligations/BatchAssessObligations/BatchAssessObligationsValidator.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Obligations/BatchAssessObligations/BatchAssessObligationsHandler.cs`
  - `src/backend/Elsasa.Sis.Finance/Features/Obligations/BatchAssessObligations/BatchAssessObligationsEndpoint.cs`
  - `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`

Notes:
- Protected by `Permissions.Finance.Obligations.Assess`.
- Follows ADR-003 for post-commit event dispatch.

---

### P3-S07

Title: Ad-Hoc Obligation Assessment & Obligations Query Slices

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the single-student ad-hoc obligation assessment slice (`AssessAdHocObligation`) and filtered obligations query slice (`GetObligations`) in `Elsasa.Sis.Finance/Features/Obligations/` (`FIN-WF-02`, `UC-FIN-02`, `SCR-FIN-03`, `GAP-003`, `GAP-004`, TD-04).

Depends On: P1-S01, P1-S02, P2-S03

Repository: `src/backend`

Completion Criteria:
- Feature directory `Elsasa.Sis.Finance/Features/Obligations/` contains:
  1. `AssessAdHocObligation` (`POST /api/finance/obligations`):
     - Payload: `StudentId`, `ChargeType`, `Description`, `Amount` (`BIGINT`), `DueDate`, `AcademicPeriodId`, `Notes`.
     - Validates positive amount and valid due date.
     - Resolves or auto-provisions student financial account via `IStudentAccountService` fallback.
     - Resolves academic period label from `operations.operational_periods`.
     - Inserts obligation with `charge_rule_id = NULL`, `status = 'assessed'`, `authorized_by = CurrentUser`.
     - Inserts sequence-1 `finance.obligation_due_schedules` (`scheduled_amount = Amount`, `due_date = DueDate`, `status = 'pending'`).
     - Publishes `ObligationAssessedEvent` post-commit.
     - Returns created obligation summary DTO.
  2. `GetObligations` (`GET /api/finance/obligations`):
     - Supports query parameters: `studentId`, `status`, `academicPeriodId`, `chargeType`, pagination (`page`, `pageSize`).
     - Returns paginated list of obligation DTOs including student details, charge rule, amounts, due dates, and schedule status.
- Protected by `Permissions.Finance.Obligations.Assess` and `Permissions.Finance.Obligations.View`.

Implementation Notes:
- Implemented single-student ad-hoc obligation assessment slice under `src/backend/Elsasa.Sis.Finance/Features/Obligations/AssessAdHocObligation/`:
  - `AssessAdHocObligationRequest.cs`: Accepts `StudentId`, `ChargeType`, `Description`, `Amount` (BIGINT IDR), `DueDate`, `AcademicPeriodId`, optional `Notes`, and optional `PolicyId`.
  - `AssessAdHocObligationValidator.cs`: Enforces valid non-empty IDs, string lengths, positive amounts, and valid due dates.
  - `AssessAdHocObligationResponse.cs`: Rich summary DTO capturing obligation metadata and initial sequence-1 schedule ID.
  - `AssessAdHocObligationHandler.cs`: Verifies student existence, auto-provisions/retrieves financial account via `IStudentAccountService`, resolves academic period label from `operations.operational_periods` (with fallbacks), resolves active policy, writes obligation (`charge_rule_id = NULL`, status `assessed`, `is_mandatory = 1`) and sequence-1 due schedule in an atomic transaction, and publishes `ObligationAssessedEvent` strictly post-commit.
  - `AssessAdHocObligationEndpoint.cs`: Minimal API endpoint (`POST /api/finance/obligations`) protected by `Permissions.Finance.Obligations.Assess`.
- Implemented filtered obligations query slice under `src/backend/Elsasa.Sis.Finance/Features/Obligations/GetObligations/`:
  - `ObligationScheduleDto.cs`: Projects attached due schedules.
  - `ObligationListItemDto.cs`: Projects obligation details joined with student identity, charge rule code, and governing policy.
  - `GetObligationsResponse.cs`: Standard paginated container using `PaginatedList<ObligationListItemDto>`.
  - `GetObligationsHandler.cs`: Executes filtered count and paginated query with Dapper, batch-loading attached due schedules for matching obligations.
  - `GetObligationsEndpoint.cs`: Minimal API endpoint (`GET /api/finance/obligations`) protected by `Permissions.Finance.Obligations.View`.
- Registered `AssessAdHocObligationHandler` and `GetObligationsHandler` in `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`.
- Added unit tests in `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/AssessAdHocObligationValidatorTests.cs` verifying validation rules, pagination math, and DI registration.
- Build succeeded with 0 warnings and 0 errors; all 26 architecture tests and 14 unit tests pass cleanly.

Changed Files:
- `src/backend/Elsasa.Sis.Finance/Features/Obligations/AssessAdHocObligation/AssessAdHocObligationRequest.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Obligations/AssessAdHocObligation/AssessAdHocObligationValidator.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Obligations/AssessAdHocObligation/AssessAdHocObligationResponse.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Obligations/AssessAdHocObligation/AssessAdHocObligationHandler.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Obligations/AssessAdHocObligation/AssessAdHocObligationEndpoint.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Obligations/GetObligations/ObligationScheduleDto.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Obligations/GetObligations/ObligationListItemDto.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Obligations/GetObligations/GetObligationsResponse.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Obligations/GetObligations/GetObligationsHandler.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Obligations/GetObligations/GetObligationsEndpoint.cs`
- `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/AssessAdHocObligationValidatorTests.cs`

Notes:
- Handles individual fees (e.g. lost ID card, late library fee, field trip) without requiring a pre-existing charge rule.

---

### P3-S08

Title: Billing Document Compilation & Itemization Slices (`Features/Billing/`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement the unified billing document compilation and statement pipeline in `Elsasa.Sis.Finance/Features/Billing/` supporting point-in-time bills (invoices) and periodic statements with frozen line items and primary guarantor contact resolution (`FIN-WF-04`, `UC-FIN-03`, `GAP-005`, TD-05, Resolved Decision 1).

Depends On: P1-S01, P1-S02, P3-S06, P3-S07

Repository: `src/backend`

Completion Criteria:
- Feature directory `src/backend/Elsasa.Sis.Finance/Features/Billing/` is created with:
  1. `GenerateBillingDocument` (`POST /api/finance/billing-documents/generate`):
     - Payload: `StudentId`, `ObligationIds` (optional list; if omitted, compiles all open obligations), `PaymentDueDate`, `Notes`.
     - Resolves primary recipient from `students.student_guardian_relationships` joined with `students.guardians` (`is_financial_guarantor = 1` first, fallback `is_primary_contact = 1`, fallback student name).
     - Generates document number: `BIL-{Year}-{Sequence:D5}`.
     - Computes `total_billed_amount` as the exact sum of included obligations.
     - Inserts header row into `finance.billing_documents` with `document_type = 'bill'`, `delivery_channel = 'portal'`, `delivery_status = 'issued'`.
     - Freezes line items into `finance.billing_document_items`:
       - `line_sequence = 1..N`
       - `item_type = 'obligation'`
       - `reference_entity_type = 'financial_obligations'`
       - `reference_entity_id = obligation.id`
       - `description = obligation.description`
       - `amount = obligation.original_amount`
     - Updates referenced obligations status to `issued`.
     - Executes inside atomic `SqlTransaction`.
  2. `GenerateStatement` (`POST /api/finance/billing-documents/statement`):
     - Compiles periodic financial statement across `billing_period_start` and `billing_period_end`.
     - Document number: `STM-{Year}-{Sequence:D5}`, `document_type = 'statement'`.
  3. `GetBillingDocuments` (`GET /api/finance/billing-documents`):
     - Queries billing documents with filters (`studentId`, `documentType`, `deliveryStatus`).
  4. `GetBillingDocumentById` (`GET /api/finance/billing-documents/{id}`):
     - Projects detailed invoice DTO including recipient contact details, payment due date, total amount, and frozen itemized line items list.
- Immutability invariant enforced: `billing_documents` and `billing_document_items` are append-only.

Implementation Notes:
- Authored unified billing document compilation pipeline under `src/backend/Elsasa.Sis.Finance/Features/Billing/`:
  1. `Common/`:
     - `BillingRecipientResolver.cs`: Resolves primary recipient from `students.student_guardian_relationships` joined with `students.guardians` prioritizing financial contact (`is_financial_contact = 1`), then primary contact (`is_primary_contact = 1`), then earliest active relationship, falling back to student identity.
     - `BillingDocumentNumberGenerator.cs`: Thread-safe query-driven document sequence generator producing `BIL-{Year}-{Sequence:D5}` for bills and `STM-{Year}-{Sequence:D5}` for statements.
  2. `GenerateBillingDocument/`:
     - `GenerateBillingDocumentRequest.cs`, `GenerateBillingDocumentValidator.cs`, `GenerateBillingDocumentResponse.cs`.
     - `GenerateBillingDocumentHandler.cs`: Executes point-in-time bill compilation inside an atomic `SqlTransaction`, validates open obligations (`assessed`/`issued`), computes `total_billed_amount`, freezes itemized lines (`line_sequence = 1..N`, `item_type = 'obligation'`), updates obligation status to `issued`, and enforces append-only immutability.
     - `GenerateBillingDocumentEndpoint.cs`: Maps `POST /api/finance/billing-documents/generate` requiring `Permissions.Finance.Billing.Generate`.
  3. `GenerateStatement/`:
     - `GenerateStatementRequest.cs`, `GenerateStatementValidator.cs`, `GenerateStatementResponse.cs`.
     - `GenerateStatementHandler.cs`: Compiles periodic statements across `billing_period_start` and `billing_period_end` with frozen line items without altering obligation status.
     - `GenerateStatementEndpoint.cs`: Maps `POST /api/finance/billing-documents/statement` requiring `Permissions.Finance.Billing.Generate`.
  4. `GetBillingDocuments/`:
     - `BillingDocumentListItemDto.cs`, `GetBillingDocumentsHandler.cs`, `GetBillingDocumentsEndpoint.cs`: Filtered and paginated listing by `studentId`, `documentType`, and `deliveryStatus` requiring `Permissions.Finance.Billing.View`.
  5. `GetBillingDocumentById/`:
     - `BillingDocumentDetailResponse.cs`, `GetBillingDocumentByIdHandler.cs`, `GetBillingDocumentByIdEndpoint.cs`: Projects full document snapshot with frozen items list requiring `Permissions.Finance.Billing.View`.
- Registered all feature handlers in `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`.
- Authored unit test suite in `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/BillingDocumentTests.cs` covering request validators, pagination mathematics, and DI resolution.
- Solution builds cleanly with 0 warnings and 0 errors; all 26 architecture tests and 28 unit tests pass.

Changed Files:
- `src/backend/Elsasa.Sis.Finance/Features/Billing/Common/BillingRecipientResolver.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/Common/BillingDocumentNumberGenerator.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateBillingDocument/GenerateBillingDocumentRequest.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateBillingDocument/GenerateBillingDocumentValidator.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateBillingDocument/GenerateBillingDocumentResponse.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateBillingDocument/GenerateBillingDocumentHandler.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateBillingDocument/GenerateBillingDocumentEndpoint.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateStatement/GenerateStatementRequest.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateStatement/GenerateStatementValidator.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateStatement/GenerateStatementResponse.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateStatement/GenerateStatementHandler.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateStatement/GenerateStatementEndpoint.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GetBillingDocuments/BillingDocumentListItemDto.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GetBillingDocuments/GetBillingDocumentsHandler.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GetBillingDocuments/GetBillingDocumentsEndpoint.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GetBillingDocumentById/BillingDocumentDetailResponse.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GetBillingDocumentById/GetBillingDocumentByIdHandler.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GetBillingDocumentById/GetBillingDocumentByIdEndpoint.cs`
- `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/BillingDocumentTests.cs`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Protected by `Permissions.Finance.Billing.Generate` and `Permissions.Finance.Billing.View`.

---

### P3-S09

Title: Finance Domain Dependency Injection, Permissions & Endpoint Routing

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Wire all Finance domain services, MediatR handlers, FluentValidation endpoint filters, and Minimal API route groups into `Elsasa.Sis.Finance` and `Elsasa.Sis.Api`, configuring policy-based authorization claim requirements (`GAP-008`, TD-07, ADR-001, ADR-004, ADR-009).

Depends On: P2-S03, P2-S04, P2-S05, P3-S06, P3-S07, P3-S08

Repository: `src/backend`

Completion Criteria:
- `DependencyInjection.cs` in `Elsasa.Sis.Finance` is updated to register:
  - All MediatR request handlers and notification handlers (`StudentEnrolledEventHandler`).
  - FluentValidation validators across all Finance feature slices.
  - Internal account provisioning service (`IStudentAccountService`).
- Endpoint mapping class `FinanceEndpoints.cs` in `Elsasa.Sis.Finance/Endpoints/` is authored implementing `IEndpoint`:
  - Maps route group `/api/finance`.
  - Maps policy endpoints: `POST /policies`, `POST /policies/{id}/approve`, `POST /policies/{id}/rules`, `GET /policies`, `GET /policies/{id}`.
  - Maps obligation endpoints: `POST /obligations/batch-assess`, `POST /obligations`, `GET /obligations`.
  - Maps billing endpoints: `POST /billing-documents/generate`, `POST /billing-documents/statement`, `GET /billing-documents`, `GET /billing-documents/{id}`.
  - Maps account endpoints: `GET /accounts`, `GET /accounts/{studentId}`.
  - Applies authorization claim requirements matching `Permissions.Finance.*`.
- `Program.cs` in `Elsasa.Sis.Api` registers `AddFinanceDomain()` and maps Finance endpoints.
- Solution builds cleanly with 0 errors and 0 warnings.

Implementation Notes:
- Added `FluentValidation.DependencyInjectionExtensions` to `src/backend/Elsasa.Sis.Finance/Elsasa.Sis.Finance.csproj`.
- Updated `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs` to register all FluentValidation validators within `FinanceAssemblyMarker.Assembly` alongside existing MediatR notification handlers, domain services (`IStudentAccountService`, `FinanceModule`), all 14 feature slice handlers, and authorization policies for all `Permissions.Finance.*` claim requirements.
- Authored `src/backend/Elsasa.Sis.Finance/Endpoints/FinanceEndpoints.cs` implementing `IEndpoint` mapping route group `/api/finance` and root metadata endpoint `GET /api/finance`.
- Verified all 14 individual feature slice endpoints (`CreateFinancialPolicyEndpoint`, `ApproveFinancialPolicyEndpoint`, `CreateChargeRuleEndpoint`, `GetFinancialPoliciesEndpoint`, `GetFinancialPolicyByIdEndpoint`, `BatchAssessObligationsEndpoint`, `AssessAdHocObligationEndpoint`, `GetObligationsEndpoint`, `GenerateBillingDocumentEndpoint`, `GenerateStatementEndpoint`, `GetBillingDocumentsEndpoint`, `GetBillingDocumentByIdEndpoint`, `GetStudentFinancialAccountEndpoint`, `SearchStudentFinancialAccountsEndpoint`) implement `IEndpoint` and are auto-discovered via `app.MapEndpoints(DependencyInjection.Stage1Assemblies)`.
- Verified `Elsasa.Sis.Api/Configuration/DependencyInjection.cs` wires `AddFinanceDomain()` and includes `FinanceAssemblyMarker.Assembly` in `Stage1Assemblies` and `DomainAssemblies`.
- Added unit test suite in `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/FinanceDomainWiringTests.cs` verifying DI resolution of handlers, FluentValidation validators, authorization policies, and `FinanceEndpoints`.
- Built solution with 0 warnings and 0 errors; all 26 architecture tests and 32 finance unit tests pass.

Changed Files:
- `src/backend/Elsasa.Sis.Finance/Elsasa.Sis.Finance.csproj`
- `src/backend/Elsasa.Sis.Finance/DependencyInjection.cs`
- `src/backend/Elsasa.Sis.Finance/Endpoints/FinanceEndpoints.cs`
- `src/backend/Elsasa.Sis.Api/Program.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/FinanceDomainWiringTests.cs`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Adheres to ADR-004 Minimal API and ADR-009 authorization architecture.

---

## P4 - Backend Automated Verification & Test Harness

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P4-S10

Title: Backend Unit Tests (Validators, Due Date Engine, Events)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Author comprehensive unit test suites in `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/` verifying feature validators, rule-driven due date calculations, and domain event records.

Depends On: P3-S09

Repository: `src/backend`

Completion Criteria:
- Unit test classes authored in `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/`:
  1. `CreateFinancialPolicyValidatorTests.cs`: Verifies required policy code, name, valid category, and date ordering.
  2. `CreateChargeRuleValidatorTests.cs`: Verifies base amount >= 0, valid recurrence pattern, valid due rule types.
  3. `BatchAssessObligationsValidatorTests.cs`: Verifies mode is 'preview' or 'commit', charge rule ID, and academic period ID.
  4. `AssessAdHocObligationValidatorTests.cs`: Verifies student ID, description, amount > 0, and future/present due date.
  5. `GenerateBillingDocumentValidatorTests.cs`: Verifies student ID and payment due date.
  6. `DueDateCalculationTests.cs`: Verifies:
     - `days_after_issuance`: correct day additions.
     - `fixed_day_of_month`: correct day placement and clamping on 28/30/31-day months and leap years.
     - `fixed_calendar_date`: exact date parsing.
  7. `FinanceEventContractsTests.cs`: Verifies `ObligationAssessedEvent` and `StudentEnrolledEvent` properties and UTC initialization.
- All unit tests pass cleanly with `dotnet test`.

Implementation Notes:
- Authored unit test suites in `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/`:
  1. `CreateFinancialPolicyValidatorTests.cs`: Comprehensive coverage for policy code regex format (`FP-YYYY-NNN`), required strings, category max length, business purpose, and date ordering (`EndDate >= EffectiveDate`).
  2. `CreateChargeRuleValidatorTests.cs`: Comprehensive coverage for base amount > 0, strict 'IDR' currency check, recurrence pattern, valid due rule types (`days_after_issuance`, `fixed_day_of_month`, `fixed_calendar_date`), and dependent value constraints.
  3. `BatchAssessObligationsValidatorTests.cs`: Tests for `preview` and `commit` modes (case-insensitive), required `ChargeRuleId`, `AcademicPeriodId`, cycle label, and optional grade/campus length constraints.
  4. `AssessAdHocObligationValidatorTests.cs`: Extended with tests verifying student ID, description, amount > 0, charge type, academic period ID, and present/future due dates.
  5. `GenerateBillingDocumentValidatorTests.cs`: Dedicated validator unit tests verifying required student ID, default/valid payment due dates, and notes max length.
  6. `DueDateCalculationTests.cs`: Verifies rule-driven calculations: `days_after_issuance` day additions, `fixed_day_of_month` day placement and clamping across 28-day non-leap Feb, 29-day leap Feb, 30-day, and 31-day months, and `fixed_calendar_date` parsing.
  7. `FinanceEventContractsTests.cs`: Verifies `ObligationAssessedEvent` and `StudentEnrolledEvent` constructors, property values, decimal conversion, and UTC occurred-on timestamp initialization.
- Updated `GenerateBillingDocumentValidator.cs` to explicitly validate `PaymentDueDate` is not default when supplied.
- All 127 Finance unit tests and 26 architecture tests pass cleanly with `dotnet test`.

Changed Files:
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateBillingDocument/GenerateBillingDocumentValidator.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/CreateFinancialPolicyValidatorTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/CreateChargeRuleValidatorTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/BatchAssessObligationsValidatorTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/AssessAdHocObligationValidatorTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/GenerateBillingDocumentValidatorTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/DueDateCalculationTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/FinanceEventContractsTests.cs`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Fast in-memory unit tests executing without database dependencies.

---

### P4-S11

Title: Database Schema & Migration Integration Tests (Testcontainers SQL Server 2022)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Author integration test suite in `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/Migration/` using Testcontainers SQL Server 2022 to verify migration `20261104_0201` execution, table dropping, partial unique indexes, account backfill, and seed data.

Depends On: P1-S01, P3-S09

Repository: `src/backend`

Completion Criteria:
- Integration test class `FinanceMigrationIntegrationTests.cs` is authored:
  - Executes DbUp migration scripts against a fresh SQL Server 2022 container.
  - Verifies stub table `finance.policies` is dropped.
  - Verifies all 7 tables exist in schema `finance` with expected columns and foreign key constraints.
  - Verifies partial unique index on `finance.student_financial_accounts(student_id)` rejects duplicate account inserts for the same student.
  - Verifies backfill query created account records for existing enrolled students in `students.students`.
  - Verifies active Financial Policy `FP-2026-001` exists and is `active`.
  - Verifies the 3 core charge rules (`CR-2026-APP`, `CR-2026-ENR`, `CR-2026-SPP-SD`) exist with correct `BIGINT` amounts (500000, 15000000, 750000).
- All integration tests pass cleanly with `dotnet test`.

Implementation Notes:
- Authored integration test suite `FinanceMigrationIntegrationTests.cs` under `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/Migration/` inheriting `BaseIntegrationTest`.
- Implemented 14 exhaustive integration tests covering:
  1. `FinanceMigration_Should_RunSuccessfullyViaDatabaseMigrator`: Executes `DatabaseMigrator.UpgradeDatabase` idempotently against target database.
  2. `FinanceMigration_Should_BeJournaledInSchemaVersions`: Verifies migration script `20261104_0201_Finance_ObligationsBilling_Tables.sql` is journaled in `dbo._SchemaVersions`.
  3. `ObsoleteStubTable_Policies_Should_BeDropped`: Asserts `OBJECT_ID(N'finance.policies', N'U') IS NULL`.
  4. `FinancialPoliciesTable_Should_ContainAllColumnsAndIndexes`: Verifies all 18 columns, PK, unique code index, category/status index, and self-referencing supersedes foreign key (`ON DELETE NO ACTION`).
  5. `ChargeRulesTable_Should_ContainAllColumnsAndIndexes`: Verifies all 20 columns, PK, unique rule code index, policy/status index, `base_amount` `BIGINT` data type, and policy foreign key (`ON DELETE NO ACTION`).
  6. `StudentFinancialAccountsTable_Should_ContainAllColumnsAndIndexes_AndNoMutableBalanceColumn`: Verifies all 12 columns, PK, unique account number index, unique student index, status index, and asserts zero mutable balance columns (`balance`, `current_balance`, `total_balance`) exist on the table.
  7. `FinancialObligationsTable_Should_ContainAllColumnsAndIndexes`: Verifies all 22 columns, PK, unique obligation number index, student/status index, account/period composite index, `original_amount` `BIGINT` data type, and foreign keys to accounts, charge rules, and policies (`ON DELETE NO ACTION`).
  8. `ObligationDueSchedulesTable_Should_ContainAllColumnsAndIndexes_WithCascadeDelete`: Verifies all 12 columns, PK, unique installment sequence index, due date/status index, `scheduled_amount` `BIGINT` data type, and obligation foreign key with `ON DELETE CASCADE`.
  9. `BillingDocumentsTable_Should_ContainAllColumnsAndIndexes`: Verifies all 20 columns, PK, unique document number index, student index, status/channel index, `total_billed_amount` `BIGINT` data type, and student account foreign key (`ON DELETE NO ACTION`).
  10. `BillingDocumentItemsTable_Should_ContainAllColumnsAndIndexes_WithCascadeDelete`: Verifies all 10 columns, PK, unique line sequence index, entity reference index, `amount` `BIGINT` data type, and billing document foreign key with `ON DELETE CASCADE`.
  11. `UniqueStudentAccountInvariant_Should_RejectDuplicateAccountForSameStudent`: Tests inserting first account succeeds, while inserting a duplicate account for the same `student_id` throws `SqlException` 2601 or 2627.
  12. `BackfillQuery_Should_IdempotentlyCreateAccountsForExistingEnrolledStudents`: Sets up enrolled student, executes migration 20261104_0201 Section 8 backfill query, asserts account creation with `ACC-2026-` prefix, status `active`, currency `IDR`, creator `MIGRATION_BACKFILL`, and asserts strict idempotency when run a second time.
  13. `ActiveFinancialPolicy_Should_ExistAndBeActive`: Verifies core seeded policy `FP-2026-001` exists with expected ID, name "Kebijakan Biaya Pendidikan TP 2026/2027", category `tuition`, version `1.0`, status `active`, approving authority "Direktur Keuangan", and creator `SYSTEM_SEED`.
  14. `CoreChargeRules_Should_ExistWithExpectedBigintAmounts`: Verifies all 3 core charge rules (`CR-2026-APP` = 500,000 IDR, `CR-2026-ENR` = 15,000,000 IDR, `CR-2026-SPP-SD` = 750,000 IDR) exist with exact `BIGINT` values, correct charge types, recurrence patterns, due rule derivations, and verifies seed idempotency.
- Fixed missing brace in `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/AssessAdHocObligationValidatorTests.cs`.
- Executed `dotnet test` with all 14 migration integration tests passing cleanly. Full test suite: 757 passed, 26 architecture tests passed, 0 failures.

Changed Files:
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/Migration/FinanceMigrationIntegrationTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/UnitTests/Finance/AssessAdHocObligationValidatorTests.cs`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Targets real Microsoft SQL Server 2022 container matching production storage engine.

---

### P4-S12

Title: End-to-End Finance Workflows Integration Tests (Testcontainers SQL Server 2022)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Author integration test suites in `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/` verifying complete end-to-end workflows across the account lifecycle, policy governance, batch assessment, ad-hoc assessment, derived balance, and billing document compilation.

Depends On: P3-S09, P4-S11

Repository: `src/backend`

Completion Criteria:
- Integration test suites authored:
  1. `StudentAccountLifecycleIntegrationTests.cs`: Publishing `StudentEnrolledEvent` triggers `StudentEnrolledEventHandler` to provision active account.
  2. `FinancialPolicyWorkflowIntegrationTests.cs`:
     - Creates policy in `draft` status.
     - Approves policy as Finance Authority &rarr; transitions to `active`.
     - Creates charge rule attached to policy.
  3. `BatchAssessmentWorkflowIntegrationTests.cs`:
     - Executes `preview` mode: returns candidate counts, duplicate flags, without modifying database.
     - Executes `commit` mode: inserts obligations and sequence-1 due schedules, verifies `ObligationAssessedEvent` was dispatched.
     - Re-running batch detects existing obligations and skips duplicates.
  4. `AdHocAssessmentWorkflowIntegrationTests.cs`:
     - Assesses ad-hoc fee for student, verifies sequence-1 due schedule, auto-provisions missing account if needed.
  5. `DerivedAccountBalanceIntegrationTests.cs`:
     - Assesses obligations and simulates payment settlement; verifies account balance query returns exact derived calculation without denormalized balance column.
  6. `BillingDocumentWorkflowIntegrationTests.cs`:
     - Compiles open obligations into `bill`, resolves primary financial guarantor from Student domain, verifies frozen line items in `billing_document_items`, verifies obligations status updated to `issued`.
- Tests satisfy Stage 1 Completion Criteria 6, 7, and 10.
- All integration tests pass cleanly.

Implementation Notes:
- Authored reusable data seeding helper `FinanceIntegrationTestHelpers.cs` under `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/`.
- Authored all 6 required integration test suites in `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/`:
  1. `StudentAccountLifecycleIntegrationTests.cs`: Verifies `StudentEnrolledEvent` publishing triggers `StudentEnrolledEventHandler` to provision active account with formatted account number, idempotency on repeated publishing, `IStudentAccountService.GetOrCreateAccountAsync` idempotent behavior, and HTTP endpoint query.
  2. `FinancialPolicyWorkflowIntegrationTests.cs`: Verifies draft policy creation via `POST /api/finance/policies`, separation-of-duties enforcement where non-authority is forbidden (403), Finance Authority approval transitioning status to `active` with approved date and authority via `POST /api/finance/policies/{id}/approve`, attaching charge rule to active policy, rejecting charge rule attachment to draft policy, and detailed query verification.
  3. `BatchAssessmentWorkflowIntegrationTests.cs`: Verifies `preview` mode candidate dry-run metrics without modifying database records, `commit` mode multi-table persistence generating obligations and sequence-1 due schedules, `ObligationAssessedEvent` domain event dispatch, and duplicate detection/skipping on subsequent preview/commit runs.
  4. `AdHocAssessmentWorkflowIntegrationTests.cs`: Verifies ad-hoc obligation assessment with automatic provisioning of missing student financial account, sequence-1 due schedule creation, `ObligationAssessedEvent` publication, and existing account reuse.
  5. `DerivedAccountBalanceIntegrationTests.cs`: Verifies dynamic on-the-fly derived balance calculation reflecting assessed, settled, and cancelled obligations, and proves no denormalized mutable balance column exists on `finance.student_financial_accounts` per ADR-002.
  6. `BillingDocumentWorkflowIntegrationTests.cs`: Verifies compilation of open obligations into an immutable bill (`BIL-{yyyy}-{seq}`), primary financial guarantor resolution from `students.student_guardian_relationships` / `students.guardians` per GAP-005, frozen itemized line items in `finance.billing_document_items`, transition of obligation status to `issued`, conflict on duplicate compilation, statement generation (`STM-{yyyy}-{seq}`), and query endpoints.
- Updated `TestEventCollector.cs` and `IntegrationTestWebAppFactory.cs` to capture `ObligationAssessedEvent` notifications.
- Added project references to `Elsasa.Sis.Finance` and `Elsasa.Sis.Students` in `Elsasa.Sis.IntegrationTests.csproj`.
- Remediated Finding F-P4-S12-01: Added `EnsureActivePolicyAsync` to `FinanceIntegrationTestHelpers.cs` and wired it into `AdHocAssessmentWorkflowIntegrationTests`, `DerivedAccountBalanceIntegrationTests`, and `BillingDocumentWorkflowIntegrationTests` to prevent `fk_finance_obligations_policy` foreign key constraint failures when database tables are wiped between tests.
- Remediated Finding F-P4-S12-02: Updated `CreateChargeRuleHandler.cs` to verify governing policy is in `active` status, returning `409 Conflict` (`Finance.Policy.NotActive`) when attempting to attach charge rules to `draft` policies.
- Corrected `BillingDocumentWorkflowIntegrationTests.cs` and `GenerateBillingDocumentHandler.cs` ensuring only unbilled open obligations (`assessed`) are compiled into bills, fixed pagination deserialization to `PaginatedList<BillingDocumentListItemDto>`, aligned `ReferenceEntityType` to `'financial_obligations'` per ARCHITECTURE, and reset `Factory.CurrentUser` in `FinancialPolicyWorkflowIntegrationTests.cs`.
- Satisfies Stage 1 Completion Criteria 6, 7, and 10.
- All 26 Finance integration tests pass cleanly against Microsoft SQL Server 2022 (`Passed: 26, Failed: 0, Skipped: 0`).
- All 127 Finance unit tests and 26 architecture tests build and pass cleanly.

Changed Files:
- `src/backend/Elsasa.Sis.Finance/Features/Policies/CreateChargeRule/CreateChargeRuleHandler.cs`
- `src/backend/Elsasa.Sis.Finance/Features/Billing/GenerateBillingDocument/GenerateBillingDocumentHandler.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/Fixtures/TestEventCollector.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/Fixtures/IntegrationTestWebAppFactory.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/Elsasa.Sis.IntegrationTests.csproj`
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/FinanceIntegrationTestHelpers.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/StudentAccountLifecycleIntegrationTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/FinancialPolicyWorkflowIntegrationTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/BatchAssessmentWorkflowIntegrationTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/AdHocAssessmentWorkflowIntegrationTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/DerivedAccountBalanceIntegrationTests.cs`
- `tests/backend/Elsasa.Sis.IntegrationTests/Features/Finance/BillingDocumentWorkflowIntegrationTests.cs`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Exercises HTTP endpoints via `WebApplicationFactory<Program>` or direct MediatR command dispatch.


---

## P5 - Frontend State, API Clients & Validation Schemas

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P5-S13

Title: TypeScript Types, Zod Schemas & API Client Modules

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Author TypeScript interfaces, Zod validation schemas, and typed Axios API client modules under `src/frontend/src/features/finance/` for policies, obligations, billing documents, accounts, and shared IDR formatting helpers (`GAP-006`, TD-02, TD-08, ADR-004).

Depends On: P3-S09

Repository: `src/frontend`

Completion Criteria:
- Modules created under `src/frontend/src/features/finance/`:
  1. `policies/`:
     - `types/policies.types.ts`: `FinancialPolicyDto`, `ChargeRuleDto`, `CreateFinancialPolicyRequest`, `CreateChargeRuleRequest`.
     - `schemas/policies.schema.ts`: Zod schemas for policy and charge rule creation.
     - `api/policies.api.ts`: Typed Axios functions (`getPolicies`, `getPolicyById`, `createPolicy`, `approvePolicy`, `createChargeRule`).
  2. `obligations/`:
     - `types/obligations.types.ts`: `FinancialObligationDto`, `DueScheduleDto`, `BatchAssessRequest`, `BatchAssessPreviewResponse`, `BatchAssessCommitResponse`, `AssessAdHocObligationRequest`.
     - `schemas/obligations.schema.ts`: Zod schemas for batch assessment and ad-hoc charge assessment.
     - `api/obligations.api.ts`: Typed Axios functions (`getObligations`, `batchAssessObligations`, `assessAdHocObligation`).
  3. `billing/`:
     - `types/billing.types.ts`: `BillingDocumentDto`, `BillingDocumentItemDto`, `GenerateBillRequest`, `GenerateStatementRequest`.
     - `schemas/billing.schema.ts`: Zod schemas for generating bills and statements.
     - `api/billing.api.ts`: Typed Axios functions (`getBillingDocuments`, `getBillingDocumentById`, `generateBill`, `generateStatement`).
  4. `accounts/`:
     - `types/accounts.types.ts`: `StudentFinancialAccountDto`, `AccountSummaryDto`, `AccountBalanceDto`.
     - `schemas/accounts.schema.ts`: Zod schemas for account search and ad-hoc actions.
     - `api/accounts.api.ts`: Typed Axios functions (`getStudentAccount`, `searchStudentAccounts`).
  5. `shared/`:
     - `currency.utils.ts`: Formats `BIGINT` integer IDR values (`formatIdr(amount)` &rarr; `"Rp 750.000"`).
     - `financeStatus.utils.ts`: Status tags and severity helpers for policies, obligations, and billing documents.
- TypeScript compiles cleanly with `pnpm vue-tsc --noEmit`.

Implementation Notes:
- Authored complete TypeScript interfaces, Zod validation schemas, and typed Axios API client modules under `src/frontend/src/features/finance/`:
  1. `shared/`:
     - `currency.utils.ts`: Exact integer IDR formatting (`formatIdr`) and numeric string parser (`parseIdr`) handling number, BigInt, and string without decimal loss.
     - `financeStatus.utils.ts`: Severity and label mappings for policies, obligations, billing documents, and accounts compatible with PrimeVue Tag.
     - `index.ts`: Barrel export.
  2. `policies/`:
     - `types/policies.types.ts`: `FinancialPolicyDto`, `ChargeRuleDto`, `CreateFinancialPolicyRequest`, `CreateChargeRuleRequest`, `ApproveFinancialPolicyRequest`, and responses.
     - `schemas/policies.schema.ts`: `createFinancialPolicySchema`, `createChargeRuleSchema`, and `approveFinancialPolicySchema` with date ordering refinements and string constraints.
     - `api/policies.api.ts`: Typed Axios client functions (`getPolicies`, `getPolicyById`, `createPolicy`, `approvePolicy`, `createChargeRule`).
     - `index.ts`: Barrel export.
  3. `obligations/`:
     - `types/obligations.types.ts`: `FinancialObligationDto`, `DueScheduleDto`, `BatchAssessCandidateDto`, `BatchAssessRequest`, `BatchAssessPreviewResponse`, `BatchAssessCommitResponse`, `AssessAdHocObligationRequest`, and response models.
     - `schemas/obligations.schema.ts`: `batchAssessObligationsSchema` and `assessAdHocObligationSchema`.
     - `api/obligations.api.ts`: Typed Axios client functions (`getObligations`, `batchAssessObligations`, `assessAdHocObligation`).
     - `index.ts`: Barrel export.
  4. `billing/`:
     - `types/billing.types.ts`: `BillingDocumentDto`, `BillingDocumentItemDto`, `GenerateBillRequest`, `GenerateStatementRequest`, and response containers.
     - `schemas/billing.schema.ts`: `generateBillSchema` and `generateStatementSchema` with date range validation.
     - `api/billing.api.ts`: Typed Axios client functions (`getBillingDocuments`, `getBillingDocumentById`, `generateBill`, `generateStatement`).
     - `index.ts`: Barrel export.
  5. `accounts/`:
     - `types/accounts.types.ts`: `StudentFinancialAccountDto`, `AccountSummaryDto`, `AccountBalanceDto`, `OpenObligationDto`, `RecentBillingDocumentDto`, and query params.
     - `schemas/accounts.schema.ts`: `searchStudentAccountsSchema` and `studentAccountParamsSchema`.
     - `api/accounts.api.ts`: Typed Axios client functions (`getStudentAccount`, `searchStudentAccounts`).
     - `index.ts`: Barrel export.
  6. Top-level barrel export in `src/frontend/src/features/finance/index.ts`.
- Authored comprehensive test suites under `src/frontend/src/features/finance/__tests__/`:
  - `shared.utils.spec.ts` (10 tests)
  - `finance.schemas.spec.ts` (15 tests)
  - `finance.api.spec.ts` (14 tests)
- Verified `pnpm typecheck` (`vue-tsc --noEmit`) passes cleanly with 0 errors.
- Verified all 79 test files (701 unit and integration tests) pass cleanly across Vitest.

Changed Files:
- `src/frontend/src/features/finance/shared/currency.utils.ts`
- `src/frontend/src/features/finance/shared/financeStatus.utils.ts`
- `src/frontend/src/features/finance/shared/index.ts`
- `src/frontend/src/features/finance/policies/types/policies.types.ts`
- `src/frontend/src/features/finance/policies/schemas/policies.schema.ts`
- `src/frontend/src/features/finance/policies/api/policies.api.ts`
- `src/frontend/src/features/finance/policies/index.ts`
- `src/frontend/src/features/finance/obligations/types/obligations.types.ts`
- `src/frontend/src/features/finance/obligations/schemas/obligations.schema.ts`
- `src/frontend/src/features/finance/obligations/api/obligations.api.ts`
- `src/frontend/src/features/finance/obligations/index.ts`
- `src/frontend/src/features/finance/billing/types/billing.types.ts`
- `src/frontend/src/features/finance/billing/schemas/billing.schema.ts`
- `src/frontend/src/features/finance/billing/api/billing.api.ts`
- `src/frontend/src/features/finance/billing/index.ts`
- `src/frontend/src/features/finance/accounts/types/accounts.types.ts`
- `src/frontend/src/features/finance/accounts/schemas/accounts.schema.ts`
- `src/frontend/src/features/finance/accounts/api/accounts.api.ts`
- `src/frontend/src/features/finance/accounts/index.ts`
- `src/frontend/src/features/finance/index.ts`
- `src/frontend/src/features/finance/__tests__/shared.utils.spec.ts`
- `src/frontend/src/features/finance/__tests__/finance.schemas.spec.ts`
- `src/frontend/src/features/finance/__tests__/finance.api.spec.ts`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Adheres to frontend architecture standards and integer monetary representation.

---

### P5-S14

Title: TanStack Query & Mutation Composables

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement TanStack Query composables and cache invalidation strategies under `src/frontend/src/features/finance/` for reactive data fetching and mutation handling (`GAP-006`, TD-08).

Depends On: P5-S13

Repository: `src/frontend`

Completion Criteria:
- Composables authored:
  1. `policies/composables/useFinancePolicies.ts`:
     - `useFinancePoliciesQuery(filters)`
     - `useFinancePolicyDetailQuery(policyId)`
     - `useCreatePolicyMutation()` (invalidates policies query)
     - `useApprovePolicyMutation()` (invalidates policies query)
     - `useCreateChargeRuleMutation()` (invalidates policy detail query)
  2. `obligations/composables/useFinanceObligations.ts`:
     - `useFinanceObligationsQuery(filters)`
     - `useBatchAssessPreviewMutation()`
     - `useBatchAssessCommitMutation()` (invalidates obligations query and account balance query)
     - `useAdHocAssessMutation()` (invalidates obligations query and account balance query)
  3. `billing/composables/useFinanceBilling.ts`:
     - `useBillingDocumentsQuery(filters)`
     - `useBillingDocumentDetailQuery(documentId)`
     - `useGenerateBillMutation()` (invalidates billing documents query and obligations query)
     - `useGenerateStatementMutation()`
  4. `accounts/composables/useFinanceAccounts.ts`:
     - `useStudentAccountQuery(studentId)`
     - `useSearchStudentAccountsQuery(params)`
- Query keys structured cohesively: `financeKeys.policies.*`, `financeKeys.obligations.*`, `financeKeys.billing.*`, `financeKeys.accounts.*`.
- TypeScript compiles cleanly.

Implementation Notes:
- Authored cohesive query key factory `financeKeys` in `src/frontend/src/features/finance/shared/queryKeys.ts` supporting hierarchical query invalidation (`financeKeys.policies.*`, `financeKeys.obligations.*`, `financeKeys.billing.*`, `financeKeys.accounts.*`).
- Authored standard toast notification helper `src/frontend/src/features/finance/shared/toastNotifications.ts` for PrimeVue toast integration.
- Authored complete TanStack Query composable suites:
  1. `policies/composables/useFinancePolicies.ts`:
     - `useFinancePoliciesQuery(filters)` using `CACHE_TIERS.OPERATIONAL`
     - `useFinancePolicyDetailQuery(policyId)` using `CACHE_TIERS.OPERATIONAL`
     - `useCreatePolicyMutation()` invalidating `financeKeys.policies.all`
     - `useApprovePolicyMutation()` invalidating `financeKeys.policies.all`
     - `useCreateChargeRuleMutation()` invalidating `financeKeys.policies.detail(variables.policyId)`
  2. `obligations/composables/useFinanceObligations.ts`:
     - `useFinanceObligationsQuery(filters)` using `CACHE_TIERS.ACTIVE_QUEUE`
     - `useBatchAssessPreviewMutation()` for dry-run previews without cache invalidation
     - `useBatchAssessCommitMutation()` invalidating `financeKeys.obligations.all` and `financeKeys.accounts.all`
     - `useAdHocAssessMutation()` invalidating `financeKeys.obligations.all` and `financeKeys.accounts.all` (and specific student detail)
  3. `billing/composables/useFinanceBilling.ts`:
     - `useBillingDocumentsQuery(filters)` using `CACHE_TIERS.ACTIVE_QUEUE`
     - `useBillingDocumentDetailQuery(documentId)` using `CACHE_TIERS.TRANSACTIONAL`
     - `useGenerateBillMutation()` invalidating `financeKeys.billing.all`, `financeKeys.obligations.all`, and `financeKeys.accounts.all`
     - `useGenerateStatementMutation()` invalidating `financeKeys.billing.all`
  4. `accounts/composables/useFinanceAccounts.ts`:
     - `useStudentAccountQuery(studentId)` using `CACHE_TIERS.TRANSACTIONAL` for real-time derived balance projection
     - `useSearchStudentAccountsQuery(params)` using `CACHE_TIERS.ACTIVE_QUEUE`
- Updated feature index and sub-barrel exports across `policies`, `obligations`, `billing`, `accounts`, and `shared`.
- Authored exhaustive Vitest test suite `src/frontend/src/features/finance/__tests__/finance.composables.spec.ts` (21 unit tests covering all composables, query key derivations, invalidation sweeps, and error toasts).
- Verified `vue-tsc --noEmit` compiles cleanly with 0 errors.
- Verified all 80 frontend test files (722 unit tests) pass cleanly across Vitest.

Changed Files:
- `src/frontend/src/features/finance/shared/queryKeys.ts`
- `src/frontend/src/features/finance/shared/toastNotifications.ts`
- `src/frontend/src/features/finance/shared/index.ts`
- `src/frontend/src/features/finance/policies/composables/useFinancePolicies.ts`
- `src/frontend/src/features/finance/policies/index.ts`
- `src/frontend/src/features/finance/obligations/composables/useFinanceObligations.ts`
- `src/frontend/src/features/finance/obligations/index.ts`
- `src/frontend/src/features/finance/billing/composables/useFinanceBilling.ts`
- `src/frontend/src/features/finance/billing/index.ts`
- `src/frontend/src/features/finance/accounts/composables/useFinanceAccounts.ts`
- `src/frontend/src/features/finance/accounts/index.ts`
- `src/frontend/src/features/finance/__tests__/finance.composables.spec.ts`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Adheres to frontend architecture guidelines: server state encapsulated in TanStack Query composables with cache invalidation pipelines.

---

## P6 - Frontend Workspaces & Portal UI Components

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P6-S15

Title: Financial Policy Workspace UI (`SCR-FIN-04`, `/finance/policies`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement `SCR-FIN-04` Financial Policy Workspace at `/finance/policies` for the Finance Authority to govern policies, version charge rules, and grant formal approvals (`FIN-WF-01`, `UC-FIN-10`, `SCR-FIN-04`, `GAP-006`, TD-08).

Depends On: P5-S14

Repository: `src/frontend`

Completion Criteria:
- Components created in `src/frontend/src/features/finance/policies/`:
  - `views/FinancialPolicyWorkspaceView.vue`: Master-detail layout displaying policies table, status pills (`draft`, `active`, `superseded`), selected policy details, and attached charge rules table.
  - `components/CreatePolicyModal.vue`: PrimeVue Dialog with VeeValidate and Zod validation for creating a policy draft (`code`, `name`, `category`, `purpose`, `effectiveDate`).
  - `components/CreateChargeRuleModal.vue`: PrimeVue Dialog for adding a charge rule (`ruleCode`, `name`, `chargeType`, `baseAmount`, `recurrencePattern`, `dueRuleType`, `dueRuleValue`, `applicableProgram`).
  - Approval action button: Visible and enabled for users with `Permissions.Finance.Policies.Approve` (`Finance Authority`) to approve draft policies.
- Route mapped in `src/frontend/src/app/router/routes.ts`: `/finance/policies` &rarr; `FinancialPolicyWorkspaceView.vue`, meta: `permission: 'Permissions.Finance.Policies.View'`.
- Successfully renders and interacts cleanly without console errors.

Implementation Notes:
- Authored client permission constants in `src/frontend/src/features/finance/policies/constants/permissions.ts`.
- Authored `CreatePolicyModal.vue` PrimeVue dialog integrated with VeeValidate and Zod schema (`createFinancialPolicySchema`) for drafting policies with validation for code, name, category, purpose, and date ranges.
- Authored `CreateChargeRuleModal.vue` PrimeVue dialog integrated with VeeValidate and Zod schema (`createChargeRuleSchema`) for adding charge rules attached to governing policies with integer IDR base amounts and due rule derivation settings.
- Authored `FinancialPolicyWorkspaceView.vue` implementing the complete master-detail layout for `SCR-FIN-04`:
  - Master panel listing policies with status pills (`StatusPill`), category tags, and version badges.
  - Category and status pill filters with reactive counter badges, plus debounced text search.
  - Selected policy dossier projection showing business purpose, effective dates, approval state, and creator.
  - Attached charge rules table with formatted IDR amounts (`formatIdr`), recurrence patterns, due rule parameters, and applicability scopes.
  - Formal policy approval workflow dialog enabled for users with `Permissions.Finance.Policies.Approve` (`Finance Authority`) when policy is in `draft` status, transitioning it to `active`.
  - Add Charge Rule button enabled for authorized policy administrators.
- Re-exported components and views in `src/frontend/src/features/finance/policies/index.ts`.
- Mapped `/finance/policies` route in `src/frontend/src/app/router/routes.ts` protected by `Permissions.Finance.Policies.View`.
- Added navigation link in `src/frontend/src/app/layouts/AppLayout.vue` under Finance domain.
- Authored comprehensive Vitest test suite `src/frontend/src/features/finance/__tests__/FinancialPolicyWorkspaceView.spec.ts` (8 tests covering master-detail rendering, approval authorization gating, approval submission mutation, and modal dialogs).
- Verified `vue-tsc --noEmit` typecheck passes with 0 errors and all unit tests pass cleanly across Vitest.

Changed Files:
- `src/frontend/src/features/finance/policies/constants/permissions.ts`
- `src/frontend/src/features/finance/policies/components/CreatePolicyModal.vue`
- `src/frontend/src/features/finance/policies/components/CreateChargeRuleModal.vue`
- `src/frontend/src/features/finance/policies/views/FinancialPolicyWorkspaceView.vue`
- `src/frontend/src/features/finance/policies/index.ts`
- `src/frontend/src/app/router/routes.ts`
- `src/frontend/src/app/layouts/AppLayout.vue`
- `src/frontend/src/features/finance/__tests__/FinancialPolicyWorkspaceView.spec.ts`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Adheres to Planning screen pattern specified in `screen-inventory.md`.

---

### P6-S16

Title: Billing Cycle Workspace UI (`SCR-FIN-01`, `/finance/billing-cycles`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement `SCR-FIN-01` Billing Cycle Workspace at `/finance/billing-cycles` for the Finance Officer to execute guided batch fee assessments with candidate preview, exception review, and bulk obligation posting (`FIN-WF-02`, `UC-FIN-02`, `SCR-FIN-01`, `GAP-006`, TD-08).

Depends On: P5-S14

Repository: `src/frontend`

Completion Criteria:
- Components created in `src/frontend/src/features/finance/billing/`:
  - `views/BillingCycleWorkspaceView.vue`: Guided multi-step billing cycle wizard layout.
  - `components/BillingCycleStepper.vue`: Step navigation tracking:
    - Step 1: Select Charge Rule & Academic Period (dropdown of active charge rules and operational periods).
    - Step 2: Candidate Preview & Exception Validation (displays preview statistics, candidate count, duplicate indicators, gross amount).
    - Step 3: Confirmation & Commit (prompts user to post obligations).
    - Step 4: Completion Summary & Optional Bill Generation trigger.
  - `components/CandidatePreviewTable.vue`: PrimeVue DataTable displaying previewed candidates, student number, student name, grade, amount, and duplicate badge.
- Route mapped in `src/frontend/src/app/router/routes.ts`: `/finance/billing-cycles` &rarr; `BillingCycleWorkspaceView.vue`, meta: `permission: 'Permissions.Finance.Obligations.Assess'`.
- Interacts seamlessly with `useBatchAssessPreviewMutation` and `useBatchAssessCommitMutation`.

Implementation Notes:
- Authored `CandidatePreviewTable.vue` in `src/frontend/src/features/finance/billing/components/` utilizing PrimeVue DataTable, Column, Tag, with exact IDR formatting (`formatIdr`), due date display, student search filtering, pagination, and prominent duplicate exclusion and eligible status badges.
- Authored `BillingCycleStepper.vue` in `src/frontend/src/features/finance/billing/components/` implementing multi-step cycle progress tracking (Step 1: Scope & Parameters, Step 2: Candidate Preview, Step 3: Confirm & Commit, Step 4: Completion & Next Actions) with visual step indicators, checkmark icons on completion, and backward navigation support.
- Authored `BillingCycleWorkspaceView.vue` in `src/frontend/src/features/finance/billing/views/` realizing `SCR-FIN-01`:
  - Step 1: Charge rule dropdown from active financial policies, operational period dropdown from selected academic year, auditable billing cycle label auto-suggestion, and optional campus/grade cohort filters.
  - Step 2: Pre-posting exception evaluation driven by `useBatchAssessPreviewMutation`, presenting candidate statistics (total evaluated, eligible, duplicate count, gross assessment), duplicate conflict warning banner (ADR-003, GAP-003), and interactive candidate preview table.
  - Step 3: Review summary breakdown of charge rule parameters, period label, eligible student count, and gross obligation amount with authoritative financial commit warning notice.
  - Step 4: Bulk obligation posting driven by `useBatchAssessCommitMutation`, completion summary banner, committed metrics breakdown, optional batch bill compilation trigger dialog driven by `useGenerateBillMutation`, ledger link to `/finance/obligations`, and wizard reset trigger.
- Exported components and views in `src/frontend/src/features/finance/billing/components/index.ts` and `src/frontend/src/features/finance/billing/index.ts`.
- Mapped `/finance/billing-cycles` route in `src/frontend/src/app/router/routes.ts` protected by `Permissions.Finance.Obligations.Assess`.
- Added "Billing Cycles" navigation item in `src/frontend/src/app/layouts/AppLayout.vue` under Finance domain.
- Authored comprehensive Vitest test suite `src/frontend/src/features/finance/__tests__/BillingCycleWorkspaceView.spec.ts` (10 tests covering stepper indicators, candidate table formatting and search filtering, mandatory form validation, preview mutation dispatch, confirmation commit mutation dispatch, wizard reset, and batch bill generation).
- Aligned Zod validation schemas in `policies.schema.ts` with test expectations for enums and notes length.
- Verified `vue-tsc --noEmit` compiles cleanly with 0 errors and all 89 frontend test files (780 tests) pass cleanly across Vitest.

Changed Files:
- `src/frontend/src/features/finance/billing/components/CandidatePreviewTable.vue`
- `src/frontend/src/features/finance/billing/components/BillingCycleStepper.vue`
- `src/frontend/src/features/finance/billing/components/index.ts`
- `src/frontend/src/features/finance/billing/views/BillingCycleWorkspaceView.vue`
- `src/frontend/src/features/finance/billing/index.ts`
- `src/frontend/src/features/finance/policies/schemas/policies.schema.ts`
- `src/frontend/src/app/router/routes.ts`
- `src/frontend/src/app/layouts/AppLayout.vue`
- `src/frontend/src/features/finance/__tests__/BillingCycleWorkspaceView.spec.ts`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Adheres to Cycle / Planning pattern specified in `screen-inventory.md`.

---

### P6-S17

Title: Student Accounts Workbench UI (`SCR-FIN-03`, `/finance/accounts`) & Family Portal Tab (`SCR-SELF-02`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Implement `SCR-FIN-03` Student Accounts Workbench at `/finance/accounts` for student financial ledger inquiries, ad-hoc charge assessments, and billing document viewing, and extend `SCR-SELF-02` Family Portal with a billing tab (`FIN-WF-04`, `UC-FIN-03`, `SCR-FIN-03`, `SCR-SELF-02`, `GAP-006`, TD-08).

Depends On: P5-S14, P6-S15, P6-S16

Repository: `src/frontend`

Completion Criteria:
- Components created in `src/frontend/src/features/finance/accounts/`:
  - `views/StudentAccountsWorkbenchView.vue`: Student search auto-complete, student financial account dossier layout.
  - `components/AccountBalanceCard.vue`: Card displaying account number, status badge, total assessed, total settled, and prominent derived balance in IDR.
  - `components/ObligationsTable.vue`: Table of obligations showing obligation number, charge type, original amount, due date, and status. Includes button to trigger bill generation for open obligations.
  - `components/BillingDocumentsTable.vue`: Table of issued bills and statements with action to view invoice details.
  - `components/AssessAdHocFeeModal.vue`: PrimeVue Dialog to assess an individual student charge (`chargeType`, `description`, `amount`, `dueDate`).
  - `components/InvoiceDetailModal.vue`: Modal displaying formatted invoice snapshot with recipient contact, billing period, and frozen itemized table.
- Family Portal extension:
  - `src/frontend/src/features/portal/FamilyBillingTab.vue`: Payer-facing tab displaying open bills, due dates, amount, and detailed line items.
  - Integrated into `FamilyTasksView.vue` or family portal routes.
- Route mapped in `src/frontend/src/app/router/routes.ts`: `/finance/accounts` &rarr; `StudentAccountsWorkbenchView.vue`, meta: `permission: 'Permissions.Finance.Accounts.View'`.
- Full navigation and modal interactions work cleanly.

Implementation Notes:
- Authored client permission constants in `src/frontend/src/features/finance/accounts/constants/permissions.ts`.
- Authored `AccountBalanceCard.vue` displaying account number, status badge, total assessed, total settled, and prominent derived current balance in IDR with action buttons.
- Authored `ObligationsTable.vue` displaying obligations with obligation number, description, charge type, formatted IDR amount, due date, status pill, multi-select capability, and compile bill trigger button.
- Authored `BillingDocumentsTable.vue` displaying billing documents with document number, type, recipient details, issue date, due date, amount, delivery status, and invoice view action.
- Authored `AssessAdHocFeeModal.vue` PrimeVue dialog with VeeValidate/Zod schema validation to assess single-student fee obligations.
- Authored `InvoiceDetailModal.vue` displaying formatted invoice snapshot with recipient contact, billing timeline, and frozen itemized table.
- Authored `StudentAccountsWorkbenchView.vue` realizing `SCR-FIN-03` at `/finance/accounts` with student search auto-complete, student accounts directory, and 360-degree dossier layout.
- Authored `FamilyBillingTab.vue` and `FamilyTasksView.vue` in `src/frontend/src/features/portal/` realizing `SCR-SELF-02` with outstanding balance banner, open bills list, and invoice viewing.
- Updated barrel exports in `src/frontend/src/features/finance/accounts/index.ts` and `src/frontend/src/features/portal/index.ts`.
- Mapped `/finance/accounts` and `/finance/accounts/:studentId` in `src/frontend/src/app/router/routes.ts` protected by `Permissions.Finance.Accounts.View`, and `/portal/family` for `SCR-SELF-02`.
- Added "Student Accounts" navigation item in `src/frontend/src/app/layouts/AppLayout.vue` under Finance domain.
- Authored comprehensive test suite in `src/frontend/src/features/finance/__tests__/StudentAccountsWorkbenchView.spec.ts` (11 tests).
- Verified `vue-tsc --noEmit` and all 90 frontend test files (791 tests) pass cleanly across Vitest.

Changed Files:
- `src/frontend/src/features/finance/accounts/constants/permissions.ts`
- `src/frontend/src/features/finance/accounts/components/AccountBalanceCard.vue`
- `src/frontend/src/features/finance/accounts/components/ObligationsTable.vue`
- `src/frontend/src/features/finance/accounts/components/BillingDocumentsTable.vue`
- `src/frontend/src/features/finance/accounts/components/AssessAdHocFeeModal.vue`
- `src/frontend/src/features/finance/accounts/components/InvoiceDetailModal.vue`
- `src/frontend/src/features/finance/accounts/views/StudentAccountsWorkbenchView.vue`
- `src/frontend/src/features/finance/accounts/index.ts`
- `src/frontend/src/features/portal/FamilyBillingTab.vue`
- `src/frontend/src/features/portal/FamilyTasksView.vue`
- `src/frontend/src/features/portal/index.ts`
- `src/frontend/src/features/finance/obligations/schemas/obligations.schema.ts`
- `src/frontend/src/app/router/routes.ts`
- `src/frontend/src/app/layouts/AppLayout.vue`
- `src/frontend/src/features/finance/__tests__/StudentAccountsWorkbenchView.spec.ts`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Adheres to Work Queue pattern specified in `screen-inventory.md`.

---

## P7 - Frontend Automated Verification & Component Testing

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P7-S18

Title: Vitest Unit Tests for Schemas, API Clients & Composables

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Author unit test suites under `src/frontend/src/features/finance/**/__tests__/` verifying Zod validation schemas, API client wrappers, and TanStack Query composables with mocked HTTP responses.

Depends On: P5-S13, P5-S14

Repository: `src/frontend`

Completion Criteria:
- Test suites created:
  1. `policies.schema.spec.ts`: Tests policy and charge rule creation validation rules.
  2. `obligations.schema.spec.ts`: Tests batch assess and ad-hoc charge validation rules.
  3. `billing.schema.spec.ts`: Tests bill generation validation rules.
  4. `policies.api.spec.ts`: Tests Axios client functions for policy endpoints.
  5. `obligations.api.spec.ts`: Tests Axios client functions for obligation endpoints.
  6. billing.api.spec.ts: Tests Axios client functions for billing document endpoints.
  7. `accounts.api.spec.ts`: Tests Axios client functions for account endpoints.
  8. `currencyUtils.spec.ts`: Tests IDR currency integer formatting with various numbers.
- All unit tests pass cleanly with `pnpm test:unit`.

Implementation Notes:
- Authored dedicated unit test suites under `src/frontend/src/features/finance/__tests__/`:
  1. `policies.schema.spec.ts` (13 tests): Validates policy draft creation, policy code format (`FP-YYYY-NNN`), date ordering rules (`endDate >= effectiveDate`), required fields, charge rule creation, negative/fractional base amount rejection, recurrence patterns, due rule types, and policy approval schemas.
  2. `obligations.schema.spec.ts` (9 tests): Validates batch billing cycle assessment schema across preview and commit modes, charge rule and academic period UUIDs, label constraints, optional campus/grade filters, and ad-hoc fee assessment schema (positive integer amount, ISO date, UUIDs).
  3. `billing.schema.spec.ts` (11 tests): Validates bill generation schema with optional obligation ID lists, payment due dates, and statement generation across date ranges (enforcing `billingPeriodEnd >= billingPeriodStart`).
  4. `policies.api.spec.ts` (7 tests): Tests Axios client functions for `getPolicies`, `getPolicyById`, `createPolicy`, `approvePolicy`, and `createChargeRule` with parameter sanitization and encoded URLs.
  5. `obligations.api.spec.ts` (5 tests): Tests Axios client functions for `getObligations`, `batchAssessObligations` (preview and commit), and `assessAdHocObligation`.
  6. `billing.api.spec.ts` (5 tests): Tests Axios client functions for `getBillingDocuments`, `getBillingDocumentById`, `generateBill`, and `generateStatement`.
  7. `accounts.api.spec.ts` (3 tests): Tests Axios client functions for `getStudentAccount` (360-degree dossier) and `searchStudentAccounts`.
  8. `currencyUtils.spec.ts` (9 tests): Validates IDR integer currency formatting (`formatIdr`) and parsing (`parseIdr`) with regular integers, BigInt values, numeric strings, zero, negative numbers, null, undefined, and non-numeric inputs without decimal precision loss.
  9. `accounts.schema.spec.ts` (7 tests): Validates student account search parameters and UUID params.
  10. Verified `finance.composables.spec.ts` (21 tests) covering `useFinancePolicies`, `useFinanceObligations`, `useFinanceBilling`, and `useFinanceAccounts` TanStack Query composables, mutations, query key factory, and cache invalidation.
- Removed deprecated monolithic files `finance.schemas.spec.ts` and `finance.api.spec.ts`.
- Verified all 87 test files (762 tests) pass cleanly across Vitest with `pnpm test:unit`.
- Verified TypeScript compilation passes cleanly with 0 errors via `vue-tsc --noEmit`.

Changed Files:
- `src/frontend/src/features/finance/__tests__/policies.schema.spec.ts`
- `src/frontend/src/features/finance/__tests__/obligations.schema.spec.ts`
- `src/frontend/src/features/finance/__tests__/billing.schema.spec.ts`
- `src/frontend/src/features/finance/__tests__/policies.api.spec.ts`
- `src/frontend/src/features/finance/__tests__/obligations.api.spec.ts`
- `src/frontend/src/features/finance/__tests__/billing.api.spec.ts`
- `src/frontend/src/features/finance/__tests__/accounts.api.spec.ts`
- `src/frontend/src/features/finance/__tests__/currencyUtils.spec.ts`
- `src/frontend/src/features/finance/__tests__/accounts.schema.spec.ts`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Fast in-memory unit tests using Vitest.

---

### P7-S19

Title: Vitest Component & Workspace Integration Tests

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:  
Author component and view integration tests under `src/frontend/src/features/finance/**/__tests__/` verifying rendering, user interactions, dialogs, and permission gating for all Finance workspaces.

Depends On: P6-S15, P6-S16, P6-S17, P7-S18

Repository: `src/frontend`

Completion Criteria:
- Test suites created / verified:
  1. `FinancialPolicyWorkspaceView.spec.ts`:
     - Verifies rendering of policy list and charge rules table.
     - Verifies policy creation dialog opens and submits data.
     - Verifies approval button is displayed only when authorized.
  2. `BillingCycleWorkspaceView.spec.ts`:
     - Verifies stepper navigation across steps 1, 2, 3, 4.
     - Verifies candidate preview table renders eligible students and duplicate badges.
     - Verifies commit execution triggers mutation.
  3. `StudentAccountsWorkbenchView.spec.ts`:
     - Verifies account balance card displays formatted IDR derived balance.
     - Verifies ad-hoc charge modal opens, validates, and submits.
     - Verifies invoice detail modal renders frozen line items.
  4. `FamilyBillingTab.spec.ts`:
     - Verifies portal billing tab lists issued bills and due dates for parents.
- All component tests pass cleanly with `pnpm test:unit`.
- Clean TypeScript compilation (`vue-tsc --noEmit`).

Implementation Notes:
- Authored and verified complete workspace component and view integration test suites under `src/frontend/src/features/finance/__tests__/`:
  1. `FinancialPolicyWorkspaceView.spec.ts` (10 tests):
     - Verifies master-detail view layout, category tags, version badges, and policy table rendering.
     - Verifies dossier details and attached charge rules table with formatted integer IDR amounts.
     - Verifies permission-gated approval button display (only for users with `Permissions.Finance.Policies.Approve`).
     - Verifies approval dialog opens and dispatches `useApprovePolicyMutation` with approving authority identity.
     - Verifies policy creation modal opens on authorized button click and submits valid draft policy data via `useCreatePolicyMutation`.
  2. `BillingCycleWorkspaceView.spec.ts` (10 tests):
     - Verifies stepper navigation across steps 1, 2, 3, 4 with completed checkmarks and backward navigation.
     - Verifies candidate preview table rendering eligible students, formatted IDR base amounts, search filtering, and duplicate badges (`Duplicate (Excluded)` vs `Eligible`).
     - Verifies step 1 form validation, preview mutation execution with statistics, step 3 confirmation breakdown, and commit mutation posting obligations to step 4.
  3. `StudentAccountsWorkbenchView.spec.ts` (13 tests):
     - Verifies `AccountBalanceCard.vue` rendering account number, status badge, total assessed, total settled, and derived current balance in formatted IDR.
     - Verifies `ObligationsTable.vue` displaying open obligations with batch compile bill trigger.
     - Verifies `BillingDocumentsTable.vue` displaying document records with view details trigger.
     - Verifies `AssessAdHocFeeModal.vue` opening, validating form fields, submitting single-student charges, and invoking `assessAdHocObligation`.
     - Verifies `InvoiceDetailModal.vue` rendering frozen line items snapshot with recipient contact information and total IDR amount.
     - Verifies 360-degree workbench dossier view flow and opening ad-hoc modal from balance card.
  4. `FamilyBillingTab.spec.ts` (6 tests):
     - Verifies parent-facing billing tab displaying outstanding balance summary banner in IDR (`formatIdr`).
     - Verifies list of issued bills with document numbers, issue dates, and payment due dates.
     - Verifies filter switching across 'All Documents', 'Open Bills', and 'Settled'.
     - Verifies zero balance empty state when all bills are settled.
     - Verifies invoice details snapshot modal opening and emitting `viewInvoice`.
- Verified TypeScript compilation passes cleanly with 0 errors via `vue-tsc --noEmit`.
- Verified all 91 test files (801 tests) pass cleanly across Vitest with `pnpm test:unit`.

Changed Files:
- `src/frontend/src/features/finance/__tests__/FamilyBillingTab.spec.ts`
- `src/frontend/src/features/finance/__tests__/FinancialPolicyWorkspaceView.spec.ts`
- `src/frontend/src/features/finance/__tests__/StudentAccountsWorkbenchView.spec.ts`
- `docs/issues/IS006 - FIN-OBLIGATIONS-BILLING-IMPLEMENTATION-PLAN.md`

Notes:
- Uses `@vue/test-utils` and mocked TanStack Query providers.

---

# 6. Change Log

- **2026-11-04 (Initial Plan Formulation):** Formulated comprehensive 7-phase, 19-slice executable implementation plan for Stage 1 Obligations & Billing under the Knowledge-Centric SDLC. Enforced strict continuous numbering (`P1-S01` through `P7-S19`), single-repository ownership per slice, integer IDR monetary standard, derived balance engine, and dual-mode obligation assessment architecture. Granted `Execution Approval: APPROVED`.

