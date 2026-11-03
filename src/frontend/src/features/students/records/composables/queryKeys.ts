import type { StudentSearchParams } from '../types/records.types';

/**
 * Standardized TanStack Query key factories for Students Records.
 * Follows hierarchical pattern to enable precise targeting or broad hierarchy cache invalidation.
 */
export const studentRecordsKeys = {
  all: ['students', 'records'] as const,
  lists: () => [...studentRecordsKeys.all, 'list'] as const,
  list: (params?: StudentSearchParams) =>
    params !== undefined
      ? ([...studentRecordsKeys.lists(), params] as const)
      : ([...studentRecordsKeys.lists()] as const),
  details: () => [...studentRecordsKeys.all, 'detail'] as const,
  detail: (studentId?: string | null) =>
    studentId !== undefined && studentId !== null && studentId !== ''
      ? ([...studentRecordsKeys.details(), studentId] as const)
      : ([...studentRecordsKeys.details()] as const),
};
