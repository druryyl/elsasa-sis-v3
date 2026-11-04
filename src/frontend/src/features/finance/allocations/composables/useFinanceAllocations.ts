import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  suggestAllocations,
  allocatePayment,
  reallocatePayment,
  getStudentReceivables,
} from '../api/allocations.api';
import type {
  AllocatePaymentPayload,
  AllocatePaymentResponse,
  GetStudentReceivablesParams,
  GetStudentReceivablesResponse,
  ReallocatePaymentPayload,
  ReallocatePaymentResponse,
  SuggestAllocationsParams,
  SuggestAllocationsResponse,
} from '../types/allocations.types';
import { financeKeys } from '../../shared/queryKeys';
import {
  tryGetToast,
  notifySuccess,
  notifyError,
  type FinanceMutationOptions,
  type FinanceQueryOptions,
} from '../../shared/toastNotifications';

/**
 * Reactive TanStack Query hook computing smart priority-based allocation suggestion distributions (TD-05, OQ-003).
 * Prioritizes mandatory admission/enrollment fees first, followed by oldest tuition FIFO.
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus) to ensure fresh balances.
 */
export function useSuggestAllocationsQuery(
  params: MaybeRefOrGetter<SuggestAllocationsParams | null | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<SuggestAllocationsResponse>({
    queryKey: computed(() => {
      const p = toValue(params);
      return p
        ? financeKeys.allocations.suggestion(p)
        : ([...financeKeys.allocations.suggestions(), null] as const);
    }),
    queryFn: () => suggestAllocations(toValue(params)!),
    enabled: computed(() => {
      const p = toValue(params);
      const hasTarget = Boolean(p?.studentId || p?.applicantId);
      const hasAmount = Boolean(p && typeof p.paymentAmount === 'number' && p.paymentAmount > 0);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(p) && hasTarget && hasAmount && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching dynamic student receivables, aging brackets, and outstanding balances (SCR-FIN-03).
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useStudentReceivablesQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  params?: MaybeRefOrGetter<GetStudentReceivablesParams | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<GetStudentReceivablesResponse>({
    queryKey: computed(() => {
      const id = toValue(studentId) || '';
      const p = toValue(params);
      return financeKeys.receivables.byStudent(id, p?.openOnly);
    }),
    queryFn: () => getStudentReceivables(toValue(studentId)!, toValue(params)),
    enabled: computed(() => {
      const resolvedId = toValue(studentId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Mutation hook committing explicit payment allocations across one or more student obligations (SCR-FIN-02).
 * Automatically invalidates allocations, payments, obligations, student accounts, and receivables caches.
 */
export function useAllocatePaymentMutation(
  options?: FinanceMutationOptions<AllocatePaymentResponse, AllocatePaymentPayload>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: AllocatePaymentPayload) => allocatePayment(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.allocations.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.payments.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.obligations.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.accounts.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.receivables.all });

      notifySuccess(
        toast,
        'Allocations Committed',
        `Successfully applied ${data.allocatedItemsCount} allocation item(s).`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Payment Allocation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook correcting an erroneous allocation via append-only reallocation lineage (SCR-FIN-03).
 * Supersedes prior allocations, links replacement lineage, and recalculates obligations statuses.
 * Automatically invalidates allocations, payments, obligations, student accounts, and receivables caches.
 */
export function useReallocatePaymentMutation(
  options?: FinanceMutationOptions<ReallocatePaymentResponse, ReallocatePaymentPayload>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: ReallocatePaymentPayload) => reallocatePayment(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.allocations.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.payments.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.obligations.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.accounts.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.receivables.all });

      notifySuccess(
        toast,
        'Reallocation Committed',
        `Successfully superseded ${data.supersededAllocationsCount} allocation(s) and applied ${data.newAllocationsCount} replacement(s).`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Payment Reallocation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
