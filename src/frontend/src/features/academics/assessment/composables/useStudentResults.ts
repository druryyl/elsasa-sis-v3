import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { resultsApi } from '../api/resultsApi';
import type { StudentPublishedResult } from '../types';
import { resultKeys } from '../constants/queryKeys';

// ============================================================================
// Types
// ============================================================================

export interface StudentResultsQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching published academic results for student/family portal (SCR-SELF-03, P8-S20).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useStudentResultsQuery(
  periodId: MaybeRefOrGetter<string | null | undefined>,
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  options?: StudentResultsQueryOptions,
) {
  return useQuery<StudentPublishedResult[]>({
    queryKey: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      const resolvedStudentId = toValue(studentId);
      return resultKeys.studentResults(resolvedPeriodId, resolvedStudentId);
    }),
    queryFn: () => {
      const resolvedPeriodId = toValue(periodId);
      if (!resolvedPeriodId) {
        throw new Error('Academic Period ID is required to fetch student published results.');
      }
      return resultsApi.getStudentResults(resolvedPeriodId, toValue(studentId) ?? undefined);
    },
    enabled: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedPeriodId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

// ============================================================================
// Consolidated Composable
// ============================================================================

/**
 * Composable providing self-service published grades query for students/families (P8-S20).
 */
export function useStudentResults(
  periodId: MaybeRefOrGetter<string | null | undefined>,
  studentId?: MaybeRefOrGetter<string | null | undefined>,
  options?: StudentResultsQueryOptions,
) {
  const query = useStudentResultsQuery(periodId, studentId, options);

  return {
    studentResultsQuery: query,
    results: computed(() => query.data.value ?? []),
    studentResults: computed(() => query.data.value ?? []),
    isLoading: query.isLoading,
    isPending: query.isPending,
    isError: query.isError,
    error: query.error,
    refetch: query.refetch,
  };
}
