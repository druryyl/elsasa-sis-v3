# ISSUE

## Metadata

ID: CR-FIN-PAYMENTS-RECEIVABLES
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-1 Payments & Receivables Core Submodules

## Source

Reported By: Drury
Reported Date: 2026-11-04

## Description

Implement the core submodules of Stage 1 "Payments & Receivables" under the Finance domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses two core submodules and one specialized inquiry gate:
1. **Payment Receipt & Confirmation (`FIN-WF-05`):**
   - Manage payer records (`payers`) representing parents, guardians, sponsors, or organizations responsible for making payments on behalf of students.
   - Record payment transactions (`payments`) received through diverse supported payment methods (bank transfer, cash, POS card, QRIS, virtual account, cheque) in IDR against student financial accounts (`student_financial_accounts`).
   - Distinctly separate payment submission from formal verification and confirmation by an authorized Finance Officer (`confirmation_status`: `submitted`, `confirmed`, `rejected`, `reversed`).
   - Issue official sequential payment receipt vouchers (`receipt_number`, `receipt_issued_at`) with immutable audit logging upon confirmation.
2. **Payment Allocation & Reallocation (`FIN-WF-06`):**
   - Apply confirmed payment value or available credit balances to settle specific outstanding financial obligations (`financial_obligations`).
   - Support one-to-one, split allocation across multiple obligations (one-to-many), and multiple payments settling a single obligation (many-to-one), as well as partial settlements (`payment_allocations`).
   - Retain unallocated funds as excess payment/account credit.
   - Support auditable reallocation or reversal of erroneous allocations with preserved historical lineage (`superseded_by_id`, `reversal_reason`).
3. **Enrollment Payment Gate (`FIN-WF-13`, `GAP-01`):**
   - Provide a real-time inquiry gate for the Students domain (`SCR-STU-01`, `STD-WF-03`) to verify whether mandatory admission/enrollment obligations for an accepted applicant have reached `confirmed` status and full allocation.
   - Support authorized administrative override per `GAP-01` resolution (hard block by default with logged administrative override).

Scope Boundaries & Deferrals:
Stage 2 and Stage 3 capabilities of Payments & Receivables and broader Finance modules are explicitly out of scope for this issue:
- Receivables aging tracking, overdue management, reminder dispatches (`overdue_reminders`), and installment payment arrangements (`FIN-WF-07`) are deferred to Stage 2.
- Benefits management (scholarships, discounts, waivers) (`FIN-WF-03`) are deferred to Stage 2.
- Refunds, disputes, and financial adjustments (`FIN-WF-08`, `FIN-WF-10`) are deferred to Stage 2.
- Full credit transfers and advanced overpayment management (`FIN-WF-09`) are deferred to Stage 2.
- Formal purpose-specific exit financial clearance (`FIN-WF-11`) is deferred to Stage 3.

## Desired Outcome

1. Payers and parents can submit fee payments with external transaction references or documentary proof (`SCR-SELF-02`, `UC-FIN-01`, `FIN-WF-05`).
2. Finance Officers can verify submitted payments against bank deposits/statements, confirm receipt with immutable audit trail, and issue official payment receipts (`SCR-FIN-02`, `UC-FIN-04`, `FIN-WF-05`).
3. Finance Officers can allocate confirmed payment funds to outstanding obligations (application fees, enrollment fees, tuition) and execute auditable reallocations when necessary (`SCR-FIN-02`, `SCR-FIN-03`, `UC-FIN-05`, `FIN-WF-06`).
4. Real-time inquiry interface / gate confirms enrollment fee settlement status to the Students domain (`SCR-STU-01`, `UC-FIN-09`, `FIN-WF-13`, `GAP-01`), enabling student intake unblocking.
5. Student accounts reflect updated balances and allocation history dynamically in `SCR-FIN-03` without ledger desynchronization.
6. Meets Stage 1 Completion Criteria:
   - **Criterion 8:** "A payment is received, confirmed, and allocated against an outstanding obligation."
   - **Criterion 9:** "Enrollment payment confirmation is available to Students when policy requires it."
   - **Criterion 10:** "All domain events for the above are recorded and historically traceable."

## Current Situation

1. Schema status: The database currently provisions tables for `financial_policies`, `charge_rules`, `student_financial_accounts`, `financial_obligations`, `obligation_due_schedules`, `billing_documents`, and `billing_document_items` (from Stage 1 Obligations & Billing `IS006`). However, the tables for payers, payments, and allocations (`payers`, `payments`, `payment_allocations`) specified in `docs/entities/finance-entities-tables.md` (Submodule 3) are not yet provisioned.
2. Backend status: In `src/backend/Elsasa.Sis.Finance`, `FinanceModule.cs` contains only a stub `IsEnrollmentFeePaidAsync` returning `false`. There are no domain models, repositories, commands/queries, MediatR handlers, or API endpoints for payer registration, payment recording, payment confirmation, payment allocation, reallocation, or enrollment payment gate inquiry.
3. Frontend status: `SCR-FIN-02` (Payment Verification Queue) is not yet implemented. `SCR-FIN-03` (Student Accounts Workbench) has only an initial shell without payment and allocation inspection. `SCR-SELF-02` (Family Tasks & Requests Portal) does not yet support payment submission or receipt display. `SCR-STU-01` (Student Intake Workbench) does not yet integrate real-time payment confirmation for enrollment.
4. Upstream prerequisites: Calendar & Periods (`IS001`, `IS003`), Student Admission (`IS002`), Student Enrollment & Identity (`IS004`), Placement & Status (`IS005`), and Finance Obligations & Billing (`IS006`) provide the active academic periods, student identities, financial accounts, and assessed fee obligations required to receive and allocate payments.

## Evidence

- Roadmap Specification: `docs/sub-modul-and-development-phases.md` (Stage 1, Module: Payments & Receivables — core submodules: Payment Receipt & Confirmation, Payment Allocation & Reallocation; Section D Criteria 8, 9, 10; Section E GAP-01 resolution).
- Finance Domain Blueprint: `docs/domains/finance-domain.md` (Section: Payment Receipt and Confirmation; Section: Payment Allocation; Section: Credit and Overpayment; Section: Student Financial Account; Section: Indonesian School Context & Operational Principles).
- Finance Entities & Schema Specification: `docs/entities/finance-entities.md` (Submodule 3 — Payments & Receivables: `Payer`, `Payment`, `Payment Allocation`; Resolved Decision 4: Shared Payer Entity Model; Resolved Decision 5: Student-Centric Account Model; Resolved Decision 6: Monetary Values in IDR).
- Finance Database Tables Specification: `docs/entities/finance-entities-tables.md` (Submodule 3: `payers`, `payments`, `payment_allocations`, lines 501–607).
- Operational Workflow Discovery: `docs/operational-workflow-discovery.md` (`FIN-WF-05: Receive and Confirm Payment`, `FIN-WF-06: Allocate or Reallocate Payment`, `FIN-WF-13: Confirm Enrollment Payment Status`; Section 6 GAP-01 Resolution).
- Use Cases: `docs/use-cases.md` (`UC-FIN-01: Submit Fee Payment`, `UC-FIN-04: Confirm Payment Receipt`, `UC-FIN-05: Allocate Payment to Obligations`, `UC-FIN-09: Confirm Enrollment Payment Gate`).
- Screen Inventory: `docs/screen-inventory.md` (`SCR-FIN-02: Payment Verification Queue`, `SCR-FIN-03: Student Accounts Workbench (Stage 1 initial scope)`, `SCR-SELF-02: Family Tasks & Requests Portal`, `SCR-STU-01: Student Intake Workbench`).
- Existing Baseline Code: `src/backend/Elsasa.Sis.Finance/Services/FinanceModule.cs`, `src/frontend/src/features/finance/`.

## Notes

- This artifact serves as the formal change intake. It defines WHAT capability is requested and the operational outcomes expected, maintaining solution neutrality without prescribing technical architecture, API schemas, or UI implementations.
- Scope boundary discipline: Core payment receipt, cashier confirmation, obligation allocation, and enrollment payment gate belong to this Stage 1 core issue. Benefits (scholarships/discounts/waivers), receivables aging, overdue reminders, installment payment arrangements, refunds, disputes, adjustments, and financial clearance belong to Stage 2 and Stage 3 and must not be conflated with this issue.
- Technical feasibility assessment, domain modeling, feature decomposition, and architecture updates will be performed in downstream SDLC stages.
