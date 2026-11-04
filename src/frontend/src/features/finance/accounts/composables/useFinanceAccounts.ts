import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import type { PaginatedResponse } from '@/shared/types/common';
import { getStudentAccount, searchStudentAccounts } from '../api/accounts.api';
import type {
  AccountSummaryDto,
  SearchStudentAccountsParams,
  StudentFinancialAccountDto,
} from '../types/accounts.types';
import { financeKeys } from '../../shared/queryKeys';
import type { FinanceQueryOptions } from '../../shared/toastNotifications';

/**
 * Reactive TanStack Query hook fetching the complete 360-degree student financial account dossier (TD-02, SCR-FIN-03).
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus) for real-time balance accuracy.
 */
export function useStudentAccountQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<AccountSummaryDto>({
    queryKey: computed(() => financeKeys.accounts.detail(toValue(studentId) || '')),
    queryFn: () => getStudentAccount(toValue(studentId)!),
    enabled: computed(() => {
      const resolvedId = toValue(studentId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook searching and paginating student financial accounts joined with derived balances (SCR-FIN-03).
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useSearchStudentAccountsQuery(
  params?: MaybeRefOrGetter<SearchStudentAccountsParams | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<PaginatedResponse<StudentFinancialAccountDto>>({
    queryKey: computed(() => financeKeys.accounts.list(toValue(params))),
    queryFn: () => searchStudentAccounts(toValue(params)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}
