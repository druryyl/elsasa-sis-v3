import type { GetInterventionsParams } from '../types';

/**
 * TanStack Query key factories for Academic Interventions (SCR-ACD-06, UC-ACD-11, P7-S20).
 */
export const interventionKeys = {
  all: ['academics', 'interventions'] as const,
  atRisk: (periodId?: string | null | undefined, gradeLevel?: string | null | undefined) =>
    ['academics', 'interventions', 'at-risk', periodId, gradeLevel] as const,
  active: (periodId?: string | null | undefined) =>
    ['academics', 'interventions', 'active', periodId] as const,
  lists: () => [...interventionKeys.all, 'list'] as const,
  list: (params?: GetInterventionsParams) =>
    [...interventionKeys.lists(), params] as const,
  details: () => [...interventionKeys.all, 'detail'] as const,
  detail: (id?: string | null | undefined) =>
    [...interventionKeys.details(), id] as const,
} as const;
