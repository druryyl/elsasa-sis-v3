import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { correctionsApi } from '../api/correctionsApi';
import type {
  AcademicRecordCorrection,
  AcademicResult,
  CorrectResultRequest,
} from '../types';
import { correctionKeys, resultKeys } from '../constants/queryKeys';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Types
// ============================================================================

export interface RecordCorrectionQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface RecordCorrectionMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface CorrectResultVariables {
  id: string;
  request: CorrectResultRequest;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching audit correction history for an academic result (UC-ACD-19, P8-S20).
 * Cache tier: TRANSACTIONAL (stale immediately, 5m gc, refetch on window focus).
 */
export function useCorrectionHistoryQuery(
  resultId: MaybeRefOrGetter<string | null | undefined>,
  options?: RecordCorrectionQueryOptions,
) {
  return useQuery<AcademicRecordCorrection[]>({
    queryKey: computed(() => {
      const resolvedResultId = toValue(resultId);
      return correctionKeys.history(resolvedResultId);
    }),
    queryFn: () => {
      const resolvedResultId = toValue(resultId);
      if (!resolvedResultId) {
        throw new Error('Academic Result ID is required to fetch correction history.');
      }
      return correctionsApi.getCorrectionHistory(resolvedResultId);
    },
    enabled: computed(() => {
      const resolvedResultId = toValue(resultId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedResultId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.TRANSACTIONAL.staleTime,
    gcTime: CACHE_TIERS.TRANSACTIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.TRANSACTIONAL.refetchOnWindowFocus,
  });
}

// ============================================================================
// Mutations
// ============================================================================

/**
 * Mutation hook for authorizing an in-place auditable correction to an academic result (UC-ACD-19, P6-S14).
 * Invalidates result caches and correction audit history upon success.
 */
export function useCorrectResultMutation(
  options?: RecordCorrectionMutationOptions<AcademicResult, CorrectResultVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, request }: CorrectResultVariables) =>
      correctionsApi.correctResult(id, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: resultKeys.all });
      queryClient.invalidateQueries({ queryKey: correctionKeys.all });
      queryClient.invalidateQueries({
        queryKey: correctionKeys.history(variables.id),
      });
      queryClient.invalidateQueries({
        queryKey: correctionKeys.detail(variables.id),
      });
      notifySuccess(
        toast,
        'Academic Record Corrected',
        `Result updated successfully. Reason logged: "${variables.request.reason.slice(0, 30)}..."`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Record Correction Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ============================================================================
// Consolidated Composable
// ============================================================================

/**
 * Primary composable for managing academic result corrections and audit history (P8-S20).
 * Bundles correction history query with correctResult mutation.
 */
export function useRecordCorrections(
  resultId?: MaybeRefOrGetter<string | null | undefined>,
  options?: RecordCorrectionQueryOptions,
) {
  const correctionsQuery = useCorrectionHistoryQuery(resultId, options);
  const correctMutation = useCorrectResultMutation();

  return {
    correctionsQuery,
    corrections: computed(() => correctionsQuery.data.value ?? []),
    isLoading: correctionsQuery.isLoading,
    isPending: correctionsQuery.isPending,
    isError: correctionsQuery.isError,
    error: correctionsQuery.error,
    refetch: correctionsQuery.refetch,

    correctResult: correctMutation,
  };
}
