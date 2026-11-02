import type { MatrixProjectionParams } from '../api/calendar.api';

/**
 * Standardized TanStack Query key factories for Operations Calendar & Periods.
 * Structured to enable precise single-item targeting or broad hierarchy invalidation.
 */

export const academicYearKeys = {
  all: ['academicYears'] as const,
  lists: () => [...academicYearKeys.all, 'list'] as const,
  list: (params?: { status?: string }) =>
    params !== undefined
      ? ([...academicYearKeys.lists(), params] as const)
      : ([...academicYearKeys.lists()] as const),
  details: () => [...academicYearKeys.all, 'detail'] as const,
  detail: (id: string) => [...academicYearKeys.details(), id] as const,
};

export const operationalPeriodKeys = {
  all: ['operationalPeriods'] as const,
  byAcademicYear: (academicYearId: string) => ['operationalPeriods', academicYearId] as const,
};

export const calendarKeys = {
  all: ['calendars'] as const,
  byAcademicYear: (academicYearId: string) => ['calendars', academicYearId] as const,
  active: (academicYearId: string) => ['calendars', academicYearId, 'active'] as const,
  matrixAll: ['calendarMatrix'] as const,
  matrix: (calendarId: string, params?: MatrixProjectionParams) =>
    params !== undefined
      ? (['calendarMatrix', calendarId, params] as const)
      : (['calendarMatrix', calendarId] as const),
};

export const schoolClosureKeys = {
  all: ['schoolClosures'] as const,
  byAcademicYear: (academicYearId: string) => ['schoolClosures', academicYearId] as const,
};

export const amendmentKeys = {
  all: ['calendarAmendments'] as const,
  byCalendar: (calendarId: string) => ['calendarAmendments', calendarId] as const,
};

export const recordCorrectionKeys = {
  all: ['recordCorrections'] as const,
  byAcademicYear: (academicYearId: string) => ['recordCorrections', academicYearId] as const,
};
