import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import type { PaginatedResponse } from '@/shared/types/common';
import {
  getObligations,
  batchAssessObligations,
  assessAdHocObligation,
} from '../api/obligations.api';
import type {
  FinancialObligationDto,
  GetObligationsParams,
  BatchAssessRequest,
  BatchAssessPreviewResponse,
  BatchAssessCommitResponse,
  AssessAdHocObligationRequest,
  AssessAdHocObligationResponse,
} from '../types/obligations.types';
import { financeKeys } from '../../shared/queryKeys';
import {
  tryGetToast,
  notifySuccess,
  notifyError,
  type FinanceMutationOptions,
  type FinanceQueryOptions,
} from '../../shared/toastNotifications';

/**
 * Reactive TanStack Query hook fetching financial obligations matching query filters.
 * Uses ACTIVE_QUEUE cache tier (30s stale, 5m gc, refetch on window focus).
 */
export function useFinanceObligationsQuery(
  filters?: MaybeRefOrGetter<GetObligationsParams | undefined>,
  options?: FinanceQueryOptions,
) {
  return useQuery<PaginatedResponse<FinancialObligationDto>>({
    queryKey: computed(() => financeKeys.obligations.list(toValue(filters))),
    queryFn: () => getObligations(toValue(filters)),
    enabled: computed(() => (options?.enabled !== undefined ? toValue(options.enabled) : true)),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

export type BatchAssessPreviewVariables =
  | BatchAssessRequest
  | Omit<BatchAssessRequest, 'mode'>;

/**
 * Mutation hook running a dry-run billing assessment cycle preview (SCR-FIN-01).
 * Calculates candidate lists and duplicate detection without mutating server state.
 */
export function useBatchAssessPreviewMutation(
  options?: FinanceMutationOptions<BatchAssessPreviewResponse, BatchAssessPreviewVariables>,
) {
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: BatchAssessPreviewVariables) =>
      batchAssessObligations({
        ...variables,
        mode: 'preview',
      }) as Promise<BatchAssessPreviewResponse>,
    onSuccess: (data, variables) => {
      notifySuccess(
        toast,
        'Preview Ready',
        `Evaluated ${data.totalCandidatesCount} students (${data.eligibleCandidatesCount} eligible, ${data.duplicateCount} duplicate).`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Assessment Preview Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

export type BatchAssessCommitVariables =
  | BatchAssessRequest
  | Omit<BatchAssessRequest, 'mode'>;

/**
 * Mutation hook committing a batch billing cycle assessment (SCR-FIN-01).
 * Creates authoritative liability records and sequence-1 due schedules.
 * Invalidates obligations queries and student account balance queries upon success.
 */
export function useBatchAssessCommitMutation(
  options?: FinanceMutationOptions<BatchAssessCommitResponse, BatchAssessCommitVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: BatchAssessCommitVariables) =>
      batchAssessObligations({
        ...variables,
        mode: 'commit',
      }) as Promise<BatchAssessCommitResponse>,
    onSuccess: (data, variables) => {
      // Invalidate obligations queries and account balance queries
      queryClient.invalidateQueries({ queryKey: financeKeys.obligations.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.accounts.all });

      notifySuccess(
        toast,
        'Batch Assessment Committed',
        `Successfully assessed ${data.eligibleCandidatesCount} student obligations.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Batch Assessment Commit Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook creating a single-student ad-hoc obligation (SCR-FIN-03).
 * Invalidates obligations queries and student account balance queries upon success.
 */
export function useAdHocAssessMutation(
  options?: FinanceMutationOptions<AssessAdHocObligationResponse, AssessAdHocObligationRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: AssessAdHocObligationRequest) => assessAdHocObligation(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.obligations.all });
      queryClient.invalidateQueries({ queryKey: financeKeys.accounts.all });
      if (variables.studentId) {
        queryClient.invalidateQueries({
          queryKey: financeKeys.accounts.detail(variables.studentId),
        });
      }

      notifySuccess(
        toast,
        'Obligation Assessed',
        `Ad-hoc obligation '${data.obligationNumber}' assessed successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Ad-hoc Assessment Failed', options);
      options?.onError?.(error, variables);
    },
  });
}
