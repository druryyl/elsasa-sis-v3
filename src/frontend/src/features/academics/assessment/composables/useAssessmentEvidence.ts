import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { evidenceApi } from '../api/evidenceApi';
import type {
  AssessmentEvidence,
  RecordEvidenceRequest,
} from '../types';
import { evidenceKeys } from '../constants/queryKeys';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// ============================================================================
// Types
// ============================================================================

export interface EvidenceQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface EvidenceMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface RecordEvidenceVariables {
  assessmentId: string;
  request: RecordEvidenceRequest;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching assessment evidence roster (ACD-WF-08, UC-ACD-07, P8-S20).
 * Cache tier: TRANSACTIONAL (stale immediately, 5m gc, refetch on window focus).
 */
export function useAssessmentEvidenceQuery(
  assessmentId: MaybeRefOrGetter<string | null | undefined>,
  filter?: MaybeRefOrGetter<{ studentId?: string; applicantId?: string } | undefined>,
  options?: EvidenceQueryOptions,
) {
  return useQuery<AssessmentEvidence[]>({
    queryKey: computed(() => {
      const resolvedAssessmentId = toValue(assessmentId);
      const resolvedFilter = toValue(filter);
      return evidenceKeys.byAssessment(resolvedAssessmentId, resolvedFilter);
    }),
    queryFn: () => {
      const resolvedAssessmentId = toValue(assessmentId);
      if (!resolvedAssessmentId) {
        throw new Error('Assessment ID is required to fetch evidence.');
      }
      return evidenceApi.listByAssessment(resolvedAssessmentId, toValue(filter));
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
 * Mutation hook for recording student or applicant assessment evidence (UC-ACD-07, P3-S06).
 */
export function useRecordEvidenceMutation(
  options?: EvidenceMutationOptions<AssessmentEvidence, RecordEvidenceVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ assessmentId, request }: RecordEvidenceVariables) =>
      evidenceApi.record(assessmentId, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: evidenceKeys.all });
      queryClient.invalidateQueries({
        queryKey: evidenceKeys.byAssessment(variables.assessmentId),
      });
      notifySuccess(
        toast,
        'Evidence Recorded',
        `Evidence '${data.evidenceType}' captured successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Evidence Recording Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

// ============================================================================
// Consolidated Composable
// ============================================================================

/**
 * Primary composable for managing assessment evidence intake and queries (P8-S20).
 * Bundles evidence roster query with recordEvidence mutation.
 */
export function useAssessmentEvidence(
  assessmentId?: MaybeRefOrGetter<string | null | undefined>,
  filter?: MaybeRefOrGetter<{ studentId?: string; applicantId?: string } | undefined>,
  options?: EvidenceQueryOptions,
) {
  const evidenceQuery = useAssessmentEvidenceQuery(assessmentId, filter, options);
  const recordMutation = useRecordEvidenceMutation();

  return {
    evidenceQuery,
    evidence: computed(() => evidenceQuery.data.value ?? []),
    isLoading: evidenceQuery.isLoading,
    isPending: evidenceQuery.isPending,
    isError: evidenceQuery.isError,
    error: evidenceQuery.error,
    refetch: evidenceQuery.refetch,

    recordEvidence: recordMutation,
  };
}
