import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { learningProgressApi } from '../api/learningProgressApi';
import type {
  ConcludeParticipationRequest,
  ConcludeParticipationResponse,
  LearningProgress,
  RecordProgressRequest,
} from '../types';
import { learningProgressKeys, reportCardKeys } from '../constants/queryKeys';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// Re-export query keys for convenience
export { learningProgressKeys };

// ============================================================================
// Types
// ============================================================================

export interface LearningProgressQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface LearningProgressMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching student learning progress ledger (UC-ACD-12, P7-S20).
 * Query key: ['academics', 'learning-progress', studentId, periodId]
 */
export function useStudentProgressQuery(
  studentId: MaybeRefOrGetter<string | null | undefined>,
  periodId?: MaybeRefOrGetter<string | null | undefined>,
  options?: LearningProgressQueryOptions,
) {
  return useQuery<LearningProgress[]>({
    queryKey: computed(() => {
      const resolvedStudentId = toValue(studentId);
      const resolvedPeriodId = toValue(periodId);
      return learningProgressKeys.byStudent(resolvedStudentId, resolvedPeriodId);
    }),
    queryFn: () => {
      const resolvedStudentId = toValue(studentId);
      if (!resolvedStudentId) {
        throw new Error('Student ID is required to fetch learning progress.');
      }
      return learningProgressApi.getStudentProgress(
        resolvedStudentId,
        toValue(periodId) ?? undefined,
      );
    },
    enabled: computed(() => {
      const resolvedStudentId = toValue(studentId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedStudentId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching unit participant progress roster (UC-ACD-12, P7-S20).
 * Query key: ['academics', 'learning-progress', 'unit', unitId]
 */
export function useUnitProgressQuery(
  unitId: MaybeRefOrGetter<string | null | undefined>,
  periodId?: MaybeRefOrGetter<string | null | undefined>,
  options?: LearningProgressQueryOptions,
) {
  return useQuery<LearningProgress[]>({
    queryKey: computed(() => {
      const resolvedUnitId = toValue(unitId);
      return learningProgressKeys.byUnit(resolvedUnitId);
    }),
    queryFn: () => {
      const resolvedUnitId = toValue(unitId);
      if (!resolvedUnitId) {
        throw new Error('Learning Unit ID is required to fetch unit progress roster.');
      }
      return learningProgressApi.getUnitProgress(
        resolvedUnitId,
        toValue(periodId) ?? undefined,
      );
    },
    enabled: computed(() => {
      const resolvedUnitId = toValue(unitId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedUnitId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

// ============================================================================
// Mutations
// ============================================================================

/**
 * Mutation hook for recording student learning progress, supporting out-of-period completion date stamping and RPL (UC-ACD-12, P7-S20).
 * Invalidates learning progress ledger, records, and participation queries upon success.
 */
export function useRecordProgressMutation(
  options?: LearningProgressMutationOptions<LearningProgress, RecordProgressRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: RecordProgressRequest) =>
      learningProgressApi.recordProgress(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: learningProgressKeys.all });
      queryClient.invalidateQueries({ queryKey: ['academics', 'records'] });
      queryClient.invalidateQueries({ queryKey: reportCardKeys.all });
      queryClient.invalidateQueries({ queryKey: ['academics', 'participations'] });
      notifySuccess(
        toast,
        'Learning Progress Recorded',
        `Progress marked as ${data.progressStatus ?? data.status} for student.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Record Progress', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for formally concluding unit participation for a student (UC-ACD-12, P7-S20).
 * Invalidates learning progress, records, and participation queries upon success.
 */
export function useConcludeParticipationMutation(
  options?: LearningProgressMutationOptions<
    ConcludeParticipationResponse,
    ConcludeParticipationRequest
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: ConcludeParticipationRequest) =>
      learningProgressApi.concludeParticipation(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: learningProgressKeys.all });
      queryClient.invalidateQueries({ queryKey: ['academics', 'records'] });
      queryClient.invalidateQueries({ queryKey: reportCardKeys.all });
      queryClient.invalidateQueries({ queryKey: ['academics', 'participations'] });
      notifySuccess(
        toast,
        'Participation Concluded',
        'Student unit participation formally concluded.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Conclude Participation', options);
      options?.onError?.(error, variables);
    },
  });
}

// Aliases matching criteria naming
export const recordProgressMutation = useRecordProgressMutation;
export const concludeParticipationMutation = useConcludeParticipationMutation;

/**
 * Unified feature composable for Learning Progress (UC-ACD-12, P7-S20).
 */
export function useLearningProgress(
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  periodId?: MaybeRefOrGetter<string | null | undefined>,
) {
  const studentProgressQuery = useStudentProgressQuery(studentId, periodId);
  const recordMutation = useRecordProgressMutation();
  const concludeMutation = useConcludeParticipationMutation();

  return {
    studentProgressQuery,
    recordProgressMutation: recordMutation,
    concludeParticipationMutation: concludeMutation,
  };
}
