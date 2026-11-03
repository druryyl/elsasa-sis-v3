import type { AdmissionQueueFilterParams } from '../types/admission.types';

/**
 * Standardized TanStack Query key factories for Students Admission.
 * Follows hierarchical pattern to enable precise targeting or broad hierarchy cache invalidation.
 */
export const admissionKeys = {
  all: ['admission'] as const,
  lists: () => [...admissionKeys.all, 'list'] as const,
  list: (filters?: AdmissionQueueFilterParams) =>
    filters !== undefined
      ? ([...admissionKeys.lists(), filters] as const)
      : ([...admissionKeys.lists()] as const),
  details: () => [...admissionKeys.all, 'detail'] as const,
  detail: (id: string) => [...admissionKeys.details(), id] as const,
  status: (
    numberOrParams?: string | { applicationNumber?: string; dateOfBirth?: string },
    dob?: string,
  ) => {
    if (typeof numberOrParams === 'object' && numberOrParams !== null) {
      return [
        ...admissionKeys.all,
        'status',
        numberOrParams.applicationNumber ?? '',
        numberOrParams.dateOfBirth ?? '',
      ] as const;
    }
    return [...admissionKeys.all, 'status', numberOrParams ?? '', dob ?? ''] as const;
  },
  periods: () => [...admissionKeys.all, 'periods'] as const,
  activePeriods: () => [...admissionKeys.all, 'periods', 'active'] as const,
};
