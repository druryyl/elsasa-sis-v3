import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import {
  getApplicantAssessmentScores,
  type ApplicantEntranceAssessmentScoreDto,
} from '../api/applicantAssessmentApi';

export interface ApplicantAssessmentQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

/**
 * Reactive TanStack Query hook fetching authorized diagnostic entrance assessment results from Academics module (SCR-STU-01, GAP-07, P9-S23).
 * Cache tier: ACTIVE_QUEUE (30s stale, 5m gc, refetch on window focus).
 */
export function useApplicantAssessmentScoresQuery(
  applicantId: MaybeRefOrGetter<string | null | undefined>,
  options?: ApplicantAssessmentQueryOptions,
) {
  return useQuery<ApplicantEntranceAssessmentScoreDto[]>({
    queryKey: computed(() => ['admission', 'applicant-assessment-scores', toValue(applicantId)]),
    queryFn: () => {
      const id = toValue(applicantId);
      if (!id) return [];
      return getApplicantAssessmentScores(id);
    },
    enabled: computed(() => {
      const id = toValue(applicantId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(id) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Primary composable for viewing applicant entrance assessment scores in the Student Intake Workbench.
 */
export function useApplicantAssessment(
  applicantId: MaybeRefOrGetter<string | null | undefined>,
  options?: ApplicantAssessmentQueryOptions,
) {
  const query = useApplicantAssessmentScoresQuery(applicantId, options);

  return {
    scoresQuery: query,
    scores: computed(() => query.data.value ?? []),
    isLoading: query.isLoading,
    isPending: query.isPending,
    isError: query.isError,
    error: query.error,
    refetch: query.refetch,
  };
}
