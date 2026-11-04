import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import type { PaginatedResponse } from '@/shared/types/common';
import { getPayers, getPayerById, createPayer } from '../api/payers.api';
import type {
  CreatePayerPayload,
  CreatePayerResponse,
  GetPayersParams,
  PayerDto,
  PayerListItemDto,
} from '../types/payers.types';
import { financeKeys } from '../../shared/queryKeys';
import {
  tryGetToast,
  notifySuccess,
  notifyError,
  type FinanceMutationOptions,
  type FinanceQueryOptions,
} from '../../shared/toastNotifications';

/**
 * Reactive TanStack Query hook fetching payers matching query filters and search terms (OQ-002, SCR-FIN-02).
 * Uses OPERATIONAL cache tier (5m stale, 15m gc, refetch on window focus) for lookup & autocomplete.
 */
export function usePayersQuery(
  filters?: MaybeRefOrGetter<GetPayersParams | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<PaginatedResponse<PayerListItemDto>>({
    queryKey: computed(() => financeKeys.payers.list(toValue(filters))),
    queryFn: () => getPayers(toValue(filters)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching comprehensive details and payment history for a single payer (TD-03).
 * Uses OPERATIONAL cache tier.
 */
export function usePayerByIdQuery(
  payerId: MaybeRefOrGetter<string | null | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<PayerDto>({
    queryKey: computed(() => financeKeys.payers.detail(toValue(payerId) || '')),
    queryFn: () => getPayerById(toValue(payerId)!),
    enabled: computed(() => {
      const resolvedId = toValue(payerId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Mutation hook creating a new explicit payer entity (sponsor, embassy, guardian) (OQ-002).
 * Automatically invalidates payers queries upon success.
 */
export function useCreatePayerMutation(
  options?: FinanceMutationOptions<CreatePayerResponse, CreatePayerPayload>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (payload: CreatePayerPayload) => createPayer(payload),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.payers.all });

      notifySuccess(
        toast,
        'Payer Registered',
        `Payer '${data.payerName}' (${data.payerNumber}) registered successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Payer Registration Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
