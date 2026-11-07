import { computed, toValue, type MaybeRefOrGetter } from 'vue';
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { CACHE_TIERS } from '@/app/providers/queryClient';
import { cohortReviewApi } from '../api/cohortReviewApi';
import type {
  AcademicAchievement,
  BatchDetermineStandingRequest,
  BatchDetermineStandingResponse,
  BatchIssueProgressionRequest,
  BatchIssueProgressionResponse,
  CohortProgressionProposal,
  CohortStandingProposal,
  ConferAchievementRequest,
  CorrectProgressionRequest,
  CorrectStandingRequest,
  GetRecommendationsParams,
  GetStandingsParams,
  ProgressionItem,
  StandingItem,
} from '../types';
import { cohortReviewKeys } from '../constants/queryKeys';
import {
  notifyError,
  notifySuccess,
  tryGetToast,
  type ToastOptions,
} from './toastNotifications';

// Re-export query keys for external consumers
export { cohortReviewKeys };

// ============================================================================
// Types
// ============================================================================

export interface CohortReviewQueryOptions {
  enabled?: MaybeRefOrGetter<boolean>;
}

export interface CohortReviewMutationOptions<TData, TVariables> extends ToastOptions {
  onSuccess?: (data: TData, variables: TVariables) => void;
  onError?: (error: unknown, variables: TVariables) => void;
}

export interface BatchCommitCohortVariables {
  standingRequest?: BatchDetermineStandingRequest;
  progressionRequest?: BatchIssueProgressionRequest;
}

export interface BatchCommitCohortResult {
  standingResponse?: BatchDetermineStandingResponse;
  progressionResponse?: BatchIssueProgressionResponse;
}

export interface CorrectStandingVariables {
  id: string;
  request: CorrectStandingRequest;
}

export interface CorrectProgressionVariables {
  id: string;
  request: CorrectProgressionRequest;
}

// ============================================================================
// Queries
// ============================================================================

/**
 * Reactive TanStack Query hook fetching dynamic cohort standing evaluation (TD-03, P7-S20).
 * Query key: ['academics', 'cohort-review', 'evaluate-standing', periodId, gradeLevel]
 */
export function useEvaluateStandingQuery(
  periodId: MaybeRefOrGetter<string | null | undefined>,
  gradeLevel: MaybeRefOrGetter<string | null | undefined>,
  options?: CohortReviewQueryOptions,
) {
  return useQuery<CohortStandingProposal[]>({
    queryKey: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      const resolvedGradeLevel = toValue(gradeLevel);
      return cohortReviewKeys.evaluateStanding(resolvedPeriodId, resolvedGradeLevel);
    }),
    queryFn: () => {
      const resolvedPeriodId = toValue(periodId);
      const resolvedGradeLevel = toValue(gradeLevel);
      if (!resolvedPeriodId || !resolvedGradeLevel) {
        throw new Error('Academic Period ID and Grade Level are required to evaluate standings.');
      }
      return cohortReviewApi.evaluateStanding({
        academicPeriodId: resolvedPeriodId,
        gradeLevel: resolvedGradeLevel,
      });
    },
    enabled: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      const resolvedGradeLevel = toValue(gradeLevel);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedPeriodId && resolvedGradeLevel) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching dynamic cohort progression recommendations (TD-03, P7-S20).
 * Query key: ['academics', 'cohort-review', 'evaluate-progression', periodId, gradeLevel]
 */
export function useEvaluateProgressionQuery(
  periodId: MaybeRefOrGetter<string | null | undefined>,
  gradeLevel: MaybeRefOrGetter<string | null | undefined>,
  options?: CohortReviewQueryOptions,
) {
  return useQuery<CohortProgressionProposal[]>({
    queryKey: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      const resolvedGradeLevel = toValue(gradeLevel);
      return cohortReviewKeys.evaluateProgression(resolvedPeriodId, resolvedGradeLevel);
    }),
    queryFn: () => {
      const resolvedPeriodId = toValue(periodId);
      const resolvedGradeLevel = toValue(gradeLevel);
      if (!resolvedPeriodId || !resolvedGradeLevel) {
        throw new Error('Academic Period ID and Grade Level are required to evaluate progression.');
      }
      return cohortReviewApi.evaluateProgression({
        academicPeriodId: resolvedPeriodId,
        gradeLevel: resolvedGradeLevel,
      });
    },
    enabled: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      const resolvedGradeLevel = toValue(gradeLevel);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedPeriodId && resolvedGradeLevel) && isEnabled;
    }),
    staleTime: CACHE_TIERS.ACTIVE_QUEUE.staleTime,
    gcTime: CACHE_TIERS.ACTIVE_QUEUE.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.ACTIVE_QUEUE.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching certified academic standings (UC-ACD-17, P7-S20).
 * Query key: ['academics', 'cohort-review', 'standings']
 */
export function useCohortStandingsQuery(
  params?: MaybeRefOrGetter<GetStandingsParams | undefined>,
  options?: CohortReviewQueryOptions,
) {
  return useQuery<StandingItem[]>({
    queryKey: computed(() => {
      const resolvedParams = toValue(params);
      return cohortReviewKeys.standings(resolvedParams);
    }),
    queryFn: () => {
      const resolvedParams = toValue(params) ?? {};
      return cohortReviewApi.getStandings(resolvedParams);
    },
    enabled: computed(() => {
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching conferred academic achievements (UC-ACD-21, P7-S20).
 * Query key: ['academics', 'cohort-review', 'achievements']
 */
export function useCohortAchievementsQuery(
  periodId: MaybeRefOrGetter<string | null | undefined>,
  gradeLevel?: MaybeRefOrGetter<string | null | undefined>,
  options?: CohortReviewQueryOptions,
) {
  return useQuery<AcademicAchievement[]>({
    queryKey: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      const resolvedGradeLevel = toValue(gradeLevel);
      return cohortReviewKeys.achievements(resolvedPeriodId, resolvedGradeLevel);
    }),
    queryFn: () => {
      const resolvedPeriodId = toValue(periodId);
      if (!resolvedPeriodId) {
        throw new Error('Academic Period ID is required to fetch period achievements.');
      }
      return cohortReviewApi.getPeriodAchievements(resolvedPeriodId);
    },
    enabled: computed(() => {
      const resolvedPeriodId = toValue(periodId);
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return Boolean(resolvedPeriodId) && isEnabled;
    }),
    staleTime: CACHE_TIERS.OPERATIONAL.staleTime,
    gcTime: CACHE_TIERS.OPERATIONAL.gcTime,
    refetchOnWindowFocus: CACHE_TIERS.OPERATIONAL.refetchOnWindowFocus,
  });
}

/**
 * Reactive TanStack Query hook fetching promotion recommendations roster.
 */
export function useCohortRecommendationsQuery(
  params?: MaybeRefOrGetter<GetRecommendationsParams | undefined>,
  options?: CohortReviewQueryOptions,
) {
  return useQuery<ProgressionItem[]>({
    queryKey: computed(() => {
      const resolvedParams = toValue(params);
      return cohortReviewKeys.recommendations(resolvedParams);
    }),
    queryFn: () => {
      const resolvedParams = toValue(params) ?? {};
      return cohortReviewApi.getRecommendations(resolvedParams);
    },
    enabled: computed(() => {
      const isEnabled = options?.enabled !== undefined ? toValue(options.enabled) : true;
      return isEnabled;
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
 * Mutation hook for executing atomic batch standing and progression commitments (UC-ACD-17, P7-S20).
 * Invalidates cohort review, standings, and academic records caches upon success.
 */
export function useBatchCommitCohortMutation(
  options?: CohortReviewMutationOptions<
    BatchCommitCohortResult,
    BatchCommitCohortVariables
  >,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: async (variables: BatchCommitCohortVariables): Promise<BatchCommitCohortResult> => {
      let standingResponse: BatchDetermineStandingResponse | undefined;
      let progressionResponse: BatchIssueProgressionResponse | undefined;

      if (variables.standingRequest) {
        standingResponse = await cohortReviewApi.batchDetermineStanding(variables.standingRequest);
      }
      if (variables.progressionRequest) {
        progressionResponse = await cohortReviewApi.batchIssueProgression(variables.progressionRequest);
      }

      return { standingResponse, progressionResponse };
    },
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: cohortReviewKeys.all });
      queryClient.invalidateQueries({ queryKey: ['academics', 'standings'] });
      queryClient.invalidateQueries({ queryKey: ['academics', 'records'] });
      notifySuccess(
        toast,
        'Cohort Committed',
        'Cohort standings and progression recommendations committed successfully.',
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Cohort Commitment Failed', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for conferring academic honors, subject distinctions, or competition awards (UC-ACD-21, P7-S20).
 * Invalidates achievement caches and cohort review upon success.
 */
export function useConferAchievementMutation(
  options?: CohortReviewMutationOptions<AcademicAchievement, ConferAchievementRequest>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: (request: ConferAchievementRequest) =>
      cohortReviewApi.conferAchievement(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: cohortReviewKeys.achievements() });
      queryClient.invalidateQueries({ queryKey: cohortReviewKeys.all });
      notifySuccess(
        toast,
        'Achievement Conferred',
        `Conferred "${data.title}" successfully.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Confer Achievement', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for executing an auditable in-place correction to a certified academic standing record (UC-ACD-15, P7-S20).
 * Invalidates standing caches, cohort review, and academic records upon success.
 */
export function useCorrectStandingMutation(
  options?: CohortReviewMutationOptions<StandingItem, CorrectStandingVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, request }: CorrectStandingVariables) =>
      cohortReviewApi.correctStanding(id, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: cohortReviewKeys.standings() });
      queryClient.invalidateQueries({ queryKey: cohortReviewKeys.all });
      queryClient.invalidateQueries({ queryKey: ['academics', 'records'] });
      notifySuccess(
        toast,
        'Standing Corrected',
        `Academic standing corrected to ${data.standing}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Correct Standing', options);
      options?.onError?.(error, variables);
    },
  });
}

/**
 * Mutation hook for executing an auditable in-place correction to a certified progression recommendation record (UC-ACD-15, P7-S20).
 */
export function useCorrectProgressionMutation(
  options?: CohortReviewMutationOptions<ProgressionItem, CorrectProgressionVariables>,
) {
  const queryClient = useQueryClient();
  const toast = tryGetToast();

  return useMutation({
    mutationFn: ({ id, request }: CorrectProgressionVariables) =>
      cohortReviewApi.correctProgression(id, request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: cohortReviewKeys.recommendations() });
      queryClient.invalidateQueries({ queryKey: cohortReviewKeys.all });
      notifySuccess(
        toast,
        'Progression Corrected',
        `Progression recommendation corrected to ${data.recommendedProgression}.`,
        options,
      );
      options?.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      notifyError(toast, error, 'Failed to Correct Progression', options);
      options?.onError?.(error, variables);
    },
  });
}

// Aliases matching criteria naming
export const batchCommitCohortMutation = useBatchCommitCohortMutation;
export const conferAchievementMutation = useConferAchievementMutation;
export const correctStandingMutation = useCorrectStandingMutation;
export const correctProgressionMutation = useCorrectProgressionMutation;

/**
 * Unified feature composable for Cohort Academic Review (SCR-ACD-09, UC-ACD-17, P7-S20).
 */
export function useCohortReview(
  periodId?: MaybeRefOrGetter<string | null | undefined>,
  gradeLevel?: MaybeRefOrGetter<string | null | undefined>,
) {
  const evaluateStandingQuery = useEvaluateStandingQuery(periodId, gradeLevel);
  const evaluateProgressionQuery = useEvaluateProgressionQuery(periodId, gradeLevel);
  const standingsQuery = useCohortStandingsQuery(
    computed(() => {
      const pId = toValue(periodId);
      const gLevel = toValue(gradeLevel);
      return {
        academicPeriodId: pId || undefined,
        gradeLevel: gLevel || undefined,
      };
    }),
  );
  const achievementsQuery = useCohortAchievementsQuery(periodId, gradeLevel);

  const batchCommitMutation = useBatchCommitCohortMutation();
  const conferAchievement = useConferAchievementMutation();
  const correctStanding = useCorrectStandingMutation();
  const correctProgression = useCorrectProgressionMutation();

  return {
    evaluateStandingQuery,
    evaluateProgressionQuery,
    standingsQuery,
    achievementsQuery,
    batchCommitCohortMutation: batchCommitMutation,
    conferAchievementMutation: conferAchievement,
    correctStandingMutation: correctStanding,
    correctProgressionMutation: correctProgression,
  };
}
