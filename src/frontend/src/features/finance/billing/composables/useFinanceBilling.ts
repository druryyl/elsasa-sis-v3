import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import type { PaginatedResponse } from '@/shared/types/common';
import {
  getBillingDocuments,
  getBillingDocumentById,
  generateBill,
  generateStatement,
} from '../api/billing.api';
import type {
  BillingDocumentDto,
  GetBillingDocumentsParams,
  GenerateBillRequest,
  GenerateBillingDocumentResponse,
  GenerateStatementRequest,
  GenerateStatementResponse,
} from '../types/billing.types';
import { financeKeys } from '../../shared/queryKeys';
import {
  tryGetToast,
  notifySuccess,
  notifyError,
  type FinanceMutationOptions,
  type FinanceQueryOptions,
} from '../../shared/toastNotifications';

/**
 * Reactive TanStack Query hook fetching billing documents with optional filters (FIN-WF-04, SCR-FIN-03).
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useBillingDocumentsQuery(
  filters?: MaybeRefOrGetter<GetBillingDocumentsParams | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<PaginatedResponse<BillingDocumentDto>>({
    queryKey: computed(() => financeKeys.billing.list(toValue(filters))),
    queryFn: () => getBillingDocuments(toValue(filters)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching a specific billing document with itemized snapshot breakdown.
 * Uses TRANSACTIONAL cache tier (immediate staleness, 5m gc, refetch on window focus).
 */
export function useBillingDocumentDetailQuery(
  documentId: MaybeRefOrGetter<string | null | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<BillingDocumentDto>({
    queryKey: computed(() => financeKeys.billing.detail(toValue(documentId) || '')),
    queryFn: () => getBillingDocumentById(toValue(documentId)!),
    enabled: computed(() => {
      const resolvedId = toValue(documentId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

/**
 * Mutation hook compiling open student obligations into an authorized bill/invoice (FIN-WF-04, UC-FIN-03).
 * Automatically invalidates billing document queries and obligations queries upon success.
 */
export function useGenerateBillMutation(
  options?: FinanceMutationOptions<GenerateBillingDocumentResponse, GenerateBillRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: GenerateBillRequest) => generateBill(request),
    onSuccess: (data, variables) => {
      // Invalidate billing documents, obligations, and accounts caches
      queryClient.invalidateQueries({ queryKey: financeKeys.billing.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.obligations.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.accounts.all });

      notifySuccess(
        toast,
        'Bill Generated',
        `Billing document '${data.documentNumber}' generated successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Generate Bill Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook compiling a periodic financial statement across a specified date range.
 * Automatically invalidates billing document queries upon success.
 */
export function useGenerateStatementMutation(
  options?: FinanceMutationOptions<GenerateStatementResponse, GenerateStatementRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: GenerateStatementRequest) => generateStatement(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.billing.all });

      notifySuccess(
        toast,
        'Statement Generated',
        `Periodic statement '${data.documentNumber}' generated successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Generate Statement Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
