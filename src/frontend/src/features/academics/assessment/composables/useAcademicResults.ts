import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { resultsApi } from '../api/resultsApi';
import type {
  AcademicResult,
  ApproveSummativeResultsRequest,
  ApproveSummativeResultsResponse,
  EvaluateLearningRequest,
  PublishAcademicResultsRequest,
  PublishAcademicResultsResponse,
} from '../types';
import { assessmentKeys, resultKeys } from '../constants/queryKeys';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Types
// ============================================================================

export interface ResultsQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface ResultsMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface EvaluateVariables {
  assessmentId: string;
  request: EvaluateLearningRequest;
}

export interface ApproveSummativeVariables {
  assessmentId: string;
  request?: ApproveSummativeResultsRequest;
}

export interface PublishResultsVariables {
  assessmentId: string;
  request?: PublishAcademicResultsRequest;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching academic results roster for an assessment (UC-ACD-06, P8-S20).
 * Cache tier: TRANSACTIONAL (stale immediately, 5m gc, refetch on window focus).
 */
export function useAcademicResultsQuery(
  assessmentId: MaybeRefOrGetter<string | null | undefined>,
  filter?: MaybeRefOrGetter<{ status?: string; onlyFinal?: boolean } | undefined>,
  options?: ResultsQueryOptions,
) {
  return useQuery<AcademicResult[]>({
    queryKey: computed(() => {
      const resolvedAssessmentId = toValue(assessmentId);
      const resolvedFilter = toValue(filter);
      return resultKeys.byAssessment(resolvedAssessmentId, resolvedFilter);
    }),
    queryFn: () => {
      const resolvedAssessmentId = toValue(assessmentId);
      if (!resolvedAssessmentId) {
        throw new Error('Assessment ID is required to fetch academic results.');
      }
      return resultsApi.listByAssessment(resolvedAssessmentId, toValue(filter));
    },
    enabled: computed(() => {
      const resolvedAssessmentId = toValue(assessmentId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedAssessmentId) && isEnabled;
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
 * Mutation hook for evaluating student or applicant learning (UC-ACD-06, P4-S08).
 * Autonomously publishes formative marks when configured.
 */
export function useEvaluateMutation(
  options?: ResultsMutationOptions<AcademicResult, EvaluateVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ assessmentId, request }: EvaluateVariables) =>
      resultsApi.evaluate(assessmentId, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: resultKeys.all });
      queryClient.invalidateQueries({
        queryKey: resultKeys.byAssessment(variables.assessmentId),
      });
      queryClient.invalidateQueries({ queryKey: assessmentKeys.all });
      queryClient.invalidateQueries({
        queryKey: assessmentKeys.detail(variables.assessmentId),
      });
      notifySuccess(
        toast,
        'Evaluation Recorded',
        `Academic result recorded with status '${data.status}'.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Evaluation Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for approving summative results by Academic Authority / Coordinator (UC-ACD-16, P4-S09).
 */
export function useApproveSummativeMutation(
  options?: ResultsMutationOptions<ApproveSummativeResultsResponse, ApproveSummativeVariables | string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: ApproveSummativeVariables | string) => {
      const assessmentId = typeof variables === 'string' ? variables : variables.assessmentId;
      const request = typeof variables === 'string' ? undefined : variables.request;
      return resultsApi.approve(assessmentId, request);
    },
    onSuccess: (data, variables) => {
      const assessmentId = typeof variables === 'string' ? variables : variables.assessmentId;
      queryClient.invalidateQueries({ queryKey: resultKeys.all });
      queryClient.invalidateQueries({
        queryKey: resultKeys.byAssessment(assessmentId),
      });
      notifySuccess(
        toast,
        'Results Approved',
        `Approved ${data.approvedCount} summative result(s).`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Approve Summative Results Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for publishing approved academic results to portals (UC-ACD-16, P4-S10).
 */
export function usePublishResultsMutation(
  options?: ResultsMutationOptions<PublishAcademicResultsResponse, PublishResultsVariables | string>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (variables: PublishResultsVariables | string) => {
      const assessmentId = typeof variables === 'string' ? variables : variables.assessmentId;
      const request = typeof variables === 'string' ? undefined : variables.request;
      return resultsApi.publish(assessmentId, request);
    },
    onSuccess: (data, variables) => {
      const assessmentId = typeof variables === 'string' ? variables : variables.assessmentId;
      queryClient.invalidateQueries({ queryKey: resultKeys.all });
      queryClient.invalidateQueries({
        queryKey: resultKeys.byAssessment(assessmentId),
      });
      notifySuccess(
        toast,
        'Results Published',
        `Published ${data.publishedCount} academic result(s) to student portal.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Publish Results Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// Aliases matching criteria
export const useEvaluateLearningMutation = useEvaluateMutation;
export const useApproveSummativeResultsMutation = useApproveSummativeMutation;
export const usePublishAcademicResultsMutation = usePublishResultsMutation;

// ============================================================================
// Consolidated Composable
// ============================================================================

/**
 * Primary composable for managing academic results under an assessment (P8-S20).
 * Bundles results roster query with evaluate, approveSummative, and publishResults mutations.
 */
export function useAcademicResults(
  assessmentId?: MaybeRefOrGetter<string | null | undefined>,
  filter?: MaybeRefOrGetter<{ status?: string; onlyFinal?: boolean } | undefined>,
  options?: ResultsQueryOptions,
) {
  const resultsQuery = useAcademicResultsQuery(assessmentId, filter, options);
  const evaluateMutation = useEvaluateMutation();
  const approveSummativeMutation = useApproveSummativeMutation();
  const publishResultsMutation = usePublishResultsMutation();

  return {
    resultsQuery,
    results: computed(() => resultsQuery.data.value ?? []),
    isLoading: resultsQuery.isLoading,
    isPending: resultsQuery.isPending,
    isError: resultsQuery.isError,
    error: resultsQuery.error,
    refetch: resultsQuery.refetch,

    evaluate: evaluateMutation,
    approveSummative: approveSummativeMutation,
    publishResults: publishResultsMutation,
  };
}
