import type { GetRecommendationsParams, GetStandingsParams } from '../types';

/**
 * TanStack Query key factories for Cohort Academic Review (SCR-ACD-09, UC-ACD-17, P7-S20).
 */
export const cohortReviewKeys = {
  all: ['academics', 'cohort-review'] as const,
  evaluateStanding: (periodId?: string | null | undefined, gradeLevel?: string | null | undefined) =>
    ['academics', 'cohort-review', 'evaluate-standing', periodId, gradeLevel] as const,
  evaluateProgression: (periodId?: string | null | undefined, gradeLevel?: string | null | undefined) =>
    ['academics', 'cohort-review', 'evaluate-progression', periodId, gradeLevel] as const,
  standings: (params?: GetStandingsParams) =>
    params
      ? (['academics', 'cohort-review', 'standings', params] as const)
      : (['academics', 'cohort-review', 'standings'] as const),
  achievements: (periodId?: string | null | undefined, gradeLevel?: string | null | undefined) =>
    periodId || gradeLevel
      ? (['academics', 'cohort-review', 'achievements', periodId, gradeLevel] as const)
      : (['academics', 'cohort-review', 'achievements'] as const),
  recommendations: (params?: GetRecommendationsParams) =>
    params
      ? (['academics', 'cohort-review', 'recommendations', params] as const)
      : (['academics', 'cohort-review', 'recommendations'] as const),
} as const;
