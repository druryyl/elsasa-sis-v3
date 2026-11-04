# Review Record: P6-S17 Student Accounts Workbench UI (SCR-FIN-03, /finance/accounts) & Family Portal Tab (SCR-SELF-02)

- **Slice ID:** P6-S17
- **Title:** Student Accounts Workbench UI (SCR-FIN-03, /finance/accounts) & Family Portal Tab (SCR-SELF-02)
- **Review Date:** 2026-11-04
- **Review Decision:** GO
- **Review Iteration:** 0

---

## 1. Scope & Objective Verification

| Component / Requirement | Evaluation | Status |
|---|---|---|
| `views/StudentAccountsWorkbenchView.vue` | Delivers the complete `SCR-FIN-03` Student Accounts Workbench at `/finance/accounts` and `/finance/accounts/:studentId`. Features student search auto-complete with debounced querying (`useSearchStudentAccountsQuery`), directory table of enrolled student financial accounts, and 360-degree ledger dossier layout upon student selection. Connects tabs for obligations and billing documents with ledger actions. | SATISFIED |
| `components/AccountBalanceCard.vue` | Displays formatted student financial account dossier header: account number, active status pill, opened date, zero-decimal currency code label (`IDR`), total assessed, total settled, and prominent derived current balance card with dynamic styling/tags based on outstanding liabilities (`formatIdr`). Action buttons trigger fee assessment modal and bill compilation dialog. | SATISFIED |
| `components/ObligationsTable.vue` | PrimeVue DataTable projecting student obligations: obligation number, description, academic period label, charge type badge, formatted IDR amount, due date with calendar icon, and status badge (`assessed`, `issued`, `partially_settled`, `settled`). Provides quick search filtering, multi-select checkboxes, and action buttons to compile selected or compile all open obligations into an authorized invoice. | SATISFIED |
| `components/BillingDocumentsTable.vue` | PrimeVue DataTable projecting issued billing documents and statements: document number, document type (`bill` vs `statement`), addressed recipient with contact and relationship details, issue date, payment due date, total billed amount in IDR, delivery status pill, and action button emitting `viewInvoice` to inspect frozen snapshot line items. | SATISFIED |
| `components/AssessAdHocFeeModal.vue` | PrimeVue Dialog allowing single-student fee assessment (`SCR-FIN-03`, `FIN-WF-02`, `UC-FIN-02`). Uses VeeValidate and Zod schema (`assessAdHocObligationSchema`) to validate `studentId`, `chargeType`, `description`, `amount` (IDR integer), `dueDate` (defaulting to 14 days), `academicPeriodId`, and optional notes. Auto-resolves active academic years and operational periods via queries. Dispatches `useAdHocAssessMutation`. | SATISFIED |
| `components/InvoiceDetailModal.vue` | PrimeVue Dialog displaying complete point-in-time billing document snapshot (`TD-05`). Fetches detailed invoice data via `useBillingDocumentDetailQuery`, displaying institutional header, delivery status and channel, addressed recipient details, billing timeline dates, frozen itemized line items table (`billing_document_items`), total billed amount in IDR, internal notes, and browser print action. | SATISFIED |
| Family Portal (`FamilyBillingTab.vue`) | Payer-facing billing tab for `SCR-SELF-02`. Displays total outstanding balance banner with contextual status alerts, filter toggles (`all`, `unpaid`, `paid`), and list of issued bills with formatted amounts, due dates, recipient names, and invoice inspection modal trigger. Connects to `useBillingDocumentsQuery`. | SATISFIED |
| Family Portal Integration (`FamilyTasksView.vue`) | Integrates `FamilyBillingTab.vue` as primary tab alongside self-service tasks, mapped at route `/portal/family`. | SATISFIED |
| Route Configuration (`routes.ts`) | Routes `/finance/accounts` and `/finance/accounts/:studentId` mapped to `StudentAccountsWorkbenchView.vue` protected by `meta: { permission: 'Permissions.Finance.Accounts.View' }`. Route `/portal/family` mapped to `FamilyTasksView.vue`. | SATISFIED |
| Layout Navigation (`AppLayout.vue`) | Adds `Student Accounts` navigation item under Finance section (`to: '/finance/accounts'`, icon: `Wallet`, permission: `'Permissions.Finance.Accounts.View'`). | SATISFIED |
| Barrel Exports (`index.ts`) | Re-exports all account components, views, schemas, types, composables, constants, and portal components in `src/frontend/src/features/finance/accounts/index.ts` and `src/frontend/src/features/portal/index.ts`. | SATISFIED |
| TypeScript Compilation (`vue-tsc --noEmit`) | Passes cleanly with 0 errors across the entire frontend workspace. | SATISFIED |
| Automated Unit & Integration Tests | Vitest test suite `src/frontend/src/features/finance/__tests__/StudentAccountsWorkbenchView.spec.ts` (11 unit and component integration tests) executes cleanly and verifies all components, dossier switching, ad-hoc fee modal, invoice detail modal, and family portal components. | SATISFIED |

---

## 2. Architecture & Code Quality Verification

- **Architectural Conformance (ADR-002, ADR-003, ADR-004, ADR-009, GAP-006, TD-02, TD-05, TD-08):**
  - **Derived Balance Presentation (ADR-002, TD-02):** The frontend faithfully renders the dynamic derived balance (`totalAssessed - totalSettled`) computed by backend query projection without reliance on any mutable balance column.
  - **Monetary Precision Standard:** All monetary values are handled and displayed strictly as integer IDR without decimal precision loss using the `formatIdr` utility.
  - **Frozen Snapshot Immutability (TD-05):** `InvoiceDetailModal.vue` renders the immutable point-in-time line items from `finance.billing_document_items`.
  - **Permission-Based UI Gating (ADR-009):** `StudentAccountsWorkbenchView.vue` and `AccountBalanceCard.vue` check `Permissions.Finance.Accounts.View`, `Permissions.Finance.Obligations.Assess`, and `Permissions.Finance.Billing.Generate` using `useAuthStore().hasPermission()`.
- **State Separation & Query Composables:**
  - TanStack Query composables manage server cache invalidation upon ad-hoc obligation assessment and bill generation.
  - Component hierarchy maintains clean decoupling between presentation components and view orchestration.

---

## 3. Findings

No BLOCKER, MAJOR, or MINOR findings.

- **[NOTE-01]:** In `FamilyBillingTab.vue`, an optional `payNow` event is defined on the component interface. In Stage 2 (Payments & Reconciliation), this can be wired directly to the payment gateway or receipt upload desk when student self-service payment intake is introduced.

---

## 4. Remediation History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 0 | 2026-11-04 | GO | Initial review: Student Accounts Workbench UI (`SCR-FIN-03`), Family Portal billing extension (`SCR-SELF-02`), modal dialogs, router mapping, and Vitest test suite fully satisfy completion criteria. |

---

## 5. Final Review Decision

**GO**
