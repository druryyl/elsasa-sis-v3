# Review Record: P5-S14 TanStack Query & Mutation Composables

- **Slice ID:** P5-S14
- **Title:** TanStack Query & Mutation Composables
- **Review Date:** 2026-11-04
- **Review Decision:** GO
- **Review Iteration:** 0

---

## 1. Scope & Objective Verification

| Module / Requirement | Evaluation | Status |
|---|---|---|
| `shared/queryKeys.ts` | Hierarchical TanStack Query key factory `financeKeys` covering policies (`all`, `lists`, `list`, `details`, `detail`), obligations (`all`, `lists`, `list`, `details`, `detail`), billing (`all`, `lists`, `list`, `details`, `detail`), and accounts (`all`, `lists`, `list`, `details`, `detail`, `balances`, `balance`). | SATISFIED |
| `shared/toastNotifications.ts` | Standardized PrimeVue toast notification helper (`notifySuccess`, `notifyError`, `tryGetToast`, `ToastOptions`, `FinanceMutationOptions`, `FinanceQueryOptions`) supporting headless test environments and RFC 7807 `ApiError` problem details. | SATISFIED |
| `policies/composables/useFinancePolicies.ts` | Implements `useFinancePoliciesQuery(filters)` and `useFinancePolicyDetailQuery(policyId)` configured with `CACHE_TIERS.OPERATIONAL`. Implements mutations `useCreatePolicyMutation()` (invalidates `financeKeys.policies.all`), `useApprovePolicyMutation()` (invalidates `financeKeys.policies.all`), and `useCreateChargeRuleMutation()` (invalidates `financeKeys.policies.detail(variables.policyId)`). | SATISFIED |
| `obligations/composables/useFinanceObligations.ts` | Implements `useFinanceObligationsQuery(filters)` configured with `CACHE_TIERS.ACTIVE_QUEUE`. Implements `useBatchAssessPreviewMutation()` (dry-run without invalidation), `useBatchAssessCommitMutation()` (invalidates `financeKeys.obligations.all` and `financeKeys.accounts.all`), and `useAdHocAssessMutation()` (invalidates `financeKeys.obligations.all`, `financeKeys.accounts.all`, and specific student detail). | SATISFIED |
| `billing/composables/useFinanceBilling.ts` | Implements `useBillingDocumentsQuery(filters)` with `CACHE_TIERS.ACTIVE_QUEUE` and `useBillingDocumentDetailQuery(documentId)` with `CACHE_TIERS.TRANSACTIONAL`. Implements mutations `useGenerateBillMutation()` (invalidates `financeKeys.billing.all`, `financeKeys.obligations.all`, `financeKeys.accounts.all`) and `useGenerateStatementMutation()` (invalidates `financeKeys.billing.all`). | SATISFIED |
| `accounts/composables/useFinanceAccounts.ts` | Implements `useStudentAccountQuery(studentId)` using `CACHE_TIERS.TRANSACTIONAL` for real-time derived balance projection without staleness, and `useSearchStudentAccountsQuery(params)` using `CACHE_TIERS.ACTIVE_QUEUE`. | SATISFIED |
| Barrel Exports (`index.ts`) | Clean barrel exports updated across `policies`, `obligations`, `billing`, `accounts`, `shared`, and top-level `src/frontend/src/features/finance/index.ts`. | SATISFIED |
| TypeScript Compilation (`vue-tsc --noEmit`) | Executed `pnpm --dir src/frontend typecheck` cleanly with 0 type errors. | SATISFIED |
| Vitest Test Suite (`finance.composables.spec.ts`) | 21 dedicated unit tests verifying all query hooks, mutations, query key derivations, cache invalidation scopes, and toast notifications. Full frontend test suite (80 files, 722 tests) passes cleanly. | SATISFIED |

---

## 2. Architecture & Code Quality Verification

- **Cache Tier Alignment:** Caching strategies strictly conform to frontend architecture standards defined in `src/frontend/src/app/providers/queryClient.ts`:
  - `OPERATIONAL` tier (5m stale, 15m gc) applied to governing financial policies and charge rules.
  - `ACTIVE_QUEUE` tier (30s stale, 5m gc) applied to operational obligation listings, billing document lists, and student search queues.
  - `TRANSACTIONAL` tier (immediate staleness, 5m gc) applied to dynamic 360-degree student financial accounts and detailed point-in-time billing document snapshots.
- **Cache Invalidation Architecture:** Mutations trigger granular and domain-level query invalidations:
  - Batch assessment and ad-hoc assessment invalidate obligations and student accounts, guaranteeing ledger freshness across workbenches.
  - Bill compilation invalidates billing documents, obligations, and accounts, ensuring open obligation lists refresh immediately.
  - Charge rule creation targets specific policy detail cache via `financeKeys.policies.detail(variables.policyId)`.
- **Defensive Notification Infrastructure:** `tryGetToast()` cleanly handles execution outside Vue setup or during unit testing where PrimeVue `useToast` is unprovided, preventing headless test crashes.
- **Reactivity & Ergonomics:** All composable query parameters accept `MaybeRefOrGetter` and use `toValue` inside Vue `computed` wrappers, enabling automatic reactive refetching upon filter modifications.

---

## 3. Findings

No BLOCKER, MAJOR, or MINOR findings.

- **[NOTE-01]:** `useStudentAccountQuery` accurately leverages `CACHE_TIERS.TRANSACTIONAL` (staleTime: 0) to uphold the architecture requirement that student financial account balances are dynamically derived and must never display stale ledger balances.

---

## 4. Remediation History

| Iteration | Date | Decision | Summary |
|---|---|---|---|
| 0 | 2026-11-04 | GO | Initial review: All 4 composable suites, query key factories, toast notifications, cache invalidation pipelines, and automated test suites satisfy slice completion criteria and target architecture. |

---

## 5. Final Review Decision

**GO**
