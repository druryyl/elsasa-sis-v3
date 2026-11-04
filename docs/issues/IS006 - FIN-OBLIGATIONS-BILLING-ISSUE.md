# ISSUE

## Metadata

ID: CR-FIN-OBLIGATIONS-BILLING
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage-1 Obligations & Billing Full Scope

## Source

Reported By: Drury
Reported Date: 2026-11-04

## Description

Implement the full scope of Stage 1 "Obligations & Billing" under the Finance domain as planned in the SIS implementation roadmap (`docs/sub-modul-and-development-phases.md`).

The scope encompasses four cohesive submodules:
1. **Financial Policy & Charge Rules:** Establish, version, and approve institutional financial policies and concrete charge rules that govern student financial obligations. Defines fee categories (tuition, admission fee, enrollment fee, activity fee, etc.), base amounts in IDR, recurrence schedules (one-time, monthly, per semester, per term, annual), applicability criteria (campus, grade level, academic program), and due-date determination rules (fixed calendar date, days after issuance, fixed day of month) without silently rewriting historical policies.
2. **Student Financial Account:** Establish and maintain the student-centric financial ledger anchor (`student_financial_accounts`) strictly in a 1:1 relationship with official student identity. Manages account status (`active`, `suspended`, `closed`) and provides a coherent, explainable position of student obligations and balances (where balance is dynamically derived from obligations, payments, credits, and adjustments rather than an independently editable column).
3. **Obligation Assessment & Issuance:** Calculate, generate, and post monetary charges/liabilities (`financial_obligations`) against student financial accounts based on approved charge rules and student placement context (e.g., application fee, enrollment fee, recurring tuition per GAP-06). Enforces immutable audit logging (capturing actor ID, timestamp, and charge payload) and manages standard due schedules (`obligation_due_schedules`).
4. **Billing & Statement Communication:** Compile, approve, and issue authorized point-in-time bills (invoices) and periodic financial statements (`billing_documents`, `billing_document_items`) addressed to authorized recipients (parents, guardians, or payers). Tracks document delivery status across communication channels (portal, email, printed) and provides itemized charge breakdowns without duplicating underlying financial transactions.

Payment collection, verification, allocation, and enrollment payment gates belong to the companion Stage 1 module `Payments & Receivables` (`FIN-WF-05`, `FIN-WF-06`, `FIN-WF-13`). Scholarships, discounts, waivers (Benefits), overdue receivables tracking, payment arrangements, refunds, adjustments, and financial clearance are explicitly deferred to Stage 2 and Stage 3.

## Desired Outcome

1. Finance Authorities can configure, version, and approve institutional financial policies and charge rules (`SCR-FIN-04`, `UC-FIN-10`, `FIN-WF-01`) for academic periods.
2. Student financial accounts (`student_financial_accounts`) are provisioned and maintained in a strict 1:1 relationship with student records (`SCR-FIN-03`), maintaining explainable, audit-traceable balances.
3. Finance Officers can preview, assess, validate exceptions, and post financial obligations (`SCR-FIN-01`, `UC-FIN-02`, `FIN-WF-02`) against student accounts based on applicable charge rules (supporting initial core charges: application fee, enrollment fee, and tuition fee).
4. Billing documents (bills/invoices and financial statements) are generated, itemized, and communicated to authorized parents/payers (`SCR-FIN-01`, `UC-FIN-03`, `FIN-WF-04`), visible in `SCR-SELF-02` (Family Tasks & Requests Portal).
5. Meets Stage 1 Completion Criteria:
   - **Criterion 6:** "At least one financial policy and charge rule (e.g., tuition) is defined and an obligation is assessed against an enrolled student."
   - **Criterion 7:** "A bill or statement communicating obligations is produced for an authorized recipient."
   - **Criterion 10:** "All domain events for the above are recorded and historically traceable."
6. Provides the authoritative obligations baseline required by Stage 1 Payments & Receivables (`Payment Receipt & Confirmation`, `Payment Allocation & Reallocation`, and the `Enrollment Payment Gate` under `FIN-WF-13`).

## Current Situation

1. Schema status: Only a rudimentary stub table `finance.policies` exists from migration `20261101_0301_Finance_Policies.sql`. The comprehensive tables specified in `docs/entities/finance-entities-tables.md` for Submodule 1 (`finance.financial_policies`, `finance.charge_rules`, `finance.student_financial_accounts`, `finance.financial_obligations`, `finance.obligation_due_schedules`, `finance.billing_documents`, and `finance.billing_document_items`) are not yet provisioned.
2. Backend status: `src/backend/Elsasa.Sis.Finance` contains only an assembly marker and a stub `FinanceModule` that returns `false` by default for `IsEnrollmentFeePaidAsync`. No domain models, MediatR handlers, vertical slice endpoints, or business rules exist for creating policies, charge rules, student accounts, assessing obligations, or generating billing documents.
3. Frontend status: `src/frontend/src/features/finance/obligations/views/ObligationsListView.vue` is a placeholder shell. There are no UI workspaces for `SCR-FIN-04` (Financial Policy Workspace), `SCR-FIN-01` (Billing Cycle Workspace), or `SCR-FIN-03` (Student Accounts Workbench).
4. Upstream prerequisites: Operations Calendar & Periods (`CR-OPS-CALENDAR-PERIODS`, `CR-OPS-PERIOD-ACTIVATION`) is operational, providing academic year and period context. Student Admission (`CR-STU-ADMISSION`), Enrollment & Identity (`CR-STU-ENROLLMENT-IDENTITY`), and Placement & Status (`CR-STU-PLACEMENT-STATUS`) provide official student identity, guardian relationships, and active placement context.

## Evidence

- Roadmap Specification: `docs/sub-modul-and-development-phases.md` (Stage 1, Module: Obligations & Billing — full scope; Section D Criteria 6, 7, 10; Section E GAP-01 & GAP-06 resolutions).
- Finance Domain Blueprint: `docs/domains/finance-domain.md` (Section: Financial Policies and Charge Rules; Section: Student Financial Account; Section: Obligation Assessment and Issuance; Section: Billing and Statement Communication; Section: Indonesian School Context & Operational Principles).
- Finance Entities & Schema Specification: `docs/entities/finance-entities.md` (Submodule 1 — Obligations & Billing: `Financial Policy`, `Charge Rule`, `Student Financial Account`, `Financial Obligation`, `Due Schedule`, `Billing Document`; Resolved Decision 1: Unified Billing Document Model; Resolved Decision 5: Student-Centric Account Model; Resolved Decision 6: Monetary Values in IDR).
- Finance Database Tables Specification: `docs/entities/finance-entities-tables.md` (Submodule 1: `financial_policies`, `charge_rules`, `student_financial_accounts`, `financial_obligations`, `obligation_due_schedules`, `billing_documents`, `billing_document_items`, lines 23–270).
- Operational Workflow Discovery: `docs/operational-workflow-discovery.md` (`FIN-WF-01: Define or Adopt Financial Policy`, `FIN-WF-02: Assess and Issue Financial Obligation`, `FIN-WF-04: Issue Bill or Financial Statement`; Section 6 GAP-06 Resolution).
- Use Cases: `docs/use-cases.md` (`UC-FIN-10: Establish Financial Policy`, `UC-FIN-02: Assess Financial Obligations`, `UC-FIN-03: Issue Billing Statement`).
- Screen Inventory: `docs/screen-inventory.md` (`SCR-FIN-04: Financial Policy Workspace`, `SCR-FIN-01: Billing Cycle Workspace`, `SCR-FIN-03: Student Accounts Workbench (Stage 1 initial scope)`, `SCR-SELF-02: Family Tasks & Requests Portal`).
- Existing Baseline Code: `src/backend/Elsasa.Sis.Finance/FinanceAssemblyMarker.cs`, `src/backend/Elsasa.Sis.Finance/Services/FinanceModule.cs`, `src/backend/Elsasa.Sis.Database/Scripts/20261101_0301_Finance_Policies.sql`, `src/frontend/src/features/finance/obligations/views/ObligationsListView.vue`.

## Notes

- This artifact serves as the formal change intake. It defines WHAT capability is requested and the operational outcomes expected, maintaining solution neutrality without prescribing technical architecture, API schemas, or UI implementations.
- Scope boundary discipline: Payment collection, verification, allocation, and enrollment payment gates belong to the companion Stage 1 module `Payments & Receivables` (`CR-FIN-PAYMENTS-RECEIVABLES` / `FIN-WF-05`, `FIN-WF-06`, `FIN-WF-13`). Scholarships, discounts, waivers (Benefits), overdue receivables tracking, payment arrangements, refunds, adjustments, and financial clearance belong to Stage 2 and Stage 3 and must not be conflated with this Stage 1 core issue.
- Technical feasibility assessment, domain modeling, feature decomposition, and architecture updates will be performed in downstream SDLC stages.
