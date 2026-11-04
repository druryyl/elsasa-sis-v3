import type { FinancialPolicyFilters } from '../policies/types/policies.types';
import type { GetObligationsParams } from '../obligations/types/obligations.types';
import type { GetBillingDocumentsParams } from '../billing/types/billing.types';
import type { SearchStudentAccountsParams } from '../accounts/types/accounts.types';
import type { GetPayersParams } from '../payers/types/payers.types';
import type { GetPaymentsParams } from '../payments/types/payments.types';
import type { SuggestAllocationsParams } from '../allocations/types/allocations.types';

/**
 * Standardized TanStack Query key factories for Finance bounded context.
 * Enables granular single-record invalidation or broad domain-level cache sweeps.
 */
export const financeKeys = {
  all: ['finance'] as const,

  policies: {
    all: ['finance', 'policies'] as const,
    lists: () => [...financeKeys.policies.all, 'list'] as const,
    list: (filters?: FinancialPolicyFilters) =>
      filters !== undefined
        ? ([...financeKeys.policies.lists(), filters] as const)
        : ([...financeKeys.policies.lists()] as const),
    details: () => [...financeKeys.policies.all, 'detail'] as const,
    detail: (id: string) => [...financeKeys.policies.details(), id] as const,
  },

  obligations: {
    all: ['finance', 'obligations'] as const,
    lists: () => [...financeKeys.obligations.all, 'list'] as const,
    list: (params?: GetObligationsParams) =>
      params !== undefined
        ? ([...financeKeys.obligations.lists(), params] as const)
        : ([...financeKeys.obligations.lists()] as const),
    details: () => [...financeKeys.obligations.all, 'detail'] as const,
    detail: (id: string) => [...financeKeys.obligations.details(), id] as const,
  },

  billing: {
    all: ['finance', 'billing'] as const,
    lists: () => [...financeKeys.billing.all, 'list'] as const,
    list: (params?: GetBillingDocumentsParams) =>
      params !== undefined
        ? ([...financeKeys.billing.lists(), params] as const)
        : ([...financeKeys.billing.lists()] as const),
    details: () => [...financeKeys.billing.all, 'detail'] as const,
    detail: (id: string) => [...financeKeys.billing.details(), id] as const,
  },

  accounts: {
    all: ['finance', 'accounts'] as const,
    lists: () => [...financeKeys.accounts.all, 'list'] as const,
    list: (params?: SearchStudentAccountsParams) =>
      params !== undefined
        ? ([...financeKeys.accounts.lists(), params] as const)
        : ([...financeKeys.accounts.lists()] as const),
    details: () => [...financeKeys.accounts.all, 'detail'] as const,
    detail: (studentId: string) => [...financeKeys.accounts.details(), studentId] as const,
    balances: () => [...financeKeys.accounts.all, 'balance'] as const,
    balance: (studentId: string) => [...financeKeys.accounts.balances(), studentId] as const,
  },

  payers: {
    all: ['finance', 'payers'] as const,
    lists: () => [...financeKeys.payers.all, 'list'] as const,
    list: (params?: GetPayersParams) =>
      params !== undefined
        ? ([...financeKeys.payers.lists(), params] as const)
        : ([...financeKeys.payers.lists()] as const),
    details: () => [...financeKeys.payers.all, 'detail'] as const,
    detail: (id: string) => [...financeKeys.payers.details(), id] as const,
  },

  payments: {
    all: ['finance', 'payments'] as const,
    lists: () => [...financeKeys.payments.all, 'list'] as const,
    list: (params?: GetPaymentsParams) =>
      params !== undefined
        ? ([...financeKeys.payments.lists(), params] as const)
        : ([...financeKeys.payments.lists()] as const),
    details: () => [...financeKeys.payments.all, 'detail'] as const,
    detail: (id: string) => [...financeKeys.payments.details(), id] as const,
    receipts: () => [...financeKeys.payments.all, 'receipt'] as const,
    receipt: (id: string) => [...financeKeys.payments.receipts(), id] as const,
  },

  allocations: {
    all: ['finance', 'allocations'] as const,
    suggestions: () => [...financeKeys.allocations.all, 'suggest'] as const,
    suggestion: (params: SuggestAllocationsParams) =>
      [...financeKeys.allocations.suggestions(), params] as const,
  },

  receivables: {
    all: ['finance', 'receivables'] as const,
    byStudent: (studentId: string, openOnly?: boolean) =>
      [...financeKeys.receivables.all, studentId, { openOnly }] as const,
  },
} as const;
