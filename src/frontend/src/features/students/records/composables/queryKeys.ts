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
  history: (id: string) => [...studentRecordsKeys.detail(id), 'history'] as const,
};

export const queryKeys = {
  students: {
    all: studentRecordsKeys.all,
    lists: studentRecordsKeys.lists,
    list: studentRecordsKeys.list,
    details: studentRecordsKeys.details,
    detail: studentRecordsKeys.detail,
    history: (id: string) => [...queryKeys.students.detail(id), 'history'] as const,
  },
};
