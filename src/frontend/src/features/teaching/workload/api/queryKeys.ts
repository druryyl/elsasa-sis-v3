import type {
  GetCapacityConflictsParams,
  GetWorkloadCommitmentsParams,
  GetWorkloadExceptionsParams,
  GetWorkloadPoliciesParams,
} from './types';

/**
 * Standardized TanStack Query key factories for Teaching Workload (P7-S22, SCR-TCH-03).
 * Follows hierarchical pattern to enable precise targeting or broad hierarchy cache invalidation.
 */
export const workloadKeys = {
  all: ['teaching', 'workload'] as const,
  policies: () => [...workloadKeys.all, 'policies'] as const,
  policyList: (params?: GetWorkloadPoliciesParams) =>
    params !== undefined
      ? ([...workloadKeys.policies(), params] as const)
      : ([...workloadKeys.policies()] as const),
  commitments: () => [...workloadKeys.all, 'commitments'] as const,
  commitmentList: (params?: GetWorkloadCommitmentsParams) =>
    params !== undefined
      ? ([...workloadKeys.commitments(), params] as const)
      : ([...workloadKeys.commitments()] as const),
  exceptions: () => [...workloadKeys.all, 'exceptions'] as const,
  exceptionList: (params?: GetWorkloadExceptionsParams) =>
    params !== undefined
      ? ([...workloadKeys.exceptions(), params] as const)
      : ([...workloadKeys.exceptions()] as const),
  exception: (id?: string | null) =>
    id !== undefined && id !== null && id !== ''
      ? ([...workloadKeys.exceptions(), id] as const)
      : ([...workloadKeys.exceptions()] as const),
  conflicts: () => [...workloadKeys.all, 'conflicts'] as const,
  conflictList: (params?: GetCapacityConflictsParams) =>
    params !== undefined
      ? ([...workloadKeys.conflicts(), params] as const)
      : ([...workloadKeys.conflicts()] as const),
};

export const teachingWorkloadKeys = workloadKeys;
