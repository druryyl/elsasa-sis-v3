import type { GetTeacherUnavailabilityWindowsParams } from './types';

/**
 * Standardized TanStack Query key factories for Teaching Availability (P7-S23, SCR-TCH-04).
 * Follows hierarchical pattern to enable precise targeting or broad hierarchy cache invalidation.
 */
export const availabilityKeys = {
  all: ['teaching', 'availability'] as const,
  teachers: () => [...availabilityKeys.all, 'teacher'] as const,
  teacher: (teacherId?: string | null) =>
    teacherId !== undefined && teacherId !== null && teacherId !== ''
      ? ([...availabilityKeys.teachers(), teacherId] as const)
      : ([...availabilityKeys.teachers()] as const),
  teacherAvailability: (teacherId?: string | null, periodId?: string | null) =>
    periodId !== undefined && periodId !== null && periodId !== ''
      ? ([...availabilityKeys.teacher(teacherId), 'declared', periodId] as const)
      : ([...availabilityKeys.teacher(teacherId), 'declared'] as const),
  windows: (
    teacherId?: string | null,
    params?: GetTeacherUnavailabilityWindowsParams,
  ) =>
    params !== undefined
      ? ([...availabilityKeys.teacher(teacherId), 'windows', params] as const)
      : ([...availabilityKeys.teacher(teacherId), 'windows'] as const),
  window: (windowId?: string | null) =>
    windowId !== undefined && windowId !== null && windowId !== ''
      ? ([...availabilityKeys.all, 'window', windowId] as const)
      : ([...availabilityKeys.all, 'window'] as const),
};

export const teachingAvailabilityKeys = availabilityKeys;
